(* 
 Copyright 2012 Henrik Feldt
  
 Licensed under the Apache License, Version 2.0 (the "License"); you may not use
 this file except in compliance with the License. You may obtain a copy of the 
 License at 
 
     http://www.apache.org/licenses/LICENSE-2.0 
 
 Unless required by applicable law or agreed to in writing, software distributed
 under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
 CONDITIONS OF ANY KIND, either express or implied. See the License for the 
 specific language governing permissions and limitations under the License.
*)
namespace MassTransit.Transports.AzureServiceBus.Receiver

open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging

open FSharp.Control

open System
open System.Threading
open System.Runtime.CompilerServices
open System.Collections.Concurrent

open MassTransit.Transports.AzureServiceBus
open MassTransit.Transports.AzureServiceBus.Receiver.Queue
open MassTransit.Async.AsyncRetry

type internal Agent<'T> = AutoCancelAgent<'T>

/// concurrently outstanding asynchronous requests (workers)
type Concurrency = uint32

/// communication with the worker agent
type RecvMsg =
  | Start
  | Pause
  | Halt of AsyncReplyChannel<unit>
  | SubscribeQueue of QueueDescription * Concurrency
  | UnsubscribeQueue of QueueDescription
  | SubscribeTopic of TopicDescription * Concurrency
  | UnsubscribeTopic of TopicDescription
  | AsyncFaultOccurred of exn

/// State-keeping structure, mapping a description to a pair of cancellation token source and
/// receiver set list. The CancellationTokenSource can be used to stop the subscription that
/// it corresponds to.
type WorkerState =
  { QSubs : Map<QueueDescription, CancellationTokenSource * ReceiverSet list>;
    TSubs : Map<TopicDescription, 
                // unsubscribe action
                (unit -> Async<unit>) * CancellationTokenSource * ReceiverSet list> }

/// A pair of a messaging factory and a list of message receivers that
/// were created from that messaging factory.
and ReceiverSet = Pair of MessagingFactory * MessageReceiver list

type ReceiverExceptionEventArgs(ex:Exception) =
    inherit EventArgs()
    member this.Exception = ex
    
type ExceptionRaised = delegate of obj * ReceiverExceptionEventArgs -> unit

/// Create a new receiver, with a queue description,
/// a factory for messaging factories and some control flow data
type Receiver(desc   : QueueDescription,
              newMf  : (unit -> MessagingFactory),
              nm     : NamespaceManager,
              sett : ReceiverSettings) as x =

  let logger = MassTransit.Logging.Logger.Get(typeof<Receiver>)

  /// The 'scratch' buffer that tunnels messages from the ASB receivers
  /// to the consumers of the Receiver class.
  let messages = new BlockingCollection<_>(int <| sett.BufferSize)
  
  let error  = new Event<ExceptionRaised, ReceiverExceptionEventArgs>()

  /// Creates a new child token from the parent cancellation token source
  let childTokenFrom ( cts : CancellationTokenSource ) =
    let childCTS = new CancellationTokenSource()
    let reg = cts.Token.Register(fun () -> childCTS.Dispose()) // what to do with the IDisposable...?
    childCTS

  let getToken ( cts : CancellationTokenSource ) = cts.Token
  
  /// Starts stop/nthAsync new clients and messaging factories, so for stop=500, nthAsync=100
  /// it loops 500 times and starts 5 new clients
  let initReceiverSet newMf stop newReceiver pathDesc =
    let rec inner curr pairs =
      async {
        match curr with
        | _ when stop = curr ->
          // we're stopping
          return pairs
        | _ when curr % sett.NThAsync = 0u ->
          // we're at the first item, create a new pair
          logger.DebugFormat("creating new mf & recv '{0}'", (pathDesc : PathBasedEntity).Path)
          let mf = newMf ()
          let! r = pathDesc |> newReceiver mf
          let p = Pair(mf, r :: [])
          return! inner (curr + 1u) (p :: pairs)
        | _ ->
          // if we're not at an even location, just create a new receiver for
          // the same messaging factory
          match pairs with // of mf<-> receiver list
          | [] -> return failwith "curr != 1, but pairs empty. curr > 1 -> pairs.Length > 0"
          | (Pair(mf, rs) :: rest) ->
            logger.Debug(sprintf "creating new recv '%s'" (desc.ToString()))
            let! r = desc |> newReceiver mf // the new receiver
            let p = Pair(mf, r :: rs) // add the receiver to the list of receivers for this mf
            return! inner (curr + 1u) (p :: rest) }
    inner 0u []

  let initReceiverSet' : ((MessagingFactory -> PathBasedEntity -> Async<MessageReceiver>) -> PathBasedEntity -> _) = 
    initReceiverSet newMf (sett.Concurrency)

  /// creates an async workflow worker, given a message receiver client
  let worker client =
    //logger.Debug "worker called"
    async {
      while true do
        //logger.Debug "worker loop"
        let! bmsg = sett.ReceiveTimeout |> recv client
        if bmsg <> null then
          logger.Debug(sprintf "received message on '%s'" (desc.ToString()))
          messages.Add bmsg }
          
  let startPairsAsync pairs token =
    let work =
      [ for Pair(mf, rs) in pairs do
          for r in rs do 
            yield async {
              try do! worker r
              with e -> error.Trigger(x, new ReceiverExceptionEventArgs(e)) } ]
      |> Async.Parallel
      |> Async.Ignore

    Async.Start(work, token)

  /// cleans out the message buffer and disposes all messages therein
  let clearLocks () =
    async {
      while messages.Count > 0 do
        let m = ref null
        if messages.TryTake(m, TimeSpan.FromMilliseconds(4.0)) then 
          try         do! Async.FromBeginEnd((!m).BeginAbandon, (!m).EndAbandon)
                      (!m).Dispose()
          with | x -> let entry = sprintf "could not abandon message#%s" <| (!m).MessageId
                      logger.Error(entry, x) }

  /// Closes the pair of a messaging factory and a list of receivers
  let closePair pair =
    match pair with
    Pair(mf, rs) ->
      logger.InfoFormat("closing {0} receivers and their single messaging factory", rs.Length)
      if not(mf.IsClosed) then
        try         mf.Close() // this statement has cost a LOT of money in developer time, waiting for a timeout
        with | x -> logger.Error("could not close messaging factory", x)
      for r in rs do
        if not(r.IsClosed) then
          try         r.Close()
          with | x -> logger.Error("could not close receiver", x)

  /// An agent that implements the reactor pattern, reacting to messages.
  /// The mutually recursive function 'initial' uses the explicit functional
  /// state pattern as described here:
  /// http://www.cl.cam.ac.uk/~tp322/papers/async.pdf
  ///
  /// A receiver can have these states:
  /// --------------------------------
  /// * Initial (waiting for a Start or Halt(chan) message).
  /// * Starting (getting all messaging factories and receivers up and running)
  /// * Started (main message loop)
  /// * Paused (cancelling all async workflows)
  /// * AsyncFaulted (something in some asynchronous threw an exception, and we need to be aware of this!)
  /// * Halting (closing things)
  /// * Halted (listening for the Halt message, or if that was what shut the actor down, simply replies on it)
  /// * Final (implicit; reference is GCed)
  /// 
  /// with transitions:
  /// -----------------
  /// Initial -> Starting
  /// Initial -> Halting
  /// 
  /// Starting -> Started
  /// 
  /// Started -> Paused
  /// Started -> Halting
  /// 
  /// Paused -> Starting
  /// Paused -> Halting
  ///
  /// AsyncFaulted -> Halting
  ///
  /// Halting -> Halted
  /// 
  /// Halted -> Final (GC-ed here)
  /// 
  let a = Agent<RecvMsg>.Start(fun inbox ->

    let rec initial () =
      async {
        logger.Debug "initial"
        let emptyState = { QSubs = Map.empty; TSubs = Map.empty }
        let! msg = inbox.Receive ()
        match msg with
        | Start ->
          // create WorkerState for initial subscription (that of the queue)
          // and move to the started state
          try let! rSet = initReceiverSet' newReceiver desc
              let mappedRSet = Map.empty |> Map.add desc (new CancellationTokenSource(), rSet)
              return! starting { QSubs = mappedRSet ; TSubs = Map.empty }
          with e -> return! asyncFaulted emptyState
          
        | Halt(chan) -> return! halting emptyState (Some(chan))

        | _ ->
          // because we only care about the Start message in the initial state,
          // we will ignore all other messages.
          return! initial () }

    and starting state =
      async {
        logger.Debug "starting"
        do! desc |> create nm
        use ct = new CancellationTokenSource ()
        // start all subscriptions
        state.QSubs
        |> Seq.map (fun x -> x.Value)
        |> Seq.append (state.TSubs |> Seq.map(fun x -> let (sd,cts,rs) = x.Value in cts,rs))
        |> Seq.collect (fun ctsAndRs -> snd ctsAndRs)
        |> Seq.collect (fun (Pair(_, rs)) -> rs)
        |> Seq.iter (fun r -> Async.Start(r |> worker, ct.Token))
        return! started state ct }

    and started state cts =
      async {
        logger.DebugFormat("started '{0}'", desc.Path)
        let! msg = inbox.Receive()
        match msg with
        | Pause ->
          cts.Cancel()
          return! paused state

        | Halt(chan) ->
          logger.DebugFormat("halt '{0}'", desc.Path)
          cts.Cancel() // may throw!
          logger.DebugFormat("moving to halted state '{0}'", desc.Path)
          return! halting state (Some(chan))

        | SubscribeQueue(qd, cc) ->
          logger.DebugFormat("SubscribeQueue '{0}'", qd.Path)
          do! qd |> create nm
          // create new receiver sets for the queue description and kick them off as workflows
          let childAsyncCts = childTokenFrom cts // get a new child token to control the computation with
          let! recvSet = initReceiverSet' newReceiver qd // initialize the receivers and potentially new messaging factories
          do childAsyncCts |> getToken |> startPairsAsync recvSet // start the actual async workflow
          let qsubs' = state.QSubs.Add(qd, (childAsyncCts, recvSet)) // update the subscriptions mapping, from description to cts*ReceiverSet list.
          return! started { QSubs = qsubs' ; TSubs = state.TSubs } cts

        | UnsubscribeQueue qd ->
          logger.Warn "SKIP:UnsubscribeQueue (TODO - do we even need this?)"
          return! started state cts

        | SubscribeTopic(td, cc) ->
          logger.DebugFormat("SubscribeTopic '{0}'", td.Path)
          let childAsyncCts = childTokenFrom cts
          let sub = sett.ReceiverName
          do! td |> Topic.subscribe nm sub
          let! pairs = initReceiverSet' (Topic.newReceiver sub) td
          do childAsyncCts |> getToken |> startPairsAsync pairs
          let tsubs' = state.TSubs.Add(td, ((fun () -> sub |> Topic.unsubscribe nm td), childAsyncCts, pairs))
          return! started { QSubs = state.QSubs ; TSubs = tsubs' } cts

        | UnsubscribeTopic td ->
          logger.DebugFormat("UnsubscribeTopic '{0}'", td.Path)
          match state.TSubs.TryFind td with
          | None -> 
            logger.WarnFormat("Called UnsubscribeTopic('{0}') on non-subscribed topic!", td.Path) 
            return! started state cts

          | Some( unsubscribe, childCts, recvSet ) ->
            childCts.Cancel()
            recvSet |> List.iter (fun set -> closePair set)
            let tsubs' = state.TSubs.Remove(td)
            do! unsubscribe ()
            return! started { QSubs = state.QSubs ; TSubs = tsubs' } cts

        | Start -> return! started state cts
        
        | AsyncFaultOccurred(ex) ->
          logger.Error("asynchronous error from actor", ex)
          return! asyncFaulted state }

    and paused state =
      async {
        logger.DebugFormat("paused '{0}'", desc.Path)
        let! msg = inbox.Receive()
        match msg with
        | Start -> return! starting state
        | Halt(chan) -> return! halting state (Some(chan))
        | _ as x -> logger.Warn(sprintf "got %A, despite being paused" x) }

    and asyncFaulted state = async { return! halting state None }

    and halting state (mChan : AsyncReplyChannel<unit> option) =
      async {
        logger.DebugFormat("halting '{0}'", desc.Path)
        let subs =
          asyncSeq {
           for x in (state.QSubs |> Seq.collect (fun x -> snd x.Value))
             do yield x
           for x in (state.TSubs |> Seq.collect (fun x -> let (s,cts,rs) = x.Value in rs)) do
             yield x }
        try
          // let's do unsubscriptions first, and then Close can fail as much as it wants!
          for unsub in state.TSubs |> Seq.map (fun x -> let (s, _, _) = x.Value in s) do
            do! unsub ()
          for rs in subs do
            closePair rs
          do! clearLocks ()
        with e -> logger.Error("unable to clean up actor", e)
        // then exit
        logger.DebugFormat("halting complete", desc.Path)
        return! halted mChan }
      
    and halted mChan =
      async {
        match mChan with
        | None ->
          let! m = inbox.Receive()
          match m with
          | Halt(chan) -> chan.Reply()
          | _ -> return! halted None
        | Some(chan) -> chan.Reply() }

    initial ())
  
  do a.Error.Add(fun ex -> error.Trigger(x, new ReceiverExceptionEventArgs(ex)))
  do a.Error.Add(fun ex -> a.Post <| AsyncFaultOccurred ex)

  [<CLIEvent>]
  member __.Error = error.Publish

  /// Starts the receiver which starts the consuming from the service bus
  /// and creates the queue if it doesn't exist
  member x.Start () =
    logger.InfoFormat("start called for queue '{0}'", desc)
    a.Post Start

  /// Stops the receiver; allowing it to start once again.
  /// (Stopping is done by disposing the receiver.)
  member x.Pause () =
    logger.InfoFormat("stop called for queue '{0}'", desc)
    a.Post Pause

  member x.Subscribe ( td : TopicDescription ) =
    logger.InfoFormat("subscribe called for topic description '{0}'", td)
    a.Post <| SubscribeTopic( td, sett.Concurrency )

  member x.Unsubscribe ( td : TopicDescription ) =
    logger.InfoFormat("unsubscribe called for topic description '{0}'", td)
    a.Post <| UnsubscribeTopic( td )

  /// Returns a message if one was added to the buffer within the timeout specified,
  /// or otherwise returns null.
  member x.Get(timeout : TimeSpan) =
    let mutable item = null
    // concurrent queue doesn't like waiting TimeSpan.MaxValue
    let t' = if timeout.TotalMilliseconds > float Int32.MaxValue
             then Int32.MaxValue
             else int <| timeout.TotalMilliseconds

    let _ = messages.TryTake(&item, t')
    item

  member x.Consume() = asyncSeq { while true do yield messages.Take() }

  interface System.IDisposable with
    /// Cleans out all receivers and messaging factories.
    member x.Dispose () = 
      logger.DebugFormat("dispose called for receiver on '{0}'", desc.Path)
      a.PostAndReply(fun chan -> Halt(chan))

type ReceiverModule =
  /// <code>address</code> is required. <code>settings</code> is required.
  static member StartReceiver(address  : AzureServiceBusEndpointAddress,
                              settings : ReceiverSettings) =
    let r = new Receiver(address.QueueDescription, (fun () -> address.MessagingFactoryFactory.Invoke()),
                                   address.NamespaceManager, settings)
    r.Start ()
    r
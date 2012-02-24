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
namespace MassTransit.Async

open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging

open FSharp.Control

open System
open System.Threading
open System.Runtime.CompilerServices
open System.Collections.Concurrent

open MassTransit.AzureServiceBus
open MassTransit.Async.Queue
open MassTransit.Async.AsyncRetry

type internal Agent<'T> = AutoCancelAgent<'T>

/// communication with the worker agent
type RecvMsg =
  Start
  | Pause
  | Halt
  | SubscribeQueue of QueueDescription * Concurrency
  | UnsubscribeQueue of QueueDescription
  | SubscribeTopic of TopicDescription * Concurrency
  | UnsubscribeTopic of TopicDescription

/// concurrently outstanding asynchronous requests
and Concurrency = int

type WorkerState =
  { QSubs : Map<QueueDescription, ReceiverSet list>;
    TSubs : Map<TopicDescription, ReceiverSet list> }
//    StartedCT : CancellationTokenSource option }

/// A pair of a messaging factory and a list of message receivers that
/// were created from that messaging factory.
and ReceiverSet = Pair of MessagingFactory * MessageReceiver list

(* receiver can have these states:

  Started
  Stopped
  Disposed

  with transitions:
  
  Initial -> Starting
  
  Starting -> Started

  Started -> Paused
  Started -> Halted
  
  Paused -> Starting
  Paused -> Halted

  Halted -> Final (GC-ed here)

  with state:

  Started 
*)

/// Create a new receiver, with a queue description,
/// a factory for messaging factories and some control flow data
type Receiver(desc   : QueueDescription,
              newMf  : (unit -> MessagingFactory),
              nm     : NamespaceManager,
              ?maxReceived : int,
              ?concurrency : int,
              ?mfEveryNthConcurrentAsync : int) =
  
  /// The 'scratch' buffer that tunnels messages from the ASB receivers
  /// to the consumers of the Receiver class.
  let messages = new BlockingCollection<_>(defaultArg maxReceived 50)
  
  let logger = MassTransit.Logging.Logger.Get(typeof<Receiver>)
  let timeout = TimeSpan.FromMilliseconds 50.0
  let concurrency = defaultArg concurrency 5
  let nthAsync = defaultArg mfEveryNthConcurrentAsync 100 // in initAsyncs
  let azQDesc = desc.Inner
  
  /// Starts stop/nthAsync new clients and message factories, so for stop=500, nthAsync=100
  /// it loops 500 times and starts 5 new clients
  let initReceiverSet desc newMf stop =
    let rec inner curr pairs =
      async {
        match curr with
        | _ when stop = (curr - 1) -> 
          // we're stopping
          return pairs
        | _ when curr % nthAsync = 0 || curr = 1 ->
          // we're at the first item, create a new pair
          let mf = newMf ()
          let! r = desc |> newReceiver mf
          let p = Pair(mf, r :: [])
          return! inner (curr+1) (p :: pairs)
        | _ ->
          // if we're not at an even location, just create a new receiver for
          // the same messaging factory
          match pairs with // of mf<-> receiver list
          | [] -> failwith "curr != 1, but pairs empty. curr > 1 -> pairs.Length > 0"
          | (Pair(mf, rs) :: rest) ->
            let! r = desc |> newReceiver mf // the new receiver
            let p = Pair(mf, r :: rs) // add the receiver to the list of receivers for this mf
            return! inner (curr+1) (p :: rest) }
    inner 1 []
    
  /// creates an async workflow worker, given a message receiver client
  let worker client =
      async {
        while true do
          let! bmsg = timeout |> recv client
          if bmsg <> null then
            logger.Debug("received message")
            messages.Add bmsg
          else
            () }//logger.Debug("got null msg due to timeout receiving") }
          
  /// cleans out the message buffer and disposes all messages therein
  let clearLocks () =
    async {
      while messages.Count > 0 do
        let m = ref null
        if messages.TryTake(m, TimeSpan.FromMilliseconds(4.0)) then 
          try 
            do! Async.FromBeginEnd((!m).BeginAbandon, (!m).EndAbandon)
            (!m).Dispose()
          with 
          | x ->
            let entry = sprintf "could not abandon message#%s" <| (!m).MessageId
            logger.Error(entry, x) }

  /// An agent that implements the reactor pattern, reacting to messages.
  /// The mutually recursive function 'initial' uses the explicit functional
  /// state pattern as described here:
  /// http://www.cl.cam.ac.uk/~tp322/papers/async.pdf
  let a = Agent<RecvMsg>.Start(fun inbox ->

      let rec initial () =
        async {
          logger.Debug "initial"
          let! msg = inbox.Receive ()
          match msg with
          | Start ->
            // create WorkerState for initial subscription (that of the queue)
            // and move to the started state
            let! rSet = initReceiverSet desc newMf concurrency
            let mappedRSet = Map.empty |> Map.add desc rSet
            return! starting { QSubs = mappedRSet ;
                               TSubs = Map.empty }
          | _ ->
            // because we only care about the Start message in the initial state,
            // we will ignore all other messages.
            return! initial () }

      and starting state =
        async {
          logger.Debug "starting"
          do! desc |> create nm
          use ct = new CancellationTokenSource ()
          for x in state.QSubs do // for all queue subscriptions
            for Pair(mf, rs) in x.Value do // for all mf-receiver list pairs
              for r in rs do // for all receivers
                Async.Start(r |> worker, ct.Token) // start a worker on that queue receiver
          for x in state.TSubs do
            for Pair(mf, rs) in x.Value do
              for r in rs do
                Async.Start(r |> worker, ct.Token)
          return! started state ct }

      and started state ct =
        async {
          logger.Debug "started"
          let! msg = inbox.Receive()
          match msg with
          | Pause -> ct.Cancel() ; return! paused state
          | Halt -> ct.Cancel(); return! halted state
          | SubscribeQueue(qd, cc) ->
            let! pairs = initReceiverSet qd newMf cc
            for Pair(mf, rs) in pairs do
              for r in rs do Async.Start(r |> worker, ct.Token)
            let qsubs' = state.QSubs.Add(qd, pairs)
            return! started { QSubs = qsubs' ; TSubs = state.TSubs } ct
          | UnsubscribeQueue qd ->
            (* todo *) return! started state ct
          | SubscribeTopic(td, cc) ->
            (* todo *) return! started state ct
          | UnsubscribeTopic td ->
          (* todo *) return! started state ct
          | _ -> (* ignore Start *) return! started state ct }

      and paused state =
        async { 
          let! msg = inbox.Receive()
          match msg with
          | Start -> return! starting state
          | Halt -> return! halted state
          | _ as x -> logger.Warn(sprintf "got %A, despite being paused" x) }

      and halted state =
        async { 
          logger.Debug "halted"
          do! clearLocks ()
          (* TODO cancelling and disposing our state *)
          // then exit
          () }
      initial ())



  /// All initial message receivers and messaging factories are kicked off and then
  /// all such receivers and factories are awaited.
  let mfAndRecvsColl = Async.RunSynchronously (initAsyncs desc newMf (concurrency+1) 1 [])

  let start () =
    desc |> create nm |> Async.RunSynchronously
    for (mf, client) in mfAndRecvsColl do
      worker client |> Async.Start

  /// Closes the receivers and message factories created
  let closeColl () =
    logger.InfoFormat("closing all ({0} of them) message factories and receivers", mfAndRecvsColl.Length)
    for (mf, recv) in mfAndRecvsColl do
      if not(mf.IsClosed) then
        try mf.Close()
        with | x -> logger.Error("could not close messaging factory", x)
      if not(recv.IsClosed) then
        try recv.Close()
        with | x -> logger.Error("could not close receiver", x)


  /// Starts the receiver which starts the consuming from the service bus
  /// and creates the queue if it doesn't exist
  member x.Start () =
    logger.InfoFormat("start called for queue '{0}'", desc)
    if started then ()
    else started <- true
    start ()

  /// Stops the receiver; allowing it to start once again.
  member x.Stop () =
    logger.InfoFormat("stop called for queue '{0}'", desc)
    if not(started) then ()
    else 
      started <- false
      Async.CancelDefaultToken ()

  /// Returns a message if one was added to the buffer within the timeout specified,
  /// or otherwise returns null.
  member x.Get(timeout : TimeSpan) =
    let mutable item = null
    let _ = messages.TryTake(&item, timeout.Milliseconds)
    item

  /// Tries to take an item from the list of messages
  member internal __.TryTake(?timeout) =
    let timeout = defaultArg timeout <| TimeSpan.FromMilliseconds 50.0
    let mutable item = null
    if messages.TryTake(&item, timeout.Milliseconds)
    then Some(item)
    else None

  member __.Consume() =
    asyncSeq {
      while true do
        yield messages.Take() }

  interface System.IDisposable with
    /// Cleans out all receivers and factories.
    member x.Dispose () = 
      logger.DebugFormat("dispose called for receiver on '{0}'", desc.Path)
      x.Stop()
      closeColl ()
      clearLocks ()
      messages.Dispose()

[<Extension>]
type ReceiverModule =
  static member StartReceiver(address     : AzureServiceBusEndpointAddress,
                              maxReceived : int,
                              concurrency : int) =
    let r = new Receiver(address.QueueDescription, (fun () -> address.MessagingFactoryFactory.Invoke()), address.NamespaceManager, maxReceived, concurrency)
    r.Start ()
    r
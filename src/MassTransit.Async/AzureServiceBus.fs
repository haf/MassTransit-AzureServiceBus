// Learn more about F# at http://fsharp.net
namespace MassTransit.Async

open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging

open FSharp.Control

open System
open System.Threading

open MassTransit.Async.Queue
open MassTransit.Async.AsyncRetry

/// Messages that can be passed to the server
type private message  = Faster | Slower | Stop
type private actor    = MailboxProcessor<message>

/// Create a new receiver
type Receiver<'T>(desc   : QueueDescription,
                  newMf  : (unit -> MessagingFactory),
                  handler: (CancellationToken -> 'T -> unit),
                  deserializer : (BrokeredMessage -> Async<'T>),
                  ?maxReceived : int) =
  
  let mutable started = false
  
  let queueSize = defaultArg maxReceived 50
  let messages = new BlockingQueueAgent<'T>(queueSize)
  let cancel = Async.DefaultCancellationToken
  let error  = new Event<System.Exception>()
  let timeout = TimeSpan.FromMilliseconds 50.0
  let maxFlushInterval = TimeSpan.FromMilliseconds 50.0

  let consumer = new actor (fun inbox ->
  
    let stop (mf : MessagingFactory) (client : MessageReceiver) = 
      client.Close()
      mf.Close()

    let rec start mf =
      printfn "started receiver"
      async {
        let! client = desc |> newReceiver mf
        return! loop client mf }

    and loop client mf =
      async {
        let! bmsg = timeout |> recv client
        if bmsg <> null && inbox.CurrentQueueLength = 0 then
//          printfn "got brokered msg"
          let! msg = deserializer bmsg
//          printfn "adding to buffer"
          do! messages.AsyncAdd msg
//          printfn "calling complete on msg"
          do! Async.FromBeginEnd(bmsg.BeginComplete, bmsg.EndComplete)
          return! loop client mf
        else
          printfn "got consumer internal msg or nothing to receive from ASB"
          let! msg = inbox.TryReceive 0
          match msg with
          | Some(m) -> match m with
                       | Faster -> return! start <| newMf ()
                       | Slower -> return stop mf client
                       | Stop   ->
                         printfn "stopping"
                         //Async.CancelDefaultToken ()
                         return ()
          | None    ->
            printfn "nothing in inbox, looping"
            return! loop client mf }

    printfn "**** CALLING START ****"
    start <| newMf ())

  /// Starts a basic router server, binding to the Address property
  member self.Start () =
    if started
      then invalidOp "already started"
    else 
      started <- true
      consumer.Error.Add error.Trigger
      consumer.Start ()

  member self.Faster() =
    if started |> not 
      then invalidOp "not started"
    else
      consumer.Post Faster

  member self.Slower() =
    consumer.Post Slower

  member self.AsyncGet(?timeout) = 
    let timeout = defaultArg timeout <| TimeSpan.FromMilliseconds 50.0
    if started |> not
      then invalidOp "not started"
    else
      messages.AsyncGet(timeout.Milliseconds)

  member self.Stop () =
    printfn "sending stop msg"
    started <- false
    consumer.Post Stop

  interface IDisposable with
    member x.Dispose() =
      consumer.Post Stop

//type InboundTransport(address, connectionHandler) =
//  interface IDisposable with
//    member x.Dispose() = 
//      
//  interface InboundTransport with
//    member x.Receive(
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
type Receiver(desc   : QueueDescription,
              newMf  : (unit -> MessagingFactory),
              ?maxReceived : int,
              ?concurrency : int) =
  
  let mutable started = false
  
  let queueSize = defaultArg maxReceived 50
  let messages = new BlockingQueueAgent<_>(queueSize)
  let cancel = Async.DefaultCancellationToken
  let error  = new Event<System.Exception>()
  let timeout = TimeSpan.FromMilliseconds 50.0
  let maxFlushInterval = TimeSpan.FromMilliseconds 50.0
  let concurrency = defaultArg concurrency 10

  let worker client =
      async {
        let! cancelled = Async.CancellationToken
        while cancelled.IsCancellationRequested |> not do
          let! bmsg = timeout |> recv client
          if bmsg <> null then
            do! messages.AsyncAdd bmsg }

  /// Starts a basic router server, binding to the Address property
  member self.Start () =
    if started
      then invalidOp "already started"
    else 
      started <- true
      Async.RunSynchronously(
        async {
          for i in 1 .. concurrency do
            let mf = newMf ()
            let! client = desc |> newReceiver mf
            worker client |> Async.Start })

  member self.AsyncGet(?timeout) = 
    let timeout = defaultArg timeout <| TimeSpan.FromMilliseconds 50.0
    if started |> not
      then invalidOp "not started"
    else
      messages.AsyncGet(timeout.Milliseconds)

  member self.AsyncConsume() =
    asyncSeq {
      while true do
        let! res = messages.AsyncGet()
        yield res }

  member self.Stop () =
    printfn "sending stop msg"
    started <- false
    Async.CancelDefaultToken ()

//type InboundTransport(address, connectionHandler) =
//  interface IDisposable with
//    member x.Dispose() = 
//      
//  interface InboundTransport with
//    member x.Receive(
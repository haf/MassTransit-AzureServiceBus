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
open System.Runtime.CompilerServices
open System.Threading

open MassTransit.AzureServiceBus
open MassTransit.Async.Queue
open MassTransit.Async.AsyncRetry

/// Messages that can be passed to the server
type private message  = Faster | Slower | Stop
type private actor    = MailboxProcessor<message>

/// Create a new receiver, with a queue description, 
/// a factory for messaging factories and some control flow data
type Receiver(desc   : QueueDescription,
              newMf  : (unit -> MessagingFactory),
              ?maxReceived : int,
              ?concurrency : int,
              ?mfEveryNthConcurrentAsync : int) =
  
  let mutable started = false

  let logger = MassTransit.Logging.Logger.Get(typeof<Receiver>)
  
  let queueSize = defaultArg maxReceived 50
  let messages = new BlockingQueueAgent<_>(queueSize)
  let cancel = Async.DefaultCancellationToken
  let error  = new Event<System.Exception>()
  
  let timeout = TimeSpan.FromMilliseconds 50.0
  let maxFlushInterval = TimeSpan.FromMilliseconds 50.0
  let concurrency = defaultArg concurrency 250
  let nthAsync = defaultArg mfEveryNthConcurrentAsync 100

  let azQDesc = desc.Inner

  let worker client =
      async {
        let! cancelled = Async.CancellationToken
        while cancelled.IsCancellationRequested |> not do
          let! bmsg = timeout |> recv client
          if bmsg <> null then
            logger.Debug("received message!")
            do! messages.AsyncAdd bmsg
          else
            logger.Debug("got null msg due to timeout receiving") }

  /// Starts (stop-1)/100 new clients and message factories, so for stop=501
  /// it loops 500 times and starts 5 new clients
  let rec initAsyncs desc newMf stop curr recvs =
    async {
      match curr with
      | _ when stop = curr ->
        return recvs
      | _ when curr % nthAsync = 0 || curr = 1 ->
        logger.Info("created a new messaging factory")
        let mf = newMf ()
        let! recv = desc |> newReceiver mf
        return! initAsyncs desc newMf stop (curr+1) ((mf, recv) :: recvs)
      | _ -> 
        return! initAsyncs desc newMf stop (curr+1) recvs }

  let mfAndRecvsColl = Async.RunSynchronously (initAsyncs desc newMf (concurrency+1) 1 [])

  /// Closes the receivers and message factories created
  let closeColl () =
    logger.Info("closing all message factories and receivers")
    for (mf, recv) in mfAndRecvsColl do
      if not(mf.IsClosed) then
        try
          mf.Close()
        with
          | x -> logger.Error("could not close messaging factory", x)
      if not(recv.IsClosed) then
        try
          recv.Close()
        with
          | x -> logger.Error("could not close receiver", x)

  /// Starts the receiver which starts the consuming from the service bus.
  member x.Start () =
    logger.InfoFormat("started for queue {0}", desc)
    if started then ()
    else
      started <- true
      for (mf, client) in mfAndRecvsColl do
        worker client |> Async.Start

  /// Stops the receiver which closes all messaging factories and message receivers 
  /// that it holds onto.
  member x.Stop () =
    logger.Info("Stop called on Recveiver")
    if not(started) then ()
    else 
      started <- false
      Async.CancelDefaultToken ()

  member x.Get(timeout : TimeSpan) =
    try
      messages.Get(timeout.Milliseconds)
    with
      | :? TimeoutException -> Unchecked.defaultof<obj> :?> BrokeredMessage

  member __.AsyncGet(?timeout) = 
    let timeout = defaultArg timeout <| TimeSpan.FromMilliseconds 50.0
    messages.AsyncGet(timeout.Milliseconds)

  member __.AsyncConsume() =
    asyncSeq {
      while true do
        let! res = messages.AsyncGet()
        yield res }

  interface System.IDisposable with
    member x.Dispose () = x.Stop() ; closeColl ()

[<Extension>]
type ReceiverModule =
  static member StartReceiver(desc   : QueueDescription,
                              newMf  : Func<MessagingFactory>,
                              maxReceived : int,
                              concurrency : int) =
    let r = new Receiver(desc, (fun () -> newMf.Invoke()), maxReceived, concurrency)
    r.Start ()
    r
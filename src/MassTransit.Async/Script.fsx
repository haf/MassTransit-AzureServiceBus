#r @"..\packages\FSharpx.Core.1.4.120213\lib\FSharpx.Async.dll"
#r @"C:\Program Files\Windows Azure SDK\v1.6\ServiceBus\ref\Microsoft.ServiceBus.dll"
#r @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Runtime.Serialization.dll"
#time "on"
#load "AccountDetails.fs"
#load "Counter.fs"
open MassTransit.Async.Counter
#load "AsyncRetry.fs"
open MassTransit.Async.AsyncRetry
#load "Queue.fs"
open MassTransit.Async.Queue
#load "AzureServiceBus.fs"
open AccountDetails // customize:
open MassTransit.Async
open FSharp.Control
open System
open System.Runtime.Serialization
open System.Threading
open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging

[<Serializable>]
type A(item : int) =
  member x.Item = item
  override x.ToString() = sprintf "A %i" item

let tp = TokenProvider.CreateSharedSecretTokenProvider(issuer_name, key)
let asb_uri = ServiceBusEnvironment.CreateServiceUri("sb", ns, "")
let nm = NamespaceManager(asb_uri, NamespaceManagerSettings(TokenProvider = tp))

let qdesc = QueueDescription("WhereUWent")
qdesc.MaxSizeInMegabytes <- 1024L*5L
qdesc.EnableBatchedOperations <- true

let mfFac = (fun () -> let mfs = MessagingFactorySettings(TokenProvider = tp,
                                   NetMessagingTransportSettings = NetMessagingTransportSettings(BatchFlushInterval = TimeSpan.FromMilliseconds 50.0))
                       MessagingFactory.Create(nm.Address, mfs))

let deserializer (message : BrokeredMessage) = printfn "Deserializing message: %s" <| message.ToString() ; message.GetBody<A>()
let handler token msg = printfn "%A" msg
let concurrency = 200 // concurrent outstanding messages
let counter = counter ()

// Producer:
Async.RunSynchronously( qdesc |> delete nm )
let random = Random()
let mf = mfFac ()
for i in 1 .. concurrency do
  async {
    let! sender = qdesc |> newSender mf nm
    counter.Post Start
    let ctoken = Async.DefaultCancellationToken
    while ctoken.IsCancellationRequested |> not do
      let num = random.Next(0, 25)
      do! A(num) |> send sender 
      counter.Post Sent }
  |> Async.Start

// Receiver:
let r = new Receiver(qdesc, mfFac, 1000, concurrency)
async {
  r.Start()
  counter.Post Start
  let token = Async.DefaultCancellationToken
  let running = (fun () -> token.IsCancellationRequested |> not)
  while running() do // equivalent to transport Receive being called (this body)
    let msgs = r.AsyncConsume () |> AsyncSeq.map (fun m -> deserializer m , m)
    for (dm, bm) in msgs do
      if running() |> not then return ()
      //printfn "(THIS PLACE is equivalent to sending (%A, _) up the chain of message sinks)" dm
      // this would have to go in the MT framework somehow, would be async instead of sync and would run on a fiber of its own
      ThreadPool.QueueUserWorkItem((fun mm -> (mm :?> BrokeredMessage).Complete()), bm) |> ignore
      counter.Post Received
  ()
} |> Async.Start

let qexists = Async.RunSynchronously(  qdesc |> exists nm )
Async.RunSynchronously( qdesc |> create nm )
Async.RunSynchronously( qdesc |> delete nm )

counter.Post Stop
counter.PostAndReply(fun chan -> Report(chan))

Async.CancelDefaultToken ()
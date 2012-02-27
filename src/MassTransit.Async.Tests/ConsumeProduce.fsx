#r "../MassTransit.Async/bin/Debug/MassTransit.Async.dll"
#r "../PerformanceTesting/Utils/bin/Debug/Utils.dll"
#r "../MassTransit.AzureServiceBus/bin/Debug/MassTransit.AzureServiceBus.dll"
#r @"..\packages\FSharpx.Core.1.4.120213\lib\FSharpx.Async.dll"
#r @"..\packages\Magnum.2.0.0.4\lib\NET40\Magnum.dll"
#r @"..\packages\MassTransit.2.1.0-prerelease\lib\net40\MassTransit.dll"
#I @"..\packages\NLog.2.0.0.2000\lib\net20"
#r "NLog.dll"
NLog.Config.SimpleConfigurator.ConfigureForConsoleLogging()
#r @"..\packages\MassTransit.NLog.2.1.0-prerelease\lib\net40\MassTransit.NLogIntegration.dll"
MassTransit.Logging.Logger.UseLogger(MassTransit.NLogIntegration.Logging.NLogLogger()) // MT logging
#r @"C:\Program Files\Windows Azure SDK\v1.6\ServiceBus\ref\Microsoft.ServiceBus.dll"
#r @"..\MassTransit.AzureServiceBus\bin\Debug\MassTransit.AzureServiceBus.dll"
#r @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.ServiceModel.dll"
//#r @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Runtime.Serialization.dll"
#load "Impl.fs"
#time "on"
//#load "AccountDetails.fs" 
open AC
open Impl
open Counter
open MassTransit.AzureServiceBus
//#load "Retry.fs"
open MassTransit.Async.Retry
//#load "AsyncRetry.fs"
open MassTransit.Async.AsyncRetry
//#load "Queue.fs"
open MassTransit.Async.Queue
//#load "Receiver.fs"
open MassTransit.Async
open FSharp.Control
open System
open System.Runtime.Serialization
open System.Threading
open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging

[<Serializable>] type A(item : int) =
                   member x.Item = item
                   override x.ToString() = sprintf "A %i" item

let tp = TokenProvider.CreateSharedSecretTokenProvider(issuer_name, key)
let asb_uri = ServiceBusEnvironment.CreateServiceUri("sb", ns, "")
let nm = NamespaceManager(asb_uri, NamespaceManagerSettings(TokenProvider = tp))
let qdesc = QDesc("WhereUWent")
let mfFac = (fun () -> let mfs = MessagingFactorySettings(TokenProvider = tp,
                                   NetMessagingTransportSettings = NetMessagingTransportSettings(BatchFlushInterval = TimeSpan.FromMilliseconds 50.0))
                       MessagingFactory.Create(nm.Address, mfs))
let deserializer (message : BrokeredMessage) = printfn "Deserializing message: %s" <| message.ToString() ; message.GetBody<A>()
let concurrency = 1 // concurrent outstanding messages
let counter = counter ()

// Producer:
Async.RunSynchronously( qdesc |> delete nm )
let random = Random()
let mf = mfFac ()
let sender = Async.RunSynchronously(qdesc |> newSender mf nm)
for i in 1 .. concurrency do
  async {
    counter.Post CounterMessage.Start
    let ctoken = Async.DefaultCancellationToken
    while ctoken.IsCancellationRequested |> not do
      let num = random.Next(0, 25)
      do! A(num) |> send sender 
      counter.Post (Sent(1)) }
  |> Async.Start

// Receiver:
let r = new Receiver(qdesc, mfFac, nm)
async {
  r.Start()
  counter.Post CounterMessage.Start
  let token = Async.DefaultCancellationToken
  let running = (fun () -> token.IsCancellationRequested |> not)
  while running() do // equivalent to transport Receive being called (this body)
    let msgs = r.Consume () |> AsyncSeq.map (fun m -> deserializer m , m)
    for (dm, bm) in msgs do
      if running() |> not then return ()
      //printfn "(THIS PLACE is equivalent to sending (%A, _) up the chain of message sinks)" dm
      // this would have to go in the MT framework somehow, would be async instead of sync and would run on a fiber of its own
      ThreadPool.QueueUserWorkItem((fun mm -> (mm :?> BrokeredMessage).Complete()), bm) |> ignore
      counter.Post (Received(1))
  ()
} |> Async.Start

let qexists = Async.RunSynchronously(  qdesc |> exists nm )
Async.RunSynchronously( qdesc |> create nm )
Async.RunSynchronously( qdesc |> delete nm )

counter.Post Stop
counter.PostAndReply(fun chan -> Report(chan))

r.Start()
r.Pause()

Async.CancelDefaultToken ()
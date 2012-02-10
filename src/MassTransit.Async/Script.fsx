#r @"..\packages\FSharpx.Core.1.4.120207\lib\FSharpx.Async.dll"
#r @"C:\Program Files\Windows Azure SDK\v1.6\ServiceBus\ref\Microsoft.ServiceBus.dll"
#r @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Runtime.Serialization.dll"
#time "on"
#load "AccountDetails.fs"
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
open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging

let tp = TokenProvider.CreateSharedSecretTokenProvider(issuer_name, key)
let asb_uri = ServiceBusEnvironment.CreateServiceUri("sb", ns, "")
let nm = NamespaceManager(asb_uri, NamespaceManagerSettings(TokenProvider = tp))

let qdesc = QueueDescription("WhereUWent")
qdesc.MaxSizeInMegabytes <- 1024L*5L
qdesc.EnableBatchedOperations <- true

let mfFac = (fun () -> let mfs = MessagingFactorySettings(TokenProvider = tp,
                                   NetMessagingTransportSettings = NetMessagingTransportSettings(BatchFlushInterval = TimeSpan.FromMilliseconds 50.0))
                       MessagingFactory.Create(nm.Address, mfs))
[<Serializable>]
type A(item : int) =
  member x.Item = item


let deserializer (message : BrokeredMessage) = 
  printfn "Deserializing message: %s" <| message.ToString()
  async { return message.GetBody<A>() }
let handler token msg = printfn "%A" msg
let random = Random()

let r = new Receiver<A>(qdesc, mfFac, handler, deserializer, 1000)

// Receiver:
async {
  r.Start()
  while true do
    let! v = async { try return! r.AsyncGet() with | :? TimeoutException -> return A(-1) }
    v |> ignore
    }
    //do! Async.Sleep(20) }
|> Async.Start

// Producer:
async {
  let mf = mfFac ()
  printfn "producer: created mf"
  let! sender = qdesc |> newSender mf nm
  while true do
    let next = random.Next(0, 25)
    do! A(next) |> send sender }
    //printfn "producer: in test sent %d" next }
    //do! Async.Sleep(400) }
|> Async.Start



let qexists = Async.RunSynchronously(  qdesc |> exists nm )
let newDesc = Async.RunSynchronously( qdesc |> create nm )
let ns = Async.RunSynchronously( qdesc |> newSender (mfFac()) nm)
let del = Async.RunSynchronously( qdesc |> delete nm )

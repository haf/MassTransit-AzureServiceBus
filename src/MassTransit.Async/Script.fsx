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

open MassTransit.Async
open AccountDetails

open FSharp.Control
open System
open System.Runtime.Serialization
open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging

// Let's do some service bus hacking
let tp = TokenProvider.CreateSharedSecretTokenProvider(issuer_name, key)
let asb_uri = ServiceBusEnvironment.CreateServiceUri("sb", ns, "")
let nm = NamespaceManager(asb_uri, NamespaceManagerSettings(TokenProvider = tp))

[<Serializable>]
type A(item : int) =
  member x.Item = item

let mfFac = (fun () -> MessagingFactory.Create(asb_uri, tp))
let qdesc = QueueDescription("fsharp-testing")
qdesc.MaxSizeInMegabytes <- 1024L*5L

let deserializer (message : BrokeredMessage) = 
  printfn "Deserializing message: %s" <| message.ToString()
  async { return message.GetBody<A>() }

let handler token msg = printfn "%A" msg
let prod_interval = 500
let recv_interval = 1000
let random = Random()

//let qexists = Async.RunSynchronously(  qdesc |> exists nm )
//let newDesc = Async.RunSynchronously( qdesc |> create nm )
//let ns = Async.RunSynchronously( qdesc |> newSender (mfFac()) nm)
//let del = Async.RunSynchronously( qdesc |> delete nm )

// Producer:
async {
  let mf = mfFac ()
  printfn "producer: created mf"
  let! sender = qdesc |> newSender mf nm
  while true do
    let next = random.Next(0, 25)
    printfn "producer: sending"
    do! A(next) |> send sender
    printfn "producer: in test sent %d" next
    do! Async.Sleep(prod_interval) }
|> Async.Start

// Receiver:
async {
  while true do
    use r = new Receiver<A>(qdesc, mfFac, handler, deserializer, 1000)
    do r.Start()
    let! v = async { try return! r.AsyncGet() with | :? TimeoutException -> return A(-1) }
    printfn "receiver in test sleeping..."
    do! Async.Sleep(recv_interval) }
|> Async.Start

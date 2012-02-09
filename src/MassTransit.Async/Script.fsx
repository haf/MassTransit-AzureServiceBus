#load "AccountDetails.fs"
#r @"..\packages\FSharpx.Core.1.4.120207\lib\FSharpx.Async.dll"
#r @"..\packages\WindowsAzure.ServiceBus.1.6.0.0\lib\net40-full\Microsoft.ServiceBus.dll"
#load "AzureServiceBus.fs"

open AccountDetails
open FSharp.Control

let buffer = new BlockingQueueAgent<int>(3)

open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging

// Let's do some service bus hacking
let tp = TokenProvider.CreateSharedSecretTokenProvider(issuer_name, key)
let asb_uri = ServiceBusEnvironment.CreateServiceUri("sb", ns, "")
let mf = MessagingFactory.Create(asb_uri, tp)
let nm = NamespaceManager(asb_uri, NamespaceManagerSettings(TokenProvider = tp))

module Queue =
  let queueDescription name = async {
    let! exists = Async.FromBeginEnd(name, nm.BeginQueueExists, nm.EndQueueExists)
    return! if exists then Async.FromBeginEnd(name, nm.BeginGetQueue, nm.EndGetQueue)
            else Async.FromBeginEnd(name, nm.BeginCreateQueue, nm.EndCreateQueue)
    }


    
// The sample uses two workflows that add/take elements
// from the buffer with the following timeouts. When the producer
// timout is larger, consumer will be blocked. Otherwise, producer
// will be blocked.
//let prod_interval = 500
//let cons_interval = 1000
//
//async {
//  for i in 0 .. 10 do
//    // Sleep for some time and then add value
//    do! Async.Sleep(prod_interval)
//    do! buffer.AsyncAdd(i)
//    printfn "Added %d" i }
//|> Async.Start
//
//async {
//  while true do
//    // Sleep for some time and then get value
//    do! Async.Sleep(cons_interval)
//    let! v = buffer.AsyncGet()
//    printfn "Got %d" v }
//|> Async.Start


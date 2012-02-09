// Learn more about F# at http://fsharp.net
namespace MassTransit.Async

open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging
open FSharp.Control
open System.Threading

type private message  = Data of byte[] | Quit
type private agent    = MailboxProcessor<message>

type Receiver(desc   : QueueDescription,
              mf     : MessagingFactory,
              handler: (CancellationToken -> byte[] -> unit),
              ?anonymous : bool      ) =
  
  let mutable started = false
  
  let buffer = new BlockingQueueAgent<_>(10)
  let anonymous = defaultArg anonymous true
  
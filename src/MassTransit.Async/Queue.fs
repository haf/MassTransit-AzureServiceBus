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

module Queue =
  
  open System
  open Microsoft.ServiceBus
  open Microsoft.ServiceBus.Messaging
  open MassTransit.Logging
  open MassTransit.AzureServiceBus
  
  let logger = Logger.Get("MassTransit.Async.Queue")

  type AQD = Microsoft.ServiceBus.Messaging.QueueDescription

  let desc (nm : NamespaceManager) name =
    async {
      let! exists = Async.FromBeginEnd(name, nm.BeginQueueExists, nm.EndQueueExists)
      let beginCreate = nm.BeginCreateQueue : string * AsyncCallback * obj -> IAsyncResult
      return! if exists then Async.FromBeginEnd(name, nm.BeginGetQueue, nm.EndGetQueue)
              else Async.FromBeginEnd(name, beginCreate, nm.EndCreateQueue) }
  
  let recv (client : MessageReceiver) timeout =
    let bRecv = client.BeginReceive : TimeSpan * AsyncCallback * obj -> IAsyncResult
    async {
      return! Async.FromBeginEnd(client.BeginReceive, client.EndReceive) }
  
  let send (client : MessageSender) message =
    async {
      use bm = new BrokeredMessage(message)
      do! Async.FromBeginEnd(bm, client.BeginSend, client.EndSend) : Async<unit> }
  
  let newReceiver (mf : MessagingFactory) (desc : QueueDescription) =
    async {
      return! Async.FromBeginEnd((desc.Path),
                      (fun (p, ar, state) -> mf.BeginCreateMessageReceiver(p, ar, state)),
                      mf.EndCreateMessageReceiver) }
  
  let exists (nm : NamespaceManager ) (desc : QueueDescription) = 
    async { return! Async.FromBeginEnd((desc.Path), nm.BeginQueueExists, nm.EndQueueExists) }

  let rec create (nm : NamespaceManager) (desc : QueueDescription) =
    async {
      let! exists = desc |> exists nm
      if exists then return ()
      try
        let beginCreate = nm.BeginCreateQueue : AQD * AsyncCallback * obj -> IAsyncResult
        let! ndesc = Async.FromBeginEnd((desc.Inner), beginCreate, nm.EndCreateQueue)
        return! desc |> create nm
      with
      | :? MessagingEntityAlreadyExistsException -> return () }

  let rec delete (nm : NamespaceManager) (desc : QueueDescription) =
    async {
      let! exists = desc |> exists nm
      if exists then return ()
      try
        do! Async.FromBeginEnd((desc.Path), nm.BeginDeleteQueue, nm.EndDeleteQueue)
        return! desc |> delete nm
      with
      | :? MessagingEntityNotFoundException -> return () }

  let newSender (mf : MessagingFactory) nm (desc : QueueDescription) =
    async {
      do! desc |> create nm
      printfn "starting sender"
      return! Async.FromBeginEnd((desc.Path),
                      mf.BeginCreateMessageSender,
                      mf.EndCreateMessageSender) }
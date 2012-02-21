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

open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<Extension>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Queue =
  
  open System
  open Microsoft.ServiceBus
  open Microsoft.ServiceBus.Messaging

  open MassTransit.Logging
  open MassTransit.AzureServiceBus
  
  let logger = Logger.Get("MassTransit.Async.Queue")

  type AQD = Microsoft.ServiceBus.Messaging.QueueDescription

  /// Create a queue description from a path name asynchronously
  [<Extension;CompiledName("Desc")>]
  let desc (nm : NamespaceManager) name =
    async {
      let! exists = Async.FromBeginEnd(name, nm.BeginQueueExists, nm.EndQueueExists)
      let beginCreate = nm.BeginCreateQueue : string * AsyncCallback * obj -> IAsyncResult
      return! if exists then Async.FromBeginEnd(name, nm.BeginGetQueue, nm.EndGetQueue)
              else Async.FromBeginEnd(name, beginCreate, nm.EndCreateQueue) }
  
  /// Create a queue description from a path name synchronously.
  [<Extension;CompiledName("DescSync")>]
  let descSync nm name = 
    Async.RunSynchronously( desc nm name )

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
  
  [<Extension;CompiledName("Exists")>]
  let exists (nm : NamespaceManager ) (desc : QueueDescription) = 
    async { return! Async.FromBeginEnd((desc.Path), nm.BeginQueueExists, nm.EndQueueExists) }

  [<Extension;CompiledName("ExistsAsync")>]
  let existsAsync nm desc = Async.StartAsTask(exists nm desc)

  /// Create a queue from the given queue description asynchronously; never throws MessagingEntityAlreadyExistsException
  [<Extension;CompiledName("Create")>]
  let rec create (nm : NamespaceManager) (desc : QueueDescription) =
    async {
      let! exists = desc |> exists nm
      if exists then return ()
      try
        let beginCreate = nm.BeginCreateQueue : AQD * AsyncCallback * obj -> IAsyncResult
        let! ndesc = Async.FromBeginEnd((desc.Inner), beginCreate, nm.EndCreateQueue)
        logger.Debug "creating queue"
        return! desc |> create nm
      with
      | :? MessagingEntityAlreadyExistsException -> return () }
  
  /// Create a queue from the given queue description synchronously; never throws MessagingEntityAlreadyExistsException
  [<Extension;CompiledName("CreateAsync")>]
  let createSync nm desc = 
    Async.StartAsTask(
      async { 
        do! create nm desc
        return Unit() })

  /// Delete a queue from the given queue description asynchronously; never throws MessagingEntityNotFoundException.
  [<Extension;CompiledName("Delete")>]
  let rec delete (nm : NamespaceManager) (desc : QueueDescription) =
    async {
      let! exists = desc |> exists nm
      if exists then return ()
      try
        logger.Debug "deleting queue"
        do! Async.FromBeginEnd((desc.Path), nm.BeginDeleteQueue, nm.EndDeleteQueue)
        return! desc |> delete nm
      with
      | :? MessagingEntityNotFoundException -> return () }

  /// Delete a queue from the given queue description synchronously; never throws MessagingEntityNotFoundException.
  [<Extension;CompiledName("DeleteAsync")>]
  let deleteSync nm desc = Async.StartAsTask(delete nm desc)

  [<Extension;CompiledName("ToggleQueue")>]
  let toggle nm desc =
    async {
      do! delete nm desc
      do! create nm desc 
      return Unit() }

  [<Extension;CompiledName("ToggleQueueAsync")>]
  let toggleAsync nm desc = Async.StartAsTask(toggle nm desc)

  /// Create a new message sender from the messaging factory and the queue description
  let newSender (mf : MessagingFactory) nm (desc : QueueDescription) =
    async {
      do! desc |> create nm
      logger.Debug "starting sender"
      return! Async.FromBeginEnd((desc.Path),
                      mf.BeginCreateMessageSender,
                      mf.EndCreateMessageSender) }

  [<Extension;CompiledName("NewSenderAsync")>]
  let newSenderAsync mf nm desc = 
    Async.StartAsTask(newSender mf nm desc)
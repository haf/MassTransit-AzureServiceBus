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
namespace MassTransit.Transports.AzureServiceBus.Receiver

open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<Extension>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Queue =
  
  open System
  open Microsoft.ServiceBus
  open Microsoft.ServiceBus.Messaging
  open System.Threading.Tasks

  open MassTransit.Logging
  open MassTransit.Transports.AzureServiceBus
  open MassTransit.Async.FaultPolicies
  
  let logger = Logger.Get("MassTransit.Async.Queue")

  type AQD = Microsoft.ServiceBus.Messaging.QueueDescription

  /// Create a queue description from a path name asynchronously
  [<Extension;CompiledName("Desc")>]
  let desc (nm : NamespaceManager) name =
    asyncRetry {
      let! exists = Async.FromBeginEnd(name, nm.BeginQueueExists, nm.EndQueueExists)
      let beginCreate = nm.BeginCreateQueue : string * AsyncCallback * obj -> IAsyncResult
      return! if exists then Async.FromBeginEnd(name, nm.BeginGetQueue, nm.EndGetQueue)
              else Async.FromBeginEnd(name, beginCreate, nm.EndCreateQueue) }
  
  /// Create a queue description from a path name synchronously.
  [<Extension;CompiledName("DescSync")>]
  let descSync nm name = 
    Async.RunSynchronously( desc nm name )

  /// Perform a receive using a message receiver and a timeout.
  let recv (client : MessageReceiver) timeout =
    asyncRetry { return! Async.FromBeginEnd(timeout, client.BeginReceive, client.EndReceive) }
  
  let send (client : MessageSender) message =
    asyncRetry {
      use bm = new BrokeredMessage(message)
      do! Async.FromBeginEnd(bm, client.BeginSend, client.EndSend) : Async<unit> }
  
  let newReceiver (mf : MessagingFactory) (desc : PathBasedEntity) =
    asyncRetry {
      let! wrapped = Async.FromBeginEnd(desc.Path,
                       (fun (p, ar, state) -> mf.BeginCreateMessageReceiver(p, ar, state)),
                       mf.EndCreateMessageReceiver)
      return { new MessageReceiver with 
                   member x.BeginReceive(timeout, callback, state) =
                     wrapped.BeginReceive(timeout, callback, state)
                   member x.EndReceive(result) =
                     wrapped.EndReceive(result) 
                   member x.IsClosed = 
                     wrapped.IsClosed 
                   member x.Close () =
                     wrapped.Close() } }
  
  [<Extension;CompiledName("Exists")>]
  let exists (nm : NamespaceManager ) (desc : PathBasedEntity) = 
    asyncRetry { return! Async.FromBeginEnd(desc.Path, nm.BeginQueueExists, nm.EndQueueExists) }

  [<Extension;CompiledName("ExistsAsync")>]
  let existsAsync nm desc = Async.StartAsTask(exists nm desc)

  /// Create a queue from the given queue description asynchronously; never throws MessagingEntityAlreadyExistsException
  [<Extension;CompiledName("Create")>]
  let rec create (nm : NamespaceManager) (desc : QueueDescription) =
    asyncRetry {
      let! exists = desc |> exists nm
      if exists then return ()
      else
        try
          let beginCreate = nm.BeginCreateQueue : string * AsyncCallback * obj -> IAsyncResult
          logger.DebugFormat("creating queue '{0}'", desc)
          let! ndesc = Async.FromBeginEnd(desc.Path, beginCreate, nm.EndCreateQueue)
          return! desc |> create nm
        with | :? MessagingEntityAlreadyExistsException -> return () }
  
  /// Create a queue from the given queue description synchronously; never throws MessagingEntityAlreadyExistsException
  [<Extension;CompiledName("CreateAsync")>]
  let createAsync nm desc = 
    Async.StartAsTask(
      async {
        do! create nm desc
      }) :> Task

  /// Delete a queue from the given queue description asynchronously; never throws MessagingEntityNotFoundException.
  [<Extension;CompiledName("Delete")>]
  let rec delete (nm : NamespaceManager) (desc : QueueDescription) =
    asyncRetry {
      let! exists = desc |> exists nm
      if exists then return ()
      else
        try
          logger.DebugFormat("deleting queue '{0}'", desc)
          do! Async.FromBeginEnd(desc.Path, nm.BeginDeleteQueue, nm.EndDeleteQueue)
          return! desc |> delete nm
        with :? MessagingEntityNotFoundException -> return () }

  /// Delete a queue from the given queue description synchronously; never throws MessagingEntityNotFoundException.
  [<Extension;CompiledName("DeleteAsync")>]
  let deleteSync nm desc = Async.StartAsTask(delete nm desc)
  
  /// Naïve Drain operation, by deleting and then creating the queue,
  /// or simply creating the queue if it doesn't exist.
  [<Extension;CompiledName("ToggleQueue")>]
  let toggle nm d =
    async {
      do! delete nm d
      do! create nm d
    }

  /// Naïve Drain operation, by deleting and then creating the queue,
  /// or simply creating the queue if it doesn't exist.
  [<Extension;CompiledName("ToggleQueueAsync")>]
  let toggleAsync nm desc = Async.StartAsTask(toggle nm desc) :> Task

  /// Create a new message sender from the messaging factory and the queue description
  let newSender (mf : MessagingFactory) nm (desc : QueueDescription) =
    asyncRetry {
      do! desc |> create nm
      logger.Debug "starting sender"
      return! Async.FromBeginEnd((desc.Path),
                      mf.BeginCreateMessageSender,
                      mf.EndCreateMessageSender) }

  [<Extension;CompiledName("NewSenderAsync")>]
  let newSenderAsync mf nm desc = 
    Async.StartAsTask(newSender mf nm desc)
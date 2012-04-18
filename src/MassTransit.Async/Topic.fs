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
module Topic =

  open System
  open Microsoft.ServiceBus
  open Microsoft.ServiceBus.Messaging

  open MassTransit.Logging
  open MassTransit.AzureServiceBus
  open MassTransit.Async.FaultPolicies
  
  let logger = Logger.Get("MassTransit.Async.Topic")

  type AQT = Microsoft.ServiceBus.Messaging.TopicDescription

  [<Extension;CompiledName("Exists")>]
  let exists (nm : NamespaceManager ) (desc : PathBasedEntity) = 
    asyncRetry { return! Async.FromBeginEnd(desc.Path, nm.BeginTopicExists, nm.EndTopicExists) }

  [<Extension;CompiledName("SubscriptionExists")>]
  let subexists (nm : NamespaceManager ) (desc : PathBasedEntity) (sub : string) = 
    asyncRetry { return! Async.FromBeginEnd(desc.Path, sub, nm.BeginSubscriptionExists, nm.EndSubscriptionExists) }

  [<Extension;CompiledName("Create")>]
  let create (nm : NamespaceManager) (desc : TopicDescription) = 
    let rec create' () =
      asyncRetry {
        let! exists = desc |> exists nm
        if exists then return ()
        else
          try
            let beginCreate = nm.BeginCreateTopic : string * AsyncCallback * obj -> IAsyncResult
            logger.DebugFormat("creating topic '{0}'", desc)
            let! tdesc = Async.FromBeginEnd(desc.Path, beginCreate, nm.EndCreateTopic)
            return! create' ()
          with :? MessagingEntityAlreadyExistsException -> return () }
    create' ()
    
  [<Extension;CompiledName("CreateAsync")>]
  let createAsync nm desc = 
    Async.StartAsTask(
        async {
          do! create nm desc
          return Unit() })

  /// Create a queue from the given queue description asynchronously; never throws MessagingEntityAlreadyExistsException
  [<Extension;CompiledName("Subscribe")>]
  let subscribe (nm : NamespaceManager) (subName : string) (desc : TopicDescription)  : Async<unit> =
    asyncRetry {
      let! exists = subName |> subexists nm desc
      if exists then return ()
      else
          do! create nm desc
          let beginCreate = nm.BeginCreateSubscription : string * string * AsyncCallback * obj -> IAsyncResult
          try
            let! sub = Async.FromBeginEnd(desc.Path, subName, beginCreate, nm.EndCreateSubscription)
            return ()
          with
            | :? MessagingEntityAlreadyExistsException -> return () }

  let newReceiver (sub : string) (mf : MessagingFactory) (desc : PathBasedEntity) =
     async {
      let wrapped = mf.CreateSubscriptionClient(desc.Path, sub)
      return { new MessageReceiver with 
                   member x.BeginReceive(timeout, callback, state) =
                     wrapped.BeginReceive(timeout, callback, state)
                   member x.EndReceive(result) =
                     wrapped.EndReceive(result) 
                   member x.IsClosed = 
                     wrapped.IsClosed 
                   member x.Close () =
                     wrapped.Close() } }

  let unsubscribe (nm : NamespaceManager) (desc : TopicDescription) (sub : string) =
    asyncRetry {
      do! Async.FromBeginEnd(desc.Path, sub, nm.BeginDeleteSubscription, nm.EndDeleteSubscription) }
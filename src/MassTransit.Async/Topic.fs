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
  
  let logger = Logger.Get("MassTransit.Async.Topic")

  type AQT = Microsoft.ServiceBus.Messaging.TopicDescription

  [<Extension;CompiledName("Exists")>]
  let exists (nm : NamespaceManager ) (desc : PathBasedEntity) = 
    async { return! Async.FromBeginEnd(desc.Path, nm.BeginTopicExists, nm.EndTopicExists) }

  /// Create a queue from the given queue description asynchronously; never throws MessagingEntityAlreadyExistsException
  [<Extension;CompiledName("Create")>]
  let subscribe (nm : NamespaceManager) (subName : string) (desc : TopicDescription)  : Async<SubscriptionDescription> =
    let rec first_create () =
      async {
        let! exists = desc |> exists nm
        if exists then return! (then_create_subscription () : Async<SubscriptionDescription>)
        try
          let beginCreate = nm.BeginCreateTopic : string * AsyncCallback * obj -> IAsyncResult
          logger.DebugFormat("creating topic '{0}'", desc)
          let! tdesc = Async.FromBeginEnd(desc.Path, beginCreate, nm.EndCreateTopic)
          return! first_create ()
        with | :? MessagingEntityAlreadyExistsException -> return! then_create_subscription () }
    and then_create_subscription ()  : Async<SubscriptionDescription> =
      async {
        let beginCreate = nm.BeginCreateSubscription : string * string * AsyncCallback * obj -> IAsyncResult
        return! Async.FromBeginEnd(desc.Path, subName, beginCreate, nm.EndCreateSubscription) }
    first_create ()
namespace MassTransit.Transports.AzureServiceBus

open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging
open System
open System.Threading.Tasks
open MassTransit
open MassTransit.Util

[<Serializable>]
type Unit() = class end

type PathBasedEntity =
  abstract member Path : string

type EntityDescription =
  abstract member ExtensionData : Runtime.Serialization.ExtensionDataObject
  // TODO: more getters!

type QueueDescription =
  inherit IEquatable<QueueDescription>
  inherit IComparable<QueueDescription>
  inherit IComparable
  inherit PathBasedEntity
  inherit EntityDescription

type TopicDescription =
  inherit IEquatable<TopicDescription>
  inherit IComparable<TopicDescription>
  inherit IComparable
  inherit PathBasedEntity
  inherit EntityDescription

type AzureServiceBusEndpointAddress =
  inherit IEndpointAddress
  inherit IDisposable
  [<NotNull>]
  abstract member TokenProvider : TokenProvider
  [<NotNull>]
  abstract member MessagingFactoryFactory : Func<MessagingFactory>
  [<NotNull>]
  abstract member NamespaceManager : NamespaceManager
  [<NotNull>]
  abstract member CreateQueue : unit -> Task<Unit>
  [<CanBeNull>]
  abstract member QueueDescription : QueueDescription
  [<CanBeNull>]
  abstract member TopicDescription : TopicDescription
  [<NotNull>]
  abstract member ForTopic : string -> AzureServiceBusEndpointAddress

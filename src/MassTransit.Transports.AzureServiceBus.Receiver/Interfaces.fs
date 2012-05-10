namespace MassTransit.Transports.AzureServiceBus

open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging
open System
open System.Threading.Tasks
open MassTransit
open MassTransit.Util
open System.Runtime.Serialization

type SenderSettings =
  abstract member MaxOutstanding : int

type MessageSender =
  inherit IDisposable
  abstract member BeginSend : BrokeredMessage * AsyncCallback * obj -> IAsyncResult
  abstract member EndSend : IAsyncResult -> unit

type ReceiverSettings =
  abstract member Concurrency : uint32 with get
  abstract member BufferSize : uint32 with get
  abstract member NThAsync : uint32 with get
  abstract member ReceiveTimeout : TimeSpan with get
  abstract member ReceiverName : string with get

type MessageReceiver =
  abstract member BeginReceive : TimeSpan * AsyncCallback * obj -> IAsyncResult
  abstract member EndReceive : IAsyncResult -> BrokeredMessage
  abstract member IsClosed : bool with get
  abstract member Close : unit -> unit

type PathBasedEntity =
  abstract member Path : string with get

type EntityDescription =
  abstract member IsReadOnly : bool with get
  abstract member ExtensionData : Runtime.Serialization.ExtensionDataObject with get
  abstract member DefaultMessageTimeToLive : TimeSpan with get
  abstract member MaxSizeInMegabytes : int64 with get
  abstract member RequiresDuplicateDetection : bool with get
  abstract member DuplicateDetectionHistoryTimeWindow : TimeSpan with get
  abstract member SizeInBytes : int64 with get
  abstract member EnableBatchedOperations : bool with get

type QueueDescription =
  inherit IEquatable<QueueDescription>
  inherit IComparable<QueueDescription>
  inherit IComparable
  inherit PathBasedEntity
  inherit EntityDescription
  /// How long before the consumed message is re-inserted into the queue?
  abstract member LockDuration : TimeSpan with get
  /// Whether the queue is configured to be session-ful;
  /// allowing a primitive key-value store as well as de-duplication
  /// on a per-receiver basis.
  abstract member RequiresSession : bool with get
  abstract member EnableDeadLetteringOnMessageExpiration : bool with get
  /// The maximum times to try to redeliver a message before moving it
  /// to a poision message queue. Note - this property would preferrably be int.MaxValue, since MassTransit
  /// handles the poison message queues itself and there's no need to use the 
  /// AzureServiceBus API for this.
  abstract member MaxDeliveryCount : int with get
  /// Gets the number of messages present in the queue
  abstract member MessageCount : int64 with get
  /// Gets the inner <see cref="Microsoft.ServiceBus.Messaging.QueueDescription"/>.
  abstract member Inner : Microsoft.ServiceBus.Messaging.QueueDescription with get

type TopicDescription =
  inherit IEquatable<TopicDescription>
  inherit IComparable<TopicDescription>
  inherit IComparable
  inherit PathBasedEntity
  inherit EntityDescription
  abstract member IsReadOnly : bool with get
  /// You can use this if your entity is session-ful, to keep track of entity state.
  abstract member ExtensionData : ExtensionDataObject with get
  abstract member DefaultMessageTimeToLive : TimeSpan with get
  abstract member MaxSizeInMegabytes : int64 with get
  /// Only for session-ful work
  abstract member RequiresDuplicateDetection : bool with get
  /// Only for session-ful work
  abstract member DuplicateDetectionHistoryTimeWindow : TimeSpan with get
  /// Gets the queue size in bytes
  abstract member SizeInBytes : int64 with get
  /// Should be true in this transport.
  abstract member EnableBatchedOperations : bool with get

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
  abstract member CreateQueue : unit -> Task
  [<CanBeNull>]
  abstract member QueueDescription : QueueDescription
  [<CanBeNull>]
  abstract member TopicDescription : TopicDescription
  [<NotNull>]
  abstract member ForTopic : string -> AzureServiceBusEndpointAddress

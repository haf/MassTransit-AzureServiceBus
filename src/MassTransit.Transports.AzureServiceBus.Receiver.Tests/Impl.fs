module Impl

open System
open MassTransit.Transports.AzureServiceBus

type QDesc(path) =
  let inner = Microsoft.ServiceBus.Messaging.QueueDescription(path)
  do inner.EnableBatchedOperations <- true
  do inner.MaxSizeInMegabytes <- 5L * 1024L
  override x.GetHashCode () = path.GetHashCode ()
  interface QueueDescription with
    member x.Path = path
    member x.IsReadOnly = inner.IsReadOnly
    member x.ExtensionData = inner.ExtensionData
    member x.LockDuration = inner.LockDuration
    member x.MaxSizeInMegabytes = inner.MaxSizeInMegabytes
    member x.RequiresDuplicateDetection =inner.RequiresDuplicateDetection
    member x.RequiresSession =inner.RequiresSession
    member x.DefaultMessageTimeToLive =inner.DefaultMessageTimeToLive
    member x.EnableDeadLetteringOnMessageExpiration =inner.EnableDeadLetteringOnMessageExpiration
    member x.DuplicateDetectionHistoryTimeWindow =inner.DuplicateDetectionHistoryTimeWindow
    member x.MaxDeliveryCount = inner.MaxDeliveryCount
    member x.EnableBatchedOperations = inner.EnableBatchedOperations
    member x.SizeInBytes = inner.SizeInBytes
    member x.MessageCount = inner.MessageCount
    member x.Inner = inner
  interface System.IComparable with
    member x.CompareTo other =
      match other with
      | null -> -1
      | :? QueueDescription as o -> compare path o.Path
      | _ -> invalidArg "other" <| sprintf "cannot compare a %A (!) and %A" other x
  interface System.IComparable<QueueDescription> with
    member x.CompareTo other = compare path other.Path
  interface System.IEquatable<QueueDescription> with
    member x.Equals d = d.Path = path
  override x.Equals o = (x :> QueueDescription).CompareTo o = 0

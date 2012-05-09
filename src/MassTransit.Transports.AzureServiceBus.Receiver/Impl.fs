module Impl

open System
open MassTransit.Transports.AzureServiceBus
open MassTransit.AzureServiceBus

type QDesc(path) =
  let inner = Microsoft.ServiceBus.Messaging.QueueDescription(path)
  do inner.EnableBatchedOperations <- true
  do inner.MaxSizeInMegabytes <- 5L * 1024L
  override x.GetHashCode () = path.GetHashCode ()
  interface QueueDescription with
    member x.Path = path
    member x.LockDuration = inner.LockDuration
    member x.RequiresSession = inner.RequiresSession
    member x.EnableDeadLetteringOnMessageExpiration = inner.EnableDeadLetteringOnMessageExpiration
    member x.MaxDeliveryCount = inner.MaxDeliveryCount
    member x.MessageCount = inner.MessageCount
    member x.Inner = inner
    member x.IsReadOnly = inner.IsReadOnly
    member x.DuplicateDetectionHistoryTimeWindow =inner.DuplicateDetectionHistoryTimeWindow
    member x.RequiresDuplicateDetection =inner.RequiresDuplicateDetection
    member x.ExtensionData = inner.ExtensionData
    member x.MaxSizeInMegabytes = inner.MaxSizeInMegabytes
    member x.EnableBatchedOperations = inner.EnableBatchedOperations
    member x.SizeInBytes = inner.SizeInBytes
    member x.DefaultMessageTimeToLive =inner.DefaultMessageTimeToLive
  interface System.IComparable with
    member x.CompareTo other =
      match other with
      | null -> -1
      | :? QueueDescription as o -> compare path o.Path
      | _ -> invalidArg "other" <| sprintf "cannot compare a %A (!) and %A" other x
  interface System.IComparable<QueueDescription> with
    member x.CompareTo other = 
      if other <> null then compare path other.Path else -1
  interface System.IEquatable<QueueDescription> with
    member x.Equals d =
      if d <> null then d.Path = path else false
  override x.Equals o =
    (x :> QueueDescription).CompareTo o = 0

type MsgSender(wrapped : Microsoft.ServiceBus.Messaging.MessageSender) =
  interface MessageSender with
    member x.BeginSend(timeout, callback, state) =
      wrapped.BeginSend(timeout, callback, state)
    member x.EndSend(result) =
      wrapped.EndSend(result)
  interface IDisposable with
    member x.Dispose() =
      wrapped.Close()
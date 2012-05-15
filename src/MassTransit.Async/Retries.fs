namespace MassTransit.Async

module Retries =
  open MassTransit.Async.AsyncRetry
  open System
  open System.Threading.Tasks

  /// Execute the action, retrying according to the specified policy
  let retry<'a> (policy : RetryPolicy) (start : AsyncCallback * obj -> IAsyncResult) (endRes : IAsyncResult -> 'a) =
    let ar = AsyncRetryBuilder policy
    ar { return! Async.FromBeginEnd(start, endRes) } |> Async.StartAsTask
  
  [<CompiledName("Retry")>]
  let retryCS<'a> (policy : RetryPolicy) (start : Func<AsyncCallback, obj, IAsyncResult>) (endRes : Func<IAsyncResult, 'a>) =
    retry<'a> policy (fun (ac, state) -> start.Invoke(ac, state)) (fun iar -> endRes.Invoke(iar))

  let retryA (policy : RetryPolicy) (start : AsyncCallback * obj -> IAsyncResult) (endRes : IAsyncResult -> unit) =
    let ar = AsyncRetryBuilder policy
    ar { do! Async.FromBeginEnd(start, endRes) } |> Async.StartAsTask :> Task

  [<CompiledName("Retry")>]
  let retryCSA (policy : RetryPolicy) (start : Func<AsyncCallback, obj, IAsyncResult>) (endRes : Action<IAsyncResult>) =
    retryA policy (fun (ac, state) -> start.Invoke(ac, state)) (fun iar -> endRes.Invoke(iar))
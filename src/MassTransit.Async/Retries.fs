namespace MassTransit.Async

module Retries =
    open MassTransit.Async.AsyncRetry
    open System
    open System.Threading.Tasks

    /// Execute the action, retrying according to the specified policy
    [<CompiledName("Retry")>]
    let retry ( policy : RetryPolicy ) ( action : Action ) =
        AsyncRetryBuilder policy { action.Invoke() |> ignore } 
        |> Async.StartAsTask :> Task
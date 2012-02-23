#load "Retry.fs"
open MassTransit.Async.FancyRetries
#load "TransientErrorHandling.fs"
open MassTransit.Async.TransientErrorHandling
// https://gist.github.com/1869125
#r @"C:\Program Files\Windows Azure SDK\v1.6\ServiceBus\ref\Microsoft.ServiceBus.dll"
#r "System.ServiceModel"
open System

// Example
let random = new Random()
let test = retry { return 1 / random.Next(0, 2) }
let test2 = retry { return (printfn "*" ; raise <| TimeoutException() ; 0) }

let exnTest = exnRetryCust<TimeoutException> (fun (c, e) -> printfn "%i" c ; c < 2, TimeSpan.FromSeconds(0.1))

(test2, exnTest) ||> run

(test, RetryPolicies.NoRetry()) ||> run
(test, RetryPolicies.Retry 10) ||> run |> ignore

let asyncRtest =
  async {
    return retryWithPolicy exnTest (
      retry {
        return 42 }) }

#load "Retry.fs"
// https://gist.github.com/1869125
#r @"C:\Program Files\Windows Azure SDK\v1.6\ServiceBus\ref\Microsoft.ServiceBus.dll"

open System
open MassTransit.Async.FancyRetries

// Example
let random = new Random()
let test = retry { return 1 / random.Next(0, 2) }
let test2 = retry { return (raise <| TimeoutException() ; 0) }



/// retry timeouts 9 times with a 5 second delay
let exnRetry<'ex when 'ex :> exn> ts =
  RetryPolicy( ShouldRetry(fun (count, ex) -> (match box ex with | :? 'ex -> true | _ -> false) && count < 9, ts) )

(test2, exnRetry<TimeoutException> (TimeSpan.FromSeconds(0.1))) ||> run


(test, RetryPolicies.NoRetry()) ||> run
(test, RetryPolicies.Retry 10) ||> run |> ignore

open System.ServiceModel
open Microsoft.ServiceBus
open Microsoft.ServiceBus.Messaging

let serverOverloaded = [| exnRetry<TimeoutException>;
                          exnRetry<ServerBusyException> |]
                       |> Array.map (fun fn -> fn TimeSpan.Zero)
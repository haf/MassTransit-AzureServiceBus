#I "../MassTransit.Async/bin/Debug"
#r "MassTransit.Async.dll"
open MassTransit.Async.Retry
open MassTransit.Async.FaultPolicies
open MassTransit.Async.AsyncRetry

// https://gist.github.com/1869125
#r @"C:\Program Files\Windows Azure SDK\v1.6\ServiceBus\ref\Microsoft.ServiceBus.dll"
#r "System.ServiceModel"
open System

// Example
let random = new Random()
let test = retry { return 1 / random.Next(0, 2) }
let test2 = retry { return (printfn "*" ; raise <| TimeoutException() ; 0) }

let exnTest = exnRetryCust<TimeoutException> (fun (c, e) -> printfn "%i" c ; c < 2, TimeSpan.FromSeconds(0.1))

//(test2, exnTest) ||> run
//(test, RetryPolicies.NoRetry()) ||> run
//(test, RetryPolicies.Retry 10) ||> run |> ignore

// sample async functions
let doThis () = async { return 1 }
let doThat a = async { return a + 1 }

// this is how one could use it
let asyncRtest =
  asyncRetry {
    let! r = doThis ()
    return! doThat r }

Async.RunSynchronously(asyncRtest)
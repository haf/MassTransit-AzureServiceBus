module FaultPoliciesTests

open System
open MassTransit.Async.AsyncRetry
open MassTransit.Async.FaultPolicies

open NUnit.Framework

// Retry Framework:

exception TestException of string

let failedAsync c =
  async {
    c := !c + 1
    return raise <| TestException("MUDDY WATERS") }

let failedAsyncInt c =
  async {
    c := !c + 1
    if 1/1 = 1 then return raise <| TestException("MUDDY WATERS")
    else return 0 }

let setup policyUnderTest =
  let counter = ref 0
  let builder = AsyncRetryBuilder(policyUnderTest)
  builder, failedAsync counter, counter

let setupInt policyUnderTest =
  let counter = ref 0
  let builder = AsyncRetryBuilder(policyUnderTest)
  builder, failedAsyncInt counter, counter

[<Test>]
let ``NoRetry():bind should be called once and then never again``() =

  let builder, failer, c = setup <| RetryPolicies.NoRetry()

  Assert.Throws<TestException>(fun () ->
    builder { do! failer
              return () }
    |> Async.RunSynchronously) |> ignore

  Assert.That(!c, Is.EqualTo(1), "only called once: no retry")

[<Test>]
let ``Retry(1):bind should be called twice and then never again`` () =
  
  let builder, failer, c = setup <| RetryPolicies.Retry(1)
  
  Assert.Throws<TestException>(fun () ->
    builder { do! failer
              return () }
    |> Async.RunSynchronously) |> ignore
  
  Assert.That(!c, Is.EqualTo(2), "one single call + one single retry")

[<Test>]
let ``Retry(2):bind should be called thrice (two retries) and then never again`` () =
  
  let builder, failer, c = setup <| RetryPolicies.Retry(2)
  
  Assert.Throws<TestException>(fun () ->
    builder { do! failer
              return () }
    |> Async.RunSynchronously) |> ignore
  
  Assert.That(!c, Is.EqualTo(3), "one single call + two retries")


[<Test>]
let ``Retry(1, Effect):bind Effect should be called at least once``() =

  let wasCalled = ref false
  let builder, failer, c = setup <| RetryPolicies.Retry(1, (fun _ -> async{ wasCalled := true ; return ()} ))
  
  Assert.Throws<TestException>(fun () ->
    builder { do! failer
              return () }
    |> Async.RunSynchronously) |> ignore

  Assert.That(!c, Is.EqualTo(2), "one single call + one single retry")
  Assert.That(!wasCalled, Is.True, "it SHOULD have called the effect")

[<Test>]
let ``Retry(1):tryWith,delay,bind should call twice (one retry). Exception from try-branch.``() =
  
  let builder, failer, c = setup <| RetryPolicies.Retry(1)

  builder { 
    try
      do! failer
      return ()
    with e ->
      return () }
  |> Async.RunSynchronously // : unit

  Assert.That(!c, Is.EqualTo(2), "one single call + one single retry")

[<Test>]
let ``Retry(1):tryWith,bind with tryWith and delay binding, should call twice (one retry). Exception from both.``() =

  let builder, failer, c = setup <| RetryPolicies.Retry(1)

  Assert.Throws<TestException>(fun () ->
    builder {
      try
        do! failer
        return ()
      with e ->
        return raise <| TestException("This exception is returned 'plainly' and should not be counted") }
    |> Async.RunSynchronously) |> ignore

  Assert.That(!c, Is.EqualTo(2), "one single call + one single retry")

[<Test>]
let ``Retry(1):tryWith,bind with failure in both 'try' and 'with' expressions; should call twice for each (retry once for each).``() =
  
  let builder, failer, c = setup <| RetryPolicies.Retry(1)

  builder {
    try
      do! failer
      return ()
    with e ->
      try do! failer with _ -> // this line retries once
        return () }
  |> Async.RunSynchronously

  Assert.That(!c, Is.EqualTo(4), "one single call + one single retry")

[<Test>]
let ``Retry(2):returnFrom should be called thrice (two retries)``() =
  let builder, failer, c = setupInt <| RetryPolicies.Retry(2)
  Assert.Throws<TestException>(fun _ -> builder { return! failer } |> Async.RunSynchronously |> printfn "%i") |> ignore
  Assert.That(!c, Is.EqualTo(3), "because even return!-type expressions should be retried (they can be used interchangeably with do!)")

// Transient Faults:

[<Test>]
let ``each consecutive number in expback should be at least twice previous``() =
  let numbers = [for i in 1.0 .. 14.0 do yield expBack i 15.0]
  let zipped = Seq.zip (Seq.take (numbers.Length - 1) numbers) (List.tail numbers)
  zipped |> Seq.iter (fun (prev, next) -> Assert.That(prev * 2.0, Is.LessThanOrEqualTo(next)))

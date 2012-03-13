module FaultPoliciesTests

open System
open MassTransit.Async.AsyncRetry
open MassTransit.Async.FaultPolicies

open NUnit.Framework

// Retry Framework:

exception TestException of string

[<Test>]
let ``NoRetry() policy should never retry, so only one exception was caught``() =
  let subject = RetryPolicies.NoRetry()
  let builder = AsyncRetryBuilder(subject)
  let called = ref 0
  Assert.Throws<TestException>(fun () ->
    builder { try return raise <| TestException("MUD SLUNG")
              with e -> called := !called + 1 ; return raise e }
    |> Async.RunSynchronously) |> ignore
  Assert.That(!called, Is.EqualTo(1), "only called once")

[<Test>]
let ``Retry single time should receive two exceptions`` () =
  let subject = RetryPolicies.Retry(1)
  let builder = AsyncRetryBuilder(subject)
  let called = ref 0
  Assert.Throws<TestException>(fun () ->
    builder { try return raise <| TestException("MUDDY WATERS")
              with e -> called := !called + 1 ; return raise e }
    |> Async.RunSynchronously) |> ignore
  Assert.That(!called, Is.EqualTo(2), "one single retry")
// Transient Faults:

[<Test>]
let ``each consecutive number in expback should be at least twice previous``() =
  let numbers = [for i in 1.0 .. 14.0 do yield expBack i 15.0]
  let zipped = Seq.zip (Seq.take (numbers.Length - 1) numbers) (List.tail numbers)
  zipped |> Seq.iter (fun (prev, next) -> Assert.That(prev * 2.0, Is.LessThanOrEqualTo(next)))
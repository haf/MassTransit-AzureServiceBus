module TransientFaults

open System
open MassTransit.Async.AsyncRetry
open MassTransit.Async.FaultPolicies

open NUnit.Framework

exception TestException of string

let failedAsync c =
  async {
    c := !c + 1
    return raise <| TestException("MUDDY WATERS") }

let setup policyUnderTest =
  let counter = ref 0
  let builder = AsyncRetryBuilder(policyUnderTest)
  builder, failedAsync counter, counter

// Transient Faults:
[<Test>]
let ``each consecutive number in expback should be at least twice previous``() =
  let numbers = [for i in 1.0 .. 14.0 do yield expBack i 15.0]
  let zipped = Seq.zip (Seq.take (numbers.Length - 1) numbers) (List.tail numbers)
  zipped |> Seq.iter (fun (prev, next) -> Assert.That(prev * 2.0, Is.LessThanOrEqualTo(next)))
  
let runWithComposedPolicy composed =
  let builder, failer, counter = setup composed

  Assert.Throws<TestException>(fun _ ->
    builder {
      do! failer
      () }
    |> Async.RunSynchronously) |> ignore

  Assert.That(!counter, Is.EqualTo(2))

[<Test>]
let ``composing two policies will retry a single policy says so (retry last)``() =
  let first = RetryPolicies.NoRetry()
  let second = RetryPolicies.Retry(1)
  runWithComposedPolicy <| compose first second

[<Test>]
let ``composing two policies will retry a single policy says so (retry first)``() =
  let first = RetryPolicies.Retry(1)
  let second = RetryPolicies.NoRetry()
  runWithComposedPolicy <| compose first second

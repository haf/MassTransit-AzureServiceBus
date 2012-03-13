module FaultPoliciesTests

open System
open MassTransit.Async.Retry
open MassTransit.Async.FaultPolicies

open NUnit.Framework

[<Test>]
let ``each consecutive number in expback should be at least twice previous``() =
  let numbers = [for i in 1.0 .. 14.0 do yield expBack i 15.0]
  let zipped = Seq.zip (Seq.take (numbers.Length - 1) numbers) (List.tail numbers)
  zipped |> Seq.iter (fun (prev, next) -> Assert.That(prev * 2.0, Is.LessThanOrEqualTo(next)))


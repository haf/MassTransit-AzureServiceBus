(* 
 Copyright 2012 Henrik Feldt
  
 Licensed under the Apache License, Version 2.0 (the "License"); you may not use
 this file except in compliance with the License. You may obtain a copy of the 
 License at 
 
     http://www.apache.org/licenses/LICENSE-2.0 
 
 Unless required by applicable law or agreed to in writing, software distributed
 under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
 CONDITIONS OF ANY KIND, either express or implied. See the License for the 
 specific language governing permissions and limitations under the License.
*)
namespace MassTransit.Async

open System.Runtime.CompilerServices
open System.Runtime.InteropServices

open System
open System.Threading

[<AutoOpen>]
module AsyncRetry =
  
  let logger = MassTransit.Logging.Logger.Get("MassTransit.Async.AsyncRetry")

  /// Invoked with the thrown exception; should perform any asynchronous side effect desired, e.g.
  /// doing an Asynchronous Sleep.
  type RetryEffect = exn -> Async<unit>

  /// Contains logic for deciding whether something should be retried.
  type ShouldRetry = ShouldRetry of (RetryCount * LastException -> bool * RetryEffect)
  and RetryCount = int
  and LastException = exn
  
  /// Description of what this policy does - used in exception messages and
  /// for providing better debugging support with chained policies.
  type PolicyDescription = string

  type RetryPolicy = RetryPolicy of ShouldRetry * PolicyDescription

  type RetryResult<'T> = 
    | RetrySuccess of 'T
    | RetryFailure of exn
    
  type Retry<'T> = Retry of (RetryPolicy -> RetryResult<'T>)

  /// Different pre-defined retry policies
  type RetryPolicies() =
  
    static member NoRetry () : RetryPolicy =
      RetryPolicy( ShouldRetry (fun (retryCount, _) -> (false, fun _ -> async{ () } )),
        "This policy never retries" )
    
    static member Retry (retryCount : int , delayFunction : RetryEffect) : RetryPolicy =
      RetryPolicy( ShouldRetry (fun (currentRetryCount, _) -> (currentRetryCount < retryCount, delayFunction)),
        sprintf "This policy retries %i times" retryCount)
    
    static member Retry (currentRetryCount : int) : RetryPolicy =
      RetryPolicies.Retry(currentRetryCount, fun _ -> async{ () } )
  
  /// Compose p1 and p2 together, such that if the first policy says to retry
  /// then use that policy to retry, otherwise call the second policy's decision method
  /// and let that decide on whether to continue retrying.
  let compose p1 p2 =
    // http://fssnip.net/7h
    match p1 with
    RetryPolicy(ShouldRetry(fn), description1) ->
      match p2 with
      RetryPolicy(ShouldRetry(fn'), description2) ->
        RetryPolicy(ShouldRetry( (fun (c,e) ->
          let (cont, effect) = fn(c,e)
          if cont then cont, effect
          else
            let (cont', effect') = fn'(c,e)
            cont', effect') ),
          sprintf "Composed policy of { '%s', '%s' }" description1 description2)

  // async work, continuation, retries left, maybe exception
  let bind w f policy =
    let rec bind' retryCount =
      async {
        match policy with
        RetryPolicy(ShouldRetry shouldRetry, description) ->
          try
            let! v = w
            return! f v
          with e ->
            match shouldRetry(retryCount, e) with
            | (true, retryEffect) -> // make the retryDelay into an Async<unit> rather than TimeSpan and migrate all usages of retry to this monad
                logger.Info(sprintf "AsyncRetry.bind caught %A, using %s; retrying" e description)
                do! retryEffect e
                let retryCount' = retryCount + 1
                return! bind' retryCount'
            | (false, _) ->
                return raise e }
    bind' 0

  let ret w = async { return w }

  let retFrom w policy =
    bind w (fun x -> async { return x }) policy

  let delay f = async { return! f() }

  let zero () = async { () }

  let tryWith f handler =
    async {
      try return! f // Went like this: f |> delay |> (fun f -> tryWith f handler)
      with x -> return! handler x }

  let using resource work = 
    async {
      use r = resource
      return! work r }

  type AsyncRetryBuilder(policy) =
    member x.Bind(work, f : ('a -> Async<'T>)) = bind work f policy
    member x.Return(a) = ret a
    member x.ReturnFrom(a) = retFrom a policy
    member x.Delay(f) = delay f
    member x.Zero() = zero ()
    member x.TryWith(work, handler) = tryWith work handler
    member x.Using<'T, 'U when 'T :> IDisposable>(resource : 'T, work : ('T -> Async<'U>)) = 
      using resource work

    /// retry timeouts 9 times with a ts delay if fAcc returns true and the generic parameter matches the exception
  let exnRetryLong<'ex when 'ex :> exn> fAcc ts =
    RetryPolicy( ShouldRetry(fun (count, ex) -> count < 9 && (match box ex with | :? 'ex -> fAcc(ex :?> 'ex) | _ -> false), ts),
      sprintf "This policy catches %s exceptions 9 times if the passed f-n accepts the exception" <| typeof<'ex>.Name )

  /// retry timeouts 9 times with a ts delay if the generic parameter matches the exception
  let exnRetry<'ex when 'ex :> exn> = exnRetryLong<'ex> (fun _ -> true)

  let exnRetryCust<'ex when 'ex :> exn> f =
    RetryPolicy ( ShouldRetry( fun (c, e) -> (match box e with | :? 'ex -> f(c,e) | _ -> false, fun _ -> async { return() })),
      sprintf "This policy catches %s exceptions and continues if the passed f-n returns true"  <| typeof<'ex>.Name )
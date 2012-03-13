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

  type ShouldRetry = ShouldRetry of (RetryCount * LastException -> bool * RetryDelay)
  and RetryCount = int
  and LastException = exn
  and RetryDelay = TimeSpan
  and PolicyDescription = string

  type RetryPolicy = RetryPolicy of ShouldRetry * PolicyDescription

  type RetryResult<'T> = 
    | RetrySuccess of 'T
    | RetryFailure of exn
    
  type Retry<'T> = Retry of (RetryPolicy -> RetryResult<'T>)

  type RetryPolicies() =
  
    static member NoRetry () : RetryPolicy =
      RetryPolicy( ShouldRetry (fun (retryCount, _) -> (retryCount < 1, TimeSpan.Zero)), 
        "This policy never retries" )
    
    static member Retry (retryCount : int , intervalBewteenRetries : RetryDelay) : RetryPolicy =
      RetryPolicy( ShouldRetry (fun (currentRetryCount, _) -> (currentRetryCount < retryCount, intervalBewteenRetries)), 
        sprintf "This policy retries %i times" retryCount)
    
    static member Retry (currentRetryCount : int) : RetryPolicy =
      RetryPolicies.Retry(currentRetryCount, TimeSpan.Zero)

  // async work, continuation, retries left, maybe exception
  let bind w f policy : Async<'T> =
    let rec bind' retryCount =
      async {
        let (RetryPolicy(ShouldRetry shouldRetry, description)) = policy
        try
          let! v = w
          return f v
        with e ->
          logger.Info(sprintf "AsyncRetry.bind caught %A, using %s" e description)
          let retryCount' = retryCount + 1
          match shouldRetry(retryCount, e) with
          | (true, retryDelay) -> // make the retryDelay into an Async<unit> rather than TimeSpan and migrate all usages of retry to this monad
              do! Async.Sleep <| retryDelay.Milliseconds
              return! bind' retryCount'
          | (false, _) -> 
              return raise <| e }
    bind' 0
  let ret a = async { return a }
  let delay f = async { return! f() }
  let tryWith f handler =
    async {
      try return! f
      with x -> return! handler x }
  let using resource work = 
    async {
      use r = resource
      return! work r }

  type AsyncRetryBuilder(policy) =
    member x.Bind(work, f : ('a -> Async<'T>)) = bind work f policy
    member x.Return(a) = ret a
    member x.ReturnFrom(a) = a
    member x.Delay(f) = delay f
    member x.Zero() = ()
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
    RetryPolicy ( ShouldRetry( fun (c, e) -> (match box e with | :? 'ex -> f(c,e) | _ -> false, TimeSpan.Zero)),
      sprintf "This policy catches %s exceptions and continues if the passed f-n returns true"  <| typeof<'ex>.Name )
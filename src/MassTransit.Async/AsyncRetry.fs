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

/// Composition of policies for retrying
[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AsyncRetry =
  
  open System
  open System.Threading
  open MassTransit.Async.Retry

  let logger = MassTransit.Logging.Logger.Get("MassTransit.Async.AsyncRetry")

  // async work, continuation, retries left, maybe exception
  let rec bind w f n : Async<'T> =
    match n with
    | 0 -> async { let! v = w in return! f v }
    | _ ->
      async {
        try let! v = w in return! f v
        with ex ->
          logger.Warn("retry failed", ex)
          return! bind w f (n-1) }

  let ret a = async { return a }

  let delay f = async { return! f() }
 
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
    member x.Using<'T, 'U when 'T :> IDisposable>(resource : 'T, work : ('T -> Async<'U>)) = 
      using resource work

  let asyncRetry = AsyncRetryBuilder(1) // (finalPolicy)

//type RetryBuilder(max) = 
//  member x.Return a = a               // Enable 'return'
//  member x.Delay f  = f                // Gets wrapped body and returns it (as it is)
//                                       // so that the body is passed to 'Run'
//  member x.Zero  = failwith "Zero"    // Support if .. then 
//  member x.Run f =                    // Gets function created by 'Delay'
//    let rec loop = function
//      | 0, Some(ex) -> raise ex
//      | n, _        -> try f() with ex -> loop (n-1, Some(ex))
//    loop(max, None)
//
//let retry = RetryBuilder(4)


//      try 
//        let! v = work
//        return! f v
//      with e ->
//        return! bind work f }
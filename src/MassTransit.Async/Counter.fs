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

open System.Diagnostics

type Message = Start | Stop | Report of AsyncReplyChannel<Reply> | Received | Sent
and  Reply = Counters of int * int * Option<Stopwatch>

module Counter =

  let counter () = MailboxProcessor<Message>.Start(fun inbox ->
      let rec loop received sent stopwatch =
        async {
          let! msg = inbox.Receive()
          match msg with 
          | Start ->
            do! loop 0 0 (Some(Stopwatch.StartNew()))
          | Stop ->
            stopwatch.Value.Stop()
            // Async.CancelDefaultToken ()
            printfn "received: %i, sent: %i, in %s" received sent (stopwatch.Value.Elapsed.ToString())
            return ()
          | Report(chan) -> 
            chan.Reply(Counters(received, sent, stopwatch))
            do! loop received sent stopwatch
          | Received -> 
            do! loop (received+1) sent stopwatch
          | Sent     -> 
            do! loop received (sent+1) stopwatch
          do! loop received sent stopwatch }
      loop 0 0 None)
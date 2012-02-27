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
module Counter

open System.Diagnostics

type CounterMessage = 
  Start
  | Stop
  | Report of AsyncReplyChannel<Reply>
  | Received of int
  | Sent of int
and  Reply = Counters of int * int * Option<Stopwatch>

type MultiCounterMessage =
  Event of string * int
  | BucketReport of string * AsyncReplyChannel<Option<Reply>>

let counter () = MailboxProcessor<CounterMessage>.Start(fun inbox ->
    let rec loop received sent stopwatch =
      async {
        let! msg = inbox.Receive()
        match msg with
        | Start ->
          do! loop 0 0 (Some(Stopwatch.StartNew()))
        | Stop ->
          stopwatch.Value.Stop()
          return ()
        | Report(chan) -> 
          chan.Reply(Counters(received, sent, stopwatch))
          do! loop received sent stopwatch
        | Received(num) -> 
          do! loop (received+num) sent stopwatch
        | Sent(num) -> 
          do! loop received (sent+num) stopwatch
        do! loop received sent stopwatch }
    loop 0 0 None)

let multiCounter () = MailboxProcessor<MultiCounterMessage>.Start(fun inbox ->
  let rec loop (buckets : Map<string, MailboxProcessor<CounterMessage>>)=
    async {
      let! msg = inbox.Receive()
      match msg with
      | Event(target, value) ->
        match buckets.TryFind target with
        | Some(t) -> 
          t.Post(Sent(1))
          do! loop buckets
        | None -> 
          let t = counter ()
          t.Post(Sent(1))
          do! loop <| buckets.Add(target, t)
      | BucketReport(target, replyChan) ->
        match buckets.TryFind target with
        | Some(t) ->
          let! r = t.PostAndAsyncReply(fun chan -> Report(chan))
          replyChan.Reply(Some(r))
        | None ->
          replyChan.Reply(None) }
  loop Map.empty)
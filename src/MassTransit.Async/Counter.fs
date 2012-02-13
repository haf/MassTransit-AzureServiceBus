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
          | Received -> do! loop (received+1) sent stopwatch
          | Sent     -> do! loop received (sent+1) stopwatch
          do! loop received sent stopwatch }
      loop 0 0 None)
namespace MassTransit.Async

module FaultPolicies =

  open System
  open MassTransit.Async.AsyncRetry

  open System.ServiceModel
  open System.Net.Sockets
  open Microsoft.ServiceBus
  open Microsoft.ServiceBus.Messaging

  /// Exponential back off function.
  /// [for i in 1.0 .. 14.0 do yield expBack i]
  /// val it : float list =
  ///   [2.14358881; 4.594972986; 9.849732676; 21.11377675; 45.25925557; 97.01723378;
  ///    207.9650567; 445.7915685; 955.5938177; 2048.400215; 4390.927778;
  ///    9412.343651; 20176.19453; 20176.19453]
  let expBack x cut =
    let fx x' = 1.1**(8.0*x')
    match x with 
    | _ when x <= cut -> fx x
    | _ -> fx cut

  /// Overloaded things should be backed off of: http://www.wolframalpha.com/input/?i=f%28x%29+%3D+1.1^%288x%29+from+1+to+10
  /// this should also be in the service bus innards, not client lib; implement a push-back mechanism.
  let serverOverloaded =
    seq {
      yield exnRetryCust<TimeoutException>
      // busy? That's good! Ain't paying ya to do nothin'.
      yield exnRetryCust<ServerBusyException>
      // But are you TOO busy? Hey, let me get you a cup of tea!
      yield exnRetryCust<ServerTooBusyException> }
    |> Seq.map(fun f -> f (fun (count, ex) -> count < 10, TimeSpan.FromMilliseconds(expBack (float count) 13.0)))

  /// this should be in the service bus innards, not in a client library; except perhaps if we can't send messages anymore
  /// and then it should possibly throw HeartBeatMissingException or something similar
  let badNetwork =
    seq { 
      yield exnRetry<CommunicationException>          // communication exception?
      yield exnRetry<MessagingCommunicationException> // then we need another one as well!
      yield exnRetryLong<SocketException> (fun ex -> ex.SocketErrorCode = SocketError.TimedOut)
      yield exnRetry<ProtocolException> }
    |> Seq.map (fun pBuild -> pBuild <| TimeSpan.FromMilliseconds(5.0))

  /// these should not ever be seen by a consumer of the client library and should count towards downtime of ASB.
  let badAzure = 
    seq {
      // This exception may occur when a listener and a consumer are being
      // initialized out of sync (e.g. consumer is reaching to a listener that
      // is still in the process of setting up the Service Host).
      yield exnRetry<ServerErrorException>
      yield exnRetry<EndpointNotFoundException>
      yield exnRetryLong<UnauthorizedAccessException> (fun e -> e.Message.Contains("Error:Code:500:SubCode:T9002")) }
    |> Seq.map (fun pBuild -> pBuild <| TimeSpan.FromMilliseconds(1.0))

  /// The composition of azure, network and overloaded errors
  let transients = 
    seq {
      yield! serverOverloaded
      yield! badNetwork
      yield! badAzure }

  /// Compose p1 and p2 together, such that if the first policy says to retry
  /// then use that policy to retry, otherwise call the second policy's decision method
  /// and let that decide on whether to continue retrying.
  let compose p1 p2 =
    // http://fssnip.net/7h
    let (RetryPolicy(ShouldRetry(fn), description1))  = p1
    let (RetryPolicy(ShouldRetry(fn'), description2)) = p2
    RetryPolicy(ShouldRetry( (fun (c,e) ->
      let (cont, delay) = fn(c,e)
      if cont then cont, delay
      else
        let (cont', delay') = fn'(c,e)
        cont', delay') ),
      sprintf "Composed policy of { '%s', '%s' }" description1 description2)

  let finalPolicy = Seq.fold compose (RetryPolicies.NoRetry()) transients

  let asyncRetry = AsyncRetryBuilder(finalPolicy)// (1) // (finalPolicy)

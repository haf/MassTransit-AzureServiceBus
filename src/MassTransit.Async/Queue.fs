namespace MassTransit.Async

module Queue =
  
  open System
  open Microsoft.ServiceBus
  open Microsoft.ServiceBus.Messaging
  
  let desc (nm : NamespaceManager) name = 
    async {
      let! exists = Async.FromBeginEnd(name, nm.BeginQueueExists, nm.EndQueueExists)
      let beginCreate = nm.BeginCreateQueue : string * AsyncCallback * obj -> IAsyncResult
      return! if exists then Async.FromBeginEnd(name, nm.BeginGetQueue, nm.EndGetQueue)
              else Async.FromBeginEnd(name, beginCreate, nm.EndCreateQueue) }
  
  let recv (client : MessageReceiver) timeout =
    let bRecv = client.BeginReceive : TimeSpan * AsyncCallback * obj -> IAsyncResult
    async {
      return! Async.FromBeginEnd(client.BeginReceive, client.EndReceive) }
  
  let send (client : MessageSender) message =
    async {
      use bm = new BrokeredMessage(message)
      do! Async.FromBeginEnd(bm, client.BeginSend, client.EndSend) : Async<unit> }
  
  let newReceiver (mf : MessagingFactory) (desc : QueueDescription) =
    async {
      return! Async.FromBeginEnd((desc.Path),
                      (fun (p, ar, state) -> mf.BeginCreateMessageReceiver(p, ar, state)),
                      mf.EndCreateMessageReceiver) }
  
  let exists (nm : NamespaceManager ) (desc : QueueDescription) = 
    async { return! Async.FromBeginEnd((desc.Path), nm.BeginQueueExists, nm.EndQueueExists) }

  let rec create (nm : NamespaceManager) (desc : QueueDescription) =
    async {
      let! exists = desc |> exists nm
      if exists then return ()
      try
        let beginCreate = nm.BeginCreateQueue : QueueDescription * AsyncCallback * obj -> IAsyncResult
        let! ndesc = Async.FromBeginEnd(desc, beginCreate, nm.EndCreateQueue)
        return! desc |> create nm
      with
      | :? MessagingEntityAlreadyExistsException -> return () }

  let rec delete (nm : NamespaceManager) (desc : QueueDescription) =
    async {
      let! exists = desc |> exists nm
      if exists then return ()
      try
        do! Async.FromBeginEnd((desc.Path), nm.BeginDeleteQueue, nm.EndDeleteQueue)
        return! desc |> delete nm
      with
      | :? MessagingEntityNotFoundException -> return () }

  let newSender (mf : MessagingFactory) nm (desc : QueueDescription) =
    async {
      do! desc |> create nm
      printfn "starting sender"
      return! Async.FromBeginEnd((desc.Path),
                      mf.BeginCreateMessageSender,
                      mf.EndCreateMessageSender) }
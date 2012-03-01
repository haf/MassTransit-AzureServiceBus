## About the src folder

The source folder contains these folders:

 * **MassTransit.Async** - F# async implementation of a Receiver and some basic tests with the receiver as a FSX file. This project is referenced from the other projects and only depends on **MassTransit.AzureServiceBus**
 * **MassTransit.AzureServiceBus** contains interfaces that in many respects mirror what the Azure Service Bus team chose to keep 'internal' in their code. Also, these interfaces are used to work around bugs in the framework, such as MessageReceiver not being able to consume both queues and topics, in spite of its docs. The interfaces tend to go on the smaller side of size and have code annotations to specify nullity.
 * **MassTransit.Async.Tests** - currently not so many tests, but mostly a project that one can add packages to without being afraid that they will be linked into the main projects.
 * **MassTransit.Transports.AzureServiceBus** - the actual transport implementation. Implements many of the interfaces in **MessTransit.AzureServiceBus**.
 * **PerformanceTesting** - contains an Azure project that you can use to check the performance of the project and sending of messages with the Azure Service Bus.
 * **packages** - nuget packages.
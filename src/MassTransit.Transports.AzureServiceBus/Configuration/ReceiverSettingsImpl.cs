using System;
using MassTransit.AzureServiceBus;

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
    internal class ReceiverSettingsImpl : ReceiverSettings
    {
        public ReceiverSettingsImpl()
        {
            Concurrency = 1u;
            BufferSize = 5u;
            NThAsync = 5u;
            ReceiveTimeout = TimeSpan.FromMilliseconds(50.0);
            ReceiverName = NameHelper.GenerateRandomName();
        }

        public uint Concurrency { get; set; }
        public uint BufferSize { get; set; }
        public uint NThAsync { get; set; }
        public TimeSpan ReceiveTimeout { get; set; }
        public string ReceiverName { get; set; }
    }
}
// Copyright 2011 Ernst Naezer, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

using System;
using MassTransit.Exceptions;
using Magnum.Extensions;
using Magnum.Threading;

namespace MassTransit.Transports.AzureQueue
{
	public class ServiceBusQueueTransportFactory
        : ITransportFactory
    {
        private readonly ReaderWriterLockedDictionary<Uri, ConnectionHandler<ServiceBusQueueConnection>> _connectionCache;
        private bool _disposed;

        public ServiceBusQueueTransportFactory()
        {
            _connectionCache = new ReaderWriterLockedDictionary<Uri, ConnectionHandler<ServiceBusQueueConnection>>();
        }

        /// <summary>
        ///   Gets the scheme.
        /// </summary>
        public string Scheme
        {
            get { return "stomp"; }
        }

        /// <summary>
        ///   Builds the loopback.
        /// </summary>
        /// <param name = "settings">The settings.</param>
        /// <returns></returns>
        public IDuplexTransport BuildLoopback(ITransportSettings settings)
        {
            return new Transport(settings.Address, () => BuildInbound(settings), () => BuildOutbound(settings));
        }

        public IInboundTransport BuildInbound(ITransportSettings settings)
        {
            EnsureProtocolIsCorrect(settings.Address.Uri);

            var client = GetConnection(settings.Address);
            return new InboundServiceBusQueueTransport(settings.Address, client);
        }

        public IOutboundTransport BuildOutbound(ITransportSettings settings)
        {
            EnsureProtocolIsCorrect(settings.Address.Uri);

            var client = GetConnection(settings.Address);
            return new OutboundServiceBusQueueTransport(settings.Address, client);
        }

        public IOutboundTransport BuildError(ITransportSettings settings)
        {
            EnsureProtocolIsCorrect(settings.Address.Uri);

            var client = GetConnection(settings.Address);
            return new OutboundServiceBusQueueTransport(settings.Address, client);
        }

        /// <summary>
        ///   Ensures the protocol is correct.
        /// </summary>
        /// <param name = "address">The address.</param>
        private void EnsureProtocolIsCorrect(Uri address)
        {
            if (address.Scheme != Scheme)
                throw new EndpointException(address,
                                            string.Format("Address must start with 'stomp' not '{0}'", address.Scheme));
        }

        ConnectionHandler<ServiceBusQueueConnection> GetConnection(IEndpointAddress address)
        {
            return _connectionCache.Retrieve(address.Uri, () =>
            {
                var connection = new ServiceBusQueueConnection(address.Uri);
                var connectionHandler = new ConnectionHandlerImpl<ServiceBusQueueConnection>(connection);

                return connectionHandler;
            });
        }

        void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _connectionCache.Values.Each(x => x.Dispose());
                _connectionCache.Clear();

                _connectionCache.Dispose();
            }

            _disposed = true;
        }

        ~ServiceBusQueueTransportFactory()
		{
			Dispose(false);
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
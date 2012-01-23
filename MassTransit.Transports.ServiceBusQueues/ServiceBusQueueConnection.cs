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
using log4net;
using Magnum.Extensions;

namespace MassTransit.Transports.AzureQueue
{
	public class ServiceBusQueueConnection :
        Connection
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (ServiceBusQueueConnection));
        private readonly Uri _address;
        private StompClient _stompClient;
		
        public ServiceBusQueueConnection(Uri address)
        {
            _address = address;
        }

		public AzureQueue StompClient
        {
            get { return _stompClient; }
        }

        public void Dispose()
        {
            Disconnect();
        }

        public void Connect()
        {
            Disconnect();

            var serverAddress = new UriBuilder("ws", _address.Host, _address.Port).Uri;

            if (Log.IsInfoEnabled)
                Log.Warn("Connecting {0}".FormatWith(_address));

            _stompClient = new StompClient(cacheMessages: true);
            _stompClient.Connect(serverAddress);
        }

        public void Disconnect()
        {
            try
            {
                if (_stompClient != null)
                {
                    if (Log.IsInfoEnabled)
                        Log.Warn("Disconnecting {0}".FormatWith(_address));

                    if (_stompClient.IsConnected)
                        _stompClient.Disconnect();

                    _stompClient.Dispose();
                    _stompClient = null;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to close STOMP connection.", ex);
            }
        }
    }
}
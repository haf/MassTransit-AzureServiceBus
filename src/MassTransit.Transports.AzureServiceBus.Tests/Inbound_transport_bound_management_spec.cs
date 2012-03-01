// Copyright 2012 Henrik Feldt
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
// ReSharper disable InconsistentNaming

using Magnum.Extensions;
using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;
using Moq;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	[Scenario]
	public class Inbound_transport_bound_management_spec
	{
		Mock<ConnectionHandler<ConnectionImpl>> handler;
		Mock<AzureManagement> management;
		IInboundTransport subject;

		[Given]
		public void an_inbound_transport_with_purge_set()
		{
			management = new Mock<AzureManagement>();
			handler = new Mock<ConnectionHandler<ConnectionImpl>>();
			// mock up the actual work that connection handler does
			handler.Setup(x => x.AddBinding(management.Object))
				.Callback(() => management.Object.Bind(null));
			
			subject = new InboundTransportImpl(
				TestDataFactory.GetAddress(),
				handler.Object,
				management.Object);

			// when
			receive_is_called();
		}

		public void receive_is_called()
		{
			handler.Verify(
				x => x.AddBinding(It.IsAny<ConnectionBinding<ConnectionImpl>>()),
				Times.Never(),
				"hasn't received yet");
			
			subject.Receive(ctx => c => { }, 1.Seconds());
		}

		[Then]
		public void should_have_called_add_binding_at_last_some_time()
		{
			handler.Verify(x => x.AddBinding(It.IsAny<ConnectionBinding<ConnectionImpl>>()), Times.AtLeastOnce(),
				"the connection handler was never bound to any management");
		}

		[Then]
		public void should_have_called_add_binding_with_PerConnectionReceiver_once()
		{
			handler.Verify(x => x.AddBinding(It.IsAny<PerConnectionReceiver>()), Times.Once());
		}

		[Then]
		public void handler_binding_should_have_bound()
		{
			management.Verify(x => x.Bind(It.IsAny<ConnectionImpl>()));
		}
	}
}
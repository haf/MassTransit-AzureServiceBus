// Copyright 2011 Henrik Feldt
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
using MassTransit.Subscriptions.Coordinator;
using MassTransit.Subscriptions.Messages;
using MassTransit.Util;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class TopicBinder
		: SubscriptionObserver
	{
		readonly IServiceBus _serviceBus;

		public TopicBinder([NotNull] IServiceBus serviceBus)
		{
			if (serviceBus == null) throw new ArgumentNullException("serviceBus");
			_serviceBus = serviceBus;
		}

		public void OnSubscriptionAdded(SubscriptionAdded message)
		{

		}

		public void OnSubscriptionRemoved(SubscriptionRemoved message)
		{
			throw new NotImplementedException();
		}

		public void OnComplete()
		{
			throw new NotImplementedException();
		}
	}
}
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

using System;
using MassTransit.Transports.AzureServiceBus.Configuration;
using MassTransit.Transports.AzureServiceBus.Tests.Assumptions;

namespace MassTransit.Transports.AzureServiceBus.Tests.Framework
{
	public static class TestDataFactory
	{
		public static Uri ApplicationEndpoint = new Uri(String.Format("azure-sb://owner:{0}@{1}/my-application",
				AccountDetails.Key, AccountDetails.Namespace));

		public static Uri ContractEndpoint = new Uri(String.Format("azure-sb://owner:{0}@{1}/contract_test",
				AccountDetails.Key, AccountDetails.Namespace));

		public static AzureServiceBusEndpointAddress GetAddress()
		{
			return AzureServiceBusEndpointAddressImpl.Parse(ApplicationEndpoint);
		}

		public static A AMessage()
		{
			return new A("Ditten datten", new byte[] { 2, 4, 6, 7, Byte.MaxValue });
		}
	}
}
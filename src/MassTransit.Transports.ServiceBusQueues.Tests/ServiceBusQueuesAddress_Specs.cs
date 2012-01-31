﻿// Copyright 2011 Henrik Feldt
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
using System.Collections.Generic;
using System.Linq;
using Magnum.TestFramework;
using MassTransit.Configurators;
using MassTransit.Transports.ServiceBusQueues.Configuration;
using NUnit.Framework;

namespace MassTransit.Transports.ServiceBusQueues.Tests
{
	[Scenario]
	public class ServiceBusQueuesAddress_Specs
	{
		ServiceBusQueuesAddressImpl _endpoint;
		Uri uri;

		[When]
		public void a_servicebusqueues_address_is_given()
		{
			uri = new Uri(string.Format("sb-queues://owner:{0}@{1}/my-application",
				AccountDetails.Key, AccountDetails.Namespace));
			_endpoint = ServiceBusQueuesAddressImpl.Parse(uri);
		}

		[Then]
		public void messaging_factory_should_not_be_null()
		{
			_endpoint.MessagingFactory.ShouldNotBeNull();
		}

		[Then]
		public void token_provider_should_not_be_null()
		{
			_endpoint.TokenProvider.ShouldNotBeNull();
		}

		[Then]
		public void namespace_manager_should_not_be_null()
		{
			_endpoint.NamespaceManager.ShouldNotBeNull();
		}

		[Finally]
		public void dispose_it()
		{
			_endpoint.Dispose();
		}
	}

	[Scenario]
	public class When_giving_full_hostname_spec
	{
		ServiceBusQueuesAddressImpl _endpointExtended;
		Uri _extended;
		Uri _normal;
		ServiceBusQueuesAddressImpl _endpoint;

		[When]
		public void a_servicebusqueues_address_is_given()
		{
			var extraHost = ".servicebus.windows.net";
			_extended = GetUri(extraHost);
			_normal = GetUri("");

			_endpointExtended = ServiceBusQueuesAddressImpl.Parse(_extended);
			_endpoint = ServiceBusQueuesAddressImpl.Parse(_normal);
		}

		Uri GetUri(string extraHost)
		{
			return new Uri(string.Format("sb-queues://owner:{0}@{1}{2}/my-application",
			                                  AccountDetails.Key, AccountDetails.Namespace, extraHost));
		}

		[Then]
		public void normal_address_equals_extended_without_queues_and_userinfo_and_app()
		{
			var ext = new Uri(_extended.ToString()
				.Replace("-queues", string.Empty)
				.Replace("my-application", string.Empty));

			new UriBuilder(ext.Scheme, ext.Host, ext.Port, ext.AbsolutePath)
				.Uri
				.ShouldEqual(_endpoint.NamespaceManager.Address);
		}

		[Then]
		public void the_two_endpoints_namespace_manager_endpoints_equal()
		{
			_endpoint.NamespaceManager.Address
				.ShouldEqual(_endpointExtended.NamespaceManager.Address);
		}
	}

	[Scenario]
	public class When_giving_faulty_address_spec
	{
		Uri _faulty_app;
		Uri _missing_creds;

		[Given]
		public void two_bad_uris()
		{
			_faulty_app = new Uri(string.Format("sb-queues://owner:{0}@{1}/my-application/but_then_another_too",
				AccountDetails.Key, AccountDetails.Namespace));
			_missing_creds = new Uri(string.Format("sb-queues://owner-pass@lalala.servicebus.windows.net/app"));
		}

		[Test]
		public void something_after_app_name()
		{
			IEnumerable<ValidationResult> results;
			ServiceBusQueuesAddressImpl address;
			ServiceBusQueuesAddressImpl.TryParse(_faulty_app, out address, out results)
				.ShouldBeFalse("parse should have failed");

			AssertGotKey("Application", address, results);
		}

		[Test]
		public void missing_credentials()
		{
			IEnumerable<ValidationResult> results;
			ServiceBusQueuesAddressImpl address;
			ServiceBusQueuesAddressImpl.TryParse(_missing_creds, out address, out results)
				.ShouldBeFalse("parse should have failed");

			AssertGotKey("UserInfo", address, results);
		}

		static void AssertGotKey(string key, ServiceBusQueuesAddressImpl address, IEnumerable<ValidationResult> results)
		{
			results.ShouldNotBeNull();
			address.ShouldBeNull();

			results.Count().ShouldEqual(1);
			results.First().Key.ShouldBeEqualTo(key);
		}
	}
}
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

using System;
using System.Collections.Generic;
using System.Linq;
using Magnum.TestFramework;
using MassTransit.Configurators;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	[Scenario]
	public class When_parsing_application_endpoint_uri
	{
		AzureServiceBusEndpointAddressImpl _address;

		[When]
		public void a_servicebusqueues_address_is_given()
		{
			_address = AzureServiceBusEndpointAddressImpl.Parse(
				TestDataFactory.ApplicationEndpoint);
		}

		[Then]
		public void messaging_factory_should_not_be_null()
		{
			_address.MessagingFactoryFactory.ShouldNotBeNull();
		}

		[Then]
		public void token_provider_should_not_be_null()
		{
			_address.TokenProvider.ShouldNotBeNull();
		}

		[Then]
		public void namespace_manager_should_not_be_null()
		{
			_address.NamespaceManager.ShouldNotBeNull();
		}

		[Then]
		public void details_should_have_correct_app_name()
		{
			_address.Details.QueueOrTopicName
				.ShouldEqual("my-application");
		}

		[Then]
		public void details_should_have_correct_namespace()
		{
			_address.Details.Namespace
				.ShouldBeEqualTo(AccountDetails.Namespace);
		}

		[Then]
		public void details_should_have_correct_shared_secret()
		{
			_address.Details.PasswordSharedSecret
				.ShouldEqual(AccountDetails.Key);
		}

		[Then]
		public void details_should_have_correct_issuer()
		{
			_address.Details.UsernameIssuer
				.ShouldEqual(AccountDetails.IssuerName);
		}

		[Then]
		public void rebuilt_uri_should_be_correct()
		{
			var uriWithoutCreds = new Uri(string.Format("azure-sb://{0}/{1}", 
				AccountDetails.Namespace, "my-application"));

			_address.Uri
				.ShouldEqual(uriWithoutCreds);
		}

		[Finally]
		public void dispose_it()
		{
			_address.Dispose();
		}
	}

	[Scenario]
	public class When_giving_full_hostname_spec
	{
		AzureServiceBusEndpointAddressImpl _addressExtended;
		Uri _extended;
		Uri _normal;
		AzureServiceBusEndpointAddressImpl _address;

		[When]
		public void a_servicebusqueues_address_is_given()
		{
			var extraHost = ".servicebus.windows.net";
			_extended = GetUri(extraHost);
			_normal = GetUri("");

			_addressExtended = AzureServiceBusEndpointAddressImpl.Parse(_extended);
			_address = AzureServiceBusEndpointAddressImpl.Parse(_normal);
		}

		Uri GetUri(string extraHost)
		{
			return new Uri(string.Format("azure-sb://owner:{0}@{1}{2}/my-application",
			                                  AccountDetails.Key, AccountDetails.Namespace, extraHost));
		}

		[Then]
		public void the_two_endpoints_namespace_manager_endpoints_equal()
		{
			_address.NamespaceManager.Address
				.ShouldEqual(_addressExtended.NamespaceManager.Address);
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
			_faulty_app = new Uri(string.Format("azure-sb://owner:{0}@{1}/my-application/but_then_another_too",
				AccountDetails.Key, AccountDetails.Namespace));
			_missing_creds = new Uri(string.Format("azure-sb://owner-pass@lalala.servicebus.windows.net/app"));
		}

		[Test]
		public void something_after_app_name()
		{
			IEnumerable<ValidationResult> results;
			AzureServiceBusEndpointAddressImpl address;
			AzureServiceBusEndpointAddressImpl.TryParse(_faulty_app, out address, out results)
				.ShouldBeFalse("parse should have failed");

			AssertGotKey("Application", address, results);
		}

		[Test]
		public void missing_credentials()
		{
			IEnumerable<ValidationResult> results;
			AzureServiceBusEndpointAddressImpl address;
			AzureServiceBusEndpointAddressImpl.TryParse(_missing_creds, out address, out results)
				.ShouldBeFalse("parse should have failed");

			AssertGotKey("UserInfo", address, results);
		}

		static void AssertGotKey(string key, AzureServiceBusEndpointAddressImpl address, IEnumerable<ValidationResult> results)
		{
			results.ShouldNotBeNull();
			address.ShouldBeNull();

			results.Count().ShouldEqual(1);
			results.First().Key.ShouldBeEqualTo(key);
		}
	}

	[Scenario]
	public class When_creating_topic_address
	{
		// subjects
		AzureServiceBusEndpointAddress _queueAddress;
		AzureServiceBusEndpointAddress _topicAddress;
		
		// assertion data
		string _topicName;

		class A
		{
		}

		[Given]
		public void a_normal_address_and_its_topic_corresponding_address()
		{
			_queueAddress = AzureServiceBusEndpointAddressImpl.Parse(
				TestDataFactory.ApplicationEndpoint);

			var formatter = new AzureMessageNameFormatter();
			_topicName = formatter.GetMessageName(typeof (A)).ToString();

			_topicAddress = _queueAddress.ForTopic(_topicName);

			Assert.Throws<ArgumentNullException>(
				() => _queueAddress.ForTopic(null));
		}

		[Then]
		public void topic_address_has_same_password()
		{
			_queueAddress.Uri.UserInfo
				.ShouldEqual(_topicAddress.Uri.UserInfo);
		}

		[Then]
		public void topic_address_has_same_host()
		{
			_queueAddress.Uri.Host
				.ShouldEqual(_topicAddress.Uri.Host);
		}

		[Then]
		public void queue_address_has_queue_description()
		{
			_queueAddress.QueueDescription.ShouldNotBeNull();
		}

		[Then]
		public void queue_address_hasnt_got_topic_description()
		{
			_queueAddress.TopicDescription.ShouldBeNull();
		}

		[Then]
		public void topic_address_hasnt_got_queue_description()
		{
			_topicAddress.QueueDescription.ShouldBeNull();
		}

		[Then]
		public void topic_address_got_topic_description()
		{
			_topicAddress.TopicDescription.ShouldNotBeNull();
		}

		[Then]
		public void topic_address_contains_topic_name()
		{
			_topicAddress.TopicDescription.Path
				.ShouldContain(_topicName);
		}

		[Then]
		public void topic_address_uri_tells_its_topic()
		{
			_topicAddress.Uri.PathAndQuery
				.ShouldContain("topic=true");
		}
	}
}
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
using Magnum.TestFramework;
using MassTransit.Transports.AzureServiceBus.Configuration;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	public class Pre_shared_key_credentials_transformations_spec
	{
		// adding your own? Add one of them to the factory
		static PreSharedKeyCredentials[] Implementations = {
			new AccountDetails(),
			new Credentials("owner", "key", "ns", "app1"),
		};

		[Test, TestCaseSource("Implementations")]
		public void can_build_uri_different_app(PreSharedKeyCredentials impl)
		{
			var first = impl.BuildUri("app2");
			var second = FormatUri("app2");
			first.PathAndQuery.ShouldEqual(second.PathAndQuery);
		}

		[Test, TestCaseSource("Implementations")]
		public void can_create_new_configuration_instance_with_app2(
			PreSharedKeyCredentials impl)
		{
			var credentials = impl.WithApplication("app3");
			credentials.Application.ShouldEqual("app3");
			credentials.IssuerName.ShouldEqual(impl.IssuerName);
			credentials.Key.ShouldEqual(impl.Key);
			credentials.Namespace.ShouldEqual(impl.Namespace);
		}


		static Uri FormatUri(string app)
		{
			return new Uri(Constants.Scheme + string.Format("://{0}:{1}@{2}/{3}", "owner", "key", "ns", app));
		}
	}
}
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

namespace MassTransit.Transports.AzureServiceBus.Tests.Framework
{
	// WARNING: note: DO NOT RENAME this file, or you'll commit the account details

	/// <summary>
	/// 	Actual AccountDetails
	/// </summary>
	public class AccountDetails
		: PreSharedKeyCredentials
	{
		static readonly PreSharedKeyCredentials _instance = new Credentials(IssuerName, Key, Namespace, Application);

		private static string ENV(string name)
		{
			return Environment.GetEnvironmentVariable(name);
		}

		public static string Key
		{
			get { return ENV("AccountDetailsKey") ?? ""; }
		}

		public static string IssuerName
		{
			get { return ENV("AccountDetailsIssuerName") ?? "owner"; }
		}

		public static string Namespace
		{
			get { return ENV("AccountDetailsNamespace") ?? "mt"; }
		}

		public static string Application
		{
			get { return ENV("Application") ?? "my-application"; }
		}

		string PreSharedKeyCredentials.IssuerName
		{
			get { return _instance.IssuerName; }
		}

		string PreSharedKeyCredentials.Key
		{
			get { return _instance.Key; }
		}

		string PreSharedKeyCredentials.Namespace
		{
			get { return _instance.Namespace; }
		}

		string PreSharedKeyCredentials.Application
		{
			get { return Application; }
		}

		public Uri BuildUri(string application = null)
		{
			return _instance.BuildUri(application);
		}

		public PreSharedKeyCredentials WithApplication(string application)
		{
			return _instance.WithApplication(application);
		}
	}
}
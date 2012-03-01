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
using MassTransit.Transports.AzureServiceBus.Util;

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	/// <summary>
	/// Implementors of this interface should know how to create a uri based on the credentials supplied.
	/// </summary>
	public interface PreSharedKeyCredentials
	{
		string IssuerName { get; }
		string Key { get; }
		string Namespace { get; }
		string Application { get; }

		/// <summary>
		/// Builds a URI with the passed application (or by default the Application property).
		/// </summary>
		/// <param name="application">If you wish to have another application</param>
		/// <returns>The corresponding uri</returns>
		[NotNull]
		Uri BuildUri(string application = null);

		/// <summary>
		/// Create a new credential item based on the passed application
		/// </summary>
		/// <param name="application"></param>
		/// <returns></returns>
		PreSharedKeyCredentials WithApplication([NotNull] string application);
	}
}
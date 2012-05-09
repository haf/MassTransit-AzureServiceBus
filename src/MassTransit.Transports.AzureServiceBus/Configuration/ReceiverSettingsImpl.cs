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
using System.Text;
using MassTransit.Util;

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	/// <summary>
	///   Name generator
	/// </summary>
	[UsedImplicitly] // from F#
	public static class NameHelper
	{
		static readonly Random _r = new Random();

		/// <summary>
		///   Generate a new random name, 20 characters long, with characters a-z, lowercase.
		/// </summary>
		/// <returns> </returns>
		public static string GenerateRandomName()
		{
			var characters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
			var sb = new StringBuilder();
			for (var i = 0; i < 20; i++)
				sb.Append(characters[_r.Next(0, characters.Length - 1)]);
			return sb.ToString();
		}
	}

	class ReceiverSettingsImpl : ReceiverSettings
	{
		public ReceiverSettingsImpl()
		{
			Concurrency = 1u;
			BufferSize = 5u;
			NThAsync = 5u;
			ReceiveTimeout = TimeSpan.FromMilliseconds(50.0);
			ReceiverName = NameHelper.GenerateRandomName();
		}

		public uint Concurrency { get; set; }
		public uint BufferSize { get; set; }
		public uint NThAsync { get; set; }
		public TimeSpan ReceiveTimeout { get; set; }
		public string ReceiverName { get; set; }
	}
}
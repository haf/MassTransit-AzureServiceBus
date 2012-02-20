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
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using NUnit.Framework;

namespace MassTransit.Transports.AzureServiceBus.Tests
{
	public class Scratch
	{
		[Test, Explicit]
		public void continuation_error_handling_then_completion()
		{
			ManualResetEventSlim first = new ManualResetEventSlim(false),
			                     second = new ManualResetEventSlim(false),
			                     third = new ManualResetEventSlim(false);

			Exception ex = null;

			// each ContinueWith creates a new task
			var t = Task.Factory.StartNew(() =>
				{
					first.Set();
					throw new ApplicationException();
					//return -5;
				})
				.ContinueWith(tStart =>
					{
						second.Set();
						ex = tStart.Exception.InnerExceptions.First();

					}, TaskContinuationOptions.NotOnRanToCompletion)
				.ContinueWith(tStart => third.Set(), TaskContinuationOptions.OnlyOnRanToCompletion);

			t.Wait();

			Assert.IsTrue(first.IsSet);
			Assert.IsTrue(second.IsSet);
			Assert.IsTrue(third.IsSet);

			Assert.That(ex, Is.InstanceOf<ApplicationException>());
		}
	}
}
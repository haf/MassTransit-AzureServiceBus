using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Magnum;
using MassTransit.AzurePerformance.Messages;
using MassTransit.Transports.AzureServiceBus.Configuration;
using Microsoft.WindowsAzure.ServiceRuntime;
using log4net.Config;

namespace MassTransit.AzurePerformance.Sender
{
	public class SenderWorker : RoleEntryPoint
	{
		volatile bool _isStopping;

		public override void Run()
		{
			BasicConfigurator.Configure();
			Trace.WriteLine("Sender entry point called", "Information");
			RoleEnvironment.Stopping += (sender, args) => _isStopping = true;

			using (var sb = ServiceBusFactory.New(sbc =>
				{
					sbc.ReceiveFromComponents(
						AccountDetails.IssuerName,
						AccountDetails.Key,
						AccountDetails.Namespace,
						"sender"
						);

					sbc.UseAzureServiceBusRouting();

					sbc.Subscribe(s => s.Handler<ZoomDone>(zd =>
						{
							_isStopping = true;
						}));
				}))
			{
				var receiver = sb.GetEndpoint(new Uri(string.Format("azure-sb://{0}:{1}@{2}/receiver", 
						AccountDetails.IssuerName,
						AccountDetails.Key,
						AccountDetails.Namespace)));

				while (!_isStopping)
				{
					var msg = new ZoomImpl { Id = CombGuid.Generate() };
					receiver.Send<ZoomZoom>(msg);
				}

				Trace.WriteLine("sent nuff zooms for a day, idling again!");
				while (true) Thread.Sleep(10000);
			}
		}

		[Serializable]
		public class ZoomImpl : ZoomZoom, IEquatable<ZoomImpl>
		{
			public Guid Id { get; set; }

			public decimal Amount
			{
				get { return 1024m; }
			}

			public string Payload
			{
				get { return TestData.PayloadMessage; }
			}

			public bool Equals(ZoomImpl other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return other.Id.Equals(Id);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != typeof (ZoomImpl)) return false;
				return Equals((ZoomImpl) obj);
			}

			public override int GetHashCode()
			{
				return Id.GetHashCode();
			}
		}

		public override bool OnStart()
		{
			// Set the maximum number of concurrent connections 
			ServicePointManager.DefaultConnectionLimit = 12;

			// For information on handling configuration changes
			// see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
			ConfigureDiagnostics();

			return base.OnStart();
		}

		void ConfigureDiagnostics()
		{
		}
	}
}

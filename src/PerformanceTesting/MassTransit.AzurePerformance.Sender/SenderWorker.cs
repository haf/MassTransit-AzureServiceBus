using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Magnum;
using Magnum.Extensions;
using MassTransit.AzurePerformance.Messages;
using MassTransit.Transports.AzureServiceBus.Configuration;
using MassTransit.Transports.AzureServiceBus.Tests.Framework;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;
using LogLevel = Microsoft.WindowsAzure.Diagnostics.LogLevel;
using MassTransit.NLogIntegration;

namespace MassTransit.AzurePerformance.Sender
{
	public class SenderWorker : RoleEntryPoint
	{
		static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		volatile bool _isStopping;

		public override void Run()
		{
			_logger.Info("Sender entry point called");

			RoleEnvironment.Stopping += (sender, args) => _isStopping = true;

			var creds = GetCredentials();
			var startSignal = new ManualResetEventSlim(false);
			using (var sb = ServiceBusFactory.New(sbc =>
				{
					sbc.ReceiveFromComponents(creds);

					sbc.UseAzureServiceBusRouting();
					sbc.UseNLog();
					//sbc.UseGraphite();
					sbc.Subscribe(s =>
						{
							s.Handler<ZoomDone>(zd => { _isStopping = true; });
							s.Handler<ReadySetGo>(go => startSignal.Set());
						});
				}))
			{
				var receiver = sb.GetEndpoint(creds.BuildUri("receiver"));

				receiver.Send<SenderUp>(new { Source = creds.BuildUri() });

				startSignal.Wait();

				var count = 0;
				var watch = Stopwatch.StartNew();
				// it sends about 3 x stopcount in the time receiver has to get them
				while (!_isStopping && count != 1500)
				{
					var msg = new ZoomImpl { Id = CombGuid.Generate() };
					receiver.Send<ZoomZoom>(msg);
					count++;
				}
				watch.Stop();

				_logger.Info("sent nuff zooms {0}, in {1} seconds for a day. Idling again!",
					count, 
					watch.ElapsedMilliseconds / 1000.0);

				while (true) Thread.Sleep(5000);
			}
		}

		static PreSharedKeyCredentials GetCredentials()
		{
			var id = RoleEnvironment.CurrentRoleInstance.Id;
			var appName = "sender" + id.Substring(id.IndexOf("IN_", StringComparison.InvariantCulture) + 3);
			var creds = new AccountDetails().WithApplication(appName);
			return creds;
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
				get { return TestData.SmallPayloadMessage; }
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
			ServicePointManager.DefaultConnectionLimit = 12;
			
			ConfigureDiagnostics();
			
			return base.OnStart();
		}

		void ConfigureDiagnostics()
		{
			var everySecond = 3.Seconds();
			var dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();
			dmc.Logs.ScheduledTransferPeriod = everySecond;
			dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;
			DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", dmc);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Magnum.Extensions;
using MassTransit.AzurePerformance.Messages;
using MassTransit.Pipeline.Inspectors;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using MassTransit.Transports.AzureServiceBus.Configuration;
using NLog;
using LogLevel = Microsoft.WindowsAzure.Diagnostics.LogLevel;
using MassTransit.NLogIntegration;

namespace MassTransit.AzurePerformance.Receiver
{
	public class ReceiverWorker : RoleEntryPoint
	{
		static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		
		volatile bool _isStopping;

		public override void Run()
		{
			//BasicConfigurator.Configure(AzureAppender.New(conf =>
			//    {
			//        conf.Level = "Info";
					
			//        conf.ConfigureRepository((repo, mapper) =>
			//            {
			//                repo.Threshold = mapper("Info"); // root

			//                ((Logger) ((Hierarchy) repo).GetLogger("MassTransit.Messages")).Level = mapper("Warn");
			//            });

			//        conf.ConfigureAzureDiagnostics(d =>
			//            {
			//                d.Logs.ScheduledTransferLogLevelFilter = LogLevel.Information;
			//            });
			//    }));
			
			// This is a sample worker implementation. Replace with your logic.
			_logger.Info("starting receiver");

			RoleEnvironment.Stopping += (sender, args) => _isStopping = true;

			ConfigureDiagnostics();

			var stopping = new AutoResetEvent(false);

			long rampUp = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("RampUpCount"));
			long sampleSize = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("SampleSize"));

			long failures = 0;
			long received = 0;
			var watch = new Stopwatch();
			var datapoints = new LinkedList<DataPoint>();
			var senders = new LinkedList<IEndpoint>();
			var allSendersUp = new CountdownEvent(
				Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("WaitForNumberOfSenders")));
				
			using (var sb = ServiceBusFactory.New(sbc =>
				{
					sbc.ReceiveFromComponents(AccountDetails.IssuerName,
						AccountDetails.Key, AccountDetails.Namespace,
						"receiver");

					sbc.SetPurgeOnStartup(true);
					sbc.UseNLog();
					
					sbc.UseAzureServiceBusRouting();
				}))
			{
				UnsubscribeAction unsubscribeMe = null;
				unsubscribeMe += sb.SubscribeHandler<SenderUp>(su =>
					{
						lock (senders) 
							senders.AddLast(sb.GetEndpoint(su.Source));
						
						allSendersUp.Signal();
					});

				allSendersUp.Wait();

				senders.Each(sender => sender.Send<ReadySetGo>(new {}));

				//unsubscribeMe = sb.SubscribeHandler<IConsumeContext<ZoomZoom>>(consumeContext =>
				unsubscribeMe += sb.SubscribeHandler<ZoomZoom>(payment =>
					{
						//var payment = consumeContext.Message;
						
						long currentReceived;
						if ((currentReceived = Interlocked.Increment(ref received)) == rampUp)
							watch.Start();
						else if (currentReceived < rampUp) return;

						var localFailures = new long?();
						if (Math.Abs(payment.Amount - 1024m) > 0.0001m)
						{
							localFailures = Interlocked.Increment(ref failures);
						}

						if (currentReceived + rampUp == sampleSize || _isStopping)
						{
							unsubscribeMe();
							watch.Stop();
							stopping.Set();
						}

						if (currentReceived % 100 == 0)
						{
							var point = new DataPoint
								{
									Received = currentReceived,
									Ticks = watch.ElapsedTicks,
									Failures = localFailures ?? failures,
									SampleMessage = payment,
									Instance = DateTime.UtcNow
									/* assume all prev 100 msgs same size */
									//Size = consumeContext.BaseContext.BodyStream.Length * 100 
								};
							lock (datapoints) datapoints.AddLast(point);
							_logger.Debug(string.Format("Logging {0}", point));
						}
					});

				PipelineViewer.Trace(sb.InboundPipeline);

				stopping.WaitOne();

				sb.GetEndpoint(new Uri(string.Format("azure-sb://owner:{0}@{1}/sender", AccountDetails.Key, AccountDetails.Namespace)))
					.Send<ZoomDone>(new{});
			}

			_logger.Info(
string.Format(@"
Performance Test Done
=====================

Total messages received:  {0}
Time taken:               {1}

of which:
  Corrupt messages count: {2}
  Valid messages count:   {3}

metrics:
  Message per second:     {4}
  Total bytes transferred:{5}
  All samples' data equal:{6}

data points:
{7}
",
 sampleSize, watch.Elapsed, failures,
 sampleSize-failures,
 1000d * sampleSize / (double)watch.ElapsedMilliseconds,
 datapoints.Sum(dp => dp.Size),
 datapoints.Select(x => x.SampleMessage).All(x => x.Payload.Equals(TestData.PayloadMessage, StringComparison.InvariantCulture)),
 datapoints.Aggregate("", (str, dp) => str + dp.ToString() + Environment.NewLine)));

			_logger.Info("Idling... aka. softar.");
			
			while (true)
				Thread.Sleep(10000);

			// now have a look in Server Explorer, WADLogsTable, w/ Filter similar to "Timestamp gt datetime'2012-02-03T10:06:50Z'" 
			// (if this isn't your first deployment, or no filter if you feel like that)
		}

		class DataPoint
		{
			public long Received { get; set; }
			public long Failures { get; set; }
			public long Ticks { get; set; }
			/// <summary>Size since last data point</summary>
			public long Size { get; set; }

			public ZoomZoom SampleMessage { get; set; }

			public DateTime Instance { get; set; }

			public override string ToString() {
				return "{0} Rec:{1}, Fail:{2},  Ticks:{3}}}".FormatWith(Instance, Received, Failures, Ticks);
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
			var everySecond = 1.Seconds();
			var dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();
			dmc.Logs.ScheduledTransferPeriod = everySecond;
			dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;
			DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", dmc);
		}
	}
}

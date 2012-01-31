using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace MassTransit.AzureWorker
{
	public abstract class CqrsEngineRole : RoleEntryPoint
	{
		/// <summary>
		/// 	Implement in the inheriting class to configure the bus host.
		/// </summary>
		/// <returns> </returns>
		protected abstract CqrsEngineHost BuildHost();

		private CqrsEngineHost _host;
		private readonly CancellationTokenSource _source = new CancellationTokenSource();

		private Task _task;

		public override bool OnStart()
		{
			// this is actually azure initialization;
			_host = BuildHost();
			return base.OnStart();
		}

		public override void Run()
		{
			_task = _host.Start(_source.Token);
			_source.Token.WaitHandle.WaitOne();
		}

		public string InstanceName
		{
			get
			{
				return string.Format("{0}/{1}",
				                     RoleEnvironment.CurrentRoleInstance.Role.Name,
				                     RoleEnvironment.CurrentRoleInstance.Id);
			}
		}

		public override void OnStop()
		{
			_source.Cancel(true);

			_task.Wait(TimeSpan.FromSeconds(10));
			_host.Dispose();

			base.OnStop();
		}
	}
}
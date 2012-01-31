using System;

namespace MassTransit.AzureWorker.Events
{
	[Serializable]
	public sealed class EngineStopped : ISystemEvent
	{
		public TimeSpan Elapsed { get; private set; }

		public EngineStopped(TimeSpan elapsed)
		{
			Elapsed = elapsed;
		}

		public override string ToString()
		{
			return string.Format("Engine Stopped after {0} mins", Math.Round(Elapsed.TotalMinutes, 2));
		}
	}
}
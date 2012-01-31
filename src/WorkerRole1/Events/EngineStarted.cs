using System;

namespace MassTransit.AzureWorker.Events
{
	[Serializable]
	public sealed class EngineStarted : ISystemEvent
	{
		public readonly string[] EngineProcesses;

		public EngineStarted(string[] engineProcesses)
		{
			EngineProcesses = engineProcesses;
		}

		public override string ToString()
		{
			return string.Format("Engine started: {0}", string.Join(",", EngineProcesses));
		}
	}
}
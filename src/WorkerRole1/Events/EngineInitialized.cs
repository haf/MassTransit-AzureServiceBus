using System;

namespace MassTransit.AzureWorker.Events
{
	[Serializable]
	public sealed class EngineInitialized : ISystemEvent
	{
		public override string ToString()
		{
			return "Engine initialized";
		}
	}
}
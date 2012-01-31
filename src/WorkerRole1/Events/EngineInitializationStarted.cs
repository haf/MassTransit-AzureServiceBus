using System;

namespace MassTransit.AzureWorker.Events
{
	[Serializable]
	public sealed class EngineInitializationStarted : ISystemEvent
	{
		public override string ToString()
		{
			return "Engine initializing";
		}
	}
}
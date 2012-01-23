using System;

namespace MassTransitQueues.CqrsEngine.Events
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
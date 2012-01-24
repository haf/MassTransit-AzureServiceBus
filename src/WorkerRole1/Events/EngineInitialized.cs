using System;

namespace MassTransitQueues.CqrsEngine.Events
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
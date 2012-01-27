using System;
using System.Text;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	public class Utils
	{
		static Random r = new Random();

		public static string GenerateRandomName()
		{
			var characters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
			var sb = new StringBuilder();
			for (int i = 0; i < 20; i++)
				sb.Append(characters[r.Next(0, characters.Length - 1)]);
			return sb.ToString();
		}
	}
}
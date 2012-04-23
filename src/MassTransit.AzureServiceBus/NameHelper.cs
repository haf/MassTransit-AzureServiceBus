using System;
using System.Text;
using MassTransit.Util;

namespace MassTransit.AzureServiceBus
{
	/// <summary>
	/// Name generator
	/// </summary>
	[UsedImplicitly] // from F#
	public static class NameHelper
	{
		static readonly Random _r = new Random();

		/// <summary>
		/// Generate a new random name, 20 characters long, with characters a-z, lowercase.
		/// </summary>
		/// <returns></returns>
		public static string GenerateRandomName()
		{
			var characters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
			var sb = new StringBuilder();
			for (int i = 0; i < 20; i++)
				sb.Append(characters[_r.Next(0, characters.Length - 1)]);
			return sb.ToString();
		}
	}
}
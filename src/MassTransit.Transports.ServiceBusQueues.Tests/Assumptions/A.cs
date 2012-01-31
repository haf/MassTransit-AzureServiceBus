using System;
using System.Linq;
using MassTransit.Transports.ServiceBusQueues.Util;

namespace MassTransit.Transports.ServiceBusQueues.Tests.Assumptions
{
	[Serializable]
	public class A : IEquatable<A>
	{
		public A([NotNull] string messageContents, [NotNull] byte[] someBytes)
		{
			if (messageContents == null) throw new ArgumentNullException("messageContents");
			if (someBytes == null) throw new ArgumentNullException("someBytes");
			Contents = messageContents;
			SomeBytes = someBytes;
		}

		public string Contents { get; protected set; }
		public byte[] SomeBytes { get; protected set; }

		public bool Equals(A other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.Contents, Contents) && other.SomeBytes.SequenceEqual(SomeBytes);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (A)) return false;
			return Equals((A) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Contents.GetHashCode()*397) ^ SomeBytes.GetHashCode();
			}
		}

		public static bool operator ==(A left, A right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(A left, A right)
		{
			return !Equals(left, right);
		}
	}
}
using System;
using System.Runtime.Serialization;

namespace Terumi.Workspace
{
	[Serializable]
	public class DependencyResolveException : Exception
	{
		public DependencyResolveException()
		{
		}

		public DependencyResolveException(string message) : base(message)
		{
		}

		public DependencyResolveException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected DependencyResolveException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
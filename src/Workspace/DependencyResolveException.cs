using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

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

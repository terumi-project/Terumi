using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Terumi.Lexer;
using Terumi.Parser;

namespace Terumi.Workspace
{

	[Serializable]
	public class WorkspaceParserException : Exception
	{
		public WorkspaceParserException()
		{
		}

		public WorkspaceParserException(string message) : base(message)
		{
		}

		public WorkspaceParserException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected WorkspaceParserException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}

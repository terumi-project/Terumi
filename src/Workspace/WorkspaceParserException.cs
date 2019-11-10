using System;

namespace Terumi.Workspace
{
	[Serializable]
	public class WorkspaceParserException : Exception
	{
		public WorkspaceParserException(string message) : base(message)
		{
		}
	}
}
using System;

namespace Terumi.Lexer
{
	[Serializable]
	internal class LexingException : Exception
	{
		public LexingException(string message) : base(message)
		{
		}
	}
}
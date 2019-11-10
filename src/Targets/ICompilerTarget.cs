using System.CodeDom.Compiler;

using Terumi.Binder;

namespace Terumi.Targets
{
	public interface ICompilerTarget
	{
		void Write(IndentedTextWriter writer, IBind bind);

		void Post(IndentedTextWriter writer);
	}
}
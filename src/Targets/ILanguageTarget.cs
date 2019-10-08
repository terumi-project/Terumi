using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terumi.Targets
{
	public interface ILanguageTarget
	{
		void Write(Stream destination, Ast.CompilationUnit compilationUnit);
	}
}

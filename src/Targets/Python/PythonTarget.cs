using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terumi.Ast;

namespace Terumi.Targets.Python
{
	public class PythonTarget : ILanguageTarget
	{
		public void Write(Stream destination, CompilationUnit compilationUnit)
		{
			foreach(var item in compilationUnit.Nodes)
			{
				switch(item)
				{
					case Class @class:
					{
						PythonClass.Write(destination, @class.CompilationTypeName, @class.Members.OfType<Field>().Select(x => x.Name).ToArray());
					}
					break;

					// contracts aren't necessary in python
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Ast.CompilationTypes
{
	public class Array : ICompilationType
	{
		private readonly ICompilationType _base;

		public Array(ICompilationType @base)
		{
			_base = @base;
		}

		public string CompilationTypeName => _base.CompilationTypeName + "[]";
	}
}

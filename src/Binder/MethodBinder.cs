using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Parser;

namespace Terumi.Binder
{

	public class MethodBinder
	{
		private readonly TerumiBinder _parent;
		private readonly Class _context;
		private readonly Method _method;
		private readonly SourceFile _file;

		public MethodBinder(TerumiBinder parent, Class? context, Method method, SourceFile file)
		{
			_parent = parent;
			_context = context;
			_method = method;
			_file = file;
		}

		public CodeBody Finalize()
		{
			// TODO: oh boy this is gonna be fun
			return null;
		}
	}
}

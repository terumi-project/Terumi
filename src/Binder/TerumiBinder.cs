using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Parser;

namespace Terumi.Binder
{
	public struct TerumiBinderProject
	{
		public List<SourceFile> ProjectFiles;
		public List<BoundFile> DirectDependencies;
		public List<BoundFile> IndirectDependencies;
	}

	public struct TerumiBinderBindings
	{
		public List<BoundFile> BoundProjectFiles;
	}

	public class TerumiBinder
	{
		private readonly TerumiBinderProject _project;

		public TerumiBinder(TerumiBinderProject project)
		{
			_project = project;
		}

		public TerumiBinderBindings BindProject()
		{
			return default;
		}
	}
}

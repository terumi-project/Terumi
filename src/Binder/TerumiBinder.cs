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

	public static class TerumiBinderHelpers
	{
		public static TerumiBinderBindings Bind(this TerumiBinderProject project)
		{
			var binder = new TerumiBinder(project);

			binder.DiscoverTypes();
			binder.DiscoverFields();
			binder.DiscoverMethodHeaders();
			binder.DiscoverMethodBodies();

			return binder.Bindings;
		}
	}

	public class TerumiBinder
	{
		private readonly TerumiBinderProject _project;

		public TerumiBinder(TerumiBinderProject project)
		{
			_project = project;
		}

		public TerumiBinderBindings Bindings { get; }

		// only discover the existence of types, eg classes or contracts
		public void DiscoverTypes()
		{
		}

		// discover field names and their types
		public void DiscoverFields()
		{
		}

		// discover method headers (ctors too) - their return types, names, and parameters
		public void DiscoverMethodHeaders()
		{
		}

		// now read into the code of the method body - we have full type information at this point
		public void DiscoverMethodBodies()
		{
		}
	}
}

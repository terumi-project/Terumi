using System;
using System.Collections.Generic;
using System.Linq;
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

			return binder.Finalize();
		}
	}

	public class TerumiBinder
	{
		internal readonly TerumiBinderProject _project;
		internal readonly List<(Class, SourceFile)> _wipClasses = new List<(Class, SourceFile)>();
		internal readonly List<(Method, SourceFile)> _wipMethods = new List<(Method, SourceFile)>();

		public TerumiBinder(TerumiBinderProject project)
		{
			_project = project;
		}

		public TerumiBinderBindings Finalize()
		{
			var sourceToClasses = new Dictionary<SourceFile, List<Class>>();
			var sourceToMethods = new Dictionary<SourceFile, List<Method>>();

			foreach (var (@class, file) in _wipClasses)
			{
				if (sourceToClasses.TryGetValue(file, out var classes)) classes.Add(@class);
				else sourceToClasses[file] = new List<Class> { @class };
			}

			foreach (var (method, file) in _wipMethods)
			{
				if (sourceToMethods.TryGetValue(file, out var methods)) methods.Add(method);
				else sourceToMethods[file] = new List<Method> { method };
			}

			var allFiles = sourceToClasses
				.Select(x => x.Key)
				.Concat(sourceToMethods.Select(x => x.Key))
				.Distinct();

			var binds = new List<BoundFile>();

			foreach (var file in allFiles)
			{
				if (!sourceToClasses.TryGetValue(file, out var classes)) classes = EmptyList<Class>.Instance;
				if (!sourceToMethods.TryGetValue(file, out var methods)) methods = EmptyList<Method>.Instance;

				var bound = new BoundFile(file.PackageLevel, file.Usings, methods, classes);
				binds.Add(bound);
			}

			return new TerumiBinderBindings
			{
				BoundProjectFiles = binds
			};
		}

		// only discover the existence of types, eg classes or contracts
		public void DiscoverTypes()
		{
			foreach (var file in _project.ProjectFiles)
			{
				foreach (var @class in file.Classes)
				{
					_wipClasses.Add((new Class(@class, @class.Name), file));
				}
			}

			// TODO: verify that no two names are similar between direct dependencies and 
		}

		// discover field names and their types
		public void DiscoverFields()
		{
			foreach (var (@class, file) in _wipClasses)
			{
				foreach (var field in @class.FromParser.Fields)
				{
					@class.Fields.Add(new Field(@class, FindImmediateType(field.Type, file), field.Name));
				}
			}
		}

		// discover method headers (ctors too) - their return types, names, and parameters
		public void DiscoverMethodHeaders()
		{
			foreach (var (@class, file) in _wipClasses)
			{
				foreach (var method in @class.FromParser.Methods)
				{
					@class.Methods.Add(BindMethod(method, file));
				}
			}

			foreach (var file in _project.ProjectFiles)
			{
				foreach (var method in file.Methods)
				{
					_wipMethods.Add((BindMethod(method, file), file));
				}
			}

			Method BindMethod(Parser.Method parserMethod, SourceFile file)
			{
				var method = new Method(parserMethod, FindImmediateType(parserMethod.Type, file), parserMethod.Name);

				foreach (var parameter in parserMethod.Parameters)
				{
					method.Parameters.Add(new MethodParameter(FindImmediateType(parameter.Type, file), parameter.Name));
				}

				return method;
			}
		}

		// now read into the code of the method body - we have full type information at this point
		public void DiscoverMethodBodies()
		{
			foreach (var (@class, file) in _wipClasses)
			{
				foreach (var method in @class.Methods)
				{
					System.Diagnostics.Debug.Assert(method is Method, "A method in a class is not method - some major architectural change?");
					var userMethod = method as Method;

					var binder = new MethodBinder(this, @class, userMethod, file);
					var code = binder.Finalize();
					userMethod.Body = code;
				}
			}

			foreach (var (method, file) in _wipMethods)
			{
				// TODO: don't do code duplication
				var userMethod = method;

				var binder = new MethodBinder(this, null, userMethod, file);
				var code = binder.Finalize();
				userMethod.Body = code;
			}
		}

		internal IType FindImmediateType(string? name, SourceFile source)
		{
			// at this point 'name' is guarenteed to not be null
			if (BuiltinType.TryUse(name, out var type)) return type;

			// TODO: determine if similar elements exist, and if so, prohibit them from being named as such

			// first, search in the wip stuff for a class named similarly
			foreach (var (@class, file) in _wipClasses)
			{
				if (!source.CanUseFile(file)) continue;

				if (@class.Name == name)
				{
					return @class;
				}
			}

			// next, search in the direct dependencies for a similar type
			foreach (var dependency in _project.DirectDependencies)
			{
				if (!source.CanUseFile(dependency)) continue;

				foreach (var @class in dependency.Classes)
				{
					if (@class.Name == name)
					{
						return @class;
					}
				}
			}

			// we don't look in the indirect dependencies because we're searching for immediate types
			// to only be used within the project
			throw new InvalidOperationException($"Cannot find immediate type {name}");
		}
	}

	public static class Helpers
	{
		// these two are so we only use things in the namespaces we've included
		public static bool CanUseFile(this SourceFile source, SourceFile wantToUse)
			=> source.Usings.Contains(wantToUse.PackageLevel);

		public static bool CanUseFile(this SourceFile source, BoundFile wantToUse)
			=> source.Usings.Contains(wantToUse.Namespace);
	}
}

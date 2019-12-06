using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Terumi.Parser;
using Terumi.Targets;

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
		public List<BoundFile> DirectDependencies;
		public List<BoundFile> IndirectDependencies;
	}

	public class TerumiBinder
	{
		internal readonly TerumiBinderProject _project;
		internal readonly List<(Class, SourceFile)> _wipClasses = new List<(Class, SourceFile)>();
		internal readonly List<(Method, SourceFile)> _wipMethods = new List<(Method, SourceFile)>();
		private readonly ICompilerTarget _target;

		public TerumiBinder(TerumiBinderProject project, ICompilerTarget target)
		{
			_project = project;
			_target = target;
		}

		public bool TryBind(out List<BoundFile> bound)
		{
			DiscoverTypes();
			DiscoverFields();
			DiscoverMethodHeaders();
			DiscoverMethodBodies();

			bound = new List<BoundFile>();

			foreach (var file in _project.ProjectFiles)
			{
				var classes = _wipClasses.Where(x => x.Item2 == file).Select(x => x.Item1).ToList();
				var methods = _wipMethods.Where(x => x.Item2 == file).Select(x => x.Item1).ToList();

				bound.Add(new BoundFile(file.FilePath, file.PackageLevel, file.Usings, methods, classes));
			}

			return true;
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

			// TODO: prevent name conflicts
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

		internal bool CanUseTypeAsType(IType supposeToBe, IType tryingToUse)
		{
			// make sure they're not builtin types
			// if they're built in types they're not replaceable
			if (BuiltinType.IsBuiltinType(supposeToBe) || BuiltinType.IsBuiltinType(tryingToUse))
			{
				// if they're builtin types, we check if they're equal by reference only
				return IType.ReferenceEquals(supposeToBe, tryingToUse);
			}

			if (IType.ReferenceEquals(supposeToBe, tryingToUse))
			{
				return true;
			}

			if (supposeToBe.Fields.Count != tryingToUse.Fields.Count)
			{
				return false;
			}

			if (supposeToBe.Methods.Count != tryingToUse.Methods.Count)
			{
				return false;
			}

			for (var i = 0; i < supposeToBe.Fields.Count; i++)
			{
				var src = supposeToBe.Fields[i];
				var target = tryingToUse.Fields[i];

				if (src.Name == target.Name
					&& CanUseTypeAsType(src.Type, target.Type))
				{
					continue;
				}

				return false;
			}

			for (var i = 0; i < supposeToBe.Methods.Count; i++)
			{
				var src = supposeToBe.Methods[i];
				var target = tryingToUse.Methods[i];

				if (src.Name == target.Name
					&& CanUseTypeAsType(src.ReturnType, target.ReturnType)
					&& src.Parameters.Count == target.Parameters.Count)
				{
					for (var p = 0; p < src.Parameters.Count; p++)
					{
						if (CanUseTypeAsType(src.Parameters[p].Type, target.Parameters[p].Type))
						{
							continue;
						}

						return false;
					}

					continue;
				}

				return false;
			}

			return true;
		}

		internal IType FindImmediateType(string? name, SourceFile source)
		{
			// after this point 'name' is guarenteed to not be null
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

		internal bool FindImmediateMethod(Class? ctx, Parser.Expression.MethodCall methodCall, List<Expression> parameters, out IMethod targetMethod)
			=> FindMethod
			(
				methodCall.IsCompilerCall,
				methodCall.Name,
				parameters,
				_wipMethods.Select(x => x.Item1)
					.Concat(_project.DirectDependencies.SelectMany(x => x.Methods))
					.Concat(ctx == null ? Array.Empty<Method>() : ctx.Methods.OfType<Method>()),
				out targetMethod
			);

		internal IMethod? TryFindConsructor(IType type, List<Expression> parameters)
		{
			Debug.Assert(type is Class);
			var @class = (Class)type;

			return TryFindMethod(@class.Methods, "ctor", parameters);
		}

		public IMethod? TryFindMethod(IEnumerable<IMethod> methods, string name, List<Expression> parameters)
		{
			foreach (var method in methods)
			{
				if (method.Name != name)
				{
					continue;
				}

				if (method.Parameters.Count != parameters.Count)
				{
					continue;
				}

				for (var i = 0; i < method.Parameters.Count; i++)
				{
					if (CanUseTypeAsType(method.Parameters[i].Type, parameters[i].Type))
					{
						continue;
					}

					goto nopeNotThisOneChief;
				}

				return method;

			nopeNotThisOneChief:
				continue;
			}

			return null;
		}

		internal bool FindMethod(bool isCompilerMethod, string name, List<Expression> arguments, IEnumerable<IMethod> methods, out IMethod targetMethod)
		{
			if (isCompilerMethod)
			{
				var compilerMethod = _target.Match(name, arguments);

				if (compilerMethod != null)
				{
					targetMethod = compilerMethod;
					return true;
				}

				targetMethod = default;
				return false;
			}

			foreach (var method in methods)
			{
				if (method.Name != name) continue;
				if (method.Parameters.Count != arguments.Count) continue;

				for (var i = 0; i < method.Parameters.Count; i++)
				{
					if (method.Parameters[i].Type != arguments[i].Type) goto fail;
				}

				targetMethod = method;
				return true;

				fail:;
			}

			targetMethod = default;
			return false;
		}
	}

	public static class Helpers
	{
		// these two are so we only use things in the namespaces we've included
		public static bool CanUseFile(this SourceFile source, SourceFile wantToUse)
			=> source.UsingsWithSelf.Contains(wantToUse.PackageLevel);

		public static bool CanUseFile(this SourceFile source, BoundFile wantToUse)
			=> source.UsingsWithSelf.Contains(wantToUse.Namespace);
	}
}

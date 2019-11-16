using System.Collections.Generic;
using System.Linq;

using Terumi.Ast;
using Terumi.Targets;

namespace Terumi.Binder
{
	public class TypeInformation
	{
		private readonly ICompilerTarget _target;

		public TypeInformation(ICompilerTarget target) => _target = target;

		public List<IBind> Binds { get; set; } = new List<IBind>();
		public MethodBind Main => Binds.OfType<MethodBind>().First(x => x.Name == "main");

		public CompilerMethod? MatchMethod(string name, IEnumerable<IType> types) => _target.MatchMethod(name, types.ToArray());

		public IEnumerable<IType> AllReferenceableTypes(IBind mainBind)
			=> AllReferenceableBinds(mainBind).OfType<IType>();

		public IEnumerable<IMethod> AllReferenceableMethods(IBind mainBind)
			=> AllReferenceableBinds(mainBind).OfType<IMethod>();

		public IEnumerable<IBind> AllReferenceableBinds(IBind mainBind)
		{
			var namespaces = new List<PackageLevel>(mainBind.References)
			{
				mainBind.Namespace
			};

			foreach (var bind in Binds)
			{
				if (!namespaces.Contains(bind.Namespace))
				{
					continue;
				}

				yield return bind;
			}

			yield return CompilerDefined.Void;
			yield return CompilerDefined.String;
			yield return CompilerDefined.Number;
			yield return CompilerDefined.Boolean;
		}

		public bool TryGetType(IBind bind, string typeName, out IType type)
		{
			foreach (var item in AllReferenceableTypes(bind))
			{
				if (item is IType tType
					&& item.Name == typeName)
				{
					type = tType;
					return true;
				}
			}

			type = default;
			return false;
		}
	}
}
using System.Collections.Generic;
using System.Linq;

namespace Terumi.Binder
{
	public class TypeInformation
	{
		public static UserType Void { get; } = new UserType
		{
			IsCompilerDefined = true,
			Name = "void"
		};

		public static UserType String { get; } = new UserType
		{
			IsCompilerDefined = true,
			Name = "string"
		};

		public static UserType Number { get; } = new UserType
		{
			IsCompilerDefined = true,
			Name = "number"
		};

		public static UserType Boolean { get; } = new UserType
		{
			IsCompilerDefined = true,
			Name = "bool"
		};

		public List<IBind> Binds { get; set; } = new List<IBind>();

		public IEnumerable<IType> AllReferenceableTypes(IBind mainBind)
			=> AllReferenceableBinds(mainBind).OfType<IType>();

		public IEnumerable<IBind> AllReferenceableBinds(IBind mainBind)
		{
			var namespaces = new List<PackageLevel>(mainBind.References);
			namespaces.Add(mainBind.Namespace);

			foreach (var method in Binds)
			{
				if (!namespaces.Contains(method.Namespace))
				{
					continue;
				}

				yield return method;
			}

			yield return Void;
			yield return String;
			yield return Number;
			yield return Boolean;
		}

		public bool TryGetType(IBind mainType, string typeName, out UserType type)
		{
			foreach (var item in AllReferenceableTypes(mainType))
			{
				if (item is UserType infoItem
					&& item.Name == typeName)
				{
					type = infoItem;
					return true;
				}
			}

			type = default;
			return false;
		}
	}
}
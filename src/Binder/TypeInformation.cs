using System.Collections.Generic;

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

		public IEnumerable<IBind> AllReferenceableTypes(IBind mainType)
		{
			var namespaces = new List<PackageLevel>(mainType.References);
			namespaces.Add(mainType.Namespace);

			foreach (var item in Binds)
			{
				if (!namespaces.Contains(item.Namespace))
				{
					continue;
				}

				/*
				TODO: figure out why this was put here
				// if the type is itself, skip it
				if (item.Equals(mainType))
				{
					continue;
				}
				*/

				yield return item;
			}

			yield return Void;
			yield return String;
			yield return Number;
			yield return Boolean;
		}

		public bool TryGetItem(IBind mainType, string typeName, out IBind type)
		{
			foreach (var item in AllReferenceableTypes(mainType))
			{
				if (item.Name == typeName)
				{
					type = item;
					return true;
				}
			}

			type = default;
			return false;
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
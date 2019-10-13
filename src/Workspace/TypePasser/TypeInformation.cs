using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Terumi.Workspace.TypePasser
{

	public class TypeInformation
	{
		public static InfoItem Void { get; } = new InfoItem
		{
			IsCompilerDefined = true,
			Name = "void"
		};

		public static InfoItem String { get; } = new InfoItem
		{
			IsCompilerDefined = true,
			Name = "string"
		};

		public static InfoItem Number{ get; } = new InfoItem
		{
			IsCompilerDefined = true,
			Name = "number"
		};

		public static InfoItem Boolean{ get; } = new InfoItem
		{
			IsCompilerDefined = true,
			Name = "bool"
		};

		public TypeInformation()
		{
			// add built in types
			InfoItems.Add(Void);
			InfoItems.Add(String);
			InfoItems.Add(Number);
			InfoItems.Add(Boolean);
		}

		public ICollection<InfoItem> InfoItems { get; set; } = new List<InfoItem>();

		public IEnumerable<InfoItem> AllReferenceableTypes(InfoItem mainType)
		{
			var namespaces = new List<ICollection<string>>(mainType.NamespaceReferences);
			namespaces.Add(mainType.Namespace);

			foreach (var item in InfoItems)
			{
				if (item.IsCompilerDefined)
				{
					yield return item;
				}

				if (!namespaces.Contains(item.Namespace, SequenceEqualsEqualityComparer<string>.Instance))
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
		}

		public bool TryGetItem(InfoItem mainType, string typeName, out InfoItem type)
		{
			foreach(var item in AllReferenceableTypes(mainType))
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
	}
}

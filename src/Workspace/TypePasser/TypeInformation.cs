using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Terumi.Workspace.TypePasser
{

	public class TypeInformation
	{
		public ICollection<InfoItem> InfoItems { get; set; } = new List<InfoItem>();

		public IEnumerable<InfoItem> AllReferenceableTypes(InfoItem mainType)
		{
			var namespaces = new List<ICollection<string>>(mainType.NamespaceReferences);
			namespaces.Add(mainType.Namespace);

			foreach (var item in InfoItems)
			{
				if (!namespaces.Contains(item.Namespace, SequenceEqualsEqualityComparer<string>.Instance))
				{
					continue;
				}

				// if the type is itself, skip it

				if (item.Equals(mainType))
				{
					continue;
				}

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

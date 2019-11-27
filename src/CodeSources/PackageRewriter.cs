using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Workspace;

namespace Terumi.CodeSources
{
	public static class PackageRewriter
	{
		public static Configuration Add(Configuration configuration, ToolSnapshot snapshot)
		{
			return new Configuration
			{
				Name = configuration.Name,
				Libraries = configuration.Libraries
					.Append(new LibraryReference
					{
						Branch = snapshot.Branch,
						CommitId = snapshot.CommitId,
						GitUrl = snapshot.GitUrl,
						ProjectPath = snapshot.Path ?? ""
					})
					.ToArray()
			};
		}
	}
}

using System.Collections.Generic;
using System.Linq;

using Terumi.Lexer;
using Terumi.Parser;

namespace Terumi.Workspace
{
	public static class EnvironmentCreator
	{
		public static IEnumerable<ParsedSourceFile> ParseAllSourceFiles(this Project mainProject, StreamLexer lexer, StreamParser parser)
		{
			foreach (var dependency in mainProject.TraverseAllDependencies().Prepend(mainProject))
			{
				foreach (var source in dependency.GetSources())
				{
					try
					{
						yield return source.Parse(lexer, parser);

						// dispose the stream of the source as to release the file handle
					}
					finally
					{
						source.Source.Dispose();
					}
				}
			}
		}

		public static IEnumerable<Project> TraverseAllDependencies(this Project project, List<string> alreadySeen = default)
		{
			var seen = alreadySeen ?? new List<string>();

			foreach (var dependency in project.GetDependencies())
			{
				if (seen.Contains(dependency.Name))
				{
					continue;
				}

				seen.Add(dependency.Name);

				yield return dependency;

				foreach (var innnerDependency in TraverseAllDependencies(dependency, seen))
				{
					yield return innnerDependency;
				}
			}
		}
	}
}
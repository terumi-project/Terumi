using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terumi.Ast;
using Terumi.Lexer;

namespace Terumi.Workspace
{
	public static class EnvironmentCreator
	{
		public static IEnumerable<KeyValuePair<PackageLevel, UsingDescriptor<CompilerUnitItem>>>
			OrderByLeastDependencies(this Environment environment)
		{
			var dictCopy = environment.Code.ToDictionary(kvp => kvp.Key, kvp => new List<UsingDescriptor<CompilerUnitItem>>(kvp.Value));
			var solved = new List<(PackageLevel, UsingDescriptor<CompilerUnitItem>)>();

			{
				var solvedLevels = new List<PackageLevel>();

				// first, iterate over all the items where something uses another package level that doesn't have anything
				foreach (var (level, descriptors) in dictCopy)
				{
					// if any `using` in the descriptor is NOT found anywhere in the dictionary
					foreach (var @using in descriptors.SelectMany(descriptor => descriptor.Usings))
					{
						if (!dictCopy.Any((kvp) => kvp.Key == @using))
						{
							// the using isn't in the dictionary, we'll claim it's solved
							solvedLevels.Add(@using);
						}
					}
				}

				foreach(var level in solvedLevels)
				{
					dictCopy[level] = new List<UsingDescriptor<CompilerUnitItem>>();
				}
			}

			// while there are items
			while (dictCopy.Any((kvp) => kvp.Value.Count > 0))
			{
				// first, look at every package level
				foreach (var (level, descriptors) in dictCopy)
				{
					var remove = new List<UsingDescriptor<CompilerUnitItem>>();

					// then look at every descriptor
					foreach (var descriptor in descriptors)
					{
						// if all the usings of this descriptor is satisfied by what we've already solved
						if (descriptor.Usings.All(HasAll))
						{
							// we can yield it off to be compiled
							yield return KeyValuePair.Create(level, descriptor);

							solved.Add((level, descriptor));
							remove.Add(descriptor);
						}
					}

					foreach(var i in remove)
					{
						descriptors.Remove(i);
					}
				}
			}

			bool HasAll(PackageLevel level)
			{
				// i can't say i've fully satisfied a package level
				// unless all the level items in that package level have been satisfied

				return dictCopy.Any((kvp) => kvp.Key.LevelEquals(level) && kvp.Value.Count == 0);
			}
		}

		public static Environment ToEnvironment(this Project mainProject, StreamLexer lexer, Tokenizer.Tokenizer tokenizer)
		{
			var environment = new Environment();

			foreach(var project in mainProject.TraverseAllDependencies().Prepend(mainProject))
			{
				foreach(var sourceFile in project.GetSources())
				{
					using (sourceFile)
					{
						var tokens = lexer.ParseTokens(sourceFile.Source);
						if (!tokenizer.TryParse(tokens, out var compilerUnit))
						{
							// TODO: exception
							System.Console.WriteLine("Couldn't compile source at '" + sourceFile.Location + "'.");
							continue;
						}

						environment.AddToEnvironment(sourceFile.PackageLevel, compilerUnit);
					}
				}
			}

			return environment;
		}

		public static void AddToEnvironment(this Environment environment, PackageLevel level, CompilerUnit compilerUnit)
		{
			var currentLevel = level;
			var usings = new List<PackageLevel>();

			foreach(var item in compilerUnit.CompilerUnitItems)
			{
				if (item is PackageLevel pckLvl)
				{
					if (pckLvl.Action == PackageAction.Namespace)
					{
						currentLevel = pckLvl;
						usings.Clear();
					}
					else if (pckLvl.Action == PackageAction.Using)
					{
						usings.Add(pckLvl);
					}
				}
				else
				{
					environment.Put(currentLevel, usings, item);
				}
			}
		}

		public static IEnumerable<Project> TraverseAllDependencies(this Project project, List<string> alreadySeen = default)
		{
			var seen = alreadySeen ?? new List<string>();

			foreach(var dependency in project.GetDependencies())
			{
				if (seen.Contains(dependency.Name))
				{
					continue;
				}

				seen.Add(dependency.Name);

				yield return dependency;

				foreach(var innnerDependency in TraverseAllDependencies(dependency, seen))
				{
					yield return innnerDependency;
				}
			}
		}
	}
}

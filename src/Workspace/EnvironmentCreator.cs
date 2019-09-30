﻿using System.Collections.Concurrent;
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
		public static Environment ToEnvironment(this Project mainProject, StreamLexer lexer, Tokenizer.Tokenizer tokenizer)
		{
			var environment = new Environment();

			foreach(var project in mainProject.TraverseAllDependencies())
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

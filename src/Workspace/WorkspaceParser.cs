﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terumi.Lexer;
using Terumi.Parser;
using Terumi.SyntaxTree;

namespace Terumi.Workspace
{
	public static class WorkspaceParser
	{
		public static IEnumerable<ParsedProjectFile> ParseProject(this Project project, StreamLexer lexer, StreamParser parser)
		{
			foreach (var code in project.GetSources())
			{
				// TODO: use string directly and rid all the code of reader head/fork stuff

				var tokens = lexer.ParseTokens(Encoding.UTF8.GetBytes(code.Source).AsMemory(), code.Path);

				if (!parser.TryParse(tokens.ToArray().AsMemory(), out var compilerUnit))
				{
					throw new WorkspaceParserException($"Unable to parse source code into compiler unit: in '{project.ProjectName}', at '{code.Path}'.");
				}

				yield return code.Analyze(compilerUnit);
			}
		}

		public static ParsedProjectFile Analyze(this ProjectFile source, CompilerUnit compilerUnit)
		{
			var mainLevel = source.PackageLevel;

			var usings = new List<PackageLevel>();
			var typeDefinitions = new List<TypeDefinition>();

			var parsedTypeDefinition = false;

			for (var i = 0; i < compilerUnit.CompilerUnitItems.Length; i++)
			{
				var item = compilerUnit.CompilerUnitItems[i];

				switch (item)
				{
					case PackageLevel packageLevel:
					{
						// ensure only using/namespace at top
						if (parsedTypeDefinition)
						{
							var error = $"A package statement other than 'using'/'namespace' has been parsed. " +
	$"You may not specify any more package statements after a different kind of statement is parsed. " +
	$"Error when using '{packageLevel.Levels.ToNamespace()}' at '{source.Path}'.";

							Log.Error(error);
							break;
						}

						// if it's a using, add it
						if (packageLevel.Action == PackageAction.Using)
						{
							usings.Add(packageLevel);
							break;
						}

						// we have a namespace declaration - make sure it's first
						if (i != 0)
						{
							var error = $"The first item in the file must specify the namespace of the file. " +
	$"The namespace of the file '{source.Path}' will remain as '{mainLevel.ToNamespace()}', and not '{packageLevel.Levels.ToNamespace()}'.";

							Log.Error(error);
							break;
						}

						// good
						mainLevel = packageLevel.Levels;
					}
					break;

					case TypeDefinition typeDefinition:
					{
						parsedTypeDefinition = true;
						typeDefinitions.Add(typeDefinition);
					}
					break;
				}
			}

			return new ParsedProjectFile
			(
				// TODO: i don't like the idea of using a syntax tree class for the package level
				@namespace: mainLevel,

				usings: usings,
				typeDefinitions: typeDefinitions
			);
		}
	}
}
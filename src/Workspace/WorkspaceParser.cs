﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Terumi.Lexer;
using Terumi.Parser;
using Terumi.SyntaxTree;

namespace Terumi.Workspace
{
	public static class WorkspaceParser
	{
		private static void handle(ProjectFile code, Memory<byte> mem)
		{
			var lexer = new TerumiLexer(code.Path, mem.Span);
			while (!lexer.AtEnd()) lexer.NextToken();
		}

		// we resolve dependencies only in DEBUG
		// because i have some pretty wacky dependency plans

		public static IEnumerable<ParsedProjectFile> ParseProject(this Project project, StreamLexer lexer, StreamParser parser
#if DEBUG
		, DependencyResolver resolver
#endif
		)
		{
			foreach (var code in project.GetSources())
			{
				// TODO: use string directly and rid all the code of reader head/fork stuff

				var mem = Encoding.UTF8.GetBytes(code.Source).AsMemory();

				var stp = Stopwatch.StartNew();
				for (var i = 0; i < 10000; i++)
				{
					foreach (var _ in lexer.ParseTokens(mem, code.Path))
					{

					}
				}
				Console.WriteLine($"Old lexer per 1000 iter: {((decimal)stp.ElapsedMilliseconds / (decimal)10000)}ms");

				stp = Stopwatch.StartNew();
				for (var i = 0; i < 10000; i++)
				{
					handle(code, mem);
				}
				Console.WriteLine($"New lexer per 1000 iter: {((decimal)stp.ElapsedMilliseconds / (decimal)10000)}ms");

				var tokens = lexer.ParseTokens(mem, code.Path);
				if (!parser.TryParse(tokens.ToArray().AsMemory(), out var compilerUnit))
				{
					throw new WorkspaceParserException($"Unable to parse source code into compiler unit: in '{project.ProjectName}', at '{code.Path}'.");
				}

				yield return code.Analyze(compilerUnit);
			}

#if DEBUG
			foreach (var dependency in project.ResolveDependencies(resolver))
			{
				foreach (var file in dependency.ParseProject(lexer, parser, resolver))
				{
					yield return file;
				}
			}
#endif
		}

		public static ParsedProjectFile Analyze(this ProjectFile source, CompilerUnit compilerUnit)
		{
			var mainLevel = source.PackageLevel;

			var usings = new List<PackageLevel>();
			var typeDefinitions = new List<TypeDefinition>();
			var methods = new List<Method>();

			var parsedTypeDefinition = false;

			for (var i = 0; i < compilerUnit.CompilerUnitItems.Count; i++)
			{
				var item = compilerUnit.CompilerUnitItems[i];

				switch (item)
				{
					case PackageReference packageLevel:
					{
						// ensure only using/namespace at top
						if (parsedTypeDefinition)
						{
							var error = $"A package statement other than 'using'/'namespace' has been parsed. " +
	$"You may not specify any more package statements after a different kind of statement is parsed. " +
	$"Error when using '{packageLevel}' at '{source.Path}'.";

							Log.Error(error);
							break;
						}

						// if it's a using, add it
						if (packageLevel.Action == PackageAction.Using)
						{
							usings.Add(packageLevel.Levels);
							break;
						}

						// we have a namespace declaration - make sure it's first
						if (i != 0)
						{
							var error = $"The first item in the file must specify the namespace of the file. " +
	$"The namespace of the file '{source.Path}' will remain as '{mainLevel}', and not '{packageLevel.Levels}'.";

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

					case Method method:
					{
						parsedTypeDefinition = true;
						methods.Add(method);
					}
					break;
				}
			}

			return new ParsedProjectFile
			(
				// TODO: i don't like the idea of using a syntax tree class for the package level
				@namespace: mainLevel,

				usings: usings,
				typeDefinitions: typeDefinitions,
				methods: methods
			);
		}
	}
}
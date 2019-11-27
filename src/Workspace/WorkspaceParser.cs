using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Terumi.Binder;
using Terumi.Lexer;
using Terumi.Parser;
using Terumi.Targets;

namespace Terumi.Workspace
{
	public static class WorkspaceParser
	{
		public static TerumiBinderBindings ParseProject(this Project project, DependencyResolver resolver, ICompilerTarget target)
		{
			// TODO: make this way better, atm it's garbage
			// need to properly scope indirect and direct dependencies

			var immediateDependencies = project.ResolveDependencies(resolver);

			var binderProject = new TerumiBinderProject
			{
				ProjectFiles = new List<SourceFile>(),
				DirectDependencies = new List<BoundFile>(),
				IndirectDependencies = new List<BoundFile>()
			};

			foreach (var dependency in immediateDependencies)
			{
				var dependencyBinderProject = dependency.ParseProject(resolver, target);
				binderProject.DirectDependencies.AddRange(dependencyBinderProject.BoundProjectFiles);
			}

			foreach (var source in project.GetSources())
			{
				var parser = new TerumiParser(ParseTokens(source.Source, source.Path));
				var sourceFile = parser.ConsumeSourceFile(source.PackageLevel);
				binderProject.ProjectFiles.Add(sourceFile);
			}

			return binderProject.Bind(target);
		}

		public static List<Token> ParseTokens(string source, string fileName)
		{
			var lexer = new TerumiLexer(source, fileName);
			var tokens = lexer.ConsumeTokens();

			if (lexer.WasError)
			{
				Log.Error("Lexer error: '" + lexer.ErrorMessage + "' at " + lexer.ErrorLocation);

				// find the line where the error occured
				// TODO: function
				int start = 0;
				int offset = 0;

				for (int i = 0, line = 1; i < lexer.Source.Length; i++)
				{
					if (line == lexer.ErrorLocation.Line)
					{
						start = i;
						break;
					}

					if (lexer.Source[i] == '\n')
					{
						line++;
					}

					offset++;
				}

				int end = 0;
				for (int i = start; i < lexer.Source.Length; i++)
				{
					if (lexer.Source[i] == '\n')
					{
						end = i;
						break;
					}
				}

				ReadOnlySpan<char> lineData;

				if (end == 0)
				{
					lineData = lexer.Source.Slice(start);
				}
				else
				{
					lineData = lexer.Source.Slice(start, end);
				}

				// display the line
				Log.Error(new string(lineData));

				// display ^
				Span<char> marker = lineData.Length <= 1024 ? stackalloc char[lineData.Length] : new char[lineData.Length];

				int targetPos = lexer.ErrorLocation.BinaryOffset - offset;

				for (int i = 0; i < marker.Length; i++)
				{
					if (i == targetPos)
					{
						marker[i] = '^';
						break;
					}
				}

				Log.Error(new string(marker));
			}

			return tokens;
		}
	}
}
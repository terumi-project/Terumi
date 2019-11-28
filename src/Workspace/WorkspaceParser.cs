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
				var tokens = ParseTokens(source.Source, source.Path);
				var rom = new ReadOnlyMemory<Token>(tokens.ToArray());
				var parser = new TerumiParser(rom);

				try
				{
					var sourceFile = parser.ConsumeSourceFile(source.PackageLevel);

					binderProject.ProjectFiles.Add(sourceFile);
				}
				catch (ParserException ex)
				{
					var ctxI = ex.Index;
					var ctxTokens = ex.Context.Span;
					ReadOnlySpan<char> src = source.Source;

					var metadataLine = ctxTokens[ctxI].PositionStart.Line;
					var metadataColumn = ctxTokens[ctxI].PositionStart.Column;

					// generate a visibly meaningful error, eg.
					// main(number a string b)
					//              ^

					// we want to start by grabbing the whole line
					var search = ctxTokens[ctxI].PositionStart.BinaryOffset;

					var lineStart = search - 1; // if search is on a '\n' we want the previous line
					var lineEnd = search;

					while (lineStart >= 0
						&& src[lineStart] != '\n')
					{
						lineStart--;
					}

					// hit a '\n', we want to not include that
					lineStart++;

					// we account for lineEnd hitting the EOF
					// and we account for lineEnd-- with this hacky crud
					while (lineEnd <= src.Length
						&& (lineEnd < src.Length ? src[lineEnd] != '\n' : true))
					{
						lineEnd++;
					}

					// hit a '\n', we want to not include that
					lineEnd--;

					// now we know the entire line
					var tokenStart = search;
					var tokenEnd = ctxTokens[ctxI].PositionEnd.BinaryOffset;

					// now we know the line, and the token position
					// we can generate a pretty error now
					// first, let's grab just the line and make all the offsets relative to it
					var line = src.Slice(lineStart, lineEnd - lineStart);

					tokenStart -= lineStart;
					tokenEnd -= lineStart;
					lineEnd -= lineStart;
					lineStart = 0;

					// now let's generate a pretty image for the error position
					Span<char> image = new char[line.Length];
					image[tokenStart..tokenEnd].Fill('~');
					image[tokenStart] = '^';

					// ok now let's modify both line and image according to tabs
					// we want to replace a tab with 4 spaces
					var tabs = 0;
					for (int i = 0; i < line.Length; i++)
					{
						if (line[i] == '\t')
						{
							tabs++;
						}
					}

					Span<char> modLine = new char[line.Length + tabs * 3];
					Span<char> modImage = new char[image.Length + tabs * 3];

					int destI = 0;
					for (int i = 0; i < line.Length; i++)
					{
						if (line[i] == '\t')
						{
							for (int p = 0; p < 4; p++)
							{
								modLine[destI] = ' ';
								modImage[destI] = ' ';
								destI++;
							}
						}
						else
						{
							modLine[destI] = line[i];
							modImage[destI] = image[i];
							destI++;
						}
					}

					Log.Error($@"Error parsing '{source.Path}'@'{project.ProjectName}':
L{metadataLine}:C{metadataColumn} {ex.Message}
{new string(modLine)}
{new string(modImage)}");
				}
			}

			return binderProject.Bind(target);
		}

		public static List<Token> ParseTokens(string source, string fileName)
		{
			Log.Info($"Discovered {fileName}");
			var lexer = new TerumiLexer(source, fileName);
			var tokens = lexer.ConsumeTokens();

			if (lexer.WasError)
			{
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

				Log.Error(@$"Lexer error: '{lexer.ErrorMessage}'@{lexer.ErrorLocation}'
{new string(lineData)}
{new string(marker)}");
			}

			return tokens;
		}
	}
}
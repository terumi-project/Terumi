using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Terumi.Binder;
using Terumi.Lexer;
using Terumi.Parser;

namespace Terumi.Workspace
{
	public static class WorkspaceParser
	{
		public static TerumiBinderBindings ParseProject(this Project project, DependencyResolver resolver)
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
				var dependencyBinderProject = dependency.ParseProject(resolver);
				binderProject.DirectDependencies.AddRange(dependencyBinderProject.BoundProjectFiles);
			}

			foreach (var source in project.GetSources())
			{
				var tokens = new TerumiLexer(source.Path, Encoding.UTF8.GetBytes(source.Source)).ParseTokens();
				var parser = new TerumiParser(tokens);
				var sourceFile = parser.ConsumeSourceFile(source.PackageLevel);
				binderProject.ProjectFiles.Add(sourceFile);
			}

			return binderProject.Bind();
		}

		public static List<Token> ParseTokens(this TerumiLexer lexer)
		{
			var tokens = new List<Token>();

			while (!lexer.AtEnd())
			{
				tokens.Add(lexer.NextToken());
			}

			return tokens;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Lexer;
using Terumi.Parser;
using Terumi.SyntaxTree;

namespace Terumi.Workspace
{
	public static class SourceFileParser
	{
		public static ParsedSourceFile Parse(this SourceFile sourceFile, StreamLexer lexer, StreamParser parser)
		{
			var tokens = lexer.ParseTokens(sourceFile.Source);

			if (!parser.TryParse(tokens, out var compilerUnit))
			{
				throw new Exception("Error turning source into tokens");
			}

			var @namespace = sourceFile.PackageLevel;
			var references = new List<PackageLevel>();
			var defs = new List<TypeDefinition>();

			foreach(var item in compilerUnit.CompilerUnitItems)
			{
				switch(item)
				{
					case PackageLevel packageLevel:
					{
						if (packageLevel.Action == PackageAction.Namespace)
						{
							// TODO: if doing this twice or not at the top, yell
							@namespace = packageLevel;
						}
						else
						{
							// TODO: if already parsed a class or contract, ylll
							references.Add(packageLevel);
						}
					}
					break;

					case TypeDefinition typeDefinition:
					{
						defs.Add(typeDefinition);
					}
					break;
				}
			}

			return new ParsedSourceFile(@namespace, references, defs);
		}
	}
}

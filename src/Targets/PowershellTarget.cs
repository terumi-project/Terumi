using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Terumi.Ast;
using Terumi.Binder;

namespace Terumi.Targets
{
	public class PowershellTarget
	{
		private readonly TypeInformation _info;

		public PowershellTarget(TypeInformation info)
		{
			_info = info;
		}

		public void Write(TextWriter writer, InfoItem item)
		{
			writer.WriteLine($@"function {item.Code.Name}({Parameters(item)})
{{");

			foreach(var loc in item.Code.Statements)
			{
				HandleStatement(writer, loc);
			}

			writer.WriteLine("}");
		}

		private void HandleStatement(TextWriter writer, CodeStatement statement)
		{
			switch(statement)
			{
				case MethodCallExpression methodCallExpression:
				{
					HandleExpression(writer, 0, methodCallExpression);
				}
				break;
			}
		}

		private void HandleExpression(TextWriter writer, int resultVar, ICodeExpression expression)
		{
			int parameterVarCount = 0;
			int parameterVar = resultVar + 1;

			switch (expression)
			{
				case MethodCallExpression methodCallExpression:
				{
					foreach (var parameter in methodCallExpression.Parameters)
					{
						HandleExpression(writer, parameterVar++, parameter);
						parameterVarCount++;
					}

					if (methodCallExpression.CallingMethod.TerumiBacking == null)
					{
						// probably a compiler defined method

						switch (methodCallExpression.CallingMethod.Name)
						{
							case "println":
							{
								writer.WriteLine($"Write-Host (\"$(${parameterVar - 1})\")");
							}
							break;
						}
					}
					else
					{
						writer.Write(methodCallExpression.CallingMethod.Name);

						for (var i = 0; i < parameterVarCount; i++)
						{
							writer.Write(" $");
							writer.Write(resultVar + 1 + i);
						}

						writer.WriteLine();
					}
				}
				break;

				case ConstantLiteralExpression<BigInteger> number:
				{
					writer.WriteLine($"${resultVar} = New-Object System.Numerics.BigInteger(\"{number.Literal.ToString()}\")");
				}
				break;
			}
		}

		private string Parameters(InfoItem item)
		{
			var parameters = item.Code.Parameters;

			if (parameters.Count == 0)
			{
				return "";
			}

			if (parameters.Count == 1)
			{
				return "$" + parameters.First().Name;
			}

			return item.Code.Parameters.Select(x => "$" + x.Name).Aggregate((a, b) => a + ", " + b);
		}

		public void Post(TextWriter writer)
		{
			writer.WriteLine("main");
		}
	}
}

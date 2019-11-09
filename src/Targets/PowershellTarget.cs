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

		public void Write(TextWriter writer, IBind bind)
		{
			if (bind is MethodBind methodBind)
			{
				WriteMethod(writer, methodBind);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public void WriteMethod(TextWriter writer, MethodBind method)
		{
			writer.WriteLine($@"function {method.Name}({Parameters(method)})
{{");

			foreach (var loc in method.Statements)
			{
				HandleStatement(writer, loc);
			}

			writer.WriteLine("}");
		}

		private void HandleStatement(TextWriter writer, CodeStatement statement)
		{
			switch (statement)
			{
				case MethodCallExpression methodCallExpression:
				{
					if (TryHandleInlineExpression(methodCallExpression, out var result))
					{
						writer.WriteLine($"\t{result}");
						return;
					}

					HandleExpression(writer, 0, methodCallExpression);
				}
				break;

				case ReturnStatement returnStatement:
				{
					if (TryHandleInlineExpression(returnStatement.ReturnOn, out var result))
					{
						writer.WriteLine($"\treturn {result}");
						return;
					}

					HandleExpression(writer, 0, returnStatement.ReturnOn);
					writer.WriteLine($"\treturn $0");
				}
				break;

				case AssignmentStatement assignmentStatement:
				{
					if (!TryHandleInlineExpression(assignmentStatement.VariableAssignment, out var result))
					{
						throw new Exception("Impossible for inline to not handle variable assignment");
					}

					writer.WriteLine($"\t{result}");
				}
				break;

				default: throw new Exception("Unhandled statement " + statement);
			}
		}

		private void HandleExpression(TextWriter writer, int resultVar, ICodeExpression expression)
		{
			int parameterVarCount = 0;
			int parameterVar = resultVar + 1;

			if (TryHandleInlineExpression(expression, out var result))
			{
				writer.WriteLine($"\t${resultVar} = {result}");
				return;
			}

			// anything an inline can't do, we do

			switch (expression)
			{
				default: throw new Exception("Unhandled expression: " + expression);
			}
		}

		private bool TryHandleInlineExpression(ICodeExpression expression, out string result)
		{
			switch (expression)
			{
				case MethodCallExpression methodCallExpression:
				{
					var inlineParameters = new List<string>();

					foreach (var parameter in methodCallExpression.Parameters)
					{
						if (!TryHandleInlineExpression(parameter, out var param))
						{
							result = default;
							return false;
						}

						inlineParameters.Add(param);
					}

					var strb = new StringBuilder();

					if (methodCallExpression.CallingMethod.TerumiBacking == null)
					{
						// compiler defined method

						switch (methodCallExpression.CallingMethod.Name)
						{
							case "println":
							{
								result = "Write-Host " + inlineParameters[0];
							}
							break;

							case "concat":
							{
								strb.Append('"');

								foreach (var parameter in inlineParameters)
								{
									strb.Append("$(");
									strb.Append(parameter);
									strb.Append(')');
								}

								strb.Append('"');

								result = strb.ToString();
							}
							break;

							case "add":
							{
								strb.Append('(');
								strb.Append(inlineParameters[0]);

								for (var i = 1; i < inlineParameters.Count; i++)
								{
									strb.Append('+');
									strb.Append(inlineParameters[i]);
								}

								strb.Append(')');
								result = strb.ToString();
							}
							break;

							default:
							{
								result = default;
								return false;
							}
						}

						return true;
					}

					strb.Append('(');
					strb.Append(methodCallExpression.CallingMethod.Name);

					foreach (var parameter in inlineParameters)
					{
						strb.Append(' ');
						strb.Append(parameter);
					}

					strb.Append(')');

					result = strb.ToString();
					return true;
				}

				case ConstantLiteralExpression<BigInteger> number:
				{
					result = $"(New-Object System.Numerics.BigInteger(\"{number.Literal.ToString()}\"))";
					return true;
				}

				case ConstantLiteralExpression<string> str:
				{
					result = $"\"{Sanitize(str.Literal)}\"";
					return true;
				}

				case ConstantLiteralExpression<bool> @bool:
				{
					result = @bool.Literal ? "$TRUE" : "$FALSE";
					return true;
				}

				case ParameterReferenceExpression parameterExpression:
				{
					result = $"${parameterExpression.Parameter.Name}";
					return true;
				}

				case VariableReferenceExpression variableReferenceExpression:
				{
					result = $"${variableReferenceExpression.VarName}";
					return true;
				}

				case VariableAssignment variableAssignment:
				{
					if (!TryHandleInlineExpression(variableAssignment.Value, out var value))
					{
						throw new Exception("I didn't plan for inline expressions to be unable to handle everything oh god oh frick");
					}

					result = $"${variableAssignment.VariableName} = {value}";
					return true;
				}

				default:
				{
					throw new Exception("unhandled inline expression " + expression);
				}
			}

			result = default;
			return false;
		}

		private string Parameters(MethodBind item)
		{
			var parameters = item.Parameters;

			if (parameters.Count == 0)
			{
				return "";
			}

			if (parameters.Count == 1)
			{
				return "$" + parameters.First().Name;
			}

			return item.Parameters.Select(x => "$" + x.Name).Aggregate((a, b) => a + ", " + b);
		}

		private string Sanitize(string str)
		{
			var strb = new StringBuilder(str.Length);

			foreach (var c in str)
			{
				if (c == '\n')
				{
					strb.Append('`');
					strb.Append('n');
				}
				else if (c == '\t')
				{
					strb.Append('`');
					strb.Append('t');
				}
				else if (c == '`')
				{
					strb.Append('`');
					strb.Append('`');
				}
				else
				{
					strb.Append(c);
				}
			}

			return strb.ToString();
		}

		public void Post(TextWriter writer)
		{
			writer.WriteLine("main");
		}
	}
}
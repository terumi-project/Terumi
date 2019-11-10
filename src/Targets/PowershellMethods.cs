﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

using Terumi.Ast;
using Terumi.Binder;

namespace Terumi.Targets
{
	public class PowershellMethods : ICompilerMethods
	{
		public ICompilerTarget MakeTarget(TypeInformation typeInformation) => new PowershellTarget(typeInformation);

		public string println_string(string value) => $"Write-Host {value}";
		public string println_number(string value) => $"Write-Host {value}";
		public string println_bool(string value) => $"Write-Host {value}";
		public string concat_string_string(string a, string b) => $"\"$({a})$({b})\"";
		public string add_number_number(string a, string b) => $"{a}+{b}";
	}

	public class PowershellTarget : ICompilerTarget
	{
		private readonly TypeInformation _typeInformation;

		public PowershellTarget(TypeInformation typeInformation) => _typeInformation = typeInformation;

		public void Post(IndentedTextWriter writer) => writer.WriteLine("main");

		public void Write(IndentedTextWriter writer, IBind bind)
		{
			switch (bind)
			{
				case UserType userType: WriteUserType(writer, userType); return;
				case MethodBind methodBind: WriteMethodBind(writer, methodBind); return;
				default: throw new NotImplementedException();
			}
		}

		private void WriteUserType(IndentedTextWriter writer, UserType userType)
		{
			throw new NotImplementedException();
		}

		private void WriteMethodBind(IndentedTextWriter writer, MethodBind methodBind)
		{
			writer.WriteLine(@$"function {methodBind.Name}({GenMethodParameters(methodBind.Parameters)})
{{");
			writer.Indent++;

			foreach (var statement in methodBind.Statements)
			{
				WriteMethodStatement(writer, statement);
			}

			writer.Indent--;
			writer.WriteLine("}");
		}

		private string GenMethodParameters(List<ParameterBind> parameters)
		{
			if (parameters.Count == 0) return "";
			if (parameters.Count == 1) return "$" + parameters.First().Name;

			return parameters.Select(x => "$" + x.Name).Aggregate((a, b) => a + ", " + b);
		}

		private void WriteMethodStatement(IndentedTextWriter writer, CodeStatement statement)
			=> writer.WriteLine(HandleExpression(statement as ICodeExpression));

		private string HandleExpression(ICodeExpression codeExpression)
			=> codeExpression switch
			{
				MethodCallExpression methodCallExpression => HandleMethodCallExpression(methodCallExpression),
				ConstantLiteralExpression<string> constantLiteralExpressionString => HandleConstantLiteralExpressionString(constantLiteralExpressionString),
				ConstantLiteralExpression<BigInteger> constantLiteralExpressionNumber => HandleConstantLiteralExpressionNumber(constantLiteralExpressionNumber),
				ConstantLiteralExpression<bool> constantLiteralExpressionBoolean => HandleConstantLiteralExpressionBoolean(constantLiteralExpressionBoolean),
				ParameterReferenceExpression parameterReferenceExpression => HandleParameterReferenceExpression(parameterReferenceExpression),
				VariableReferenceExpression variableReferenceExpression => HandleVariableReferenceExpression(variableReferenceExpression),
				VariableAssignment variableAssignment => HandleVariableAssignment(variableAssignment)
				_ => throw new NotSupportedException(codeExpression.ToString())
			};
		private string HandleVariableAssignment(VariableAssignment variableAssignment) => throw new NotImplementedException();
		private string HandleVariableReferenceExpression(VariableReferenceExpression variableReferenceExpression) => throw new NotImplementedException();
		private string HandleParameterReferenceExpression(ParameterReferenceExpression parameterReferenceExpression) => throw new NotImplementedException();
		private string HandleConstantLiteralExpressionBoolean(ConstantLiteralExpression<bool> constantLiteralExpressionBoolean) => throw new NotImplementedException();
		private string HandleConstantLiteralExpressionNumber(ConstantLiteralExpression<BigInteger> constantLiteralExpressionNumber) => throw new NotImplementedException();
		private string HandleConstantLiteralExpressionString(ConstantLiteralExpression<string> constantLiteralExpressionString) => throw new NotImplementedException();
		private string HandleMethodCallExpression(MethodCallExpression methodCallExpression) => throw new NotImplementedException();
	}

	/*
	public class PowershellTarget : IPowershellTarget
	{
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

					if (methodCallExpression.CallingMethod is CompilerMethod compilerMethod)
					{
						// TODO: better compiler defined intrinsics
						// compiler defined method

						switch (compilerMethod.Name)
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
					result = $"(New-Object System.Numerics.BigInteger(\"{number.Value.ToString()}\"))";
					return true;
				}

				case ConstantLiteralExpression<string> str:
				{
					result = $"\"{Sanitize(str.Value)}\"";
					return true;
				}

				case ConstantLiteralExpression<bool> @bool:
				{
					result = @bool.Value ? "$TRUE" : "$FALSE";
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
	*/
}
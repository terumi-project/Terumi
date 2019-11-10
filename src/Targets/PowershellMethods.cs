using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
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
		{
			switch (statement)
			{
				case IfStatement ifStatement:
				{
					writer.WriteLine($"If ({HandleExpression(ifStatement.Comparison)}) {{");
					writer.Indent++;

					foreach (var bodyStatement in ifStatement.Statements)
					{
						WriteMethodStatement(writer, bodyStatement);
					}

					writer.Indent--;
					writer.WriteLine("}");
				}
				return;
			}

			writer.WriteLine(HandleExpression(statement as ICodeExpression));
		}

		private string HandleExpression(ICodeExpression codeExpression)
			=> codeExpression switch
			{
				MethodCallExpression methodCallExpression => HandleMethodCallExpression(methodCallExpression),
				ConstantLiteralExpression<string> constantLiteralExpressionString => HandleConstantLiteralExpressionString(constantLiteralExpressionString),
				ConstantLiteralExpression<BigInteger> constantLiteralExpressionNumber => HandleConstantLiteralExpressionNumber(constantLiteralExpressionNumber),
				ConstantLiteralExpression<bool> constantLiteralExpressionBoolean => HandleConstantLiteralExpressionBoolean(constantLiteralExpressionBoolean),
				ParameterReferenceExpression parameterReferenceExpression => HandleParameterReferenceExpression(parameterReferenceExpression),
				VariableReferenceExpression variableReferenceExpression => HandleVariableReferenceExpression(variableReferenceExpression),
				VariableAssignment variableAssignment => HandleVariableAssignment(variableAssignment),
				_ => throw new NotSupportedException(codeExpression.ToString())
			};

		private string HandleMethodCallExpression(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.CallingMethod is CompilerMethod compilerMethod)
			{
				return HandleCompilerMethod(compilerMethod, methodCallExpression.Parameters);
			}

			var parameters = new List<string>();

			foreach (var expr in methodCallExpression.Parameters)
			{
				parameters.Add(HandleExpression(expr));
			}

			var strb = new StringBuilder("(");
			strb.Append(methodCallExpression.CallingMethod.Name);

			foreach (var param in parameters)
			{
				strb.Append(' ');
				strb.Append(param);
			}

			strb.Append(')');
			return strb.ToString();
		}

		private string HandleCompilerMethod(CompilerMethod compilerMethod, List<ICodeExpression> expressions)
		{
			var strings = new List<string>();

			foreach (var expression in expressions)
			{
				strings.Add(HandleExpression(expression));
			}

			return compilerMethod.Generate(strings);
		}

		private string HandleConstantLiteralExpressionString(ConstantLiteralExpression<string> constantLiteralExpressionString)
			=> $"\"{Sanitize(constantLiteralExpressionString.Value)}\"";

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

		private string HandleConstantLiteralExpressionNumber(ConstantLiteralExpression<BigInteger> constantLiteralExpressionNumber)
			=> $"(New-Object System.Numerics.BigInteger(\"{constantLiteralExpressionNumber.Value.ToString()}\"))";

		private string HandleConstantLiteralExpressionBoolean(ConstantLiteralExpression<bool> constantLiteralExpressionBoolean)
			=> constantLiteralExpressionBoolean.Value ? "$TRUE" : "$FALSE";

		private string HandleParameterReferenceExpression(ParameterReferenceExpression parameterReferenceExpression)
			=> $"${parameterReferenceExpression.Parameter.Name}";

		private string HandleVariableReferenceExpression(VariableReferenceExpression variableReferenceExpression)
			=> $"${variableReferenceExpression.VarName}";

		private string HandleVariableAssignment(VariableAssignment variableAssignment)
			=> $"${variableAssignment.VariableName} = {HandleExpression(variableAssignment.Value)}";
	}
}
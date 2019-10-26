using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Terumi.ShellNeutral;

namespace Terumi.Targets
{
	public class PowershellTarget
	{
		private readonly StreamWriter _writer;

		public PowershellTarget(StreamWriter writer)
		{
			_writer = writer;
		}

		private bool _wroteLabel = false;
		private bool _inLabel = false;
		private BigInteger _popId = -1;
		private bool _popped = false;

		private static string WriteCode(string id, string code)
		{
			return @"	if ($id -eq " + ToString(id) + @") {
" + code + @"
		return
	}";
		}

		public void Write(IEnumerable<CodeLine> lines)
		{
			// setup some boilerplate powershell crud for shell neutral
			_writer.WriteLine(@"# Terumi ShellNeutral script.
# This code is compiled and generated.

# Boilerplate setup for interpreting ShellNeutral stuff.
$scopes_Variables = New-Object System.Collections.Generic.List[System.Object]
$scopes_Label = New-Object System.Collections.Generic.List[System.Object]
$scopes_Variables.Add(@{})
$scopes_Label.Add(0)

$scopeVars = $scopes_Variables[0]
$scopeLabel = $scopes_Label[0]

function Change-Scope($label) {
    $global:scopeLabel = $label
    $global:scopes_Label[$global:scopes_Label.Count - 1] = $label
}

function Assign-Scope() {
    $global:scopeVars = $global:scopes_Variables[$global:scopes_Variables.Count - 1]
    $global:scopeLabel = $global:scopes_Label[$global:scopes_Label.Count - 1]
}

function Up-Scope($label) {
    $global:scopes_Variables.Add($global:scopes_Variables[$global:scopes_Variables.Count - 1].Clone())
    $global:scopes_Label.Add($label)
}

function Down-Scope() {
    if ($global:scopes_Variables.Count -eq 0) {
        # we will die anyways now
        return
    }
	
    if ($global:scopes_Variables.Count -gt 1) {
        $setNew = @{}

		# merge the negative bigintegers at the current layer w/ the previous
		foreach ($key in $global:scopeVars.Keys) {
			if ($key.GetType() -eq [System.Numerics.BigInteger]) {
				if ($key -lt 0) {
					$setNew[$key.ToString()] = $global:scopeVars[$key]
				}
			}
		}

        foreach ($key in $setNew.Keys) {
            $bigIntKey = New-Object System.Numerics.BigInteger $key
            $global:scopes_Variables[$global:scopes_Variables.Count - 2][$bigIntKey] = $setNew[$key]
        }
	}

    $global:scopes_Variables.RemoveAt($global:scopes_Variables.Count - 1)
    $global:scopes_Label.RemoveAt($global:scopes_Label.Count - 1)

    Assign-Scope
}

function Compiler-Defined([System.String] $id) {
" +
WriteCode("println", @"Write-Host $global:scopeVars[" + ToBigInteger(0) + "]") +
@"
}

While ($scopes_Variables.Count -gt 0)
{");

			foreach(var line in lines)
			{
				if (line.IsLabel)
				{
					WriteLabel(line.Number);
				}
				else if (line.IsGoto)
				{
					WriteGoto(line.Number);
				}
				else if (line.IsCall)
				{
					WriteCall(line.Number);
				}
				else if (line.IsCompilerFunctionCall)
				{
					WriteCompilerFunctionCall(line.String);
				}
				else if (line.IsSetLine)
				{
					WriteSet(line.Variable, line.Expression);
				}
				else if (line.IsPop)
				{
					WritePop();
				}

				_writer.WriteLine();
			}

			_writer.WriteLine("}");
		}

		private string UniqueName(BigInteger number)
		{
			using var sha256 = SHA256.Create();

			var hash = sha256.ComputeHash(number.ToByteArray());

			var strb = new StringBuilder();

			foreach (var b in hash)
			{
				strb.AppendFormat("{0:X2}", b);
			}

			return strb.ToString();
		}

		public void WriteLabel(BigInteger id)
		{
			_writer.WriteLine("\t# :" + id);

			if (_inLabel)
			{
				// we need to quit the current label to set the current label to this label
				_writer.WriteLine("\t\tChange-Scope " + ToBigInteger(id));
				CloseLabel();
			}

			_writer.Write("\t");

			if (_wroteLabel)
			{
				_writer.Write("else");
			}

			_wroteLabel = true;

			_writer.Write("if ($scopeLabel -eq ");
			_writer.Write(id);
			_writer.WriteLine(")");

			_writer.WriteLine("\t{");

			_inLabel = true;
		}

		public void WriteGoto(BigInteger id)
		{
			if (!_inLabel)
			{
				throw new Exception("Invalid code line GOTO - not in label!");
			}

			_writer.WriteLine("\t\t# GOTO " + id);
			SwitchToCase(id);
		}

		public void WriteCall(BigInteger id)
		{
			if (!_inLabel)
			{
				throw new Exception("Invalid code line 'call' - not in label!");
			}

			var callback = _popId++;

			_writer.WriteLine("\t\t# CALL " + id);

			_writer.Write("\t\tChange-Scope ");
			_writer.WriteLine(ToBigInteger(callback));

			_writer.Write("\t\tUp-Scope ");
			_writer.WriteLine(ToBigInteger(id));

			_writer.WriteLine("\t\tAssign-Scope");
			CloseLabel();

			WriteLabel(callback);
		}

		public void WriteCompilerFunctionCall(string id)
		{
			// TODO
			_writer.Write("\t\t# CALL @");
			_writer.WriteLine(id);

			_writer.Write("\t\tCompiler-Defined ");
			_writer.WriteLine(ToString(id));
		}

		public void WriteSet(CodeExpression left, CodeExpression right)
		{
			_writer.WriteLine("\t\t# SET");

			_writer.Write("\t\t$scopeVars[");
			_writer.Write(EvaluateExpression(left));
			_writer.Write("] = ");
			_writer.WriteLine(EvaluateExpression(right));
		}

		private string EvaluateExpression(CodeExpression expression)
		{
			if (expression.IsNumber)
			{
				return ToBigInteger(expression.NumberValue);
			}
			else if (expression.IsString)
			{
				// TODO: better
				return $"\"{expression.StringValue}\"";
			}
			else if (expression.IsVariable)
			{
				return $"$scopeVars[({EvaluateExpression(expression.Variable)})]";
			}
			else if (expression.IsConcatenationExpression)
			{
				StringBuilder strb = new StringBuilder("\"");

				foreach(var concatExpr in expression.Expressions)
				{
					strb.Append("$(");
					strb.Append(EvaluateExpression(concatExpr));
					strb.Append(')');
				}

				strb.Append('\"');

				return strb.ToString();
			}

			throw new ArgumentException("Unknown code expression");
		}

		public void WritePop()
		{
			if (!_inLabel)
			{
				throw new Exception("Invalid code line 'POP' - not in label!");
			}

			_writer.WriteLine("\t\t# POP");

			_writer.WriteLine("\t\tDown-Scope");
			CloseLabel();
		}

		private void SwitchToCase(BigInteger id)
		{
			if (!_inLabel)
			{
				throw new Exception("Invalid code line - cannot switch to label when not in label!");
			}

			_writer.Write("\t\tChange-Scope ");
			_writer.WriteLine(id);
			CloseLabel();
		}

		private static string ToBigInteger(BigInteger id)
		{
			// TODO: new BigInteger("id");
			return $"(New-Object System.Numerics.BigInteger \"{id.ToString()}\")";
		}

		private static string ToString(string id)
		{
			return $"\"{id}\"";
		}

		private void CloseLabel()
		{
			if (!_inLabel)
			{
				throw new Exception("Tried to close label when not in a label!");
			}

			_writer.WriteLine("\t}");
			_inLabel = false;
		}
	}
}

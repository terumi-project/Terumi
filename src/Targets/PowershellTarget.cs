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

		private bool _inLabel = false;
		private BigInteger _labelId = 0;
		private bool _popped = false;

		public void Write(IEnumerable<CodeLine> lines)
		{
			// setup some boilerplate powershell crud for shell neutral
			_writer.WriteLine(@"# Terumi ShellNeutral script.
# This code is compiled and generated.

# Boilerplate setup for interpreting ShellNeutral stuff.
public class Scope
{
	public Dictionary<dynamic, dynamic> Variables { get; set; } = new Dictionary<dynamic, dynamic>();
	public BigInteger Label { get; set; } = 0;

	public Scope Clone()
	{
		Dictionary<dynamic, dynamic> target = new Dictionary<dynamic, dynamic>();

		foreach(KeyValuePair<dynamic, dynamic> kvp in Variables)
		{
			target[kvp.Key] = kvp.Value;
		}

		return new Scope
		{
			Variables = target
			Label = Label
		};
	}
}

# Setup environment variables & helpers
List<Scope> scopes = new List<Scope>();
Scope scope = new Scope();

void UpScope(BigInteger label)
{
	Scope newScope = scope.Clone(label);
	scopes.Add(scope);
	scope = newScope;
}

void DownScope()
{
	scope = scopes[scopes.Length - 1]
	scopes.RemoveAt(scopes.Length - 1);
}

# Begin code
UpScope(0);

while(scopes.Count > 0)
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
					WriteCompilerFunctionCall(line.Number);
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
			_writer.Write("\tcase ");
			_writer.Write(id);
			_writer.WriteLine(":");
			_writer.WriteLine("\t{");
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

			_writer.WriteLine("\t\t# CALL " + id);
			_writer.Write("\t\tUpScope(");
			_writer.Write(ToBigInteger(id));
			_writer.WriteLine(");");
			SwitchToCase(id);
		}

		public void WriteCompilerFunctionCall(BigInteger id)
		{
			// TODO
			_writer.WriteLine("\t\t#TODO: Write Compiler Function Call " + id);
		}

		public void WriteSet(CodeExpression left, CodeExpression right)
		{
			_writer.WriteLine("\t\t#TODO: Set ");
		}

		public void WritePop()
		{
			if (!_inLabel)
			{
				throw new Exception("Invalid code line 'POP' - not in label!");
			}

			_writer.WriteLine("\t\t# POP");
			_writer.WriteLine("\t\tDownScope();");
			CloseLabel();
		}

		private void SwitchToCase(BigInteger id)
		{
			if (!_inLabel)
			{
				throw new Exception("Invalid code line - cannot switch to label when not in label!");
			}

			_writer.Write("scope.Label = ");
			_writer.Write(id);
			_writer.WriteLine(";");
			CloseLabel();
		}

		private string ToBigInteger(BigInteger id)
		{
			// TODO: new BigInteger("id");
			return id.ToString();
		}

		private void CloseLabel()
		{
			if (!_inLabel)
			{
				throw new Exception("Tried to close label when not in a label!");
			}

			_writer.WriteLine("\t}");
			_writer.WriteLine("\tbreak;");
			_inLabel = false;
		}
	}
}

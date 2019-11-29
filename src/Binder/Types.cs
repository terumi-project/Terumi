using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Binder
{
	// important enums

	public enum BinaryExpression
	{
		EqualTo, NotEqualTo, // == !=
		LessThan, LessThanOrEqualTo, // < <=
		GreaterThan, GreaterThanOrEqualTo, // > >=

		Add, Subtract, Multiply, Divide, Exponent, // + - * / **
	}

	public enum IncrementType
	{
		DecrementPre,
		DecrementPost,
		IncrementPre,
		IncrementPost,
	}

	public enum UnaryExpression
	{
		Not, // !
		Negate, // -
	}

	public static class EnumHelper
	{
		public static BinaryExpression ToBinaryExpression(this Lexer.TokenType tokenType)
		{
			switch (tokenType)
			{
				case Lexer.TokenType.EqualTo: return BinaryExpression.EqualTo;
				case Lexer.TokenType.NotEqualTo: return BinaryExpression.NotEqualTo;
				case Lexer.TokenType.Not: return BinaryExpression.Not;
				case Lexer.TokenType.GreaterThan: return BinaryExpression.GreaterThan;
				case Lexer.TokenType.GreaterThanOrEqualTo: return BinaryExpression.GreaterThanOrEqualTo;
				case Lexer.TokenType.LessThan: return BinaryExpression.LessThan;
				case Lexer.TokenType.LessThanOrEqualTo: return BinaryExpression.LessThanOrEqualTo;
				case Lexer.TokenType.Add: return BinaryExpression.Add;
				case Lexer.TokenType.Subtract: return BinaryExpression.Subtract;
				case Lexer.TokenType.Exponent: return BinaryExpression.Exponent;
				case Lexer.TokenType.Multiply: return BinaryExpression.Multiply;
				case Lexer.TokenType.Divide: return BinaryExpression.Divide;
				default: throw new InvalidOperationException($"Cannot convert token type {tokenType} to enum {nameof(BinaryExpression)}");
			}
		}

		public static bool IsComparisonOperator(this BinaryExpression binaryExpression)
			=> (int)binaryExpression < (int)BinaryExpression.Add;

		public static IncrementType ToIncrementType(this Parser.Expression.Increment.IncrementSide side, Lexer.TokenType tokenType)
		{
			if (side == Parser.Expression.Increment.IncrementSide.Pre)
			{
				switch (tokenType)
				{
					case Lexer.TokenType.Decrement: return IncrementType.DecrementPre;
					case Lexer.TokenType.Increment: return IncrementType.IncrementPre;
					default: throw new InvalidOperationException($"Unrecognized pre increment {tokenType}");
				}
			}
			else
			{
				switch (tokenType)
				{
					case Lexer.TokenType.Decrement: return IncrementType.DecrementPost;
					case Lexer.TokenType.Increment: return IncrementType.IncrementPost;
					default: throw new InvalidOperationException($"Unrecognized pre increment {tokenType}");
				}
			}
		}
	}

	// stringdata

	public class StringData
	{
		public class Interpolation
		{
			public Interpolation(Expression expression, int insert)
			{
				Expression = expression;
				Insert = insert;
			}

			public Expression Expression { get; }
			public int Insert { get; }
		}

		public StringData(StringBuilder value, List<Interpolation> interpolations)
		{
			Value = value;
			Interpolations = interpolations;
		}

		public StringBuilder Value { get; }
		public List<Interpolation> Interpolations { get; }
	}

	// types

	public interface IType
	{
		string TypeName { get; }

		List<Field> Fields { get; }

		List<IMethod> Methods { get; }
	}

	public class Field
	{
		public Field(IType parent, IType type, string name)
		{
			// Parent = parent;
			Type = type;
			Name = name;
		}

		// commenting this out to prevent it from being used
		// thanks to terumi's fancy type system

		// public IType Parent { get; }
		public IType Type { get; }
		public string Name { get; }
	}

	public sealed class BuiltinType : IType
	{
		public static bool IsBuiltinType(IType type)
		{
			return type == BuiltinType.Void
				|| type == BuiltinType.String
				|| type == BuiltinType.Number
				|| type == BuiltinType.Boolean;
		}

		/// <summary>
		/// Tries to take a given name and pair it with one of the right BuiltinTypes
		/// </summary>
		public static bool TryUse(string? name, out IType type)
		{
			if (name == null) { type = Void; return true; }

			return Use(Void, out type)
				|| Use(String, out type)
				|| Use(Number, out type)
				|| Use(Boolean, out type);

			bool Use(IType a, out IType type)
			{
				if (a.TypeName == name)
				{
					type = a;
					return true;
				}

				type = default;
				return false;
			}
		}

		public static IType Void { get; } = new BuiltinType("void");
		public static IType String { get; } = new BuiltinType("string");
		public static IType Number { get; } = new BuiltinType("number");
		public static IType Boolean { get; } = new BuiltinType("bool");

		private BuiltinType(string name)
		{
			TypeName = name;
		}

		public string TypeName { get; }

		public List<Field> Fields => EmptyList<Field>.Instance;

		public List<IMethod> Methods => EmptyList<IMethod>.Instance;
	}

	public interface IMethod
	{
		bool IsCompilerDefined { get; }

		IType ReturnType { get; }

		string Name { get; }

		List<MethodParameter> Parameters { get; }
	}

	public class CompilerMethod : IMethod
	{
		public CompilerMethod(IType returnType, string name, List<MethodParameter> parameters)
		{
			ReturnType = returnType;
			Name = name;
			Parameters = parameters;
		}

		public bool IsCompilerDefined => true;
		public IType ReturnType { get; }
		public string Name { get; }
		public List<MethodParameter> Parameters { get; }

		public Func<List<string>, string> CodeGen { get; set; }
	}

	//

	public class Class : IType
	{
		public Class(Parser.Class fromParser, string name)
		{
			FromParser = fromParser;
			Name = name;
		}

		public Parser.Class FromParser { get; }
		public string Name { get; }
		public List<IMethod> Methods { get; set; } = new List<IMethod>();
		public List<Field> Fields { get; set; } = new List<Field>();

		string IType.TypeName => Name;
	}

	public class Method : IMethod
	{
		public Method(Parser.Method fromParser, IType returnType, string name)
		{
			FromParser = fromParser;
			ReturnType = returnType;
			Name = name;
		}

		public bool IsCompilerDefined => false;
		public Parser.Method FromParser { get; }
		public IType ReturnType { get; }
		public string Name { get; }
		public List<MethodParameter> Parameters { get; set; } = new List<MethodParameter>();
		public CodeBody Body { get; set; }
	}

	public class MethodParameter
	{
		private IMethod? _method;

		public MethodParameter(IType type, string name)
		{
			Type = type;
			Name = name;
		}

		public IMethod Method { get => _method ?? throw new System.InvalidOperationException($"This MethodParameter has not been passed into a Method yet - cannot get the method"); }
		public IType Type { get; }
		public string Name { get; }

		/// <summary>
		/// Used to set the Method property on this parameter to link back to the method this parameter can be found in
		/// </summary>
		/// <param name="claimer"></param>
		public void Claim(IMethod claimer)
		{
			if (_method != null)
			{
				throw new System.InvalidOperationException("This MethodParameter has already been claimed by a method!");
			}

			_method = claimer;
		}

		[System.Obsolete("Using this is a code smell", false)]
		public bool IsClaimed => _method == null;
	}
}

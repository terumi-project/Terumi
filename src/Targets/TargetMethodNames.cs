using System;
using Terumi.Binder;

namespace Terumi.Targets
{
	/// <summary>
	/// A group of method names (as constants) that all terumi targets should support
	/// </summary>
	public static class TargetMethodNames
	{
		public const string TargetName = "target_name";
		public const string Panic = "panic";

		public const string IsSupported = "is_supported";
		public const string Println = "println";
		public const string Command = "command";

		public const string OperatorAnd = "operator_add";
		public const string OperatorOr = "operator_or";

		public const string OperatorNot = "operator_not";

		public const string OperatorNotEqualTo = "operator_equal_to";
		public const string OperatorEqualTo = "operator_not_equal_to";

		public const string OperatorLessThan = "operator_less_than";
		public const string OperatorGreaterThan = "operator_greater_than";
		public const string OperatorLessThanOrEqualTo = "operator_less_than_or_equal_to";
		public const string OperatorGreaterThanOrEqualTo = "operator_greater_than_or_equal_to";

		public const string OperatorAdd = "operator_add";
		public const string OperatorNegate = "operator_negate";
		public const string OperatorSubtract = "operator_subtract";
		public const string OperatorMultiply = "operator_multiply";
		public const string OperatorDivide = "operator_divide";
		public const string OperatorExponent = "operator_exponent";

		public static string ToMethodName(this BinaryExpression binaryExpression)
			=> binaryExpression switch
		{
			BinaryExpression.EqualTo => OperatorEqualTo,
			BinaryExpression.NotEqualTo => OperatorNotEqualTo,
			BinaryExpression.LessThan => OperatorLessThan,
			BinaryExpression.LessThanOrEqualTo => OperatorLessThanOrEqualTo,
			BinaryExpression.GreaterThan => OperatorGreaterThan,
			BinaryExpression.GreaterThanOrEqualTo => OperatorGreaterThanOrEqualTo,
			BinaryExpression.Add => OperatorAdd,
			BinaryExpression.Subtract => OperatorSubtract,
			BinaryExpression.Multiply => OperatorMultiply,
			BinaryExpression.Divide => OperatorDivide,
			BinaryExpression.Exponent => OperatorExponent,
			BinaryExpression.And => OperatorAnd,
			BinaryExpression.Or => OperatorOr,
			_ => throw new InvalidOperationException(),
		};

		public static string ToMethodName(this UnaryExpression unaryExpression)
			=> unaryExpression switch
		{
			UnaryExpression.Negate => OperatorNegate,
			UnaryExpression.Not => OperatorNot,
			_ => throw new InvalidOperationException()
		};
	}
}
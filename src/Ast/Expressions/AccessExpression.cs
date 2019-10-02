namespace Terumi.Ast.Expressions
{
	public class AccessExpression : Expression
	{
		public AccessExpression(Expression access)
		{
			Access = access;
		}

		public Expression Predecessor { get; set; }

		/// <summary>
		/// The expression to evaluate after accessing.
		/// <para>
		/// Example:
		/// <code>
		/// some_code().some_more_code()
		/// </code>
		/// The access is `some_more_code()`
		/// </para>
		/// </summary>
		public Expression Access { get; }
	}
}

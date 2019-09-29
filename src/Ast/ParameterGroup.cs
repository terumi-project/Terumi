namespace Terumi.Ast
{
	public class ParameterGroup
	{
		public ParameterGroup(Parameter[] parameters)
		{
			Parameters = parameters;
		}

		public Parameter[] Parameters { get; }
	}
}
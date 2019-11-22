using System.Collections.Generic;

namespace Terumi.Binder
{
	public class Method : IMethod
	{
		public Method(IType returnType, string name, List<MethodParameter> parameters, CodeBody body)
		{
			ReturnType = returnType;
			Name = name;
			Parameters = parameters;
			Body = body;

			foreach (var parameter in parameters)
			{
				parameter.Claim(this);
			}
		}

		public IType ReturnType { get; }
		public string Name { get; }
		public List<MethodParameter> Parameters { get; }
		public CodeBody Body { get; }
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
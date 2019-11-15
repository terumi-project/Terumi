using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Terumi.Binder;

namespace Terumi.VarCode.Optimizer.Alpha
{
	// stores ids of visited methods,
	// w/ helper methods to protect implementation details of accessing the data
	public class VarCodeStore
	{
		private VarCodeId _methodCounter;
		private readonly Dictionary<MethodBind, VarCodeStructure> _methods = new Dictionary<MethodBind, VarCodeStructure>();
		private readonly Dictionary<CompilerMethod, VarCodeId> _compilerIds = new Dictionary<CompilerMethod, VarCodeId>();

		public VarCodeId Id(IMethod method)
			=> method switch
			{
				MethodBind o => Rent(o).Id,
				CompilerMethod o => CompilerId(o),
				_ => throw new NotSupportedException()
			};

		public VarCodeStructure? Rent(IMethod method)
		{
			if (!(method is MethodBind methodBind))
			{
				return default;
			}

			return Rent(methodBind);
		}

		private VarCodeStructure Rent(MethodBind method)
		{
			if (_methods.TryGetValue(method, out var structure))
			{
				return structure;
			}

			var id = _methodCounter++;
			return _methods[method] = new VarCodeStructure(id, method);
		}

		private VarCodeId CompilerId(CompilerMethod compilerMethod)
		{
			if (_compilerIds.TryGetValue(compilerMethod, out var id))
			{
				return id;
			}

			id = _methodCounter++;
			_compilerIds[compilerMethod] = id;
			return id;
		}
	}

	public class VarCodeStructure
	{
		public VarCodeStructure(VarCodeId id, MethodBind methodBind)
		{
			Id = id;
			MethodBind = methodBind;
			Tree = new VarTree();
		}

		public VarCodeId Id { get; }
		public MethodBind MethodBind { get; }
		public VarTree Tree { get; }
	}
}

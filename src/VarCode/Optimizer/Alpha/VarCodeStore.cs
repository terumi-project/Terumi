using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Terumi.Binder;
using Terumi.Targets;

namespace Terumi.VarCode.Optimizer.Alpha
{
	// stores ids of visited methods,
	// w/ helper methods to protect implementation details of accessing the data
	public class VarCodeStore
	{
		private VarCodeId _methodCounter;
		private readonly Dictionary<MethodBind, VarCodeStructure> _methods = new Dictionary<MethodBind, VarCodeStructure>();
		private readonly Dictionary<CompilerMethod, VarCodeId> _compilerIds = new Dictionary<CompilerMethod, VarCodeId>();

		public VarCodeStructure Entrypoint { get; }

		public VarCodeStore(MethodBind entry, ICompilerTarget target)
		{
			Entrypoint = Rent(entry);
			Target = target;
		}

		public CompilerMethod? GetCompilerMethod(VarCodeId methodId)
		{
			return _compilerIds.FirstOrDefault(x => x.Value == methodId).Key;
		}

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

		public IEnumerable<VarCodeStructure> Structures => _methods.Values;

		public IEnumerable<KeyValuePair<VarCodeId, CompilerMethod>> CompilerMethods => _compilerIds.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

		public ICompilerTarget Target { get; }

		public VarCodeStructure? GetStructure(VarCodeId id) => Structures.FirstOrDefault(x => x.Id == id);

		public bool TryRemove(VarCodeStructure method)
			=> _methods.Remove(method.MethodBind);

		private VarCodeStructure Rent(MethodBind method)
		{
			if (_methods.TryGetValue(method, out var structure))
			{
				return structure;
			}

			var id = _methodCounter++;
			return _methods[method] = new VarCodeStructure(this, id, method);
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
		public VarCodeStructure(VarCodeStore store, VarCodeId id, MethodBind methodBind)
		{
			Store = store;
			Id = id;
			MethodBind = methodBind;
			Tree = new VarTree();
		}

#if JSON
		[Newtonsoft.Json.JsonIgnore]
#endif
		// this really just is a hack so i don't have to pass a varcodestructure & store everywhere
		public VarCodeStore Store { get; }

		public VarCodeId Id { get; }

#if JSON
		[Newtonsoft.Json.JsonIgnore]
#endif
		public MethodBind MethodBind { get; }

		public VarTree Tree { get; }
	}
}

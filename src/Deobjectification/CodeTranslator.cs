using System;
using System.Collections.Generic;
using System.Text;

using DMethod = Terumi.Deobjectification.Method;
using BMethod = Terumi.Binder.IMethod;
using Terumi.Binder;

namespace Terumi.Deobjectification
{
	// to allow for a rewrite, all data is hidden under methods
	// if something is needed, write a method to get it
	public class StoreGateway
	{
		private readonly GlobalObjectInfo _globalObject;
		private readonly List<(DMethod, BMethod, Class?)> _methods;

		public StoreGateway(GlobalObjectInfo globalObject, List<(DMethod, BMethod, Class?)> methods)
		{
			_globalObject = globalObject;
			_methods = methods;
		}
	}

	public class CodeTranslator
	{
		private readonly DMethod _target;
		private readonly BMethod _source;
		private readonly StoreGateway _store;
		private readonly Class _context;

		public CodeTranslator(DMethod target, Class context, BMethod source, StoreGateway store)
		{
			_target = target;
			_source = source;
			_store = store;
			_context = context;
		}

		public void Transcribe()
		{
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode.Optimization
{
	/// <summary>
	/// <para>
	/// "Peeling Objects" is a way to prevent object allocations. For simple
	/// instances of newing up an object just to pass it into a method, where
	/// the method calls only a method or a field on an object (think: inlined
	/// methods apply only!), newing up the object is a waste of resources.
	/// </para>
	/// 
	/// <para>
	/// When an object is peeled, several things must happen:
	/// - Deconstruction of object into individual fields
	/// - Constructor inlined at "new" site
	/// - References to field of object turned into variable calls
	/// </para>
	/// 
	/// <para>
	/// For this optimization, this is not beneficial to a scenario wwhere the
	/// object is allocated and passed into a method. see:
	/// </para>
	/// 
	/// <para>
	/// <code>
	/// class Obj
	/// {
	///		string str
	///		ctor(string s) {
	///			str = s
	///		}
	/// }
	/// 
	/// do_thing()
	/// {
	///		print_str(new Obj("Test"))
	/// }
	/// 
	/// print_str(Obj obj)
	/// {
	///		@println(obj.str)
	///	}
	/// </code>
	/// </para>
	/// 
	/// <para>This optimization is, however, beneficial to the following situation:</para>
	/// 
	/// <para>
	/// <code>
	/// do_thing()
	/// {
	///		@println(new Obj("Test").str)
	/// }
	/// </code>
	/// </para>
	/// 
	/// <para>
	/// The construction of the object causes extra excessive GC pressure and
	/// performance concerns, and this is a pattern to be frequently used in
	/// Terumi.
	/// </para>
	/// 
	/// <para>As such, the resulting optimized code should look similar to:</para>
	/// 
	/// <para>
	/// <code>
	/// do_thing()
	/// {
	///		string obj_str;
	///		obj_str = "Test"
	///		@println(obj_str)
	/// }
	/// </code>
	/// </para>
	/// </summary>
	public class PeelObjects
	{
		public static bool Peel(Method method, int fieldCount)
		{
			var instance = new PeelObjects(fieldCount);
			return instance.Examine(method.Code);
		}

		private PeelObjects(int objectCount)
		{
			_objectCount = objectCount;
		}

		private class Peelable
		{
			public Peelable(int root, Instruction.New @new)
			{
				Root = root;
				New = @new;
				Ripe = true;
			}

			private List<List<int>> _aliasScopes = new List<List<int>>();

			public int Root { get; set; }
			public bool Ripe { get; set; }
			public Instruction.New New { get; }

			public List<int> ObjectToFields = new List<int>();

			public void UpAliasScope()
			{
				_aliasScopes.Add(new List<int>());
			}

			public void DownAliasScope()
			{
				_aliasScopes.RemoveAt(_aliasScopes.Count - 1);
			}

			public bool Is(int id) => Root == id || _aliasScopes.Any(x => x.Contains(id));

			internal void AddAlias(int store) => _aliasScopes[^1].Add(store);
		}

		public bool Examine(List<Instruction> instructions)
		{
			FindPeelable(instructions, new List<Peelable>());

			if (_ripe.Count == 0)
			{
				return false;
			}

			// now every item in _ripe should be peelable

			var highest = FindHighestNumber(instructions);
			Peel(instructions, ref highest);
			return true;
		}

		public int FindHighestNumber(List<Instruction> instructions)
			=> instructions.Max(InstrExctsnsnsnsndasidnioasdioasinjADSJIODSAJIODIJOSA.GetHighestId);

		private List<Peelable> _ripe = new List<Peelable>();
		private readonly int _objectCount;

		private void Peel(List<Instruction> instructions, ref int highest)
		{
			foreach (var i in _ripe)
			{
				i.UpAliasScope();
			}

			for (var i = 0; i < instructions.Count; i++)
			{
				switch (instructions[i])
				{
					case Instruction.New o:
					{
						if (_ripe.Any(x => x.New == o, out var p))
						{
							for (var iter = 0; iter < _objectCount; iter++)
							{
								p.ObjectToFields.Add(highest++);
							}
						}

						instructions.RemoveAt(i);
						i--;
					}
					break;

					case Instruction.Assign o:
					{
						{
							if (_ripe.Any(x => x.Is(o.Value), out var p))
							{
								p.AddAlias(o.Store);
							}
						}

						// NOTE: other optimizations, such as unused variables,
						// must be in place in order for this optimization to not
						// have issues for these kinds of deconstructions

						// this is used when trying to store an object into our
						// newly peeled object
						{
							if (_ripe.Any(x => x.Is(o.Store), out var p))
							{
								instructions.RemoveAt(i);
								i--;

								for (var itr = 0; itr < _objectCount; itr++)
								{
									var tmp = highest++;
									instructions.Insert(i++, new Instruction.GetField(tmp, o.Value, itr));
									instructions.Insert(i++, new Instruction.Assign(p.ObjectToFields[itr], tmp));
								}
							}
						}
					}
					break;

					case Instruction.SetField o:
					{
						if (_ripe.Any(x => x.Is(o.VariableId), out var p))
						{
							instructions.Insert(i, new Instruction.Assign(p.ObjectToFields[o.FieldId], o.ValueId));
							instructions.RemoveAt(i + 1);
						}
					}
					break;

					case Instruction.GetField o:
					{
						if (_ripe.Any(x => x.Is(o.VariableId), out var p))
						{
							instructions.Insert(i, new Instruction.Assign(o.StoreId, p.ObjectToFields[o.FieldId]));
							instructions.RemoveAt(i + 1);
						}
					}
					break;
				}
			}

			foreach (var i in _ripe)
			{
				i.DownAliasScope();
			}
		}

		private void FindPeelable(List<Instruction> instructions, List<Peelable> peelables)
		{
			foreach (var p in peelables)
			{
				p.UpAliasScope();
			}

			// TODO: maybe should just be a list of peelables tbh
			var creations = new List<int>();
			foreach (var i in instructions)
			{
				switch (i)
				{
					case Instruction.New o:
					{
						// we add stuff in a FIFO order
						// and are pretty much guarenteed to still have the index of the peelable
						creations.Add(peelables.Count);
						peelables.Add(new Peelable(o.StoreId, o));
					}
					break;

					case Instruction.Assign o:
					{
						if (peelables.Any(x => x.Is(o.Value), out var p))
						{
							p.AddAlias(o.Store);
						}
					}
					break;

					case Instruction.SetField o:
					{
						if (peelables.Any(x => x.Is(o.ValueId), out var p))
						{
							// cannot peel objects that are stored as fields
							p.Ripe = false;
						}
					}
					break;

					case Instruction.Call o:
					{
						foreach (var arg in o.Arguments)
						{
							if (peelables.Any(x => x.Is(arg), out var p))
							{
								// cannot peel objects that are passed to methods
								p.Ripe = false;
							}
						}
					}
					break;

					case Instruction.CompilerCall o:
					{
						foreach (var arg in o.Arguments)
						{
							if (peelables.Any(x => x.Is(arg), out var p))
							{
								// cannot peel objects that are passed to methods
								p.Ripe = false;
							}
						}
					}
					break;

					case Instruction.Return o:
					{
						if (peelables.Any(x => x.Is(o.ValueId), out var p))
						{
							// cannot peel objects that are returned
							p.Ripe = false;
						}
					}
					break;

					case Instruction.If o:
					{
						FindPeelable(o.Clause, peelables);
					}
					break;

					case Instruction.While o:
					{
						FindPeelable(o.Clause, peelables);
					}
					break;
				}
			}

			creations.Reverse();
			foreach (var index in creations)
			{
				var p = peelables[index];
				peelables.RemoveAt(index);

				if (p.Ripe)
				{
					_ripe.Add(p);
				}
			}

			foreach (var p in peelables)
			{
				p.DownAliasScope();
			}
		}
	}
}

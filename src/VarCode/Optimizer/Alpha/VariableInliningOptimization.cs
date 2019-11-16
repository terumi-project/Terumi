using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PreferenceTable = System.Collections.Generic.Dictionary<Terumi.VarCode.VarCodeId, Terumi.VarCode.VarCodeId>;

namespace Terumi.VarCode.Optimizer.Alpha
{
	public class VariableInliningOptimization : IOptimization
	{
		public bool Run(VarCodeStore store)
		{
			var couldInline = false;

			foreach (var item in store.Structures)
			{
				couldInline = Inline(item) || couldInline;
			}

			return couldInline;
		}

		private bool Inline(VarCodeStructure structure)
		{
			// we will have the source ID on the left, with the preferred ID on the right
			var preferences = new PreferenceTable();

			// now we will traverse the source,
			// replacing variable ids with their preferred ones if there is a preferred one
			// and updating the preferences table as we go

			return PreferenceInline(preferences, structure.Tree.Code);
		}

		private bool PreferenceInline(PreferenceTable preferences, List<VarInstruction> instructions)
		{
			var didInline = false;

			for (var i = 0; i < instructions.Count; i++)
			{
				var instruction = instructions[i];

				switch (instruction)
				{
					case VarAssignment o when o.Value is ReferenceVarExpression varRef:
					{
						UpdatePreferences(preferences, o.VariableId, varRef.VariableId);
						instructions.RemoveAt(i);
						i--;
						didInline = true;
					}
					break;

					case VarAssignment o when o.Value is MethodCallVarExpression methodCall:
					{
						didInline = UpdateParameters(preferences, methodCall) || didInline;
					}
					break;

					case VarMethodCall o:
					{
						if (o.VariableId != null)
						{
							o.VariableId = GetPreference(preferences, (VarCodeId)o.VariableId, ref didInline);
						}

						didInline = UpdateParameters(preferences, o.MethodCallVarExpression) || didInline;
					}
					break;

					case VarIf o:
					{
						// TODO: may have to duplicate preference table for if and else bodies when they do come around
						o.ComparisonVariable = GetPreference(preferences, o.ComparisonVariable, ref didInline);
						didInline = PreferenceInline(preferences, o.TrueBody) || didInline;
					}
					break;

					case VarReturn o:
					{
						o.Id = GetPreference(preferences, o.Id, ref didInline);
					}
					break;
				}
			}

			return didInline;
		}

		private bool UpdateParameters(PreferenceTable preferences, MethodCallVarExpression methodCall)
		{
			bool didUpdate = false;

			for (var i = 0; i < methodCall.ParameterVariables.Count; i++)
			{
				methodCall.ParameterVariables[i] = GetPreference(preferences, methodCall.ParameterVariables[i], ref didUpdate);
			}

			return didUpdate;
		}

		private VarCodeId GetPreference(PreferenceTable preferences, VarCodeId id, ref bool didInline)
		{
			var result = preferences.TryGetValue(id, out var newId);
			didInline = result || didInline;
			return result ? newId : id;
		}

		private void UpdatePreferences(PreferenceTable preferences, VarCodeId left, VarCodeId right)
		{
			// if we already have a preference for the variable on the right,
			// we want to use the preference for that one

			// that way, the preferences for these things remains sane:
			// $0 = TRUE
			// $1 = $0 [1 -> 0]
			// $2 = $1 [2 -> 0]
			// $3 = $2 [3 -> 0]

			if (!preferences.TryGetValue(right, out var rightPrefer))
			{
				preferences[left] = rightPrefer;
			}
			else
			{
				// otherwise, we want to set the initial preference now
				// (this would be the first $1 -> $0)
				preferences[left] = right;
			}
		}
	}
}

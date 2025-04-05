using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerfulConnections.Helpers
{
	internal static class MonoHelper
	{
		public static void DebugPrint(this Instruction instr)
		{
			try
			{
				Plugin.LogDebug($"{instr}");
			}
			catch (Exception _)
			{
				Plugin.LogDebug($"{instr.Offset}: {instr.OpCode} _____");
			}
		}
		public static bool IsBranchInstruction(this Instruction instruction)
		{
			if (instruction == null)
				return false;

			var flowControl = instruction.OpCode.FlowControl;
			return flowControl == FlowControl.Branch ||
				   flowControl == FlowControl.Cond_Branch;
		}
		public static void ComputeStackDelta(this Instruction instruction, ref int stack_size)
		{
			FlowControl flowControl = instruction.OpCode.FlowControl;
			if (flowControl == FlowControl.Call)
			{
				IMethodSignature methodSignature = (IMethodSignature)instruction.Operand;
				if (methodSignature.HasImplicitThis() && instruction.OpCode.Code != Code.Newobj)
				{
					stack_size--;
				}
				if (methodSignature.HasParameters)
				{
					stack_size -= methodSignature.Parameters.Count;
				}
				if (instruction.OpCode.Code == Code.Calli)
				{
					stack_size--;
				}
				if (methodSignature.ReturnType.MetadataType != MetadataType.Void || instruction.OpCode.Code == Code.Newobj)
				{
					stack_size++;
				}
			}
			else
			{
				ComputePopDelta(instruction.OpCode.StackBehaviourPop, ref stack_size);
				ComputePushDelta(instruction.OpCode.StackBehaviourPush, ref stack_size);
			}
		}
		public static bool HasImplicitThis(this IMethodSignature self)
		{
			if (self.HasThis)
			{
				return !self.ExplicitThis;
			}
			return false;
		}

		public static void ComputePushDelta(StackBehaviour push_behaviour, ref int stack_size)
		{
			switch (push_behaviour)
			{
				case StackBehaviour.Push1:
				case StackBehaviour.Pushi:
				case StackBehaviour.Pushi8:
				case StackBehaviour.Pushr4:
				case StackBehaviour.Pushr8:
				case StackBehaviour.Pushref:
					stack_size++;
					break;
				case StackBehaviour.Push1_push1:
					stack_size += 2;
					break;
			}
		}

		public static void ComputePopDelta(StackBehaviour pop_behavior, ref int stack_size)
		{
			switch (pop_behavior)
			{
				case StackBehaviour.Pop1:
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
					stack_size--;
					break;
				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					stack_size -= 2;
					break;
				case StackBehaviour.Popi_popi_popi:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					stack_size -= 3;
					break;
				case StackBehaviour.PopAll:
					stack_size = 0;
					break;
			}
		}
	}
}

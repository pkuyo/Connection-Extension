using MonoMod.ModInterop;
using MonoMod.RuntimeDetour;
using PowerfulConnections.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PowerfulConnections.Helpers
{

	
	[ModExportName("ConnectionExtension")]
	public static class ExtendHelperExport
	{

		public static void HookForExitIndexWithMethod(MethodBase method)
		{
			_ = new ILHook(method, GamePlayHooks.Common_ExitIndexIL);
		}

		public static void HookForExitIndexWithDelegate(Delegate method)
		{
			_ = new ILHook(method.GetMethodInfo(), GamePlayHooks.Common_ExitIndexIL);
		}

		public static bool TryGetWorldExtension(World world, out WorldExtension extension)
		{
			extension = null;

			if (world == null)
				return false;

			if (GamePlayHooks.worldExtensions.TryGetValue(world, out var module))
			{
				extension = module;
				return true;
			}

			return false;


		}

		public static bool TryGetRoomExtension(AbstractRoom room, out AbstractRoomExtension extension)
		{
			extension = null;

			if (room == null)
				return false;

			if (GamePlayHooks.abstractRoomExtensions.TryGetValue(room, out var module))
			{
				extension = module;
				return true;
			}
			return false;
		}

		public static int ExitIndex(AbstractRoom room, int targetRoom, int fromConnectionIndex)
		{
			if(room.TryGetExtension(out var module))
				module.SetFromConnectionIndex(fromConnectionIndex);
			return room.ExitIndex(targetRoom);
		}

	}

	public static class ExtendHelper
	{

		public static void HookForExitIndex(MethodBase method) 
		{
			_ = new ILHook(method, GamePlayHooks.Common_ExitIndexIL);
		}

		public static void HookForExitIndex(Delegate method)
		{
			_ = new ILHook(method.GetMethodInfo(), GamePlayHooks.Common_ExitIndexIL);
		}

		public static bool TryGetExtension(this World world, out WorldExtension extension)
		{
			return ExtendHelperExport.TryGetWorldExtension(world, out extension);
		}

		public static bool TryGetExtension(this AbstractRoom room, out AbstractRoomExtension extension)
		{
			return ExtendHelperExport.TryGetRoomExtension(room, out extension);
		}

		public static int ExitIndexWithExtension(this AbstractRoom room, int targetRoom, int fromConnectionIndex)
		{
			return ExtendHelperExport.ExitIndex(room, targetRoom, fromConnectionIndex);
		}
		
	}
}

using MonoMod.ModInterop;
using PowerfulConnections.Helpers;
using PowerfulConnections.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConnectionExtension.Helpers
{
	//Add this line to BaseUnityPlugin.OnEnable()
	//typeof(ExtendHelperImport).ModInterop();

	//[ModImportName("ConnectionExtension")]
	//public static class ExtendHelperImport
	//{
	//	public static void HookForExitIndex(MethodBase method)
	//	{
	//		if (HookForExitIndexWithMethod != null)
	//			HookForExitIndexWithMethod(method);
	//	}

	//	public static void HookForExitIndex(Delegate method)
	//	{
	//		if (HookForExitIndexWithDelegate != null)
	//			HookForExitIndexWithDelegate(method);
	//	}

	//	public static bool TryGetExtension(this World world, out WorldExtension extension)
	//	{
	//		extension = null;
	//		if (TryGetWorldExtension != null)
	//			return TryGetWorldExtension(world, out extension);
	//		return false;

	//	}

	//	public static bool TryGetExtension(this AbstractRoom room, out AbstractRoomExtension extension)
	//	{
	//		extension = null;
	//		if (TryGetRoomExtension != null)
	//			return TryGetRoomExtension(room, out extension);
	//		return false;
	//	}
	//	public static int ExitIndexWithEntrance(this AbstractRoom room, int targetRoom, int fromConnectionIndex)
	//	{
	//		if (ExitIndex != null)
	//			return ExitIndex(room, targetRoom, fromConnectionIndex);
	//		return room.ExitIndex(targetRoom);
	//	}
	//	public static Action<MethodBase> HookForExitIndexWithMethod;

	//	public static Action<Delegate> HookForExitIndexWithDelegate;

	//	public static TryGetWorldExtensionHandler TryGetWorldExtension;

	//	public static TryGetRoomExtensionHandler TryGetRoomExtension;

	//	public static Func<AbstractRoom, int, int, int> ExitIndex;

	//	public delegate bool TryGetRoomExtensionHandler(AbstractRoom room, out AbstractRoomExtension extender);

	//	public delegate bool TryGetWorldExtensionHandler(World world, out WorldExtension extender);
	//}
}

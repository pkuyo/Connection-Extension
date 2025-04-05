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

	//	public static int ExitIndexWithEntrance(this AbstractRoom room, int targetRoom, int fromConnectionIndex)
	//	{
	//		if (ExitIndex != null) //if ConnetionExtension is not enabled, use orignal method instead.
	//			return ExitIndex(room, targetRoom, fromConnectionIndex);
	//		return room.ExitIndex(targetRoom);
	//	}

	//	public static Func<AbstractRoom, int, int, int> ExitIndex;

	//}
}

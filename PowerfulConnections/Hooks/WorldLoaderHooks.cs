using Mono.Cecil.Cil;
using MonoMod.Cil;
using PowerfulConnections.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerfulConnections.Hooks
{
	internal static class WorldLoaderHooks
	{
		public static void OnModsInit()
		{
			On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues +=
				WorldLoader_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues;
			On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;
			IL.WorldLoader.CreatingAbstractRooms += WorldLoader_CreatingAbstractRooms;
		
		}

		private static void WorldLoader_CreatingAbstractRooms(ILContext il)
		{
			ILCursor c = new(il);
			c.GotoNext(MoveType.After,
				i => i.MatchNewarr<int>()
			);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((WorldLoader self) =>
			{
				if (!loaderExtensions.TryGetValue(self, out var module))
					return;
				module.CreateAbstractRoom();
			});
		}

		private static void WorldLoader_CreatingWorld(On.WorldLoader.orig_CreatingWorld orig, WorldLoader self)
		{
			orig(self);
			if (loaderExtensions.TryGetValue(self, out var module))
				module.CreatingWorld();
		}

		private static void WorldLoader_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues(
			On.WorldLoader.orig_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues orig, WorldLoader self,
			RainWorldGame game, SlugcatStats.Name playerCharacter, SlugcatStats.Timeline timelinePosition, bool singleRoomWorld,
			string worldName, Region region, RainWorldGame.SetupValues setupValues)
		{
			if (!loaderExtensions.TryGetValue(self, out var module))
				loaderExtensions.Add(self, new WorldLoaderExtension(self));
			orig(self,game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues);
		}

		

		private readonly static ConditionalWeakTable<WorldLoader, WorldLoaderExtension> loaderExtensions = new();
	}

	internal class WorldLoaderExtension(WorldLoader owner) : Extension<WorldLoader>(owner)
	{
		public void CreatingWorld()
		{
			
			if (!ownerRef.TryGetTarget(out var self) || !self.world.TryGetExtension(out var extender))
				return;
			Plugin.Log("In Creating World extension indexs");

			extender.extendIndex = extendIndex.ToDictionary(
				i => self.world.GetAbstractRoom(i.Key).index,
				i => i.Value.ToDictionary(
					i => self.world.GetAbstractRoom(i.Key).index, 
					i => i.Value.ToDictionary(i=> i.Key, i => i.Value))
			);

			foreach(var room in self.world.abstractRooms)
			{
				if (room.TryGetExtension(out var ext))
					ext.Init();
			}
			extendIndex.Clear();
		}

		public void CreateAbstractRoom()
		{
			if (!ownerRef.TryGetTarget(out var self))
				return;
			var arr = self.roomAdder[self.cntr];
			extendIndex.Add(arr[0], new());
			for (int i = 1; i < arr.Length; i++)
			{
				var last = arr[i];
				var result = Regex.Matches(arr[i], @"<(.*?)>");
				if (result == null || result.Count == 0)
					continue;
				arr[i] = Regex.Replace(arr[i], @"<.*?>", "");
				if (!extendIndex[arr[0]].TryGetValue(arr[i], out var map))
					extendIndex[arr[0]].Add(arr[i], map = new());


				if (result.Count > 1 || !int.TryParse(Regex.Replace(result[0].Value, "[<>]", ""), out var index))
					Plugin.LogError($"{last} is not a valid connection data, Room:{arr[0]}, value:{result[0].Value}");
				else
				{
					map[index] = i-1;
					Plugin.Log($"Loading {arr[0]}-{arr[i]} : {index}-{i-1}");
				}
			}
		}

	

		// Room, TargetRoomName, TargetConnectionIndex, ToConnectionIndex
		private Dictionary<string, Dictionary<string, Dictionary<int,int>>> extendIndex = new();

	}
}

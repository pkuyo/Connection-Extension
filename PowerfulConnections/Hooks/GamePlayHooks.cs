using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using PowerfulConnections.Helpers;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerfulConnections.Hooks
{

	internal static class GamePlayHooks
	{
		public static void OnModsInit()
		{
			On.World.ctor += World_ctor;
			On.AbstractRoom.ctor += AbstractRoom_ctor;
			On.AbstractRoom.ExitIndex += AbstractRoom_ExitIndex;
			On.RWCustom.Custom.BetweenRoomsDistance += Custom_BetweenRoomsDistance;
			On.PreyTracker.DistanceEstimation += PreyTracker_DistanceEstimation;

			IL.VoidSpawnWorldAI.DirectionFinder.Update += Common_ExitIndexIL;
			IL.ScavengersWorldAI.WorldFloodFiller.Update += Common_ExitIndexIL;
			IL.OverseersWorldAI.DirectionFinder.Update += Common_ExitIndexIL;
			IL.OverseersWorldAI.ShelterFinder.Update += Common_ExitIndexIL;
			IL.FirecrackerPlant.ScareObject.MakeCreatureLeaveRoom += Common_ExitIndexIL;
			IL.MoreSlugcats.YeekAI.MakeCreatureLeaveRoom += Common_ExitIndexIL;
			IL.AbstractCreatureAI.RandomMoveToOtherRoom += (il) => Common_ExitIndexIL_Impl(il, 25,1);
			IL.VoidSpawnKeeper.Initiate += Common_ExitIndexIL;
			IL.Creature.NewTile += Common_ExitIndexIL;
			IL.ThreatTracker.FleeTo_WorldCoordinate_int_int_bool_bool += Common_ExitIndexIL;
			IL.PathFinder.ConnectAITile += Common_ExitIndexIL;
			IL.PathFinder.ConnectionsOfAbstractNodeInRealizedRoom += (il) => Common_ExitIndexIL_Impl(il,75);
			IL.PathFinder.DestinationExit += Common_ExitIndexIL;
			IL.AbstractSpacePathFinder.Path += Common_ExitIndexIL;
			IL.AbstractSpaceNodeFinder.Update += Common_ExitIndexIL;
		    IL.Tracker.Ghost.Update += Common_ExitIndexIL;
			IL.MissionTracker.LeaveRoom.Act += Common_ExitIndexIL;
			IL.ShortcutHandler.Update += Common_ExitIndexIL;

			IL.DevInterface.MapPage.SaveMapConfig += MapPage_SaveMapConfig;	
			IL.HUD.RoomTransition.PlayerEnterShortcut += RoomTransition_PlayerEnterShortcut;
			IL.AbstractCreatureAI.RandomMoveToOtherRoom += AbstractCreatureAI_RandomMoveToOtherRoom;
			IL.ThreatTracker.ThreatCreature.Update += ThreatCreature_Update;

			On.World.NodeInALeadingToB_AbstractRoom_AbstractRoom += 
				World_NodeInALeadingToB_AbstractRoom_AbstractRoom;
			On.World.TotalShortCutLengthBetweenTwoConnectedRooms_AbstractRoom_AbstractRoom += 
				World_TotalShortCutLengthBetweenTwoConnectedRooms_AbstractRoom_AbstractRoom;
			On.FlyAI.FleeFromRainUpdate += FlyAI_FleeFromRainUpdate;

		}

		private static float PreyTracker_DistanceEstimation(On.PreyTracker.orig_DistanceEstimation orig, PreyTracker self, WorldCoordinate from, WorldCoordinate to, CreatureTemplate crit)
		{
			NoWarning = true;
			var re = orig(self, from, to, crit);
			NoWarning = false;

			var fromRoom = self.AI.creature.world.GetAbstractRoom(from);
			if (from.room == to.room
				|| fromRoom.realizedRoom == null
				|| !fromRoom.realizedRoom.readyForAI
				|| !fromRoom.TryGetExtension(out var extend)
				|| !extend.extendIndex.TryGetValue(to.room, out var map)
				|| map.Count == 0)
				return re;

			foreach (var i in map)
			{
				int creatureSpecificExitIndex = fromRoom.CommonToCreatureSpecificNodeIndex(i.Value, crit);
				int num = fromRoom.realizedRoom.aimap.ExitDistanceForCreatureAndCheckNeighbours(from.Tile, creatureSpecificExitIndex, crit);
				if (crit.ConnectionResistance(MovementConnection.MovementType.SkyHighway).Allowed && num > -1 && fromRoom.AnySkyAccess &&
					self.AI.creature.world.GetAbstractRoom(to).AnySkyAccess)
					num = Math.Min(num, 50);
				
				if (num > -1)
					re = Mathf.Min(re,num);
			}

			return re;
		}



		private static float Custom_BetweenRoomsDistance(On.RWCustom.Custom.orig_BetweenRoomsDistance orig, World world, WorldCoordinate a, WorldCoordinate b)
		{
			NoWarning = true;
			var re = orig(world, a, b);

			var roomA = world.GetAbstractRoom(a);
			var roomB = world.GetAbstractRoom(b);
			if(a.room == b.room || roomA.ExitIndex(b.room) < 0 || roomB.ExitIndex(a.room) < 0 || 
				(roomA.realizedRoom == null && roomB.realizedRoom == null) ||
				!roomA.TryGetExtension(out var extend) ||
				!extend.extendIndex.TryGetValue(roomB.index, out var map))
				return re;

			float num = world.GetAbstractRoom(a).size.ToVector2().magnitude;
			float num2 = world.GetAbstractRoom(b).size.ToVector2().magnitude;

			foreach(var i in map)
			{
				if (roomA.realizedRoom != null && roomA.realizedRoom.shortCutsReady)
					num = Mathf.Min(num, a.Tile.FloatDist(roomA.realizedRoom.LocalCoordinateOfNode(i.Value).Tile));
				
				if (roomB.realizedRoom != null && roomB.realizedRoom.shortCutsReady)
					num2 = Mathf.Min(num2, b.Tile.FloatDist(roomB.realizedRoom.LocalCoordinateOfNode(i.Key).Tile));
				
			}
			NoWarning = false;

			return num + num2;

		}

		private static void ThreatCreature_Update(ILContext il)
		{
			ILCursor c = new(il);
			c.EmitDelegate(() =>
			{
				NoWarning = true;
			});
			int index = 0;
			c.GotoNext(MoveType.After,
				i => i.MatchLdloc(out index),
				i => i.MatchLdcI4(-1),
				i => i.MatchBle(out _));

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, index);
			c.EmitDelegate((ThreatTracker.ThreatCreature self, int index) =>
			{
				NoWarning = false;
				if (!self.owner.AI.creature.Room.TryGetExtension(out var extend) ||
					!extend.extendIndex.TryGetValue(self.creature.BestGuessForPosition().room, out var map) ||
					map.Count == 0)
					return index;

				float minDist = float.MaxValue;
				var crit = self.owner.AI.creature;
				foreach (var i in map.Values)
				{
					if(Custom.DistLess(crit.Room.realizedRoom.ShortcutLeadingToNode(i).startCoord,crit.pos, minDist))
					{
						minDist = Custom.Dist(crit.Room.realizedRoom.ShortcutLeadingToNode(i).startCoord.Tile.ToVector2(), 
							crit.pos.Tile.ToVector2());
						index = i;
					}
				}
				return index;

			});
			c.Emit(OpCodes.Stloc, index);
		}

		public static bool NoWarning { get; private set; } = false;

		private static int World_TotalShortCutLengthBetweenTwoConnectedRooms_AbstractRoom_AbstractRoom(
			On.World.orig_TotalShortCutLengthBetweenTwoConnectedRooms_AbstractRoom_AbstractRoom orig, World self, AbstractRoom room1, AbstractRoom room2)
		{
			NoWarning = true;
			var re = orig(self, room1, room2);
			NoWarning = false;
			return re;
		}

		private static WorldCoordinate World_NodeInALeadingToB_AbstractRoom_AbstractRoom(On.World.orig_NodeInALeadingToB_AbstractRoom_AbstractRoom orig, World self, AbstractRoom roomA, AbstractRoom roomB)
		{
			NoWarning = true;
			var re = orig(self, roomA, roomB);
			NoWarning = false;
			return re;
		}
		private static void FlyAI_FleeFromRainUpdate(On.FlyAI.orig_FleeFromRainUpdate orig, FlyAI self)
		{
			NoWarning = true;
			orig(self);
			NoWarning = false;
		}


		private static void AbstractCreatureAI_RandomMoveToOtherRoom(ILContext il)
		{
			var roomIndex = new VariableDefinition(il.Body.Method.Module.TypeSystem.Int32);
			il.Body.Variables.Add(roomIndex);


			int index = -1;
			{
				ILCursor c = new(il);
				c.GotoNext(MoveType.Before,
					i => i.MatchLdloc(out index),
					i => i.MatchLdfld<WorldCoordinate>("room"),
					i => i.MatchCallvirt<AbstractRoom>("ExitIndex"));
			}

			ILCursor c2 = new(il);
			c2.GotoNext(MoveType.After,i => i.MatchCallvirt<AbstractRoom>("ExitIndex"));
			while(c2.TryGotoNext(i => i.MatchCallvirt<AbstractRoom>("ExitIndex")))
			{
				c2.GotoPrev(MoveType.After, _ => true);
				c2.Emit(OpCodes.Stloc, roomIndex);
				c2.Emit(OpCodes.Dup);
				c2.Emit(OpCodes.Ldloc, index);
				c2.EmitDelegate((AbstractRoom self, WorldCoordinate targetRoom) =>
				{
					if (self.TryGetExtension(out var extender))
						extender.SetFromConnectionIndex(targetRoom.abstractNode);
				});
				c2.Emit(OpCodes.Ldloc, roomIndex);
				c2.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<AbstractRoom>("ExitIndex"));

			}
			//foreach (var i in il.Instrs)
			//	i.DebugPrint();
		}

		private static void MapPage_SaveMapConfig(ILContext il)
		{
			ILCursor c = new(il);
			var roomIndex = new VariableDefinition(il.Body.Method.Module.TypeSystem.Int32);
			il.Body.Variables.Add(roomIndex);
			int index = -1;

			c.GotoNext(MoveType.After,
				i => i.MatchLdfld<DevInterface.RoomPanel>("roomRep"),
				i => i.MatchLdfld<DevInterface.MapObject.RoomRepresentation>("room"),
				i => i.MatchLdfld<AbstractRoom>("connections"),
				i => i.MatchLdloc(out index));

			c.GotoNext(MoveType.Before, i => i.MatchCallvirt<AbstractRoom>("ExitIndex"));
			c.Emit(OpCodes.Stloc, roomIndex);
			c.Emit(OpCodes.Dup);
			c.Emit(OpCodes.Ldloc, index);
			c.EmitDelegate((AbstractRoom self, int targetRoom) =>
			{
				if (self.TryGetExtension(out var extender))
					extender.SetFromConnectionIndex(targetRoom);
			});
			c.Emit(OpCodes.Ldloc, roomIndex);

		}

		private static void RoomTransition_PlayerEnterShortcut(ILContext il)
		{
			var local = new VariableDefinition(il.Body.Method.Module.TypeSystem.Int32);
			var roomIndex = new VariableDefinition(il.Body.Method.Module.TypeSystem.Int32);
			il.Body.Variables.Add(local);
			il.Body.Variables.Add(roomIndex);

			ILCursor c = new(il);

			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate((ShortcutData shortcut) => shortcut.destNode);
			c.Emit(OpCodes.Stloc, local);

			while(c.TryGotoNext(i => i.MatchCallOrCallvirt<AbstractRoom>("ExitIndex")))
			{
				c.GotoPrev(MoveType.After, _ => true);
				c.Emit(OpCodes.Stloc, roomIndex);
				c.Emit(OpCodes.Dup);
				c.Emit(OpCodes.Ldloc, local);
				c.EmitDelegate((AbstractRoom self, int targetRoom) =>
				{
					if (self.TryGetExtension(out var extender))
						extender.SetFromConnectionIndex(targetRoom);
				});
				c.Emit(OpCodes.Ldloc, roomIndex);
				c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<AbstractRoom>("ExitIndex"));
			}

		}



		public static void Common_ExitIndexIL(ILContext il)
		{
			Common_ExitIndexIL_Impl(il, 25);
		}
		public static void Common_ExitIndexIL_Impl(ILContext il, int maxCount = 25, int times = -1)
		{
			ILCursor c = new ILCursor(il);
			var roomIndex = new VariableDefinition(il.Body.Method.Module.TypeSystem.Int32);
			il.Body.Variables.Add(roomIndex);

			while (times != 0 && c.TryGotoNext(MoveType.Before,
				i => i.MatchCallOrCallvirt<AbstractRoom>("ExitIndex")))
			{
				c.GotoPrev(MoveType.After, _ => true);
				Plugin.LogDebug("--------------");
				Plugin.LogDebug($"Pre Match At: {il.Method.FullName}:{c.Next.Offset}");
				bool isSuccess = false;
				Instruction instr = c.Previous;
				int preIndex = 0;
				while (instr.Previous != null && preIndex < maxCount)
				{

					if(instr.MatchLdfld<AbstractRoom>("connections"))
					{
						int tmpStack = 0;
						var end = instr = instr.Next;
					
						while (!end.MatchLdelemI4())
						{
							if (end.Next == null || end.IsBranchInstruction())
							{
								if (end.Next == null) Plugin.LogWarning($"Failed to build ExitIndex: {il.Method.FullName}, Can't find LdelemI4");
								else Plugin.LogWarning($"Failed to build ExitIndex: {il.Method.FullName}, found Branch");
								tmpStack = -1;
								break;
							}
							end.ComputeStackDelta(ref tmpStack);
							end = end.Next;
							
						}

						if (tmpStack != 1)
						{
							Plugin.LogWarning($"Failed to build ExitIndex: {il.Method.FullName}, Stack overflow");
							for (var i = instr; i != end; i = i.Next)
								Plugin.LogDebug(i);
						}
						else
						{
							Plugin.LogDebug($"Match At: {il.Method.FullName}:{end.Next.Offset}");
							c.Emit(OpCodes.Stloc, roomIndex);
							c.Emit(OpCodes.Dup);
							for (var i = instr; i != end; i = i.Next)
								c.Emit(i.OpCode, i.Operand);
							c.EmitDelegate((AbstractRoom self, int targetRoom) =>
							{
								if (self.TryGetExtension(out var extender))
									extender.SetFromConnectionIndex(targetRoom);
							});
							c.Emit(OpCodes.Ldloc, roomIndex);
							Plugin.LogDebug($"Finish Emit");
							isSuccess = true;
						}
						break;
					}
					instr = instr.Previous;
					preIndex++;
				}
				c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<AbstractRoom>("ExitIndex"));
				times--;

				if (!isSuccess)
					Plugin.LogWarning($"Failed to find connection index in ExitIndex: {il.Method.FullName}, No Ldfld AbstractRoom::connections");
			}
		}



		private static int AbstractRoom_ExitIndex(On.AbstractRoom.orig_ExitIndex orig, AbstractRoom self, int targetRoom)
		{
			int result = orig(self, targetRoom);
			if(self.TryGetExtension(out var extender))
				extender.ExitIndex(targetRoom, ref result);

			return result;
		}

		private static void AbstractRoom_ctor(On.AbstractRoom.orig_ctor orig, AbstractRoom self, string name, 
			int[] connections, int index, int swarmRoomIndex, int shelterIndex, int gateIndex)
		{
			orig(self, name, connections, index, swarmRoomIndex, shelterIndex, gateIndex);
			if(!abstractRoomExtensions.TryGetValue(self, out var module))
				abstractRoomExtensions.Add(self, new AbstractRoomExtension(self));
		}

		private static void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
		{
			orig(self, game, region, name, singleRoomWorld);
			if (!worldExtensions.TryGetValue(self, out var module))
				worldExtensions.Add(self, new WorldExtension(self));
		}

		public readonly static ConditionalWeakTable<World, WorldExtension> worldExtensions = new();
		public readonly static ConditionalWeakTable<AbstractRoom, AbstractRoomExtension> abstractRoomExtensions = new();


	}

	public class AbstractRoomExtension(AbstractRoom owner) : Extension<AbstractRoom>(owner)
	{
		public void Init()
		{
			if (!ownerRef.TryGetTarget(out var self) || GamePlayHooks.NoWarning)
				return;
			extendIndex = self.world.TryGetExtension(out var worldExtender) && 
				worldExtender.extendIndex.TryGetValue(self.index, out var map) ?
				map.ToDictionary(i => i.Key, i => i.Value) :
				new();

			//if (extendIndex.Count != 0)
			//{
			//	Plugin.LogDebug(self.name);
			//	foreach (var a in extendIndex)
			//	{
			//		Plugin.LogDebug(self.world.GetAbstractRoom(a.Key));
			//		foreach (var b in a.Value)
			//			Plugin.LogDebug($"----{b.Key},{b.Value}");
			//	}
			//}
		}


		public void SetFromConnectionIndex(int fromConnectionIndex)
		{
			this.fromConnectionIndex = fromConnectionIndex;
		}

		public void ExitIndex(int targetRoom, ref int result)
		{
			if (!ownerRef.TryGetTarget(out var self) || GamePlayHooks.NoWarning)
				return;
			
			if (fromConnectionIndex == -1)
			{
				StackTrace stackTrace = new StackTrace();
				Plugin.LogWarning($"AbstractRoom::ExitIndex, Room:{self.name}, target room:{self.world.GetAbstractRoom(targetRoom).name}");
				Plugin.LogDebug(stackTrace.GetFrame(3));
			}
			if (extendIndex.TryGetValue(targetRoom, out var map) && map.TryGetValue(fromConnectionIndex, out var index))
			{
				result = index;
				Plugin.LogDebug($"{self.world.GetAbstractRoom(targetRoom).name}:{fromConnectionIndex}->{self.name}:{result}");

			}
			fromConnectionIndex = -1;
		}
		private int fromConnectionIndex = -1;

		// TargetRoomIndex, TargetConnectionIndex, ToConnectionIndex(self room)
		public Dictionary<int, Dictionary<int,int>> extendIndex;
	}

	public class WorldExtension(World owner) : Extension<World>(owner)
	{

		public Dictionary<int, Dictionary<int, Dictionary<int, int>>> extendIndex = new();

	}
}
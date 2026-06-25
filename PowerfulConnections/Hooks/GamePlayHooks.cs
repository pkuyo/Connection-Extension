using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoWeaver.Cecil;
using MonoWeaver.Patterns;
using PowerfulConnections.Helpers;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
			IL.AbstractCreatureAI.RandomMoveToOtherRoom += Common_ExitIndexIL;
			IL.VoidSpawnKeeper.Initiate += Common_ExitIndexIL;
			IL.Creature.NewTile += Common_ExitIndexIL;
			IL.ThreatTracker.FleeTo_WorldCoordinate_int_int_bool_bool += Common_ExitIndexIL;
			IL.PathFinder.ConnectAITile += Common_ExitIndexIL;
			IL.PathFinder.ConnectionsOfAbstractNodeInRealizedRoom += Common_ExitIndexIL;
			IL.PathFinder.DestinationExit += Common_ExitIndexIL;
			IL.AbstractSpacePathFinder.Path += Common_ExitIndexIL;
			IL.AbstractSpaceNodeFinder.Update += Common_ExitIndexIL;
		    IL.Tracker.Ghost.Update += Common_ExitIndexIL;
			IL.MissionTracker.LeaveRoom.Act += Common_ExitIndexIL;
			IL.ShortcutHandler.Update += Common_ExitIndexIL;
            IL.HUD.RoomTransition.PlayerEnterShortcut += Common_ExitIndexIL;
			IL.DevInterface.MapPage.SaveMapConfig += Common_ExitIndexIL;	
			IL.AbstractCreatureAI.RandomMoveToOtherRoom += Common_ExitIndexIL;

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
			try
			{
				var re = orig(self, from, to, crit);
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
                        re = Mathf.Min(re, num);
                }

                return re;
            }
			finally
			{
				NoWarning = false;
			}
		}



		private static float Custom_BetweenRoomsDistance(On.RWCustom.Custom.orig_BetweenRoomsDistance orig, World world, WorldCoordinate a, WorldCoordinate b)
		{
			try
			{
                NoWarning = true;
                var re = orig(world, a, b);

				var roomA = world.GetAbstractRoom(a);
				var roomB = world.GetAbstractRoom(b);
				if (a.room == b.room || roomA.ExitIndex(b.room) < 0 || roomB.ExitIndex(a.room) < 0 ||
					(roomA.realizedRoom == null && roomB.realizedRoom == null) ||
					!roomA.TryGetExtension(out var extend) ||
					!extend.extendIndex.TryGetValue(roomB.index, out var map))
					return re;

				float num = world.GetAbstractRoom(a).size.ToVector2().magnitude;
				float num2 = world.GetAbstractRoom(b).size.ToVector2().magnitude;

				foreach (var i in map)
				{
					if (roomA.realizedRoom != null && roomA.realizedRoom.shortCutsReady)
						num = Mathf.Min(num, a.Tile.FloatDist(roomA.realizedRoom.LocalCoordinateOfNode(i.Value).Tile));

					if (roomB.realizedRoom != null && roomB.realizedRoom.shortCutsReady)
						num2 = Mathf.Min(num2, b.Tile.FloatDist(roomB.realizedRoom.LocalCoordinateOfNode(i.Key).Tile));

				}

				return num + num2;
			}
			finally
			{
                NoWarning = false;
            }

        }

		private static void ThreatCreature_Update(ILContext il)
		{
			ILCursor c = new(il);
			c.EmitDelegate(() =>
			{
				NoWarning = true;
			});

			var match = c.Method.Match(Cil.Condition(() => P.Local<int>("exitDen") > -1)).Single();
			match.Observe((bool _, ThreatTracker.ThreatCreature self, int index) =>
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
					if (Custom.DistLess(crit.Room.realizedRoom.ShortcutLeadingToNode(i).startCoord, crit.pos, minDist))
					{
						minDist = Custom.Dist(crit.Room.realizedRoom.ShortcutLeadingToNode(i).startCoord.Tile.ToVector2(),
							crit.pos.Tile.ToVector2());
						index = i;
					}
				}
				return index;

			}, config => config.Arg(0).Capture(match.Captures.Value("exitDen"))).Store(match.Captures.Value("exitDen")).Apply();


        }

		public static bool NoWarning { get; private set; } = false;

		private static int World_TotalShortCutLengthBetweenTwoConnectedRooms_AbstractRoom_AbstractRoom(
			On.World.orig_TotalShortCutLengthBetweenTwoConnectedRooms_AbstractRoom_AbstractRoom orig, World self, AbstractRoom room1, AbstractRoom room2)
		{
			try
			{
				NoWarning = true;
				var re = orig(self, room1, room2);
				return re;
			}
			finally
			{
                NoWarning = false;
            }
        }

		private static WorldCoordinate World_NodeInALeadingToB_AbstractRoom_AbstractRoom(On.World.orig_NodeInALeadingToB_AbstractRoom_AbstractRoom orig, World self, AbstractRoom roomA, AbstractRoom roomB)
		{
			try
			{
				NoWarning = true;
				var re = orig(self, roomA, roomB);
				return re;
			}
			finally
			{
                NoWarning = false;
            }
		}
		private static void FlyAI_FleeFromRainUpdate(On.FlyAI.orig_FleeFromRainUpdate orig, FlyAI self)
		{
			try
			{
				NoWarning = true;
				orig(self);
			}
			finally
			{
                NoWarning = false;
            }
		}


	



        public static void Common_ExitIndexIL(ILContext il)
        {
			var model = il.Method.For();

            var connectMatches = model.Find(Cil.Value(
				() => P.Any<AbstractRoom>("room").connections[P.Any<int>("index")] ));

			var exitMatches = model.Find(Cil.Value(
				() => P.Any<AbstractRoom>("room").ExitIndex(P.Any<int>("index"))));

            if (connectMatches.Count == 0 || exitMatches.Count == 0)
            {
				Plugin.LogWarning($"'{il.Method.FullName}' can't find patterns, connection:{connectMatches.Count}, exit:{exitMatches.Count}");
				return;
			}

            var connectIndex = 0;
			Dictionary<ValueMatch, int> markedMatches = new Dictionary<ValueMatch, int>(exitMatches.Count);

            for (var i = 0; i < exitMatches.Count; i++)
            {
				while(connectIndex <= connectMatches.Count)
				{
					if (connectIndex == connectMatches.Count || connectMatches[connectIndex].ResultInstruction.Offset > exitMatches[i].ResultInstruction.Offset)
					{
						if (connectIndex == 0)
							break;
						var connectMatch = connectMatches[connectIndex - 1];
						if (markedMatches.TryGetValue(connectMatch, out var value)) markedMatches[connectMatch] = value + 1;
						else markedMatches[connectMatch] = 1;
						break;
                    }
                    connectIndex++;

                }
            }
			
			if (markedMatches.Count == 0)
			{
                Plugin.LogWarning($"'{il.Method.FullName}' can't find marked matched patterns. {exitMatches.Count}, {connectMatches.Count}");
                return;
            }

			foreach (var pair in markedMatches)
			{
				pair.Key.Observe
				(static (int _ , AbstractRoom room, int connectionIndex, int count) =>
				{
                    if (room.world.GetAbstractRoom(room.connections[connectionIndex]).TryGetExtension(out var extender))
					   extender.SetFromConnectionIndex(connectionIndex, count);
				},
				args => args
					.Capture(pair.Key.Captures.Value("room"))
					.Capture(pair.Key.Captures.Value("index"))
					.Constant(pair.Value)).Apply();
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
			if (!ownerRef.TryGetTarget(out var self))
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


		public void SetFromConnectionIndex(int fromConnectionIndex, int useCount)
		{
			this.fromConnectionIndex = fromConnectionIndex;
			this.useCount = useCount;

        }

		public void ExitIndex(int targetRoom, ref int result)
		{
			if (!ownerRef.TryGetTarget(out var self) || GamePlayHooks.NoWarning)
				return;

			if (extendIndex == null)
			{
				StackTrace stackTrace = new StackTrace();
				Plugin.LogWarning($"AbstractRoom::ExitIndex, Room:{self.name}, target room:{self.world.GetAbstractRoom(targetRoom).name} , Null extend index");
				Plugin.LogWarning(stackTrace);
				return;
			}
		
			if (fromConnectionIndex == -1)
			{
				StackTrace stackTrace = new StackTrace();
				Plugin.LogWarning($"AbstractRoom::ExitIndex, Room:{self.name}, target room:{self.world.GetAbstractRoom(targetRoom).name}");
				Plugin.LogWarning(stackTrace.GetFrame(3));
			}
			if (extendIndex.TryGetValue(targetRoom, out var map) && map.TryGetValue(fromConnectionIndex, out var index))
			{
				result = index;
				Plugin.LogWarning($"{self.world.GetAbstractRoom(targetRoom).name}:{fromConnectionIndex}->{self.name}:{result}");

			}
			useCount--;

            if (useCount < 0)
			{
                fromConnectionIndex = -1;
				useCount = 0;
            }
	
		}
		private int fromConnectionIndex = -1;
		private int useCount = 0;

		// TargetRoomIndex, TargetConnectionIndex, ToConnectionIndex(self room)
		public Dictionary<int, Dictionary<int,int>> extendIndex;
	}

	public class WorldExtension(World owner) : Extension<World>(owner)
	{
		public Dictionary<int, Dictionary<int, Dictionary<int, int>>> extendIndex = new();

	}
}
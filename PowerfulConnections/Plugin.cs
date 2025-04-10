
using BepInEx;
using System.IO;
using System.Linq;
using System;
using System.Security.Permissions;
using UnityEngine;
using PowerfulConnections.Hooks;
using System.Collections.Concurrent;
using ConnectionExtension;
using PowerfulConnections.Helpers;
using MonoMod.ModInterop;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace PowerfulConnections
{


	[BepInPlugin(ModId, Name, Version)]
	internal class Plugin : BaseUnityPlugin
	{

		public const string Version = "1.0.2";

		public const string Name = "Connection Extension";

		public const string ModId = "ConnectionExtension";
		public void OnEnable()
		{
			On.RainWorld.OnModsInit += RainWorld_OnModsInit; 
			typeof(ExtendHelperExport).ModInterop();
			instance = this;
		}

		public static Option option;

		private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			try
			{
				orig(self);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			if (!isLoaded)
			{
				try
				{
					option = new Option();

					WorldLoaderHooks.OnModsInit();
					GamePlayHooks.OnModsInit();
					MachineConnector.SetRegisteredOI(ModId, option);

					isLoaded = true;
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		private static bool isLoaded;


		private void Update()
		{
			while (logQueue.TryDequeue(out var logAction))
				logAction.Invoke();
			
		}



		public static void LogDebug(object m) => EnqueueLog(() => 
		{ 
			if(option.EnableDebug.Value)
				Debug.Log($"[{ModId}] [DEBUG] {m}");
		});

		public static void Log(object m) => EnqueueLog(() => Debug.Log($"[{ModId}] {m}"));
		public static void LogWarning(object m) => EnqueueLog(() => Debug.LogWarning($"[{ModId}] {m}"));
		public static void LogError(object m) => EnqueueLog(() => 
		{
			Debug.LogError($"[{ModId}] {m}");
			Debug.Log($"[{ModId}] [ERROR] {m}");
		});

		private static void EnqueueLog(Action logAction)
		{

			logQueue.Enqueue(logAction);
		}

		private static Plugin instance;
		private static readonly ConcurrentQueue<Action> logQueue = new ConcurrentQueue<Action>();
	}
}

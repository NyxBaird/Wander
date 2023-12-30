using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Nautilus.Commands;
using UnityEngine;
using UWE;

namespace Wander
{
    //This is where we'll initialize everything
    [HarmonyPatch(typeof(Player))]
    [HarmonyPatch("Awake")]
    public class Init
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            var globals = Globals.Instance;
            globals.vagabondManager = new VagabondManager();
            
            globals.Player = __instance;
            __instance.gameObject.AddComponent<PlayerMovementTracker>();

            CoroutineHost.StartCoroutine(DeferredVagabondInitialization(__instance));
        }
        
        private static IEnumerator DeferredVagabondInitialization(Player playerInstance)
        {
            // Wait for one frame to ensure Player's Awake method has completed
            yield return null;

            var globals = Globals.Instance;
            globals.Player = playerInstance;

            var helpers = Helpers.Instance;
            
            helpers.DevLog("Spawning vagabonds...");
            
            globals.vagabondManager.LoadVagabonds();

            var vagabonds = new string[]
            {
                "KooshReaper"
            };
            
            foreach (var key in vagabonds)
                if (!globals.vagabondManager.PendingSpawns.ContainsKey(key) && !globals.spawnedVagabondsByName.ContainsKey(key))
                    switch (key)
                    {
                        case "KooshReaper":
                            globals.vagabondManager.QueueVagabondSpawn(key, TechType.ReaperLeviathan, new Vector3(1173, -319, 902));
                            break;
                    }
            
            globals.vagabondManager.TrySpawnPendingVagabonds();
        }
    }

     // Handle all our time/light related details
     [HarmonyPatch(typeof(DayNightCycle))]
     [HarmonyPatch("Update")]
     public class Time
     {
         public static float OfDay;
         private static float _lightScalar;
         
         [HarmonyPostfix]
         public static void Postfix(DayNightCycle __instance)
         {
             _lightScalar = __instance.GetLightScalar();            
             OfDay = __instance.GetDayScalar();
         }
         
         //is eclipse if lightscalar dips below 5 between dayscalar of .15 and .85 
         public bool IsEclipse()
         {
             return (_lightScalar < 5 && (OfDay > .15 && OfDay < .85));
         }
     }
    
     [HarmonyPatch(typeof(IngameMenu), "SaveGameAsync")]
     public class SaveGamePatch
     {
         [HarmonyPostfix]
         public static void Postfix()
         {
             Helpers.Instance.DevLog("Woooo!! Saving!!!");
             Globals.Instance.vagabondManager.SaveVagabonds();
         }
     }
     
     [HarmonyPatch(typeof(SaveLoadManager), "ClearSlotAsync")]
     public class ClearSlotAsync
     {
         [HarmonyPostfix]
         public static void Postfix()
         {
             Helpers.Instance.DevLog("Clearing Wander files for save " + SaveLoadManager.main.GetCurrentSlot());
             VagabondManager.DeleteVagabondSaveFile(SaveLoadManager.main.GetCurrentSlot());
         }
     }
     

     // [HarmonyPatch(typeof(CellManager), "QueueForSleep")]
     // public class ManagerQueueForSleep
     // {
     //     [HarmonyPostfix]
     //     public static void Postfix(IQueue<EntityCell> cell)
     //     {
     //         Helpers.Instance.DevLog("Queueing cell manager for sleep " + cell);
     //     }
     // }
     [HarmonyPatch(typeof(EntityCell), "QueueForSleep")]
     public class QueueForSleep
     {
         [HarmonyPostfix]
         public static void Postfix(EntityCell __instance, IQueue<EntityCell> queue)
         {
             Helpers.Instance.DevLog("Unloading queue " + queue);
         }
     }
     
     // [HarmonyPatch(typeof(BatchCells), "QueueForSleep")]
     // public class QueueBatchCellsForSleep
     // {
     //     [HarmonyPostfix]
     //     public static void Postfix(Int3.Bounds bsRange, int level, IQueue<EntityCell> queue)
     //     {
     //         Helpers.Instance.DevLog("BatchCells.QueueForSleep: " + level);
     //     }
     // }
     // [HarmonyPatch(typeof(LargeWorldStreamer), "UnloadBatch")]
     // public class UnloadBatch
     // {
     //     [HarmonyPostfix]
     //     public static void Postfix(Int3 index)
     //     {
     //         Helpers.Instance.DevLog("LargeWorldStreamer.UnloadBatch Unloading batch " + index);
     //     }
     // }
     
     
}
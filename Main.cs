using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Commands;
using Nautilus.Handlers;
using UnityEngine;

namespace Wander
{
    [BepInDependency("com.snmodding.nautilus")]
    [BepInPlugin(Globals.MyGuid, Globals.PluginName, Globals.VersionString)]
    public class Wander : BaseUnityPlugin
    {
        private static readonly Harmony Harmony = new Harmony(Globals.MyGuid);

        private void Start()
        {
            ConsoleCommandsHandler.RegisterConsoleCommand<WanderCmd>("wander", (name) =>
            {
                var helpers = Helpers.Instance;
                
                if (!string.IsNullOrEmpty(name)) {
                    if (name == "load") {
                        Helpers.Instance.DevLog("Pre-Loading vagabonds...");
                        Globals.Instance.vagabondManager.LoadVagabonds();
                        return "Loading...";
                    }
                    
                    Vagabond vagabond = helpers.GetspawnedVagabondsByName(name);
                    if (vagabond != null)
                        return $"Found creature " + name;
                    
                    return $"Couldn't find vagabond {name}";
                }
                
                return $"Parameters: {name}";
            });
        }
        private delegate string WanderCmd(string myString);
        
        /// <summary>
        /// Initialise the configuration settings and patch methods
        /// </summary>
        private void Awake()
        {
            //create our instance of globals
            new Globals();
            Globals.Instance.Log = Logger;
            
            Logger.LogInfo($"PluginName: {Globals.PluginName}, VersionString: {Globals.VersionString} is loading...");
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {Globals.PluginName}, VersionString: {Globals.VersionString} is loaded.");
        }
    }
}
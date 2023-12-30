using System.Collections.Generic;
using BepInEx.Logging;

namespace Wander
{
    public class Globals
    {
        public bool DeveloperMode = true;
        
        
        private static Globals _instance;
        public static Globals Instance
        {
            get
            {
                if (_instance is null)
                    _instance = new Globals();
                return _instance;
            }
        }
        
        public const string MyGuid = "me.nyxb.wander";
        public const string PluginName = "Wander";
        public const string VersionString = "0.1";
        
        public ManualLogSource Log { get; set; }

        public Player Player;

        public VagabondManager vagabondManager;
        
        public Dictionary<string, Vagabond> spawnedVagabondsByName = new Dictionary<string, Vagabond>();
    }
}
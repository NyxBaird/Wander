using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = HarmonyLib.Tools.Logger;

namespace Wander
{
    public class Helpers
    {
        private Globals _globals;
        
        private static Helpers _instance;
        public static Helpers Instance
        {
            get
            {
                if (_instance is null)
                    _instance = new Helpers();
                
                return _instance;
            }
        }
        
        public Helpers()
        {
            _globals = Globals.Instance;
        }
        
        public void DevLog(string message)
        {
            if (_globals.DeveloperMode)
                _globals.Log.LogDebug(message);
        }
        
        // Method to add a Vagabond to the dictionary
        public void AddVagabond(string name, Vagabond vagabond)
        {
            if (!_globals.spawnedVagabondsByName.ContainsKey(name))
            {
                _globals.spawnedVagabondsByName.Add(name, vagabond);
            }
            else
            {
                DevLog($"Vagabond with name {name} already exists.");
            }
        }

        // Method to remove a Vagabond from the dictionary by name
        public bool RemoveVagabond(string name)
        {
            return _globals.spawnedVagabondsByName.Remove(name);
        }

        // Method to get a Vagabond by name
        public Vagabond GetspawnedVagabondsByName(string name)
        {
            _globals.spawnedVagabondsByName.TryGetValue(name, out Vagabond vagabond);
            return vagabond;
        }
    }
    
    public class PlayerMovementTracker : MonoBehaviour
    {
        private Vector3 lastPosition;
        private const float MovementThreshold = 5f; // 5 meters

        void Start()
        {
            // Initialize lastPosition with the current player position
            lastPosition = transform.position;
        }

        void Update()
        {
            // Calculate the distance the player has moved since the last check
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);

            // If the player has moved 5 or more meters, run the code
            if (distanceMoved >= MovementThreshold)
            {
                // Run your code here
                var globals = Globals.Instance;
                globals.vagabondManager.TrySpawnPendingVagabonds();

                // Update the last known position
                lastPosition = transform.position;
            }
        }
    }
}
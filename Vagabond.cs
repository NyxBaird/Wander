using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nautilus.Utility;
using Newtonsoft.Json;
using UnityEngine;
using UWE;
using Object = UnityEngine.Object;

namespace Wander
{
    //This class refers to SPAWNED vagabonds only
    public class Vagabond : MonoBehaviour
    {
        public string ID { get; private set; }
        public List<Vector3> Stops { get; private set; }
        public GameObject Creature;
        public TechType TechType;
        
        public void Initialize(string id, TechType type, Vector3 spawnLocation)
        {
            Helpers.Instance.DevLog("Initializing new vagabond; " + id);

            ID = id;
            TechType = type;
            Stops = new List<Vector3> { spawnLocation };
            CoroutineHost.StartCoroutine(SpawnCreature(type, spawnLocation));
        }

        private void applyTraits(Color color)
        {
            //If this is run in developer mode then highlight our vagabonds
            if (Globals.Instance.DeveloperMode)
                _applyUniqueColor(Creature, color);
        }
        
        private IEnumerator SpawnCreature(TechType creatureType, Vector3 location)
        {
            // Get the prefab for the TechType asynchronously
            var task = CraftData.GetPrefabForTechTypeAsync(creatureType, false);
            yield return task;
            
            // After the task is completed, check if the prefab is available
            var prefab = task.GetResult();
            if (prefab is null)
            {
                Debug.LogError($"Failed to load prefab for TechType: {creatureType}");
                yield break; 
            }

            // Instantiate the prefab at the specified location
            Creature = Instantiate(prefab, location, Quaternion.identity);
            if (Creature is null)
            {
                Debug.LogError("Failed to instantiate creature prefab.");
                yield break; // Exit the coroutine if instantiation failed
            }

            // Set the name of the instantiated GameObject to the ID of the Vagabond
            Creature.name = ID;

            
            // Apply traits after instantiation
            applyTraits(Color.green);
            Debug.Log($"Successfully spawned vagabond: {ID} at {location}");
        }
        
        private void _applyUniqueColor(GameObject creature, Color color)
        {
            // Access the Renderer component of the creature
            var renderers = creature.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // Create a new Material with a unique color
                var uniqueMaterial = new Material(renderer.material);
                uniqueMaterial.color = Color.green; // Choose your unique color here
        
                // Assign the new material to the renderer
                renderer.material = uniqueMaterial;
            }
        }
        
        // Method to add a stop
        public void AddStop(Vector3 stopLocation)
        {
            Stops.Add(stopLocation);
        }
        
        // Method to remove a stop by index
        public void RemoveStopAtIndex(int index)
        {
            if (index >= 0 && index < Stops.Count)
                Stops.RemoveAt(index);
            
            throw new System.IndexOutOfRangeException("Index is out of range.");
        }
        
        // Method to get a stop by index
        public Vector3 GetStopAtIndex(int index)
        {
            if (index >= 0 && index < Stops.Count)
                return Stops[index];
            
            throw new System.IndexOutOfRangeException("Index is out of range.");
        }
        
        public void SetStopAtIndex(int index, Vector3 stopLocation)
        {
            if (index >= 0) {
                if (index < Stops.Count)
                    Stops[index] = stopLocation;
                else
                    throw new ArgumentOutOfRangeException("Trying to set stop without start.");
            } else 
                throw new ArgumentOutOfRangeException("Index cannot be negative.");
        }
    }

    //This class will handle the loading/saving/spawning & other management of vagabonds
    public class VagabondManager : MonoBehaviour
    {
        public Dictionary<string, (TechType type, Vector3 spawnLocation)> PendingSpawns = new Dictionary<string, (TechType, Vector3)>();
        public static float MAX_SPAWN_DISTANCE = 300.0f;

        public void QueueVagabondSpawn(string id, TechType type, Vector3 location)
        {
            if (Globals.Instance.spawnedVagabondsByName.ContainsKey(id))
            {
                Helpers.Instance.DevLog("Trying to QueueVagabondSpawn when Globals.spawnedVagabondsByName already has that vagabond registered");
                return;
            }

            if (!PendingSpawns.ContainsKey(id))
                PendingSpawns[id] = (type, location);
        }

        public void TrySpawnPendingVagabonds()
        {
            var globals = Globals.Instance;
            var helpers = Helpers.Instance;
            var playerPosition = Globals.Instance.Player.transform.position;
            var keysToRemove = new List<string>();

            foreach (var pending in PendingSpawns.ToList())
            {
                var distanceToPlayer = Vector3.Distance(pending.Value.spawnLocation, playerPosition);
                if (distanceToPlayer > MAX_SPAWN_DISTANCE)
                    continue;

                helpers.DevLog("Trying to spawn " + pending.Key + " at distance; " + distanceToPlayer);

                // Create a new GameObject and add the Vagabond component to it
                var vagabondObject = new GameObject(pending.Key);
                var vagabondComponent = vagabondObject.AddComponent<Vagabond>();
                vagabondComponent.Initialize(pending.Key, pending.Value.type, pending.Value.spawnLocation);

                globals.spawnedVagabondsByName[pending.Key] = vagabondComponent;
                keysToRemove.Add(pending.Key); // Mark this key for removal
            }

            // Remove newly spawned vagabonds from our pending spawns list  
            foreach (var key in keysToRemove)
                PendingSpawns.Remove(key);
        }

        private static string SaveFilePath => GetSaveFilePath(SaveLoadManager.main.GetCurrentSlot());
        public void SaveVagabonds()
        {
            var helpers = Helpers.Instance;
            helpers.DevLog("Saving Wander data to " + SaveFilePath);

            if (!Globals.Instance.spawnedVagabondsByName.Any())
            {
                helpers.DevLog("No vagabonds to save.");
                return;
            }

            var dataList = Globals.Instance.spawnedVagabondsByName.Values.Select(vagabond => new VagabondSaveData(vagabond.ID, vagabond.Creature.transform.position, vagabond.TechType)).ToList();

            var json = JsonConvert.SerializeObject(dataList, Formatting.Indented);
            File.WriteAllText(SaveFilePath, json);
            helpers.DevLog("Successfully saved JSON to file.");
        }

        public void LoadVagabonds()
        {
            Helpers.Instance.DevLog("Loading vagabonds...");
            
            if (!File.Exists(SaveFilePath))
            {
                Helpers.Instance.DevLog("No vagabond save file found!");
                return;
            }
            
            var json = File.ReadAllText(SaveFilePath);
            try
            {
                var dataList = JsonConvert.DeserializeObject<List<VagabondSaveData>>(json);
                foreach (var vagabondData in dataList)
                {
                    var position = new Vector3(vagabondData.x, vagabondData.y, vagabondData.z);
                    var techType = (TechType)Enum.Parse(typeof(TechType), vagabondData.TechTypeName);
                    Helpers.Instance.DevLog($"Loaded {techType}:{vagabondData.ID} @ {position.x},{position.y},{position.z}");
                    QueueVagabondSpawn(vagabondData.ID, techType, position);
                }
            }
            catch (JsonSerializationException ex)
            {
                Helpers.Instance.DevLog("Caught " + ex.Message);
            }
        }
        
        public static void DeleteVagabondSaveFile(string slotNumber)
        {
            var saveFilePath = GetSaveFilePath(slotNumber);
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Helpers.Instance.DevLog($"Deleted vagabond save file for slot {slotNumber}.");
            }
            else
            {
                Helpers.Instance.DevLog($"No vagabond save file found for slot {slotNumber} to delete.");
            }
        }

        private static string GetSaveFilePath(string slotNumber)
        {
            return Path.Combine("BepInEx/Plugins/Wander/", slotNumber + "-wanderSaveData.json");
        }
    }

    [System.Serializable]
    public class VagabondSaveData
    {
        public string ID;
        public float x;
        public float y;
        public float z;
        public string TechTypeName; // Store the TechType as a string

        public VagabondSaveData(string id, Vector3 position, TechType techType)
        {
            ID = id;
            x = position.x;
            y = position.y;
            z = position.z;
            TechTypeName = techType.ToString(); // Convert the TechType to a string
        }
    }
}
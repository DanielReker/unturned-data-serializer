using System;
using System.Collections.Generic;
using SDG.Unturned;
using SDG.Framework.Modules;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HarmonyLib;
using SDG.Framework.Devkit;
using SDG.Framework.IO.FormattedFiles;
using SDG.Framework.Water;
using UnityEngine;

namespace UnturnedDataSerializer {
    public class Main : IModuleNexus {
        public static List<JObject> assets = new List<JObject>();

        private static readonly JsonSerializerSettings _json = new JsonSerializerSettings() {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            Converters = new JsonConverter[] {
                new ColorJsonConverter(),
                new Vector3JsonConverter(),
                new DatValueJsonConverter(),
                //new DatDictionaryJsonConverter(),
                //new AssetDefinitionJsonConverter(),
                new LevelObjectJsonConverter(),
                new ResourceSpawnpointJsonConverter(),
                new AssetJsonConverter(),
                new GUIDJsonConverter()
            }
        };

        public static JsonSerializer serializer = JsonSerializer.Create(_json);

        public static JToken SerializeToken<T>(T token) {
            if (token != null)
                return JToken.FromObject(token, Main.serializer);
            else
                return JValue.CreateNull();
        }

        public static JObject SerializeObject<T>(T token) {
            if (token != null)
                return JObject.FromObject(token, Main.serializer);
            else
                return new JObject();
        }


        public static void SerializeToFile(object obj, string path) {
            using (var streamWriter = File.CreateText(path))
            using (var jsonWriter = new JsonTextWriter(streamWriter)) {
                //CommandWindow.Log(String.Format("Exporting {0} to {1}", obj.GetType().FullName, path));
                jsonWriter.Formatting = Formatting.Indented;
                serializer.Serialize(jsonWriter, obj);
            }
        }

        static JContainer UnwrapRegions<T>(List<T>[,] objects) {
            var serializedObjects = new JArray();

            for (int regionX = 0; regionX < objects.GetLength(0); regionX++) {
                for (int regionY = 0; regionY < objects.GetLength(1); regionY++) {
                    foreach (var obj in objects[regionX, regionY]) {
                        try {
                            var serializedObject = JObject.FromObject(obj, serializer);
                        	serializedObject.Add("region", new JObject(new JProperty("x", regionX), new JProperty("y", regionY)));
                        	serializedObjects.Add(serializedObject);
                        } catch (Exception e) {
                            // TODO: Find out what causes exception when serializing California level objects
                            CommandWindow.LogError($"Exception in UnwrapRegions: {e.Message}");
                        }
                    }
                }
            }

            return serializedObjects;
        }

        public static void SerializeAssets(string directory) {
            foreach (var asset in assets) {
                if (asset["data"] != null && asset["data"]["GUID"] is JValue) {
                    string GUID = ((JValue)asset["data"]["GUID"]).Value as string;
                    SerializeToFile(asset, $"{directory}{GUID}.json");
                } else {
                    CommandWindow.LogError($"Asset {asset.ToString()} has no GUID");
                }
            }
        }

        public static void SerializeMapBounds(string directory) {
            CartographyVolume mainVolume = VolumeManager<CartographyVolume, CartographyVolumeManager>.Get().GetMainVolume();
            int imageWidth;
            int imageHeight;
            float captureWidth;
            float captureHeight;
            float terrainMinHeight;
            float terrainMaxHeight;
            GameObject GO = new GameObject();
            Transform satelliteCaptureTransform = GO.transform;
            if ((UnityEngine.Object)mainVolume != (UnityEngine.Object)null) {
                CommandWindow.Log("Cartography volume: found");
                Vector3 position;
                Quaternion rotation;
                mainVolume.GetSatelliteCaptureTransform(out position, out rotation);
                satelliteCaptureTransform.SetPositionAndRotation(position, rotation);
                Bounds worldBounds = mainVolume.CalculateWorldBounds();
                terrainMinHeight = worldBounds.min.y;
                terrainMaxHeight = worldBounds.max.y;
                Vector3 size = mainVolume.CalculateLocalBounds().size;
                imageWidth = Mathf.CeilToInt(size.x);
                imageHeight = Mathf.CeilToInt(size.z);
                captureWidth = size.x;
                captureHeight = size.z;
            } else {
                CommandWindow.Log("Cartography volume: not found");
                imageWidth = (int)Level.size;
                imageHeight = (int)Level.size;
                captureWidth = (float)Level.size - (float)Level.border * 2f;
                captureHeight = (float)Level.size - (float)Level.border * 2f;
                satelliteCaptureTransform.position = new Vector3(0.0f, 1028f, 0.0f);
                satelliteCaptureTransform.rotation = Quaternion.Euler(90f, 0.0f, 0.0f);
                terrainMinHeight = WaterVolumeManager.worldSeaLevel;
                terrainMaxHeight = Level.TERRAIN;
            }

            var bounds = new Bounds();
            Vector3 min = satelliteCaptureTransform.TransformPoint(
                new Vector3(-0.5f * captureWidth, -0.5f * captureHeight, terrainMinHeight)
            );
            Vector3 max = satelliteCaptureTransform.TransformPoint(
                new Vector3(0.5f * captureWidth, 0.5f * captureHeight, terrainMaxHeight)
            );
            bounds.Encapsulate(min);
            bounds.Encapsulate(max);

            SerializeToFile(new JObject(
                new JProperty("worldBounds", SerializeToken(bounds))
            ), Path.Combine(directory, "map_bounds.json"));
        }

        public static void WriteLevelHierarchy(IFormattedFileWriter writer) {
            uint availableInstanceID = (uint)AccessTools.Field(typeof(LevelHierarchy), "availableInstanceID").GetValue(null);
            writer.beginObject();
            writer.writeValue<uint>("Available_Instance_ID", availableInstanceID);
            writer.beginArray("Items");
            for (int index = 0; index < LevelHierarchy.instance.items.Count; ++index) {
                IDevkitHierarchyItem devkitHierarchyItem = LevelHierarchy.instance.items[index];
                if (devkitHierarchyItem.instanceID != 0U && devkitHierarchyItem.ShouldSave) {
                    writer.beginObject();
                    writer.writeValue("Type", devkitHierarchyItem.GetType().Name);
                    writer.writeValue<uint>("Instance_ID", devkitHierarchyItem.instanceID);
                    writer.writeValue<IDevkitHierarchyItem>("Item", devkitHierarchyItem);
                    writer.endObject();
                }
            }
            writer.endArray();
            writer.endObject();
        }

        private void onLevelLoaded(int level) {
            if (level <= Level.BUILD_INDEX_SETUP)
                return;

            string directory = "/app/output/";
            string mapDirectory = $"{directory}Maps/{Level.info.name}/";
            //string assetsDirectory = $"{directory}Assets/";
            Directory.CreateDirectory(mapDirectory);
            //Directory.CreateDirectory(assetsDirectory);

            SerializeToFile(LevelItems.tables, mapDirectory + "level_items_tables.json");
            SerializeToFile(UnwrapRegions(LevelItems.spawns), mapDirectory + "level_items_spawns.json");

            SerializeToFile(LevelAnimals.tables, mapDirectory + "level_animals_tables.json");
            SerializeToFile(LevelAnimals.spawns, mapDirectory + "level_animals_spawns.json");

            SerializeToFile(LevelZombies.tables, mapDirectory + "level_zombies_tables.json");
            SerializeToFile(LevelZombies.zombies, mapDirectory + "level_zombies.json");
            SerializeToFile(UnwrapRegions(LevelZombies.spawns), mapDirectory + "level_zombies_spawns.json");

            SerializeToFile(LevelVehicles.tables, mapDirectory + "level_vehicles_tables.json");
            SerializeToFile(LevelVehicles.spawns, mapDirectory + "level_vehicles_spawns.json");

            SerializeToFile(UnwrapRegions(LevelGround.trees), mapDirectory + "level_ground_trees.json");

            SerializeToFile(UnwrapRegions(LevelObjects.objects), mapDirectory + "level_objects.json");

            SerializeToFile(assets, mapDirectory + "assets.json");

            var assetMapping = AccessTools.Field(typeof(Assets), "currentAssetMapping").GetValue(null);
            var legacyAssetsTable = AccessTools.Field(assetMapping.GetType(), "legacyAssetsTable").GetValue(assetMapping);

            SerializeToFile(legacyAssetsTable, mapDirectory + "legacy_assets_table.json");

            var jTokenWriter = new JTokenWriter();
            WriteLevelHierarchy(new UnturnedJsonWriter(jTokenWriter));
            SerializeToFile(jTokenWriter.Token, mapDirectory + "level_hierarchy.json");

            File.Copy(Path.Combine(Level.info.path, "Chart.png"), mapDirectory + "Chart.png", true);
            File.Copy(Path.Combine(Level.info.path, "Map.png"), mapDirectory + "Map.png", true);

            SerializeMapBounds(mapDirectory);

            //SerializeAssets(assetsDirectory);


            CommandWindow.Log("Shutting down...");
            SDG.Unturned.SaveManager.save();
            SDG.Unturned.Provider.shutdown();
        }

        public void initialize() {
            Level.onLevelLoaded += new LevelLoaded(this.onLevelLoaded);

            Harmony harmony = new Harmony("unturnedtestmodule");
            harmony.PatchAll();
        }

        public void shutdown() {

        }
    }
}

using HarmonyLib;
using Newtonsoft.Json.Linq;
using SDG.Unturned;
using System.IO;
using System;

namespace UnturnedDataSerializer {
    [HarmonyPatch(typeof(Assets), "LoadFile")]
    internal class AssetsPatch {
        static JObject SerializeAssetData(DatDictionary assetData) {
            var dataSerialized = Main.SerializeObject(assetData);

            if (dataSerialized["Metadata"] != null && dataSerialized["Metadata"] is JObject) {
                dataSerialized.Merge(dataSerialized["Metadata"], new JsonMergeSettings() { MergeArrayHandling = MergeArrayHandling.Union });
                dataSerialized.Remove("Metadata");
            }

            if (dataSerialized["Asset"] != null && dataSerialized["Asset"] is JObject) {
                dataSerialized.Merge(dataSerialized["Asset"], new JsonMergeSettings() { MergeArrayHandling = MergeArrayHandling.Union });
                dataSerialized.Remove("Asset");
            }

            return dataSerialized;
        }


        static int assets = 0;
        static void Prefix(object file) {
            assets++;

            Type type = file.GetType();

            var path = (string)AccessTools.Field(type, "path").GetValue(file);

            string directoryName = Path.GetDirectoryName(path);
            string name = path.EndsWith("Asset.dat", StringComparison.OrdinalIgnoreCase) ? Path.GetFileName(directoryName) : Path.GetFileNameWithoutExtension(path);

            path = path.Substring(path.IndexOf('/') + 1);
            path = path.Substring(path.IndexOf('/') + 1);
            path = path.Substring(path.IndexOf('/') + 1);

            CommandWindow.Log($"Asset #{assets}: name: {name}, path: {path}");

            try {
                var assetData = (DatDictionary)AccessTools.Field(type, "assetData").GetValue(file);
                var translationData = (DatDictionary)AccessTools.Field(type, "translationData").GetValue(file);

                var assetSerialized = new JObject(
                    new JProperty("name", name),
                    new JProperty("path", path),
                    new JProperty("data", SerializeAssetData(assetData)),
                    new JProperty("translation", Main.SerializeToken(translationData))
                );

                Main.assets.Add(assetSerialized);
            } catch (Exception ex) {
                CommandWindow.Log($"EXCEPTION: {ex.Message}");
            }
        }
    }
}

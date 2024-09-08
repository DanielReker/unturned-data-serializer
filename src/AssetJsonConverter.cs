using Newtonsoft.Json;
using SDG.Unturned;
using System;

namespace UnturnedDataSerializer {
    internal class AssetJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return typeof(Asset).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var asset = (Asset)value;
            serializer.Serialize(writer, asset.GUID);
        }
    }
}
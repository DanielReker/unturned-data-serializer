using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDG.Unturned;
using System;

namespace UnturnedDataSerializer {
    internal class ResourceSpawnpointJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(ResourceSpawnpoint);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            ResourceSpawnpoint resourceSpawnpoint = (ResourceSpawnpoint)value;
            JObject o = new JObject(
                new JProperty("GUID", Main.SerializeToken(resourceSpawnpoint.guid)),
                new JProperty("point", Main.SerializeToken(resourceSpawnpoint.point))
            );
            o.WriteTo(writer);
        }
    }
}
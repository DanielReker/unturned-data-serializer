using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDG.Unturned;
using System;

namespace UnturnedDataSerializer {
    internal class LevelObjectJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(LevelObject);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            LevelObject levelObject = (LevelObject)value;
            JObject o = new JObject(
                new JProperty("GUID", Main.SerializeToken(levelObject.GUID)),
                new JProperty("instanceID", levelObject.instanceID),
                new JProperty("point", Main.SerializeToken(levelObject.transform.position))
            );
            o.WriteTo(writer);
        }
    }
}
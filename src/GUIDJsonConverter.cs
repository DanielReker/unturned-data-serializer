using Newtonsoft.Json;
using System;

namespace UnturnedDataSerializer {
    internal class GUIDJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Guid);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var GUID = (Guid)value;
            serializer.Serialize(writer, GUID.ToString("N"));
        }
    }
}
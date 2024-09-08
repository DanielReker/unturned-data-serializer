using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDG.Unturned;
using System;

namespace UnturnedDataSerializer {
    internal class DatValueJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(DatValue);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var datValue = (DatValue)value;

            if (datValue != null) {
                var jValue = JValue.FromObject(datValue.value, serializer);
                jValue.WriteTo(writer);
            } else {
                writer.WriteNull();
            }
        }
    }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

namespace UnturnedDataSerializer {
    internal class ColorJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Color);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            Color color = (Color)value;
            JObject o = new JObject(
                new JProperty("r", color.r),
                new JProperty("g", color.g),
                new JProperty("b", color.b)
            );
            o.WriteTo(writer);
        }
    }
}
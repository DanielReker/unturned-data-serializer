using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

namespace UnturnedDataSerializer {
    internal class Vector3JsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Vector3);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            Vector3 vector3 = (Vector3)value;
            JObject o = new JObject(
                new JProperty("x", vector3.x),
                new JProperty("y", vector3.y),
                new JProperty("z", vector3.z)
            );
            o.WriteTo(writer);
        }
    }
}
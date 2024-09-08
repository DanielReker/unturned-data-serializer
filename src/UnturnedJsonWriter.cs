using Newtonsoft.Json;
using SDG.Framework.IO.FormattedFiles;

namespace UnturnedDataSerializer {
    internal class UnturnedJsonWriter : IFormattedFileWriter {
        public JsonWriter jsonWriter { get; private set; }

        public UnturnedJsonWriter(JsonWriter jsonWriter) {
            this.jsonWriter = jsonWriter;
        }



        public void beginArray(string key) {
            this.writeKey(key);
            this.beginArray();
        }

        public void beginArray() {
            jsonWriter.WriteStartArray();
        }

        public void beginObject() {
            jsonWriter.WriteStartObject();
        }

        public void beginObject(string key) {
            this.writeKey(key);
            this.beginObject();
        }

        public void endArray() {
            jsonWriter.WriteEndArray();
        }

        public void endObject() {
            jsonWriter.WriteEndObject();
        }

        public void writeKey(string key) {
            jsonWriter.WritePropertyName(key);
        }

        public void writeValue(string key, string value) {
            this.writeKey(key);
            this.writeValue(value);
        }

        public void writeValue(string value) {
            jsonWriter.WriteValue(value);
        }

        public void writeValue(string key, object value) {
            this.writeKey(key);
            this.writeValue(value);
        }

        public void writeValue(object value) {
            if (value is IFormattedFileWritable)
                (value as IFormattedFileWritable).write((IFormattedFileWriter)this);
            else
                Main.serializer.Serialize(jsonWriter, value);
        }

        public void writeValue<T>(string key, T value) {
            this.writeKey(key);
            this.writeValue<T>(value);
        }

        public void writeValue<T>(T value) {
            this.writeValue((object)value);
        }
    }
}
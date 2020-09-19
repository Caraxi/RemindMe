using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace RemindMe.JsonConverters {
    class GeneralReminderConverter : JsonConverter {

        private readonly Type[] reminderTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(GeneralReminder))).ToArray();

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(GeneralReminder);
        }
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (!(reader.Value is string s)) return null;
            var t = reminderTypes.FirstOrDefault(t => t.Name == s);
            return t != null ? Activator.CreateInstance(t) : null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value is GeneralReminder r) writer.WriteValue(r.GetType().Name);
        }
    }
}

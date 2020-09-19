using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Newtonsoft.Json;
using RemindMe.Config;
using RemindMe.JsonConverters;
using RemindMe.Reminder;

namespace RemindMe {

    [JsonConverter(typeof(GeneralReminderConverter))]
    public class GeneralReminder {

        [JsonIgnore] public virtual string Name { get; } = "General Reminder";

        [JsonIgnore] public virtual string Description { get; } = "You shouldn't see this...";
        
        public virtual string GetText(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return "General Reminder";
        }

        public virtual bool ShouldShow(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return false;
        }

        public virtual ushort GetIconID(DalamudPluginInterface pluginInterface, RemindMe plugin, MonitorDisplay display) {
            return 0;
        }

        public override bool Equals(object obj) {
            return obj != null && obj.GetType() == this.GetType();
        }

    }
}

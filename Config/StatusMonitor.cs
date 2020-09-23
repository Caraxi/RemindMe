using Dalamud.Game.ClientState.Actors.Types;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace RemindMe.Config {
    public class StatusMonitor {
        public uint ClassJob;
        public uint Status;
        public uint Action;
        public float MaxDuration = 30;
        [JsonIgnore] public Status StatusData { get; set; }
        [JsonIgnore] public Action ActionData { get; set; }

        public override bool Equals(object obj) {
            if (!(obj is StatusMonitor sm)) return false;
            return sm.Status == this.Status && sm.ClassJob == this.ClassJob && sm.Action == this.Action;
        }

        public void ClickHandler(RemindMe plugin, object param) {
            if (param is Actor a) {
                plugin.PluginInterface.ClientState.Targets.SetCurrentTarget(a);
            }
            
        }
    }
}

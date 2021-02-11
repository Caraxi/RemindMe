namespace RemindMe.Config {
    public class CooldownMonitor {
        public uint ActionId = 0;
        public uint ClassJob = 0;

        public override bool Equals(object obj) {
            if (!(obj is CooldownMonitor cdm)) return false;
            return cdm.ActionId == this.ActionId && cdm.ClassJob == this.ClassJob;
        }
    }
}

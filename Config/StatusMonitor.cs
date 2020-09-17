namespace RemindMe.Config {
    public class StatusMonitor {
        public uint ClassJob;
        public uint Status;
        public uint Action;
        public float MaxDuration = 30;

        public override bool Equals(object obj) {
            if (!(obj is StatusMonitor sm)) return false;
            return sm.Status == this.Status && sm.ClassJob == this.ClassJob && sm.Action == this.Action;
        }

    }
}

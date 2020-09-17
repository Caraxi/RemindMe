using System.Numerics;

namespace RemindMe {
    internal class DisplayTimer {

        public Vector4 ProgressColor;
        public Vector4 FinishedColor;

        public float TimerMax;
        public float TimerCurrent;

        public string Name;
        public ushort IconId;

        public float TimerRemaining => TimerMax - TimerCurrent;

        public float TimerFractionComplete => TimerCurrent / TimerMax;
        public float TimerFractionRemaining => 1 - TimerFractionComplete;

    }
}

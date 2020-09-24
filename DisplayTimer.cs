using System;
using System.Numerics;

namespace RemindMe {
    public class DisplayTimer {

        public Vector4 ProgressColor;
        public Vector4 FinishedColor;

        public float TimerMax;
        public float TimerCurrent;

        public string Name;
        public ushort IconId;

        public bool AllowCountdown = true;

        public float TimerRemaining => TimerMax - TimerCurrent;

        public float TimerFractionComplete => TimerCurrent / TimerMax;
        public float TimerFractionRemaining => 1 - TimerFractionComplete;

        public bool IsComplete => TimerCurrent >= TimerMax;

        public Action<RemindMe, object> ClickAction = null;
        public object ClickParam = null;

    }
}

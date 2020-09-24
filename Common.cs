using System;
using System.Numerics;
using RemindMe.Config;

namespace RemindMe {
    public partial class RemindMe {

        public Vector4 GetBarBackgroundColor(MonitorDisplay display, DisplayTimer timer) {
            if (!timer.IsComplete) return display.BarBackgroundColor;
            if (!display.PulseReady) return timer.FinishedColor;
            var s = Math.Abs((Math.Abs(timer.TimerRemaining / (2.5f - display.PulseSpeed)) - (float)Math.Floor(Math.Abs(timer.TimerRemaining / (2.5f - display.PulseSpeed))) - 0.5f) / 2) * display.PulseIntensity;
            if (timer.FinishedColor.W < 0.75) {
                return timer.FinishedColor + new Vector4(0, 0, 0, s);
            }
            return timer.FinishedColor - new Vector4(0, 0, 0, s);

        }


    }
}

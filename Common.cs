using System;
using System.Numerics;
using ImGuiNET;
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


        public void AddTextVertical(string text, uint textColor, float scale) {

            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            var font = ImGui.GetFont();
            var size = ImGui.CalcTextSize(text);
            pos.X = (float)Math.Round(pos.X);
            pos.Y = (float)Math.Round(pos.Y) + (float)Math.Round(size.X * scale);

            foreach (var c in text) {
                var glyph = font.FindGlyph(c);
                
                drawList.PrimReserve(6, 4);

                drawList.PrimQuadUV(
                    pos + new Vector2(glyph.Y0 * scale, -glyph.X0 * scale),
                    pos + new Vector2(glyph.Y0 * scale, -glyph.X1 * scale),
                    pos + new Vector2(glyph.Y1 * scale, -glyph.X1 * scale),
                    pos + new Vector2(glyph.Y1 * scale, -glyph.X0 * scale),

                    new Vector2(glyph.U0, glyph.V0),
                    new Vector2(glyph.U1, glyph.V0),
                    new Vector2(glyph.U1, glyph.V1),
                    new Vector2(glyph.U0, glyph.V1),
                    textColor);
                pos.Y -= glyph.AdvanceX * scale;
            }

            ImGui.Dummy(new Vector2(size.Y, size.X));

        }


    }
}

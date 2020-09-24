using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace RemindMe {
    public partial class RemindMe {

        public enum FillDirection {
            FromTop, FromRight, FromBottom, FromLeft
        }

        public void DrawBar(Vector2 position, Vector2 size, float fraction, FillDirection fillDirection, Vector4 incompleteColor, Vector4 completeColor, float rounding = 5f) {

            var incompleteCorners = ImDrawCornerFlags.None;
            var completeCorners = ImDrawCornerFlags.None;

            var drawList = ImGui.GetWindowDrawList();

            var topLeft = position;
            var bottomRight = position + size;

            var incompleteTopLeft = topLeft;
            var incompleteBottomRight = bottomRight;
            var completeTopLeft = topLeft;
            var completeBottomRight = bottomRight;

            switch (fillDirection) {
                case FillDirection.FromRight:
                    incompleteCorners |= fraction <= 0 ? ImDrawCornerFlags.All : ImDrawCornerFlags.Left;
                    completeCorners |= fraction >= 1 ? ImDrawCornerFlags.All : ImDrawCornerFlags.Right;
                    incompleteBottomRight.X -= size.X * fraction;
                    completeTopLeft.X += size.X * (1 - fraction);
                    break;
                case FillDirection.FromLeft:
                    incompleteCorners |= fraction <= 0 ? ImDrawCornerFlags.All : ImDrawCornerFlags.Right;
                    completeCorners |= fraction >= 1 ? ImDrawCornerFlags.All : ImDrawCornerFlags.Left;
                    incompleteTopLeft.X += size.X * fraction;
                    completeBottomRight.X -= size.X * (1 - fraction);
                    break;
                case FillDirection.FromTop:
                    incompleteCorners |= fraction <= 0 ? ImDrawCornerFlags.All : ImDrawCornerFlags.Bot;
                    completeCorners |= fraction >= 1 ? ImDrawCornerFlags.All : ImDrawCornerFlags.Top;
                    incompleteTopLeft.Y += size.Y * fraction;
                    completeBottomRight.Y -= size.Y * (1 - fraction);
                    break;
                case FillDirection.FromBottom:
                    incompleteCorners |= fraction <= 0 ? ImDrawCornerFlags.All : ImDrawCornerFlags.Top;
                    completeCorners |= fraction >= 1 ? ImDrawCornerFlags.All : ImDrawCornerFlags.Bot;
                    incompleteBottomRight.Y -= size.Y * fraction;
                    completeTopLeft.Y += size.Y * (1 - fraction);
                    break;
            }

            if (fraction < 1) drawList.AddRectFilled(incompleteTopLeft, incompleteBottomRight, ImGui.GetColorU32(incompleteColor), rounding, incompleteCorners);
            if (fraction > 0) drawList.AddRectFilled(completeTopLeft, completeBottomRight, ImGui.GetColorU32(completeColor), rounding, completeCorners);
            
            ImGui.Dummy(size);
        }


    }
}

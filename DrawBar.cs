﻿using System;
using System.Numerics;
using ImGuiNET;

namespace RemindMe {
    public partial class RemindMe {

        public enum FillDirection {
            FromTop, FromRight, FromBottom, FromLeft
        }

        public void DrawBar(Vector2 position, Vector2 size, float fraction, FillDirection fillDirection, Vector4 incompleteColor, Vector4 completeColor, float rounding = 5f) {
            if (fraction < 0) fraction = 0;
            if (fraction > 1) fraction = 1;
            var incompleteCorners = ImDrawFlags.RoundCornersNone;
            var completeCorners = ImDrawFlags.RoundCornersNone;
            var drawList = ImGui.GetWindowDrawList();
            var topLeft = position;
            var bottomRight = position + size;
            var incompleteTopLeft = topLeft;
            var incompleteBottomRight = bottomRight;
            var completeTopLeft = topLeft;
            var completeBottomRight = bottomRight;

            switch (fillDirection) {
                case FillDirection.FromRight:
                    incompleteCorners |= fraction <= 0 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersLeft;
                    completeCorners |= fraction >= 1 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersRight;
                    incompleteBottomRight.X -= size.X * fraction;
                    completeTopLeft.X += size.X * (1 - fraction);
                    break;
                case FillDirection.FromLeft:
                    incompleteCorners |= fraction <= 0 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersRight;
                    completeCorners |= fraction >= 1 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersLeft;
                    incompleteTopLeft.X += size.X * fraction;
                    completeBottomRight.X -= size.X * (1 - fraction);
                    break;
                case FillDirection.FromTop:
                    incompleteCorners |= fraction <= 0 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersBottom;
                    completeCorners |= fraction >= 1 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersTop;
                    incompleteTopLeft.Y += size.Y * fraction;
                    completeBottomRight.Y -= size.Y * (1 - fraction);
                    break;
                case FillDirection.FromBottom:
                    incompleteCorners |= fraction <= 0 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersTop;
                    completeCorners |= fraction >= 1 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersBottom;
                    incompleteBottomRight.Y -= size.Y * fraction;
                    completeTopLeft.Y += size.Y * (1 - fraction);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fillDirection), fillDirection, null);
            }

            if (fraction < 1) drawList.AddRectFilled(incompleteTopLeft, incompleteBottomRight, ImGui.GetColorU32(incompleteColor), rounding, incompleteCorners);
            if (fraction > 0) drawList.AddRectFilled(completeTopLeft, completeBottomRight, ImGui.GetColorU32(completeColor), rounding, completeCorners);
            ImGui.Dummy(size);
        }


    }
}

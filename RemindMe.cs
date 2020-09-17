using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin;
using ImGuiNET;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe
{
    public class RemindMe : IDalamudPlugin
    {
        public string Name => "RemindMe";
        public DalamudPluginInterface PluginInterface { get; private set; }
        public RemindMeConfig PluginConfig { get; private set; }

        private IntPtr actionManagerStatic;

        public ActionManager ActionManager; 
        

        private bool drawConfigWindow = false;

        public List<Action> ActionList;


        public void Dispose()
        {
            PluginInterface.UiBuilder.OnOpenConfigUi -= OnOpenConfigUi;
            PluginInterface.UiBuilder.OnBuildUi -= this.BuildUI;
            ActionManager?.Dispose();
            RemoveCommands();
        }

        public void Initialize(DalamudPluginInterface pluginInterface) {

            drawConfigWindow = true;

            this.PluginInterface = pluginInterface;
            this.PluginConfig = (RemindMeConfig)pluginInterface.GetPluginConfig() ?? new RemindMeConfig();
            this.PluginConfig.Init(this, pluginInterface);
            ActionList = PluginInterface.Data.Excel.GetSheet<Action>().Where(a => a.IsPlayerAction).ToList();

            actionManagerStatic = pluginInterface.TargetModuleScanner.GetStaticAddressFromSig("48 89 05 ?? ?? ?? ?? C3 CC C2 00 00 CC CC CC CC CC CC CC CC CC CC CC CC CC 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ??");

            ActionManager = new ActionManager(this, actionManagerStatic);

            pluginInterface.UiBuilder.OnOpenConfigUi += OnOpenConfigUi;
            
            PluginInterface.UiBuilder.OnBuildUi += this.BuildUI;

            SetupCommands();
        }

        private void OnOpenConfigUi(object sender, EventArgs e) {
            drawConfigWindow = true;
        }

        public void SetupCommands()
        {
            PluginInterface.CommandManager.AddHandler("/remindme", new Dalamud.Game.Command.CommandInfo(OnConfigCommandHandler)
            {
                HelpMessage = $"Open config window for {this.Name}",
                ShowInHelp = true
            });
        }

        public void OnConfigCommandHandler(string command, string args)
        {
            drawConfigWindow = !drawConfigWindow;
        }

        public void RemoveCommands()
        {
            PluginInterface.CommandManager.RemoveHandler("/remindme");
        }


        private void DrawDisplays() {
            if (PluginInterface.ClientState.LocalPlayer == null) return;
            if (PluginConfig.MonitorDisplays.Count == 0) return;
            foreach (var display in PluginConfig.MonitorDisplays.Values) {
                
                if (!display.Locked) {
                    ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 0, 0, 1));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
                }

                if (display.Locked && display.OnlyInCombat && !PluginInterface.ClientState.LocalPlayer.IsStatus(StatusFlags.InCombat)) {
                    continue;
                }

                var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar;

                if (display.Locked) {
                    flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground;
                }

                ImGui.Begin($"Display##{display.Guid}", flags);
                if (!display.Locked) {
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar();
                }

                if (display.Cooldowns.Count > 0) {

                    var TimerList = new List<DisplayTimer>();

                    foreach(var cd in display.Cooldowns.Where(cd => {
                        if (cd.ClassJob != PluginInterface.ClientState.LocalPlayer.ClassJob.Id) return false;
                        var action = ActionManager.GetAction(cd.ActionId);
                        if (action == null || !action.ClassJobCategory.Value.HasClass(PluginInterface.ClientState.LocalPlayer.ClassJob.Id)) return false;
                        var cooldown = ActionManager.GetActionCooldown(action);
                        if (display.OnlyShowReady && cooldown.IsOnCooldown) return false;
                        if (display.LimitDisplayTime && cooldown.Countdown > display.LimitDisplayTimeSeconds) return false;
                        return true;
                    })) {
                        var action = ActionManager.GetAction(cd.ActionId);
                        if (action != null) {
                            var cooldown = ActionManager.GetActionCooldown(action);
                            TimerList.Add(new DisplayTimer {
                                TimerMax = cooldown.CooldownTotal,
                                TimerCurrent = cooldown.CooldownElapsed,
                                FinishedColor = new Vector4(0.70f, 0.25f, 0.25f, 0.75f),
                                ProgressColor = new Vector4(0.75f, 0.125f, 0.665f, 1),
                                IconId = action.Icon,
                                Name = action.Name
                            });
                        }
                    }

                    foreach (var cd in display.Cooldowns.Where(cd => {
                        if (cd.ClassJob != PluginInterface.ClientState.LocalPlayer.ClassJob.Id) return false;
                        var action = ActionManager.GetAction(cd.ActionId);
                        if (action == null || !action.ClassJobCategory.Value.HasClass(PluginInterface.ClientState.LocalPlayer.ClassJob.Id)) return false;
                        var cooldown = ActionManager.GetActionCooldown(action);
                        if (display.OnlyShowReady && cooldown.IsOnCooldown) return false;
                        if (display.LimitDisplayTime && cooldown.Countdown > display.LimitDisplayTimeSeconds) return false;
                        return true;
                    }).OrderBy((cd) => {
                        var action = ActionManager.GetAction(cd.ActionId);
                        var cooldown = ActionManager.GetActionCooldown(action);
                        var ret = cooldown.IsOnCooldown ? cooldown.CountdownTicks : cooldown.CompleteForTicks;
                        return ret;
                    }).ThenBy(cd => cd.ActionId)) {

                        var action = ActionManager.GetAction(cd.ActionId);
                        var cooldown = ActionManager.GetActionCooldown(action);
                        
                        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.75f, 0.125f, 0.665f, 1));

                        if (!cooldown.IsOnCooldown) {
                            var s = Math.Abs((cooldown.CompleteFor - (float) Math.Floor(cooldown.CompleteFor) - 0.5f) / 2);
                            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.70f + s, 0.25f, 0.25f, 0.75f));
                        }

                        var size = ImGui.CalcTextSize(action.Name);
                        var cPosY = ImGui.GetCursorPosY();

                        var fraction = cooldown.CooldownFraction;

                        if (display.LimitDisplayTime && cooldown.CooldownTotal > display.LimitDisplayTimeSeconds) {
                            fraction = (display.LimitDisplayTimeSeconds - cooldown.Countdown) / display.LimitDisplayTimeSeconds;
                        }



                        ImGui.ProgressBar(1 - fraction, new Vector2(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X * 2, display.RowSize), "");
                           
                        ImGui.BeginGroup();
                            
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);
                        if (display.ShowActionIcon) {
                            ImGui.SetCursorPosY(cPosY + ImGui.GetStyle().FramePadding.X);
                                
                            if (cooldown.ActionIconTexture != null) {
                                ImGui.Image(cooldown.ActionIconTexture.ImGuiHandle, new Vector2(display.RowSize - ImGui.GetStyle().FramePadding.X * 2, display.RowSize - ImGui.GetStyle().FramePadding.X * 2));
                            }
                            ImGui.SameLine();
                        }
                        ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f - size.Y / 2f));
                        ImGui.Text($"{action.Name}");

                        if (display.ShowCountdown && cooldown.IsOnCooldown) {
                            var countdownText = (cooldown.IsOnCooldown ? cooldown.Countdown : cooldown.CompleteForTicks).ToString("F1");
                            var countdownSize = ImGui.CalcTextSize(countdownText);

                            ImGui.SetCursorPosY(cPosY + (display.RowSize / 2f - countdownSize.Y / 2f));
                            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - (countdownSize.X + ImGui.GetStyle().WindowPadding.X + ImGui.GetStyle().FramePadding.X));

                            ImGui.Text(countdownText);
                        }


                        ImGui.EndGroup();

                        ImGui.SetCursorPosY(cPosY + display.RowSize + ImGui.GetStyle().ItemSpacing.Y);

                        if (!cooldown.IsOnCooldown) {
                            ImGui.PopStyleColor();
                        }

                        ImGui.PopStyleColor();
                    }
                }


                ImGui.End();

            }
        }

        private void BuildUI() {
            if (PluginInterface.ClientState.LocalPlayer == null) return;
            drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();

            DrawDisplays();

        }
    }
}

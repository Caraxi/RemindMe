using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using RemindMe.Config;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RemindMe {

    public partial class RemindMe : IDalamudPlugin {
        public string Name => "Remind Me";
        public DalamudPluginInterface PluginInterface { get; private set; }
        public RemindMeConfig PluginConfig { get; private set; }

        private IntPtr actionManagerStatic;

        public ActionManager ActionManager;

        private bool drawConfigWindow = false;

        public List<Action> ActionList;

        public IconManager IconManager;

        private readonly Stopwatch generalStopwatch = new Stopwatch();

        internal Stopwatch OutOfCombatTimer = new Stopwatch();

        internal Dictionary<uint, List<Actor>> ActorsWithStatus = new Dictionary<uint, List<Actor>>();
        private Stopwatch cacheTimer = new Stopwatch();


        public void Dispose() {
            PluginInterface.UiBuilder.OnOpenConfigUi -= OnOpenConfigUi;
            PluginInterface.UiBuilder.OnBuildUi -= this.BuildUI;
            PluginInterface.Framework.OnUpdateEvent -= FrameworkOnOnUpdateEvent;
            ActionManager?.Dispose();
            IconManager?.Dispose();
            generalStopwatch.Stop();
            OutOfCombatTimer.Stop();
            cacheTimer.Stop();
            RemoveCommands();
            PluginInterface.Dispose();
        }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            generalStopwatch.Start();
            cacheTimer.Start();
#if DEBUG
            drawConfigWindow = true;
#endif
            this.PluginInterface = pluginInterface;
            this.PluginConfig = (RemindMeConfig)pluginInterface.GetPluginConfig() ?? new RemindMeConfig();
            this.PluginConfig.Init(this, pluginInterface);

            PluginInterface.Framework.OnUpdateEvent += FrameworkOnOnUpdateEvent;

            IconManager = new IconManager(pluginInterface);
            var forcedActions = new uint[] {3};
            ActionList = PluginInterface.Data.Excel.GetSheet<Action>().Where(a => a.IsPlayerAction || forcedActions.Contains(a.RowId)).ToList();

            actionManagerStatic = pluginInterface.TargetModuleScanner.GetStaticAddressFromSig("48 89 05 ?? ?? ?? ?? C3 CC C2 00 00 CC CC CC CC CC CC CC CC CC CC CC CC CC 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ??");

            ActionManager = new ActionManager(this, actionManagerStatic);

            pluginInterface.UiBuilder.OnOpenConfigUi += OnOpenConfigUi;

            PluginInterface.UiBuilder.OnBuildUi += this.BuildUI;

            SetupCommands();
        }

        private void FrameworkOnOnUpdateEvent(Framework framework) {

            if (PluginInterface.ClientState?.LocalPlayer == null) return;
            var inCombat = PluginInterface.ClientState.LocalPlayer.IsStatus(StatusFlags.InCombat);
            if (OutOfCombatTimer.IsRunning && inCombat) {
                generalStopwatch.Restart();
                ActionManager.ResetTimers();
                OutOfCombatTimer.Stop();
                OutOfCombatTimer.Reset();
            } else if (!OutOfCombatTimer.IsRunning && !inCombat) {
                OutOfCombatTimer.Start();
            }


            if (cacheTimer.ElapsedMilliseconds >= PluginConfig.PollingRate) {
                cacheTimer.Restart();
                ActorsWithStatus.Clear();
                foreach (var a in PluginInterface.ClientState.Actors) {
                    foreach (var s in a.StatusEffects) {
                        if (s.EffectId == 0) continue;
                        var eid = (uint) s.EffectId;
                        if (!ActorsWithStatus.ContainsKey(eid)) ActorsWithStatus.Add(eid, new List<Actor>());
                        if (ActorsWithStatus[eid].Contains(a)) continue;
                        ActorsWithStatus[eid].Add(a);
                    }
                }
            }


        }

        private void OnOpenConfigUi(object sender, EventArgs e) {
            drawConfigWindow = true;
        }

        public void SetupCommands() {
            PluginInterface.CommandManager.AddHandler("/remindme", new Dalamud.Game.Command.CommandInfo(OnConfigCommandHandler) {
                HelpMessage = $"Open config window for {this.Name}",
                ShowInHelp = true
            });
        }

        public void OnConfigCommandHandler(string command, string args) {
            drawConfigWindow = !drawConfigWindow;
        }

        public void RemoveCommands() {
            PluginInterface.CommandManager.RemoveHandler("/remindme");
        }

        private void TextShadowed(string text, Vector4 foregroundColor, Vector4 shadowColor, byte shadowWidth = 1) {
            var x = ImGui.GetCursorPosX();
            var y = ImGui.GetCursorPosY();

            for (var i = -shadowWidth; i < shadowWidth; i++) {
                for (var j = -shadowWidth; j < shadowWidth; j++) {
                    ImGui.SetCursorPosX(x + i);
                    ImGui.SetCursorPosY(y + j);
                    ImGui.TextColored(shadowColor, text);
                }
            }
            ImGui.SetCursorPosX(x);
            ImGui.SetCursorPosY(y);
            ImGui.TextColored(foregroundColor, text);
        }

        private List<DisplayTimer> GetTimerList(MonitorDisplay display) {
            var timerList = new List<DisplayTimer>();
            
            try {
                if (display.Cooldowns.Count > 0) {

                    foreach (var cd in display.Cooldowns.Where(cd => {
                        if (cd.ClassJob != PluginInterface.ClientState.LocalPlayer.ClassJob.Id) return false;
                        var action = ActionManager.GetAction(cd.ActionId);
                        if (action == null || !action.ClassJobCategory.Value.HasClass(PluginInterface.ClientState.LocalPlayer.ClassJob.Id)) return false;
                        if (action.ClassJobLevel > PluginInterface.ClientState.LocalPlayer.Level) return false;
                        var cooldown = ActionManager.GetActionCooldown(action);
                        if (display.OnlyShowReady && cooldown.IsOnCooldown) return false;
                        if (display.OnlyShowCooldown && !cooldown.IsOnCooldown) return false;
                        if (display.LimitDisplayTime && cooldown.Countdown > display.LimitDisplayTimeSeconds) return false;
                        if (display.LimitDisplayReadyTime && cooldown.CompleteFor > display.LimitDisplayReadyTimeSeconds) return false;
                        return true;
                    })) {
                        var action = ActionManager.GetAction(cd.ActionId);

                        if (action != null) {
                            var cooldown = ActionManager.GetActionCooldown(action);
                            timerList.Add(new DisplayTimer {
                                TimerMax = cooldown.CooldownTotal,
                                TimerCurrent = cooldown.CooldownElapsed + cooldown.CompleteFor,
                                FinishedColor = display.AbilityReadyColor,
                                ProgressColor = display.AbilityCooldownColor,
                                IconId = IconManager.GetActionIconId(action),
                                Name = action.Name
                            });
                        }
                    }
                }
            } catch (Exception ex) {
                PluginLog.LogError("Error parsing cooldowns.");
                PluginLog.Log(ex.ToString());
            }

            try {
                if (display.StatusMonitors.Count > 0) {

                    var localPlayerAsList = new List<Actor>() { PluginInterface.ClientState.LocalPlayer };

                    foreach (var sd in display.StatusMonitors.Where(sm => {
                        if (sm.ClassJob != 0 && sm.ClassJob != PluginInterface.ClientState.LocalPlayer.ClassJob.Id) return false;
                        return true;
                    })) {
                        foreach (var sid in sd.StatusIDs) {
                            var status = PluginInterface.Data.Excel.GetSheet<Status>().GetRow(sid);
                            if (status == null) continue;

                            if (!ActorsWithStatus.ContainsKey(status.RowId)) continue;

                            foreach (var a in sd.SelfOnly ? localPlayerAsList : ActorsWithStatus[status.RowId]) {
                                if (a != null) {
                                    foreach (var se in a.StatusEffects) {
                                        if (se.OwnerId != PluginInterface.ClientState.LocalPlayer.ActorId) continue;
                                        if (display.LimitDisplayTime && se.Duration > display.LimitDisplayTimeSeconds) continue;
                                        if (se.EffectId == (short)status.RowId) {
                                            var t = new DisplayTimer {
                                                TimerMax = sd.MaxDuration,
                                                TimerCurrent = sd.MaxDuration - se.Duration,
                                                FinishedColor = display.AbilityReadyColor,
                                                ProgressColor = display.StatusEffectColor,
                                                IconId = status.Icon,
                                                Name = status.Name
                                            };

                                            if (!sd.SelfOnly) {
                                                t.TargetName = a.Name;
                                                t.ClickAction = sd.ClickHandler;
                                                t.ClickParam = a;
                                            }

                                            timerList.Add(t);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                PluginLog.LogError("Error parsing statuses.");
                PluginLog.Log(ex.ToString());
            }

            timerList.Sort((a, b) => {
                var diff = a.TimerRemaining - b.TimerRemaining;
                if (Math.Abs(diff) < 0.1) return string.CompareOrdinal(a.Name, b.Name); // Equal
                if (diff < 0) return -1;
                return 1;
            });

            foreach (var reminder in display.GeneralReminders) {
                if (reminder.ShouldShow(PluginInterface, this, display)) {
                    timerList.Insert(0, new DisplayTimer {
                        TimerMax = 1,
                        TimerCurrent = 1 + generalStopwatch.ElapsedMilliseconds / 1000f,
                        FinishedColor = display.AbilityReadyColor,
                        ProgressColor = display.StatusEffectColor,
                        IconId = reminder.GetIconID(PluginInterface, this, display),
                        Name = reminder.GetText(PluginInterface, this, display),
                        AllowCountdown = false
                    });
                }
            }

            return timerList;
        }

        private void DrawDisplays() {
            if (PluginInterface.ClientState.LocalPlayer == null) return;
            if (PluginConfig.MonitorDisplays.Count == 0) return;

            foreach (var display in PluginConfig.MonitorDisplays.Values) {

                if (display.Locked && display.OnlyInCombat) {
                    var inCombat = PluginInterface.ClientState.LocalPlayer.IsStatus(StatusFlags.InCombat);

                    if (!inCombat && !display.KeepVisibleOutsideCombat) continue;

                    if (!inCombat && display.KeepVisibleOutsideCombat) {
                        if (OutOfCombatTimer.Elapsed.TotalSeconds > display.KeepVisibleOutsideCombatSeconds) {
                            continue;
                        }
                    }
                }

                var flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar;

                if (display.Locked) {
                    flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBackground;
                    if (!display.AllowClicking || !display.IsClickableHovered) {
                        flags |= ImGuiWindowFlags.NoMouseInputs;
                    }
                }

                var timerList = display.TimerList ??= GetTimerList(display);
                
                if (timerList.Count > 0 || !display.Locked) {

                    ImGui.SetNextWindowSize(new Vector2(250, 250), ImGuiCond.FirstUseEver);
                    ImGui.SetNextWindowPos(new Vector2(250, 250), ImGuiCond.FirstUseEver);

                    if (display.IsClickableHovered || !display.Locked) {
                        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 0, 0, 1));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
                    }
                    ImGui.Begin($"Display##{display.Guid}", flags);
                    if (display.IsClickableHovered || !display.Locked) {
                        ImGui.PopStyleColor();
                        ImGui.PopStyleVar();
                    }

                    display.IsClickableHovered = false;

                    switch (display.DisplayType) {
                        case 0: {
                            DrawDisplayHorizontal(display, timerList);
                            break;
                        }
                        case 1: {
                            DrawDisplayVertical(display, timerList);
                            break;
                        }
                        case 2: {
                            DrawDisplayIcons(display, timerList);
                            break;
                        }
                        default: {
                            display.DisplayType = 0;
                            DrawDisplayHorizontal(display, timerList);
                            break;
                        }
                    }

                    ImGui.End();
                }
            }
        }
        
        private void BuildUI() {
            if (PluginInterface.ClientState.LocalPlayer == null) return;
            drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();

            DrawDisplays();

        }
    }
}

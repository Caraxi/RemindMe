using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Game.ClientState.Structs.JobGauge;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using RemindMe.Config;

namespace RemindMe {

    public unsafe partial class RemindMe : IDalamudPlugin {
        public string Name => "Remind Me";
        public DalamudPluginInterface PluginInterface { get; private set; }
        public RemindMeConfig PluginConfig { get; private set; }
        public bool InPvP { get; private set; } = false;

        private IntPtr actionManagerStatic;

        public ActionManager ActionManager;

        private bool drawConfigWindow;

        public IconManager IconManager;

        private readonly Stopwatch generalStopwatch = new Stopwatch();

        internal Stopwatch OutOfCombatTimer = new Stopwatch();

        internal Dictionary<uint, List<Actor>> ActorsWithStatus = new Dictionary<uint, List<Actor>>();
        private readonly Stopwatch cacheTimer = new Stopwatch();

        private Exception configLoadException;

        private uint* blueSpellBook;
        public uint[] BlueMagicSpellbook { get; } = new uint[24];

        public void Dispose() {
            PluginInterface.UiBuilder.OnOpenConfigUi -= OnOpenConfigUi;
            PluginInterface.UiBuilder.OnBuildUi -= this.BuildUI;
            PluginInterface.Framework.OnUpdateEvent -= FrameworkUpdate;
            ActionManager?.Dispose();
            IconManager?.Dispose();
            generalStopwatch.Stop();
            OutOfCombatTimer.Stop();
            cacheTimer.Stop();
            RemoveCommands();
            PluginInterface.Dispose();
        }

        public void LoadConfig(bool clearConfig = false) {
            try {
                if (clearConfig) {
                    this.PluginConfig = new RemindMeConfig();
                } else {
                    this.PluginConfig = (RemindMeConfig)PluginInterface.GetPluginConfig() ?? new RemindMeConfig();
                }
                this.PluginConfig.Init(this, PluginInterface);
            } catch (Exception ex) {
                PluginLog.LogError("Failed to load config.");
                PluginLog.LogError(ex.ToString());
                PluginConfig = new RemindMeConfig();
                PluginConfig.Init(this, PluginInterface);
                configLoadException = ex;
            }
        }


        public void Initialize(DalamudPluginInterface pluginInterface) {
            generalStopwatch.Start();
            cacheTimer.Start();
#if DEBUG
            drawConfigWindow = true;
#endif
            this.PluginInterface = pluginInterface;
            
            LoadConfig();

            PluginInterface.Framework.OnUpdateEvent += FrameworkUpdate;

            IconManager = new IconManager(pluginInterface);

            actionManagerStatic = pluginInterface.TargetModuleScanner.GetStaticAddressFromSig("48 89 05 ?? ?? ?? ?? C3 CC C2 00 00 CC CC CC CC CC CC CC CC CC CC CC CC CC 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ??");
            blueSpellBook = (uint*) (pluginInterface.TargetModuleScanner.GetStaticAddressFromSig("0F B7 0D ?? ?? ?? ?? 84 C0") + 0x2A);
            PluginLog.Verbose($"Blue Spell Book: {(ulong) blueSpellBook:X}");

            ActionManager = new ActionManager(this, actionManagerStatic);

            pluginInterface.UiBuilder.OnOpenConfigUi += OnOpenConfigUi;

            PluginInterface.UiBuilder.OnBuildUi += this.BuildUI;

            pluginInterface.ClientState.TerritoryChanged += TerritoryChanged;
            TerritoryChanged(this, pluginInterface.ClientState.TerritoryType);

            SetupCommands();
        }

        private void TerritoryChanged(object sender, ushort e) {
            InPvP = PluginInterface.Data.GetExcelSheet<TerritoryType>().GetRow(e)?.IsPvpZone ?? false;
        }

        private void FrameworkUpdate(Framework framework) {
            try {
                if (PluginInterface.ClientState.Condition[ConditionFlag.LoggingOut]) return;
                if (!PluginInterface.ClientState.Condition.Any()) return;
                if (PluginInterface.ClientState?.LocalPlayer?.ClassJob == null) return;
                var inCombat = PluginInterface.ClientState.Condition[ConditionFlag.InCombat];
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
                        if (!(a is PlayerCharacter || a is BattleNpc)) continue;
                        // TODO: Deal with this shit
                        if (a is BattleNpc bNpc && bNpc.NameId != 541 && *(ulong*) (a.Address + 0xF0) == 0 || ((*(uint*) (a.Address + 0x104)) & 0x10000) == 0x10000) continue;
                        foreach (var s in a.StatusEffects) {
                            if (s.EffectId == 0) continue;
                            var eid = (uint) s.EffectId;
                            if (!ActorsWithStatus.ContainsKey(eid)) ActorsWithStatus.Add(eid, new List<Actor>());
                            if (ActorsWithStatus[eid].Contains(a)) continue;
                            ActorsWithStatus[eid].Add(a);
                        }
                    }

                    // Blue Magic Spellbook
                    if (BlueMagicSpellbook != null && PluginInterface.ClientState.LocalPlayer.ClassJob.Id == 36) {
                        for (var i = 0; i < BlueMagicSpellbook.Length; i++) {
                            BlueMagicSpellbook[i] = blueSpellBook[i];
                        }
                    }
                }

            } catch (Exception ex) {
                PluginLog.Error(ex, "Error in RemindMe.FrameworkUpdate");
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


        private delegate bool ActionSpecialCheckDelegate(MonitorDisplay display, CooldownMonitor cooldownMonitor, DalamudPluginInterface pluginInterface);

        private Dictionary<uint, ActionSpecialCheckDelegate> actionSpecialChecks = new Dictionary<uint, ActionSpecialCheckDelegate> {
            { 7400, ((display, monitor, pluginInterface) => {
                // Nastrond, Only show if in Life of the Dragon
                if (pluginInterface.ClientState.LocalPlayer.ClassJob.Id != 22) return false;
                var jobBar = pluginInterface.ClientState.JobGauges.Get<DRGGauge>();
                return jobBar.BOTDState == BOTDState.LOTD;
            })},
            { 3555, ((display, monitor, pluginInterface) => {
                // Geirskogul, Only show if not in Life of the Dragon
                if (pluginInterface.ClientState.LocalPlayer.ClassJob.Id != 22) return false;
                var jobBar = pluginInterface.ClientState.JobGauges.Get<DRGGauge>();
                return jobBar.BOTDState != BOTDState.LOTD;
            })}
        };

        private List<DisplayTimer> GetTimerList(MonitorDisplay display) {
            var timerList = new List<DisplayTimer>();
            if (InPvP) return timerList;
            try {
                if (display.Cooldowns.Count > 0) {

                    foreach (var cd in display.Cooldowns.Where(cd => {
                        if (cd.ClassJob != PluginInterface.ClientState.LocalPlayer.ClassJob.Id) return false;
                        var action = ActionManager.GetAction(cd.ActionId, true);
                        if (action == null || !action.ClassJobCategory.Value.HasClass(PluginInterface.ClientState.LocalPlayer.ClassJob.Id)) return false;
                        if (action.ClassJobLevel > PluginInterface.ClientState.LocalPlayer.Level) return false;
                        if (action.ClassJob.Row == 36 && !BlueMagicSpellbook.Contains(action.RowId)) return false;
                        var cooldown = ActionManager.GetActionCooldown(action);
                        if (display.OnlyShowReady && cooldown.IsOnCooldown) return false;
                        if (display.OnlyShowCooldown && !cooldown.IsOnCooldown) return false;
                        if (display.LimitDisplayTime && cooldown.Countdown > display.LimitDisplayTimeSeconds) return false;
                        if (display.LimitDisplayReadyTime && cooldown.CompleteFor > display.LimitDisplayReadyTimeSeconds) return false;
                        if (actionSpecialChecks.ContainsKey(action.RowId)) {
                            if (!actionSpecialChecks[action.RowId](display, cd, PluginInterface)) return false;
                        }
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

                    foreach (var sd in display.StatusMonitors.Where(sm => sm.ClassJob == PluginInterface.ClientState.LocalPlayer.ClassJob.Id)) {
                        foreach (var sid in sd.StatusIDs) {
                            var status = PluginInterface.Data.Excel.GetSheet<Status>().GetRow(sid);
                            if (status == null) continue;

                            if (!ActorsWithStatus.ContainsKey(status.RowId)) continue;

                            foreach (var a in sd.SelfOnly ? localPlayerAsList : ActorsWithStatus[status.RowId]) {
                                if (a != null) {
                                    foreach (var se in a.StatusEffects) {
                                        if (sd.IsRaid == false && se.OwnerId != PluginInterface.ClientState.LocalPlayer.ActorId) continue;
                                        if (sd.LimitedZone > 0 && sd.LimitedZone != PluginInterface.ClientState.TerritoryType) continue;
                                        if (display.LimitDisplayTime && se.Duration > display.LimitDisplayTimeSeconds) continue;
                                        if (se.EffectId == (short)status.RowId) {
                                            var t = new DisplayTimer {
                                                TimerMax = sd.MaxDuration,
                                                TimerCurrent = sd.MaxDuration <= 0 ? (1 + generalStopwatch.ElapsedMilliseconds / 1000f) : (sd.MaxDuration - se.Duration),
                                                FinishedColor = display.AbilityReadyColor,
                                                ProgressColor = display.StatusEffectColor,
                                                IconId = (ushort) (status.Icon + (sd.Stacking ? se.StackCount - 1 : 0)),
                                                Name = status.Name,
                                                AllowCountdown = sd.MaxDuration > 0,
                                                StackCount = sd.Stacking ? se.StackCount : -1,
                                            };

                                            if (!sd.SelfOnly) {
                                                t.TargetName = a.Name;
                                                t.TargetNameOnly = display.StatusOnlyShowTargetName;
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
            if (PluginInterface.ClientState.Condition[ConditionFlag.LoggingOut]) return;
            if (!PluginInterface.ClientState.Condition.Any()) return;
            if (PluginInterface.ClientState.LocalPlayer == null) return;
            if (PluginConfig.MonitorDisplays.Count == 0) return;

            foreach (var display in PluginConfig.MonitorDisplays.Values.Where(d => d.Enabled)) {
                if (display.Locked && display.OnlyInCombat) {
                    var inCombat = PluginInterface.ClientState.Condition[ConditionFlag.InCombat];

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
            if (!PluginInterface.ClientState.Condition[ConditionFlag.LoggingOut]) return;
            if (!PluginInterface.ClientState.Condition.Any()) return;
            if (PluginInterface.ClientState.LocalPlayer == null) return;

            if (configLoadException != null || PluginConfig == null) {

                ImGui.PushStyleColor(ImGuiCol.TitleBg, 0x880000AA);
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, 0x880000FF);
                ImGui.Begin($"{Name} - Config Load Error", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.PopStyleColor(2);
                ImGui.Text($"{Name} failed to load the config file.");
                ImGui.Text($"Continuing will result in a loss of any configs you have setup for {Name}.");
                ImGui.Text("Please report this error.");

                if (configLoadException != null) {
                    var str = configLoadException.ToString();
                    ImGui.InputTextMultiline("###exceptionText", ref str, uint.MaxValue, new Vector2(-1, 80), ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
                }

                ImGui.Dummy(new Vector2(5));
                if (ImGui.Button("Retry Load")) {
                    PluginConfig = null;
                    configLoadException = null;
                    LoadConfig();
                }
                ImGui.SameLine();
                ImGui.Dummy(new Vector2(15));
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, 0x880000FF);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0x88000088);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x880000AA);
                if (ImGui.Button("Clear Config")) {
                    LoadConfig(true);
                    configLoadException = null;
                }
                ImGui.PopStyleColor(3);

                ImGui.End();
            } else {
                drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();
                if (!InPvP) DrawDisplays();
            }
        }
    }
}

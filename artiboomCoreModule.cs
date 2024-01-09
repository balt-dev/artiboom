using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Mono.CompilerServices.SymbolWriter;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using static Celeste.Mod.artiboom.FancyBackground;

namespace Celeste.Mod.artiboom
{
    public class ArtiboomModule : EverestModule
    {
        private const int TAIL_LENGTH = 5;
        private const float TAIL_SCALE = 0.6f;
        private const float TAIL_STEP = 40f;
        private const float TAIL_FACING_STEP = 40f;
        private readonly FollowerManager followerManager = new();
        public static ArtiboomModule Instance { get; private set; }

        private static readonly Color[] DashColors = new Color[3]{
            Calc.HexToColor("561329"),
            Calc.HexToColor("70233c"),
            Calc.HexToColor("b54f6f")
        };

        private static readonly Color[] BadelineDashColors = new Color[3]{
            Calc.HexToColor("000000"),
            Calc.HexToColor("1f0000"),
            Calc.HexToColor("330404")
        };

        public override Type SettingsType => typeof(artiboomModuleSettings);
        public static artiboomModuleSettings Settings => (artiboomModuleSettings)Instance._Settings;

        public override Type SessionType => typeof(artiboomModuleSession);
        public static artiboomModuleSession Session => (artiboomModuleSession)Instance._Session;

        private static readonly MethodInfo m_DashCoroutineEnumerator
            = typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
        
        private static readonly MethodInfo m_TextboxRoutineEnumerator 
            = typeof(Textbox).GetMethod("RunRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();

        public ArtiboomModule() {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(nameof(ArtiboomModule), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(ArtiboomModule), LogLevel.Info);
#endif
        }

        private static int StSemiDash;
        private static ILHook hook_StateMachine_ForceState;
        private static ILHook hook_StateMachine_set_State;
        private static ILHook hook_Player_DashCoroutine;
        private static ILHook hook_Textbox_RunRoutine;



        public override void Load() {
            // TODO: apply any hooks that should always be active
            On.Celeste.Player.DashBegin += ModDashBurst;
            On.Celeste.Player.UpdateHair += ModHairColor;
            IL.Celeste.BadelineOldsite.cctor += ModBadelineHairColor;
            On.Celeste.Player.CreateTrail += ModNoTrail;
            IL.Celeste.FancyText.Parse += ModFancyBackgroundParse;
            On.Celeste.Player.ctor += AddStates;
            hook_Textbox_RunRoutine = 
                new ILHook(m_TextboxRoutineEnumerator, (il) => ModFancyBackground(il, m_TextboxRoutineEnumerator.DeclaringType));
            hook_Player_DashCoroutine = 
                new ILHook(m_DashCoroutineEnumerator, ModNoDashSlash);
            hook_StateMachine_ForceState = 
                new ILHook(typeof(StateMachine).GetMethod("ForceState"), VivHack.ForceSetStateOverrideOnPlayerDash);
            hook_StateMachine_set_State = 
                new ILHook(typeof(StateMachine).GetProperty("State").GetSetMethod(), VivHack.ForceSetStateOverrideOnPlayerDash);
            followerManager.Load();
        }

        private void ModNoTrail(On.Celeste.Player.orig_CreateTrail orig, Player self) {
            // Do nothing
            return;
        }

        private static int OverrideDashCheck(StateMachine machine, int previousState, int newState) {
            if (machine.Entity is not Player) return newState; // Return without modifying state
            if(Settings.AlterDash && newState == Player.StDash)
                return StSemiDash;
            return newState;
        }

        private static void AddStates(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(self, position, spriteMode);

            StSemiDash = self.StateMachine.AddState(SemiDash.SemiDashUpdate, SemiDash.SemiDashCoroutine, SemiDash.SemiDashStart, SemiDash.SemiDashEnd);
            SemiDash.StSemiDash = StSemiDash;
        }

        private void ModHairColor(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity) {
            orig(self, applyGravity);
            int idx = -1;
            if (self.Dashes < DashColors.Length)
                idx = self.Dashes;
            if (self.Dashes == 0 && self.Inventory.Dashes == 0)
                idx = 1;
            if ((idx >= 0) & (self.StateMachine.State != Player.StStarFly)) {
                if (SaveData.Instance.Assists.PlayAsBadeline) {
                    self.Hair.Color = BadelineDashColors[idx];
                }
            }
        }

        private void ModBadelineHairColor(ILContext il) {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(MoveType.Before, 
                instr => instr.MatchLdstr("9B3FB5"),
                instr => instr.MatchCall("Monocle.Calc", "HexToColor")
            )) {
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), $"IL@{cursor.Next}: Hook failed to find hair color in BadelineOldSite. Did something else change it?");
                return;
            }
            // Remove old...
            cursor.RemoveRange(2);
            // ...and put in new
            cursor.EmitDelegate(() => BadelineDashColors[1]);
        }

        private void ModDashBurst(On.Celeste.Player.orig_DashBegin orig, Player self) {
            Level level = self.SceneAs<Level>();
		    level.Displacement.AddBurst(self.Center, 0.2f, 8f, 64f, 1f, Ease.QuadOut, Ease.QuadOut);
            self.SceneAs<Level>().ParticlesFG.Emit(
                SummitGem.P_Shatter,
                5,
                self.Position,
                Vector2.Zero,
                Color.White,
                self.Speed.Angle() + (float) Math.PI
            );
            Player.P_DashA.Color = Calc.HexToColor("efff3e");
            Player.P_DashA.Color2 = Calc.HexToColor("760e00");
            Player.P_DashA.FadeMode = ParticleType.FadeModes.Linear;
            Player.P_DashA.ColorMode = ParticleType.ColorModes.Choose;
            Player.P_DashB = Player.P_DashA;
            orig(self);
        }

        private void ModNoDashSlash(ILContext il) {
            ILCursor cursor = new(il);
            if (!cursor.TryGotoNext(
                MoveType.After,
                instr => instr.MatchCall<SlashFx>("Burst")
            )) {
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), $"IL@{cursor.Next}: Hook failed to find slash effect in DashCoroutine. Did something else change it?");
                return;
            }
            cursor.GotoNext(
                MoveType.After,
                instr => instr.MatchPop()
            );
            Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"Label set!");
            ILLabel label = cursor.MarkLabel();
            cursor.GotoPrev(
                MoveType.Before,
                instr => instr.MatchLdloc(1)
            );
            Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"Next: {cursor.Next}");
            cursor.EmitDelegate(() => Logger.Log(LogLevel.Error, nameof(ArtiboomModule), "This should never show!"));
            cursor.GotoPrev(
                MoveType.Before,
                instr => instr.MatchLdloc(1)
            );
            Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"Label jump!");
            cursor.MoveAfterLabels();
            cursor.Emit(
                OpCodes.Br,
                label
            );
            Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"Emitted :3");
        }

        public override void Unload() {
            // TODO: unapply any hooks applied in Load()
            // TODO: apply any hooks that should always be active
            On.Celeste.Player.DashBegin -= ModDashBurst;
            On.Celeste.Player.UpdateHair -= ModHairColor;
            IL.Celeste.BadelineOldsite.cctor -= ModBadelineHairColor;
            On.Celeste.Player.CreateTrail -= ModNoTrail;
            IL.Celeste.FancyText.Parse -= ModFancyBackgroundParse;

            On.Celeste.Player.ctor -= AddStates;
            hook_StateMachine_ForceState.Dispose();
            hook_StateMachine_set_State.Dispose();
            hook_Player_DashCoroutine.Dispose();
            hook_Textbox_RunRoutine.Dispose();
            followerManager.UnLoad();
        }

        [Command("playanim", "Plays a player animation")]
        #nullable enable
        public static void PlayAnimation(string? animation) {
            Player player = Engine.Scene.Tracker.GetEntity<Player>();
            if (animation is not null && player.Sprite.Animations.ContainsKey(animation)) {
                Engine.Commands.ExecuteCommand("q", []);
                player.Sprite.Stop();
                player.Sprite.Play(animation, true);            
            } else {
                Color color = Color.White;
                if (animation is not null) {
                    color = Color.Red;
                    Engine.Commands.Log($"Invalid animation {animation}!", color);
                }
                Engine.Commands.Log("Valid animations: ", color);
                Engine.Commands.Log(string.Join(", ", player.Sprite.Animations.Keys), color);
            }
        }
    }
}
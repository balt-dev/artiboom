using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.artiboom
{
    public class ArtiboomModule : EverestModule
    {
        private const int TAIL_LENGTH = 20;
        private const float TAIL_SCALE = .8f;
        private readonly FollowerManager followerManager = new();
        public static ArtiboomModule Instance { get; private set; }

        private static readonly Color[] DashColors = new Color[3]{
            Calc.HexToColor("561329"),
            Calc.HexToColor("70233c"),
            Calc.HexToColor("87354f")
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


        public ArtiboomModule()
        {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(nameof(ArtiboomModule), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(artiboomModule), LogLevel.Info);
#endif
        }

        private static int StSemiDash;
        private static ILHook hook_StateMachine_ForceState;
        private static ILHook hook_StateMachine_set_State;

        public override void Load()
        {
            // TODO: apply any hooks that should always be active
            IL.Celeste.Player.DashBegin += ModNoFreeze;
            On.Celeste.Player.UpdateHair += ModHairColor;
            IL.Celeste.BadelineOldsite.cctor += ModBadelineHairColor;
            On.Celeste.PlayerHair.Start += ModHairAmount;
            On.Celeste.PlayerHair.GetHairTexture += ModHairTexture;
            On.Celeste.PlayerHair.GetHairScale += ModHairScale;

            // TODO: Edit OnCollideH and OnCollideV to work with the new state

            On.Celeste.Player.ctor += AddStates;
            hook_StateMachine_ForceState = new ILHook(typeof(StateMachine).GetMethod("ForceState"), VivHack.ForceSetStateOverrideOnPlayerDash);
            hook_StateMachine_set_State = new ILHook(typeof(StateMachine).GetProperty("State").GetSetMethod(), VivHack.ForceSetStateOverrideOnPlayerDash);
            followerManager.Load();
        }

        private static int OverrideDashCheck(StateMachine machine, int previousState, int newState) {
            if (machine.Entity is not Player) return newState; // Return without modifying state
            Logger.Log(nameof(ArtiboomModule), $"Setting state from {previousState} to {newState} from machine length {machine.Length()}");
            if(Settings.AlterDash && newState == Player.StDash)
                return StSemiDash;
            return newState;
        }

        private static void AddStates(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(self, position, spriteMode);

            StSemiDash = self.StateMachine.AddState(SemiDash.SemiDashUpdate, SemiDash.SemiDashCoroutine, SemiDash.SemiDashStart, SemiDash.SemiDashEnd);
            SemiDash.StSemiDash = StSemiDash;
        }

        private void ModHairColor(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity)
        {
            orig(self, applyGravity);
            int idx = -1;
            if (self.Dashes < DashColors.Length)
                idx = self.Dashes;
            if (self.Dashes == 0 && self.Inventory.Dashes == 0)
                idx = 1;
            if (idx >= 0) {
                if (self.Sprite.Mode == PlayerSpriteMode.MadelineAsBadeline) {
                    self.Hair.Color = BadelineDashColors[idx];
                } else {
                    self.Hair.Color = DashColors[idx];
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

        private MTexture ModHairTexture(On.Celeste.PlayerHair.orig_GetHairTexture orig, PlayerHair self, int index)
        {
            return orig(self, 1);
        }

        private void ModHairAmount(On.Celeste.PlayerHair.orig_Start orig, PlayerHair self)
        {
            self.Nodes = new List<Vector2>();
            for (int i = 0; i < TAIL_LENGTH; i++)
            {
                self.Nodes.Add(Vector2.Zero);
            }
            orig(self);
        }

        private Vector2 ModHairScale(On.Celeste.PlayerHair.orig_GetHairScale orig, PlayerHair self, int index)
        {
            Vector2 scale = orig(self, index);
            return scale * new Vector2(TAIL_SCALE);
        }

        private void ModNoFreeze(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Engine>("TimeRate")))
            {
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), $"IL@{cursor.Next}: Hook failed to find Engine.TimeRate in Player.DashBegin.");
            }
            cursor.EmitDelegate(() =>
            {
                return Settings.AlterDash ? 0.0f : 1.0f;
            });
            cursor.Emit(OpCodes.Mul);
        }

        public override void Unload()
        {
            // TODO: unapply any hooks applied in Load()
           IL.Celeste.Player.DashBegin -= ModNoFreeze;
            On.Celeste.Player.UpdateHair -= ModHairColor;
            On.Celeste.PlayerHair.Start -= ModHairAmount;
            On.Celeste.PlayerHair.GetHairTexture -= ModHairTexture;
            On.Celeste.PlayerHair.GetHairScale -= ModHairScale;

            On.Celeste.Player.ctor -= AddStates;
            hook_StateMachine_ForceState.Dispose();
            hook_StateMachine_set_State = new ILHook(typeof(StateMachine).GetProperty("State").GetSetMethod(), VivHack.ForceSetStateOverrideOnPlayerDash);
            followerManager.unLoad();
        }
    }
}
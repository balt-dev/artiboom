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
        private static ILHook IL_DashCoroutine;
        private static ILHook hook_StateMachine_ForceState;
        private static ILHook hook_StateMachine_set_State;

        public override void Load()
        {
            // TODO: apply any hooks that should always be active
            IL.Celeste.Player.DashBegin += ModNoFreeze;
            IL_DashCoroutine = new ILHook(m_DashCoroutineEnumerator, ModKeepMomentum);
            On.Celeste.Player.CreateTrail += ModNoTrail;
            On.Celeste.Player.UpdateHair += ModHairColor;
            On.Celeste.PlayerHair.Start += ModHairAmount;
            On.Celeste.PlayerHair.GetHairTexture += ModHairTexture;
            On.Celeste.PlayerHair.GetHairScale += ModHairScale;

            On.Celeste.Player.ctor += AddStates;
            hook_StateMachine_ForceState = new ILHook(typeof(StateMachine).GetMethod("ForceState"), VivHack.ForceSetStateOverrideOnPlayerDash);
            hook_StateMachine_set_State = new ILHook(typeof(StateMachine).GetProperty("State").GetSetMethod(), VivHack.ForceSetStateOverrideOnPlayerDash);
            followerManager.Load();
        }

        private static int OverrideDashCheck(StateMachine machine, int previousState, int newState) {
            if(Settings.AlterDash && newState == Player.StDash) {
                return StSemiDash;
            }
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
            if (self.Dashes < DashColors.Length)
                self.Hair.Color = DashColors[self.Dashes];
            if (self.Dashes == 0 && self.Inventory.Dashes == 0)
                self.Hair.Color = DashColors[1];
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

        private void ModKeepMomentum(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdloc(2),
                instr => instr.MatchLdcR4(240f)))
            {
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), $"IL@{cursor.Next}: Hook failed to find dir * 240f in Player.DashCoroutine.");
            }

            cursor.EmitDelegate(() =>
            {
                return Settings.AlterDash ? 1.2f : 1.0f;
            });
            cursor.Emit(OpCodes.Mul);

            if (!cursor.TryGotoNext(MoveType.After,
                    instr => instr.MatchEndfinally()
                ))
            {
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), $"IL@{cursor.Next}: Hook failed to find finally in Player.DashCoroutine.");
            }

            Logger.Log(nameof(ArtiboomModule), $"Current cursor: {cursor.Next} @ {cursor.Index:X}");

            if (!cursor.TryGotoNext(MoveType.Before,
                    instr => instr.MatchLdsfld<SaveData>("Instance"),
                    instr => instr.MatchLdflda<SaveData>("Assists"),
                    instr => instr.MatchLdfld<Assists>("SuperDashing")
                ))
            {
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), $"IL@{cursor.Next} @ {cursor.Index:X}: Hook failed to find SuperDashing in Player.DashCoroutine.");
            }

            /*
            cursor.MoveAfterLabels();

            Logger.Log(nameof(ArtiboomModule), $"Current cursor: IL@{cursor.Next} @ {cursor.Index:X}");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, m_DashCoroutineEnumerator.DeclaringType.GetField("<>4__this"));
            cursor.EmitDelegate((Player self) =>
            {
                if (Settings.AlterDash & self.StateMachine.State != StSemiDash)
                {
                    Logger.Log(nameof(ArtiboomModule), $"Altering state machine! Previous was {self.StateMachine.State}, now is {StSemiDash}");
                    self.StateMachine.State = StSemiDash;
                    Logger.Log(nameof(ArtiboomModule), $"Altered, we good");
                }
            });
            */
            
            if (!cursor.TryGotoNext(MoveType.After,
                    instr => instr.MatchLdflda<Player>("DashDir"),
                    instr => instr.MatchLdfld<Vector2>("Y"),
                    instr => instr.MatchLdcR4(0f),
                    instr => instr.MatchBgtUn(out _)))
            {
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), $"IL@{cursor} @ {cursor.Index:X}: Hook failed to find DashDir.Y <= 0f in Player.DashCoroutine.");
            }

            if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchStfld<Player>("Speed")))
            {
                Logger.Log(LogLevel.Error, nameof(ArtiboomModule), $"IL@{cursor} @ {cursor.Index:X}: Hook failed to find stfld this.Speed in Player.DashCoroutine after DashDir.Y <= 0f.");
            }

            Logger.Log(nameof(ArtiboomModule), $"IL@{cursor} @ {cursor.Index:X}: Adding second delegate in Player.DashCoroutine to mod this.Speed after DashDir.Y <= 0f!");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, m_DashCoroutineEnumerator.DeclaringType.GetField("<>4__this"));
            cursor.EmitDelegate<Func<Vector2, Player, Vector2>>(
                (newSpeed, player) =>
                {
                    Logger.Log(nameof(ArtiboomModule), $"Dash ending in state {player.StateMachine.State}");
                    return Settings.AlterDash ? player.Speed : newSpeed;
                }
            );

        }

        private void ModNoTrail(On.Celeste.Player.orig_CreateTrail orig, Player self)
        {
            if (Settings.AlterDash)
            {
                self.SceneAs<Level>().ParticlesFG.Emit(
                    SummitGem.P_Shatter,
                    5,
                    self.Position,
                    new Vector2(3, 3),
                    Color.White,
                    self.Speed.Angle() + (float) Math.PI
                );
                self.Hair.Color = Calc.HexToColor("FFFFFF");
                self.ResetSprite(PlayerSpriteMode.Playback);
                self.ResetSpriteNextFrame(self.DefaultSpriteMode);
                return;
            }
            orig(self);
        }

        public override void Unload()
        {
            // TODO: unapply any hooks applied in Load()
            IL.Celeste.Player.DashBegin -= ModNoFreeze;
            On.Celeste.Player.CreateTrail -= ModNoTrail;
            IL_DashCoroutine.Dispose();
            followerManager.unLoad();
        }
    }
}
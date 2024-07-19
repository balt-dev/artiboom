using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using IL.System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Mono.CompilerServices.SymbolWriter;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using static Celeste.Mod.Artiboom.FancyBackground;

namespace Celeste.Mod.Artiboom;

public class ArtiboomModule : EverestModule
{
    private const int TAIL_LENGTH = 5;
    private const float TAIL_SCALE = 0.6f;
    private const float TAIL_STEP = 40f;
    private const float TAIL_FACING_STEP = 40f;
    public static ArtiboomModule Instance { get; private set; }

    private static readonly Color[] ParticleColors = [
            Calc.HexToColor("FFFFFF"),
            Calc.HexToColor("dbd148"),
            Calc.HexToColor("e57804"),
            Calc.HexToColor("c43019"),
            Calc.HexToColor("890a01"),
            Calc.HexToColor("210702")
    ];

    private static readonly Color[] BadelineDashColors = [
        Calc.HexToColor("000000"),
        Calc.HexToColor("1f0000"),
        Calc.HexToColor("330404")
    ];

    public override Type SettingsType => typeof(ArtiboomModuleSettings);
    public static ArtiboomModuleSettings Settings => (ArtiboomModuleSettings)Instance._Settings;

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
        On.Celeste.Player.CreateTrail += ModNoTrail;
        IL.Celeste.FancyText.Parse += ModFancyBackgroundParse;
        On.Celeste.Player.ctor += AddStates;
        IL.Celeste.Player.DashUpdate += ModDashTrail;
        hook_Textbox_RunRoutine = 
            new ILHook(m_TextboxRoutineEnumerator, (il) => ModFancyBackground(il, m_TextboxRoutineEnumerator.DeclaringType));
        hook_Player_DashCoroutine = 
            new ILHook(m_DashCoroutineEnumerator, ModNoDashSlash);
        hook_StateMachine_ForceState = 
            new ILHook(typeof(StateMachine).GetMethod("ForceState"), VivHack.ForceSetStateOverrideOnPlayerDash);
        hook_StateMachine_set_State = 
            new ILHook(typeof(StateMachine).GetProperty("State").GetSetMethod(), VivHack.ForceSetStateOverrideOnPlayerDash);
        FollowerManager.Load();
    }

    private static void ModNoTrail(On.Celeste.Player.orig_CreateTrail orig, Player self) {
        // Do nothing
        return;
    }

    private static int OverrideDashCheck(StateMachine machine, int _previousState, int newState) {
        if (machine.Entity is not Player) return newState; // Return without modifying state
        if(Settings.AlterDash && newState == Player.StDash)
            return StSemiDash;
        return newState;
    }

    private static void AddStates(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
        orig(self, position, spriteMode);

        StSemiDash = self.AddState("SemiDash", SemiDash.SemiDashUpdate, SemiDash.SemiDashCoroutine, SemiDash.SemiDashStart, SemiDash.SemiDashEnd);
        SemiDash.StSemiDash = StSemiDash;
    }

    private static void ModHairColor(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity) {
        orig(self, applyGravity);
        int idx = -1;
        if ((idx >= 0) & (self.StateMachine.State != Player.StStarFly)) {
            if (SaveData.Instance.Assists.PlayAsBadeline) {
                self.Hair.Color = BadelineDashColors[idx];
            }
        }
    }

    private static void ModDashTrail(ILContext il) {
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNext(MoveType.Before, 
            instr => instr.MatchLdfld<Level>("ParticlesFG")
        )) {
            Logger.Log(LogLevel.Error, nameof(ArtiboomModule), $"IL@{cursor.Next}: Hook failed to find particles in Player. Did something else change it?");
            return;
        }
        cursor.GotoPrev(MoveType.Before, instr => instr.MatchLdarg(0));
        cursor.MoveAfterLabels();
        ILLabel label = cursor.MarkLabel();
        cursor.MoveAfterLabels();
        cursor.EmitCall(((Delegate) PlayerParticle).Method); 
        cursor.Emit(OpCodes.Stloc_S, (byte)7);
        cursor.GotoPrev(
            MoveType.Before, 
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<Player>("wasDashB")
        );
        cursor.MoveAfterLabels();
        cursor.Emit(
            OpCodes.Br,
            label
        );
    } 

    private static readonly Random random = new();


    private static ParticleType PlayerParticle() {
        return new ParticleType(Player.P_DashA) {
            Color = ParticleColors[random.Next(0, ParticleColors.Length)],
            Color2 = ParticleColors[random.Next(0, ParticleColors.Length)],
            FadeMode = ParticleType.FadeModes.Late,
            ColorMode = ParticleType.ColorModes.Fade,
            LifeMax = 0.4f,
            LifeMin = 0.0f
        };
    }


    private static void ModDashBurst(On.Celeste.Player.orig_DashBegin orig, Player self) {
        Level level = self.SceneAs<Level>();
        level.Displacement.AddBurst(self.Center, 0.2f, 0f, 24f, 2f, Ease.QuadOut, Ease.QuadOut);
        self.SceneAs<Level>().ParticlesFG.Emit(
            SummitGem.P_Shatter,
            5,
            self.Position,
            Vector2.Zero,
            Color.White,
            self.Speed.Angle() + (float) Math.PI
        );
        orig(self);
    }

    private static void ModNoDashSlash(ILContext il) {
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
        ILLabel label = cursor.MarkLabel();
        cursor.GotoPrev(
            MoveType.Before,
            instr => instr.MatchLdloc(1)
        );
        cursor.GotoPrev(
            MoveType.Before,
            instr => instr.MatchLdloc(1)
        );
        cursor.MoveAfterLabels();
        cursor.Emit(
            OpCodes.Br,
            label
        );
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
        // TODO: apply any hooks that should always be active
        On.Celeste.Player.DashBegin -= ModDashBurst;
        On.Celeste.Player.UpdateHair -= ModHairColor;
        On.Celeste.Player.CreateTrail -= ModNoTrail;
        IL.Celeste.FancyText.Parse -= ModFancyBackgroundParse;
        IL.Celeste.Player.DashUpdate -= ModDashTrail;

        On.Celeste.Player.ctor -= AddStates;
        hook_StateMachine_ForceState.Dispose();
        hook_StateMachine_set_State.Dispose();
        hook_Player_DashCoroutine.Dispose();
        hook_Textbox_RunRoutine.Dispose();
        FollowerManager.Unload();
    }
}
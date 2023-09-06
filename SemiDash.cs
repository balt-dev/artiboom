using Celeste;
using Celeste.Mod;
using Celeste.Mod.artiboom;
using MonoMod;
using Monocle;
using MonoMod.Utils;
using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

public class SemiDash {

	public static int StSemiDash;

	private const float DASH_SPEED = 288f;

	private static readonly MethodInfo DASH_UPDATE = GetMethod<Player>("DashUpdate");
	private static readonly MethodInfo DASH_BEGIN = GetMethod<Player>("DashBegin");
	private static readonly MethodInfo DASH_END = GetMethod<Player>("DashEnd");

	private static Vector2 beforeDashSpeed;

	internal static object GetValue<T>(T self, string name) {
		Logger.Log(nameof(ArtiboomModule), $"Getting value {name}");
		return typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);
	}

	internal static void SetValue<T>(T self, string name, object value) {
		Logger.Log(nameof(ArtiboomModule), $"Getting value {name}");
		typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, value);
	}

	internal static object GetGetterValue<T>(T self, string name) {
		Logger.Log(nameof(ArtiboomModule), $"Getting value {name} from getter");
		return typeof(T).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod().Invoke(self, new object[]{});
	}

	internal static MethodInfo GetValueSetter<T>(T self, string name) {
		Logger.Log(nameof(ArtiboomModule), $"Getting setter of value {name}");
		return typeof(T).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance).GetSetMethod();
	}

	internal static MethodInfo GetMethod<T>(string name) {
		Logger.Log(nameof(ArtiboomModule), $"Getting method {name}");
		return typeof(T).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
	}

	internal static MethodInfo GetStaticMethod<T>(string name) {
		Logger.Log(nameof(ArtiboomModule), $"Getting static method {name}");
		return typeof(T).GetMethod(name, BindingFlags.NonPublic);
	}

    public static int SemiDashUpdate(Player self) {
        Logger.Log(nameof(ArtiboomModule), $"Semidash update! Update: \"{DASH_UPDATE}\"");
		int state = (int)DASH_UPDATE.Invoke(self, new object[]{});
		if (state == 2 || state == 5) {
			Logger.Log(nameof(ArtiboomModule), $"State changing from {state} to {StSemiDash}");
			state = StSemiDash;
		};
        return state;
    }

	public static IEnumerator SemiDashCoroutine(Player self) {
        Logger.Log(nameof(ArtiboomModule), $"Semidash coroutine!");
		yield return null;
        if (SaveData.Instance.Assists.DashAssist) {
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
        }
		Level level = self.SceneAs<Level>();
		level.Displacement.AddBurst(self.Center, 0.2f, 8f, 96f, 1f, Ease.QuadOut, Ease.QuadOut);
		/*
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
		*/
        Vector2 value = (Vector2) GetValue(self, "lastAim");
        if (self.OverrideDashDirection.HasValue) {
            value = self.OverrideDashDirection.Value;
        }

		if (value.X != 0f && Math.Abs(value.X) < 0.001f) {
            value.X = 0f;
            value.Y = Math.Sign(value.Y);
        }
        else if (value.Y != 0f && Math.Abs(value.Y) < 0.001f) {
            value.Y = 0f;
            value.X = Math.Sign(value.X);
        }

		Vector2 speed = value * DASH_SPEED;

		if (Math.Sign(beforeDashSpeed.X) == Math.Sign(speed.X) && Math.Abs(beforeDashSpeed.X) > Math.Abs(speed.X))
            speed.X = beforeDashSpeed.X;

		if (self.CollideCheck<Water>())
			speed *= 0.333f; // Arti is really bad in water

		self.Speed = speed;
		self.DashDir = value;

		SetValue(self, "gliderBoostDir", self.DashDir = value);
        level.DirectionalShake(self.DashDir, 0.2f);
        if (self.DashDir.X != 0f)
            self.Facing = (Facings)Math.Sign(self.DashDir.X);
        
		GetMethod<Player>("CallDashEvents").Invoke(self, new object[]{});

		if (self.StateMachine.PreviousState == Player.StStarFly)
        	level.Particles.Emit(FlyFeather.P_Boost, 12, self.Center, Vector2.One * 4f, (-value).Angle());

		if (self.OnGround() && self.DashDir.X != 0f && self.DashDir.Y > 0f && self.Speed.Y > 0f && (!self.Inventory.DreamDash || !self.CollideCheck<DreamBlock>(self.Position + Vector2.UnitY))) {
            self.DashDir.X = Math.Sign(self.DashDir.X);
            self.DashDir.Y = 0f;
            self.Speed.Y = 0f;
            self.Speed.X *= 1.2f;
            self.Ducking = true;
        }

		self.SceneAs<Level>().ParticlesFG.Emit(
			SummitGem.P_Shatter,
			5,
			self.Position,
			Vector2.Zero,
			Color.White,
			self.Speed.Angle() + (float) Math.PI
		);
		self.Hair.Color = Calc.HexToColor("FFFFFF");
		self.ResetSprite(PlayerSpriteMode.Playback);
		self.ResetSpriteNextFrame(self.DefaultSpriteMode);

		 if (self.DashDir.X != 0f && Input.GrabCheck) {
            SwapBlock swapBlock = self.CollideFirst<SwapBlock>(self.Position + Vector2.UnitX * Math.Sign(self.DashDir.X));
            if (swapBlock != null && swapBlock.Direction.X == (float)Math.Sign(self.DashDir.X)) {
                self.StateMachine.State = 1;
                self.Speed = Vector2.Zero;
                yield break;
            }
        }

		Vector2 swapCancel = Vector2.One;
        foreach (SwapBlock entity in level.Tracker.GetEntities<SwapBlock>().Cast<SwapBlock>()) {
            if (self.CollideCheck(entity, self.Position + Vector2.UnitY) && entity != null && entity.Swapping) {
                if (self.DashDir.X != 0f && entity.Direction.X == Math.Sign(self.DashDir.X))
                    self.Speed.X = swapCancel.X = 0f;
                if (self.DashDir.Y != 0f && entity.Direction.Y == Math.Sign(self.DashDir.Y))
                    self.Speed.Y = swapCancel.Y = 0f;
            }
        }

		yield return 0.15f;

		self.AutoJump = true;
		self.AutoJumpTimer = 0f;

		self.StateMachine.State = 0;
    }

	public static void SemiDashStart(Player self) {
        Logger.Log(nameof(ArtiboomModule), $"Semidash start! Begin: \"{DASH_BEGIN}\"");
		beforeDashSpeed = self.Speed;
        DASH_BEGIN.Invoke(self, new object[]{});
    }

    public static void SemiDashEnd(Player self) {
        Logger.Log(nameof(ArtiboomModule), $"Semidash end! End: \"{DASH_END}\"");
        DASH_END.Invoke(self, new object[]{});
    }
}
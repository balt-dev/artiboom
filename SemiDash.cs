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

	private static readonly MethodInfo NORMAL_UPDATE = GetMethod<Player>("NormalUpdate");
	private static readonly MethodInfo DASH_BEGIN = GetMethod<Player>("DashBegin");
	private static readonly MethodInfo DASH_END = GetMethod<Player>("DashEnd");
	private static readonly MethodInfo DASH_COROUTINE = GetMethod<Player>("DashCoroutine");

	internal static object GetValue<T>(T self, string name) {
		Logger.Log(nameof(ArtiboomModule), $"Getting value {name}");
		return typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);
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
        Logger.Log(nameof(ArtiboomModule), $"Semidash update! Update: \"{NORMAL_UPDATE}\"");
        if (Math.Abs(self.DashDir.Y) < 0.1f) {
			foreach (JumpThru entity in self.Scene.Tracker.GetEntities<JumpThru>().Cast<JumpThru>())
			{
				if (self.CollideCheck(entity) && self.Bottom - entity.Top <= 6f && !(bool)GetStaticMethod<Player>("DashCorrectCheck").Invoke(self, new object[]{Vector2.UnitY * (entity.Top - self.Bottom)}))
				{
                    Logger.Log(nameof(ArtiboomModule), $"MoveVExact({(int)(entity.Top - self.Bottom)})");
					self.MoveVExact((int)(entity.Top - self.Bottom));
				}
			}
			if (self.CanUnDuck && Input.Jump.Pressed && (float) GetValue(self, "jumpGraceTimer") > 0f)
			{
                Logger.Log(nameof(ArtiboomModule), $"SuperJump()");
				GetMethod<Player>("SuperJump").Invoke(self, new object[]{});
				return 0;
			}
		}
		if ((bool) GetGetterValue(self, "SuperWallJumpAngleCheck"))
		{
			if (Input.Jump.Pressed && self.CanUnDuck)
			{
				if ((bool) GetMethod<Player>("WallJumpCheck").Invoke(self, new object[]{1}))
				{
                    Logger.Log(nameof(ArtiboomModule), $"SuperWallJump(-1)");
					GetMethod<Player>("SuperWallJump").Invoke(self, new object[]{-1});
					return 0;
				}
				if ((bool) GetMethod<Player>("WallJumpCheck").Invoke(self, new object[]{-1}))
				{
                    Logger.Log(nameof(ArtiboomModule), $"SuperWallJump(1)");
				    GetMethod<Player>("SuperWallJump").Invoke(self, new object[]{1});
					return 0;
				}
			}
		}
		int state = (int)NORMAL_UPDATE.Invoke(self, new object[]{});
		if (state == 2 || state == 5) {
			Logger.Log(nameof(ArtiboomModule), $"State changing from {state} to {StSemiDash}");
			state = StSemiDash;
		};
        return state;
    }

	public static IEnumerator SemiDashCoroutine(Player self) {
        Logger.Log(nameof(ArtiboomModule), $"Semidash coroutine! Coroutine: \"{DASH_COROUTINE}\"");
		// TODO: Return Player.DashCoroutine
		object obj = DASH_COROUTINE.Invoke(self, new object[]{});
        Logger.Log(nameof(ArtiboomModule), $"{obj}");
        return (IEnumerator) obj;
    }

	public static void SemiDashStart(Player self) {
        Logger.Log(nameof(ArtiboomModule), $"Semidash start! Begin: \"{DASH_BEGIN}\"");
        DASH_BEGIN.Invoke(self, new object[]{});
    }

    public static void SemiDashEnd(Player self) {
        Logger.Log(nameof(ArtiboomModule), $"Semidash end! End: \"{DASH_END}\"");
        DASH_END.Invoke(self, new object[]{});
    }
}
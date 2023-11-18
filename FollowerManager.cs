using Microsoft.Xna.Framework;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.artiboom;
using Celeste.Mod.artiboom.Entities;
using Monocle;

internal class FollowerManager
{
	private Sofanthiel follower;

	private bool wasActiveOnLastFrame;

	private void IsPastMirror() {
		var session = SaveData.Instance;
		var area = session.CurrentSession.Area;
		var sid = area.GetSID();
		var side = area.Mode;
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"In chapter {sid} side {side}");
	}

	public void Load()
	{
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Loaded Sofanthiel.");
		IsPastMirror();
		Everest.Events.Level.OnPause += onPause;
		On.Celeste.Level.LoadLevel += onLoadLevel;
		On.Celeste.Player.Update += onPlayerUpdate;
	}

	public void unLoad()
	{
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Unloaded Sofanthiel.");
		Everest.Events.Level.OnPause += onPause;
		On.Celeste.Level.LoadLevel -= onLoadLevel;
		On.Celeste.Player.Update -= onPlayerUpdate;
	}

	private void onLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Celeste.Level self, Celeste.Player.IntroTypes playerIntro, bool isFromLoader)
	{
		orig.Invoke(self, playerIntro, isFromLoader);
		Celeste.Player entity = self.Tracker.GetEntity<Celeste.Player>();
		if (ArtiboomModule.Settings.EnableFollower && entity != null)
		{
			follower = new Sofanthiel(entity.Center + new Vector2(ArtiboomModule.Settings.FollowX * (int)entity.Facing, -ArtiboomModule.Settings.FollowY - 5f));
			self.Add(follower);
		}
		wasActiveOnLastFrame = ArtiboomModule.Settings.EnableFollower;
	}

	public void onPlayerUpdate(On.Celeste.Player.orig_Update orig, Celeste.Player self)
	{
		orig.Invoke(self);
		Celeste.Level level = Engine.Scene as Celeste.Level;
		if (!wasActiveOnLastFrame && ArtiboomModule.Settings.EnableFollower && level.Tracker.CountEntities<Sofanthiel>() == 0)
		{
			follower = new Sofanthiel(self.Center + new Vector2(ArtiboomModule.Settings.FollowX * (int)self.Facing, -ArtiboomModule.Settings.FollowY - 5f));
			level.Add(follower);
			follower.Position = self.Center + new Vector2(ArtiboomModule.Settings.FollowX * (int)self.Facing, -ArtiboomModule.Settings.FollowY - 5f);
		}
		if (wasActiveOnLastFrame && !ArtiboomModule.Settings.EnableFollower && level.Tracker.CountEntities<Sofanthiel>() > 0)
		{
			follower.Disable();
		}
		wasActiveOnLastFrame = ArtiboomModule.Settings.EnableFollower;
	}

	public static void RefreshSettings()
	{
		Celeste.Level level = Engine.Scene as Celeste.Level;
		foreach (Sofanthiel item in level.Entities.FindAll<Sofanthiel>())
		{
			item.UpdateSettings();
		}
	}

	public void onPause(Celeste.Level level, int startIndex, bool minimal, bool quickReset)
	{
		if (follower != null)
		{
			follower.UpdateSettings();
		}
	}
}
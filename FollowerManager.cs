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
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"\n\n\nAttempting to load\n\n\n");
		if (Engine.Scene is Level level) {
			var area = level.Session.Area;
			var sid = area.ChapterIndex;
			var side = area.Mode;
			Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"\n\n\nIn chapter {sid} side {side}\n\n\n");
		}
	}

	public void Load()
	{
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Loaded Sofanthiel.");
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

	private void onLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
	{
		orig.Invoke(self, playerIntro, isFromLoader);
		Player entity = self.Tracker.GetEntity<Player>();
		if (ArtiboomModule.Settings.EnableFollower && entity != null)
		{
			follower = new Sofanthiel(entity.Center + new Vector2(ArtiboomModule.Settings.FollowX * (int)entity.Facing, -ArtiboomModule.Settings.FollowY - 5f));
			self.Add(follower);
		}
		wasActiveOnLastFrame = ArtiboomModule.Settings.EnableFollower;
	}

	public void onPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self)
	{
		orig.Invoke(self);
		Level level = Engine.Scene as Level;
		if (!wasActiveOnLastFrame && ArtiboomModule.Settings.EnableFollower && level.Tracker.CountEntities<Sofanthiel>() == 0)
		{
			IsPastMirror();
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
		Level level = Engine.Scene as Level;
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
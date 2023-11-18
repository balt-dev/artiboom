using Microsoft.Xna.Framework;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.artiboom;
using Celeste.Mod.artiboom.Entities;
using Monocle;

internal class FollowerManager
{
	private Sofanthiel follower;

	private bool wasActiveOnLastFrame = false;

	private void IsPastMirror() {
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"\n\n\nAttempting to load\n\n\n");
		if (Engine.Scene is Level level) {
			var area = level.Session.Area;
			var campaign = area.GetLevelSet();
			var chapter = area.ChapterIndex;
			var side = area.Mode;
			var room = area.ID;
			Logger.Log(LogLevel.Info, nameof(ArtiboomModule), $"\n\n\nIn campaign {campaign} chapter {chapter} side {side} room # {room}\n\n\n");
		}
	}

	public void Load()
	{
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Loaded Sofanthiel.");
		Everest.Events.Level.OnPause += OnPause;
		On.Celeste.Level.UnloadLevel += OnUnloadLevel;
		On.Celeste.Player.Update += OnPlayerUpdate;
	}

	public void UnLoad()
	{
		Logger.Log(LogLevel.Info, nameof(ArtiboomModule), "Unloaded Sofanthiel.");
		Everest.Events.Level.OnPause -= OnPause;
		On.Celeste.Level.UnloadLevel -= OnUnloadLevel;
		On.Celeste.Player.Update -= OnPlayerUpdate;
	}

	private void OnUnloadLevel(On.Celeste.Level.orig_UnloadLevel orig, Level self) {
		orig.Invoke(self);
		wasActiveOnLastFrame = false;
	}

	public void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self)
	{
		orig.Invoke(self);
		if (Engine.Scene is Level level) {
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
		} else {
			wasActiveOnLastFrame = false;
		}
	}

	public static void RefreshSettings()
	{
		Level level = Engine.Scene as Level;
		foreach (Sofanthiel item in level.Entities.FindAll<Sofanthiel>())
		{
			item.UpdateSettings();
		}
	}

	public void OnPause(Level level, int startIndex, bool minimal, bool quickReset)
	{
		follower?.UpdateSettings();
	}
}
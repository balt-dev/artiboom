using Monocle;
using Microsoft.Xna.Framework;
using System;
using System.Collections;

namespace Celeste.Mod.artiboom.Entities
{
    [Tracked]
    public class Sofanthiel : Entity
    {
        public Sprite sprite;
        public BloomPoint bloom;

        public int followX = ArtiboomModule.Settings.FollowX * 2;
        public int followY = ArtiboomModule.Settings.FollowY * 2;

        public Sofanthiel(Vector2 position) : base(position)
        {
            UpdateSettings();
        }

        public override void Update()
        {
            base.Update();
            Player player = Scene.Tracker.GetEntity<Player>();

            if (player != null)
            {
                Vector2 position = Position;
                Vector2 playerPosition = player.Position + new Vector2(followX * (int)player.Facing, -followY - 5f);
                Position = position + (playerPosition - position) * (1f - (float)Math.Pow(0.01, Engine.DeltaTime));
            }
        }

        //Floors the sprite position so that the pixels are neat and don't flicker
        public override void Render()
        {
            Vector2 renderPosition = sprite.RenderPosition;
            sprite.RenderPosition = sprite.RenderPosition.Floor();
            base.Render();
            sprite.RenderPosition = renderPosition;
        }



        public IEnumerator Collect()
        {
            yield return null;
            RemoveSelf();
        }

        public void Disable()
        {
            Add(new Coroutine(Collect()));
        }

        public void UpdateSettings()
        {
            Remove(sprite);
            Remove(bloom);

            sprite = GFX.SpriteBank.Create("sofanthiel");

            followX = ArtiboomModule.Settings.FollowX * 2;
            followY = ArtiboomModule.Settings.FollowY * 2;

            Depth = 10;
            Add(bloom = new BloomPoint(0.1f, 12f));
            Add(sprite);

        } 
    }

    // don't care enough to not code duplicate
    [Tracked]
    public class CommunicationMark : Entity
    {
        public Sprite sprite;
        public BloomPoint bloom;

        public int followY = ArtiboomModule.Settings.FollowY * 3;

        public CommunicationMark(Vector2 position) : base(position)
        {
            UpdateSettings();
        }

        public override void Update()
        {
            base.Update();
            Player player = Scene.Tracker.GetEntity<Player>();

            if (player != null)
            {
                Vector2 position = Position;
                Vector2 playerPosition = player.Position + new Vector2(0, -followY - 5f);
                Position = position + (playerPosition - position) * (1f - (float)Math.Pow(0.000001, Engine.DeltaTime));
            }
        }

        //Floors the sprite position so that the pixels are neat and don't flicker
        public override void Render()
        {
            Vector2 renderPosition = sprite.RenderPosition;
            sprite.RenderPosition = sprite.RenderPosition.Floor();
            base.Render();
            sprite.RenderPosition = renderPosition;
        }



        public IEnumerator Collect()
        {
            yield return null;
            RemoveSelf();
        }

        public void Disable()
        {
            Add(new Coroutine(Collect()));
        }

        public void UpdateSettings()
        {
            Remove(sprite);
            Remove(bloom);

            sprite = GFX.SpriteBank.Create("communication_mark");

            followY = ArtiboomModule.Settings.FollowY * 3;

            Depth = 10;
            Add(bloom = new BloomPoint(0.5f, 16f));
            Add(sprite);

        } 
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace XNAUtility.Graphics
{
    public class FrameRateCounter : DrawableGameComponent
    {
        ContentManager content;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;

        int frameRate = 0;
        int frameCounter = 0;
        TimeSpan elapsedTime = TimeSpan.Zero;

        Vector2 resolution;

        public FrameRateCounter(Game game, SpriteBatch batch, SpriteFont font, Vector2 res)
            : base(game)
        {
            content = new ContentManager(game.Services);

            spriteBatch = batch;
            spriteFont = font;
            resolution = res;
        }

        public override void Update(GameTime gameTime)
        {
            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                frameRate = frameCounter;
                frameCounter = 0;
            }
        }


        public override void Draw(GameTime gameTime)
        {
            frameCounter++;

            string fps = string.Format("fps: {0}", frameRate);

            Vector2 stringSize = spriteFont.MeasureString(fps);

            spriteBatch.Begin();

            spriteBatch.DrawString(spriteFont, fps, new Vector2(resolution.X - stringSize.X, resolution.Y - stringSize.Y), Color.Black);

            spriteBatch.End();
        }
    }
}

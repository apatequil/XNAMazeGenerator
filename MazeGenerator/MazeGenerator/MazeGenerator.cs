using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using MazeGenerator.Maze;

using XNAUtility.Graphics;

namespace MazeGenerator
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class MazeGenerator : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Generator generator;

        // maze sizes
        Point small = new Point(64, 36);
        Point medium = new Point(96, 54);
        Point large = new Point(128, 72);
        Point ultra = new Point(256, 144);

        public MazeGenerator()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            this.IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;

            graphics.ApplyChanges();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // small
            generator = new Generator(this, small.X, small.Y, new Point(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
            // medium
            //generator = new Generator(this, medium.X, medium.Y, new Point(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
            // large
            //generator = new Generator(this, large.X, large.Y, new Point(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
            // ultra
            //generator = new Generator(this, ultra.X, ultra.Y, new Point(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));


            // Add the frame counter
            // Register the frame counter
            Components.Add(new FrameRateCounter(this, spriteBatch, Content.Load<SpriteFont>("Fonts\\font"), new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight)));
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

            generator.Update(Mouse.GetState(), Keyboard.GetState(), gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            generator.Draw(gameTime);

            base.Draw(gameTime);
        }
    }
}

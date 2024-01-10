using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MazeGenerator.Maze
{
    public class Generator
    {
        private enum CellWall { North, South, East, West, None};
        private enum MazeMode { Input, Running, Finished, Pause };

        private const int NORTHVAL = 1;
        private const int SOUTHVAL = 2;
        private const int EASTVAL = 4;
        private const int WESTVAL = 8;

        private List<int> northWalls = new List<int>();
        private List<int> southWalls = new List<int>();
        private List<int> eastWalls = new List<int>();
        private List<int> westWalls = new List<int>();

        private MazeGenerator game;

        private Point tileSize = new Point(10, 10);
        private Point mazeSize = new Point(128, 72);
        private Point resolution = new Point(1280, 720); // default to 360 resolution

        private int generationSpeed = 0;
        private int runTime = 0;

        private SpriteBatch spriteBatch;
        private Texture2D pixel;
        private Texture2D alphaPixel;
        private Texture2D overlay;
        private SpriteFont font;

        private MazeMode mode = MazeMode.Input;

        // Color of the walls
        private Color wallColor = Color.DarkGreen;
        // Incomplete background color
        private Color incompleteColor = Color.Gray;
        // Complete background color
        private Color completeColor = Color.Black;
        // On the stack color
        private Color visitedColor = Color.Blue;
        // Active cell color
        private Color activeColor = Color.Purple;
        // Locked cell color
        private Color lockedColor = Color.Red;

        private Point currentCell = new Point(0, 0);
        private int totalVisitedCount = 0;
        private List<Cell> visited = new List<Cell>();

        private Random rnd = new Random();

        private Cell[,] maze;

        MouseState previousMouseState;
        KeyboardState previousKeyboardState;
        private bool displayText = true;
        private MazeMode previousMode = MazeMode.Input;

        public Generator(MazeGenerator g, int width, int height, Point resolution)
        {
            game = g;
            maze = new Cell[width, height];

            tileSize = new Point(resolution.X / width, resolution.Y / height);

            mazeSize = new Point(width, height);

            InitializeMaze();

            spriteBatch = new SpriteBatch(g.GraphicsDevice);
            pixel = game.Content.Load<Texture2D>("Graphics\\pixel");
            alphaPixel = game.Content.Load<Texture2D>("Graphics\\pixel");
            alphaPixel.SetData(new[] { Color.FromNonPremultiplied(255, 255, 255, 75) });
            overlay = game.Content.Load<Texture2D>("Graphics\\braidCastle");
            //overlay = game.Content.Load<Texture2D>("Graphics\\test2");
            font = game.Content.Load<SpriteFont>("Fonts\\font");

            // Let's also try to auto-lock cells based on the picture
            AutoLockCells();
        }

        private void InitializeMaze()
        {
            maze = new Cell[mazeSize.X, mazeSize.Y];

            tileSize = new Point(resolution.X / mazeSize.X, resolution.Y / mazeSize.Y);

            for (int i = 0; i < mazeSize.X; i++)
                for (int j = 0; j < mazeSize.Y; j++)
                    maze[i, j] = new Cell(new Point(i, j));

            mazeSize = new Point(mazeSize.X, mazeSize.Y);

            PopulateWallLists();

            maze[currentCell.X, currentCell.Y].HasVisitor = true;
            maze[currentCell.X, currentCell.Y].IsActive = true;
            maze[currentCell.X, currentCell.Y].IsOnStack = true;
            visited.Add(maze[currentCell.X, currentCell.Y]);
            totalVisitedCount = 1;
        }

        private void AutoLockCells()
        {
            // To auto-lock the cells, what we want to do is get the grayscale value of all the pixels that are
            // in a cell from the back image. If the average color is below a certain threshold, we lock the cell.
            // The idea is to lock the cells where there is substantial image stuff going on in the background.
            
            // Get the color array
            Color[] textureColors = new Color[overlay.Width * overlay.Height];
            overlay.GetData<Color>(textureColors);

            // Let's translate that to a 2d array to make it easier to get the pixels in a cell
            int[,] grayScaleColors = new int[overlay.Width,overlay.Height];
            for (int i = 0; i < overlay.Width; i++)
            {
                for (int j = 0; j < overlay.Height; j++)
                {
                    Color tempColor = textureColors[(j * overlay.Width) + i];

                    grayScaleColors[i, j] = (tempColor.R + tempColor.G + tempColor.B) / 3;
                }
            }

            // At this point we have a the grayscale values for each pixel. Loop through the maze
            // and average each cell's grayscale to get the final grayscale value

            // Get a pixelCount which will hold the number of pixels in a cell. We'll divide the
            // sum of grayscale by this to get the final value
            int pixelCount = tileSize.X * tileSize.Y;
            for (int i = 0; i < mazeSize.X; i++)
            {
                for (int j = 0; j < mazeSize.Y; j++)
                {
                    // Get the cell.
                    Cell tempCell = maze[i,j];
                    Rectangle cellBounds = new Rectangle(tempCell.Location.X * tileSize.X, tempCell.Location.Y * tileSize.Y, tileSize.X, tileSize.Y);

                    // Reset the grayscale value for the cell
                    int runningTotal = 0;

                    // Loop through each pixel that is contained in the cell and add the grayscale
                    for (int k = cellBounds.Left; k < cellBounds.Right; k++)
                    {
                        for (int l = cellBounds.Top; l < cellBounds.Bottom; l++)
                        {
                            runningTotal += grayScaleColors[k, l];
                        }
                    }

                    // Get the average grayscale
                    runningTotal = runningTotal / pixelCount;

                    // If the grayscale is within a certain range, lock the cell
                    if (runningTotal < 210)
                        maze[i, j].IsLocked = true;
                }
            }

            // Do a double-pass to try and "clean up" the locking a bit. This will have
            // a lower threshold to lock if there are neighbors that are already locked
            /*for (int i = 0; i < mazeSize.X; i++)
            {
                for (int j = 0; j < mazeSize.Y; j++)
                {
                    // Get the cell.
                    Cell tempCell = maze[i, j];
                    Rectangle cellBounds = new Rectangle(tempCell.Location.X * tileSize.X, tempCell.Location.Y * tileSize.Y, tileSize.X, tileSize.Y);

                    // Reset the grayscale value for the cell
                    int runningTotal = 0;

                    // Loop through each pixel that is contained in the cell and add the grayscale
                    for (int k = cellBounds.Left; k < cellBounds.Right; k++)
                    {
                        for (int l = cellBounds.Top; l < cellBounds.Bottom; l++)
                        {
                            runningTotal += grayScaleColors[k, l];
                        }
                    }

                    // Get the average grayscale
                    runningTotal = runningTotal / pixelCount;

                    List<Cell> neighbors = GetUnvisitedNeighbors(tempCell);

                    bool lockedneighbors = false;

                    for(int m = 0; m < neighbors.Count; m++)
                        if (neighbors[m].IsLocked)
                        {
                            lockedneighbors = true;
                            break;
                        }
                    // If the grayscale is within a certain range, lock the cell
                    if (lockedneighbors && runningTotal < 150)
                        maze[i, j].IsLocked = true;
                }
            }*/
        }

        private void PopulateWallLists()
        {
            // Possible values for each room. Sum up the door values for the cell's total value
            // North wall = 1
            // East wall = 4
            // South wall = 2
            // West wall = 8

            // North wall values: 1, 3, 5, 7, 9, 11, 13, 15
            northWalls.Add(1);
            northWalls.Add(3);
            northWalls.Add(5);
            northWalls.Add(7);
            northWalls.Add(9);
            northWalls.Add(11);
            northWalls.Add(13);
            northWalls.Add(15);

            // East wall values: 4, 5, 6, 7, 12, 13, 14, 15
            eastWalls.Add(4);
            eastWalls.Add(5);
            eastWalls.Add(6);
            eastWalls.Add(7);
            eastWalls.Add(12);
            eastWalls.Add(13);
            eastWalls.Add(14);
            eastWalls.Add(15);

            // South wall values: 2, 3, 6, 7, 10, 11, 12, 14, 15
            southWalls.Add(2);
            southWalls.Add(3);
            southWalls.Add(6);
            southWalls.Add(7);
            southWalls.Add(10);
            southWalls.Add(11);
            southWalls.Add(14);
            southWalls.Add(15);

            // West wall values: 8, 9, 10, 11, 12, 13, 14, 15
            westWalls.Add(8);
            westWalls.Add(9);
            westWalls.Add(10);
            westWalls.Add(11);
            westWalls.Add(12);
            westWalls.Add(13);
            westWalls.Add(14);
            westWalls.Add(15);

        }

        public void Update(MouseState ms, KeyboardState ks, GameTime gt)
        {
            // Save the previous mode.
            previousMode = mode;

            runTime = runTime + gt.ElapsedGameTime.Milliseconds;
            if (runTime > generationSpeed)
            {
                // toggle the text display
                if (ks.IsKeyDown(Keys.V) && !previousKeyboardState.IsKeyDown(Keys.V))
                    displayText = !displayText;
                // Check the mode
                switch(mode)
                {
                    case MazeMode.Input:
                        // If the left-mouse button is down, lock the cell
                        if (ms.LeftButton == ButtonState.Pressed)
                        {
                            // Get the cell that has been clicked
                            Point cell = new Point(ms.X / tileSize.X, ms.Y / tileSize.Y);
                            
                            if(cell.X >= 0 && cell.X < mazeSize.X && cell.Y >= 0 && cell.Y < mazeSize.Y)
                                // Toggle the cell's locked status
                                maze[cell.X, cell.Y].IsLocked = true;
                        }
                        // If the right-mouse button is down, lock the cell
                        else if (ms.RightButton == ButtonState.Pressed)
                        {
                            // Get the cell that has been clicked
                            Point cell = new Point(ms.X / tileSize.X, ms.Y / tileSize.Y);

                            if (cell.X >= 0 && cell.X < mazeSize.X && cell.Y >= 0 && cell.Y < mazeSize.Y)
                                // Toggle the cell's locked status
                                maze[cell.X, cell.Y].IsLocked = false;
                        }
                        else if (ms.MiddleButton == ButtonState.Pressed)
                        {
                            // Get the cell that has been clicked
                            Point cell = new Point(ms.X / tileSize.X, ms.Y / tileSize.Y);

                            List<Cell> cellArea = GetArea(maze[cell.X,cell.Y]);
                            foreach (Cell areaCell in cellArea)
                                areaCell.IsLocked = true;
                        }
                        

                        // Check if we need to lock everything
                        if (ks.IsKeyDown(Keys.L))
                            LockAllCells();
                        else if (ks.IsKeyDown(Keys.U))
                            UnLockAllCells();
                        else if (ks.IsKeyDown(Keys.A))
                        {
                            UnLockAllCells();
                            AutoLockCells();
                        }
                        // Check if we need to start the generation
                        else if (ks.IsKeyDown(Keys.Space))
                            mode = MazeMode.Running;
                        break;
                    case MazeMode.Running:
                        Step();

                        // Check if we need to pause
                        if (ks.IsKeyDown(Keys.Space) && previousMode != MazeMode.Pause)
                            mode = MazeMode.Pause;
                        break;
                    case MazeMode.Pause:
                        // Check if we need to resume
                        if (ks.IsKeyDown(Keys.Space) && previousMode != MazeMode.Running)
                            mode = MazeMode.Running;
                        break;
                    case MazeMode.Finished:
                        // Check if we need to reset the maze
                        if (ks.IsKeyDown(Keys.R))
                        {
                            mode = MazeMode.Input;
                            InitializeMaze();
                        }
                        break;
                }
                // Reset the runtime
                runTime = 0;
            }

            // Save the previous states
            previousKeyboardState = ks;
            previousMouseState = ms;
        }

        private List<Cell> GetArea(Cell cell)
        {
            if (cell.IsLocked)
                return new List<Cell>();

            currentCell.X = cell.Location.X;
            currentCell.Y = cell.Location.Y;

            List<Cell> area = new List<Cell>();
            area.Add(cell);
            List<Cell> visited = new List<Cell>();
            visited.Add(cell);

            while (visited.Count > 0)
            {

                // Get a list of all neighboring cells that haven't been visited (all walls intact)
                List<Cell> neighbors = GetUnvisitedNeighbors(maze[currentCell.X, currentCell.Y]);

                for (int i = 0; i < neighbors.Count; i++)
                {
                    if (area.Contains(neighbors[i]))
                    {
                        neighbors.RemoveAt(i);
                        i--;
                    }
                }

                if (neighbors.Count > 0)
                {
                    int randomItem = rnd.Next(0, neighbors.Count);

                    // Get the random neighbor
                    Cell randomNeighbor = neighbors[randomItem];
                    area.Add(randomNeighbor);
                    visited.Add(randomNeighbor);
                }
                else
                {
                    // Remove the current cell and set it's active flag to false;
                    visited.RemoveAt(visited.Count - 1);

                    // Check if the visited stack is empty. If not, set the last item to active
                    if (visited.Count > 0)
                    {
                        Cell newCurrent = visited[visited.Count - 1];
                        currentCell = new Point(newCurrent.Location.X, newCurrent.Location.Y);
                    }
                }
            }

            return area;
        }

        private void LockAllCells()
        {
            for (int i = 0; i < mazeSize.X; i++)
                for (int j = 0; j < mazeSize.Y; j++)
                    maze[i, j].IsLocked = true;
        }

        private void UnLockAllCells()
        {
            for (int i = 0; i < mazeSize.X; i++)
                for (int j = 0; j < mazeSize.Y; j++)
                    maze[i, j].IsLocked = false;
        }

        public void Draw(GameTime gt)
        {
            spriteBatch.Begin();
            // Draw The Background
            //spriteBatch.Draw(pixel, new Rectangle(0, 0, resolution.X, resolution.Y), incompleteColor);
            spriteBatch.Draw(overlay, new Rectangle(0, 0, resolution.X, resolution.Y), Color.White);

            // Fill The Cells
            for (int i = 0; i < mazeSize.X; i++)
            {
                for (int j = 0; j < mazeSize.Y; j++)
                {
                    // Fill the cell background
                    FillCell(spriteBatch, maze[i,j]);
                }
            }

            // Draw the cell walls
            for (int i = 0; i < mazeSize.X; i++)
            {
                for (int j = 0; j < mazeSize.Y; j++)
                {
                    // Draw the cell walls
                    DrawCellWalls(spriteBatch, maze[i,j]);
                }
            }

            // Draw the current Mode
            string text = "";

            switch (mode)
            {
                case MazeMode.Input:
                    text = "Input (mouse to toggle locks. space to run)";
                    break;
                case MazeMode.Running:
                    text = "Running (space to pause)";
                    break;
                case MazeMode.Pause:
                    text = "Paused (space to resume)";
                    break;
                case MazeMode.Finished:
                    text = "Finished (r to reset)";
                    break;
            }

            Vector2 textWidth = font.MeasureString(text);

            if(displayText)
                spriteBatch.DrawString(font, text, new Vector2(0f, 0f), Color.White);

            spriteBatch.End();
        }

        private void DrawCellWalls(SpriteBatch batch, Cell currentCell)
        {
            if (currentCell.IsLocked && mode == MazeMode.Finished)
                return;

            Rectangle drawRect = new Rectangle();
            drawRect.X = currentCell.Location.X * tileSize.X;
            drawRect.Y = currentCell.Location.Y * tileSize.Y;
            drawRect.Width = tileSize.X;
            drawRect.Height = tileSize.Y;

            // Draw the North wall if needed
            if (northWalls.IndexOf(currentCell.CellValue) >= 0)
                spriteBatch.Draw(pixel, new Rectangle(drawRect.X, drawRect.Y, drawRect.Width, 1), wallColor);
            // Draw the East wall if needed
            if (eastWalls.IndexOf(currentCell.CellValue) >= 0)
                spriteBatch.Draw(pixel, new Rectangle(drawRect.X + tileSize.X, drawRect.Y, 1, drawRect.Height), wallColor);
            // Draw the South wall if needed
            if (southWalls.IndexOf(currentCell.CellValue) >= 0)
                spriteBatch.Draw(pixel, new Rectangle(drawRect.X, drawRect.Y + tileSize.Y, drawRect.Width, 1), wallColor);
            // Draw the West wall if needed
            if (westWalls.IndexOf(currentCell.CellValue) >= 0)
                spriteBatch.Draw(pixel, new Rectangle(drawRect.X, drawRect.Y, 1, drawRect.Height), wallColor);
        }

        private void FillCell(SpriteBatch batch, Cell currentCell)
        {
            Rectangle drawRect = new Rectangle();
            drawRect.X = currentCell.Location.X * tileSize.X;
            drawRect.Y = currentCell.Location.Y * tileSize.Y;
            drawRect.Width = tileSize.X;
            drawRect.Height = tileSize.Y;

            if (currentCell.IsLocked && mode != MazeMode.Finished)
            {
                spriteBatch.Draw(alphaPixel, drawRect, lockedColor);
                return;
            }

            if (mode == MazeMode.Input)
                return;

            if (currentCell.IsActive)
            {
                spriteBatch.Draw(pixel, drawRect, activeColor);
                return;
            }

            if (currentCell.IsOnStack)
            {
                spriteBatch.Draw(pixel, drawRect, visitedColor);
                return;
            }
            else if (!currentCell.IsOnStack && currentCell.HasVisitor)
            {
                //spriteBatch.Draw(pixel, drawRect, completeColor);
                return;
            }

            //spriteBatch.Draw(pixel, drawRect, incompleteColor);
        }

        public void Step()
        {
            // If we have visited every cell in the maze, we can exit
            if (visited.Count == 0)
                return;

            // Get a list of all neighboring cells that haven't been visited (all walls intact)
            List<Cell> neighbors = GetUnvisitedNeighbors(maze[currentCell.X, currentCell.Y]);

            if (neighbors.Count > 0)
            {                
                int randomItem = rnd.Next(0, neighbors.Count);

                // Get the random neighbor
                Cell randomNeighbor = neighbors[randomItem];
                Cell current = maze[currentCell.X, currentCell.Y];
                CellWall blockingWall = CellWall.None;


                // Determine if the neighbor is:
                // North
                if (randomNeighbor.Location.Y < current.Location.Y)
                    blockingWall = CellWall.North;
                // East
                else if (randomNeighbor.Location.X > current.Location.X)
                    blockingWall = CellWall.East;
                // South
                else if (randomNeighbor.Location.Y > current.Location.Y)
                    blockingWall = CellWall.South;
                // West
                else if (randomNeighbor.Location.X < current.Location.X)
                    blockingWall = CellWall.West;

                // Remove the blocking wall for each cell. For example, if the blocking
                // wall is North, remove the North wall from the current cell and the 
                // South wall from the neighbor
                switch (blockingWall)
                {
                    case CellWall.North:
                        current.CellValue = current.CellValue - NORTHVAL;
                        randomNeighbor.CellValue = randomNeighbor.CellValue - SOUTHVAL;
                        break;
                    case CellWall.East:
                        current.CellValue = current.CellValue - EASTVAL;
                        randomNeighbor.CellValue = randomNeighbor.CellValue - WESTVAL;
                        break;
                    case CellWall.South:
                        current.CellValue = current.CellValue - SOUTHVAL;
                        randomNeighbor.CellValue = randomNeighbor.CellValue - NORTHVAL;
                        break;
                    case CellWall.West:
                        current.CellValue = current.CellValue - WESTVAL;
                        randomNeighbor.CellValue = randomNeighbor.CellValue - EASTVAL;
                        break;
                    case CellWall.None:
                        return;
                }

                // Flag the current cell as no longer active
                current.IsActive = false;

                // Commit the current cell to the maze
                maze[currentCell.X, currentCell.Y] = current;

                // Increase the total visited count
                totalVisitedCount++;

                // Make the current cell the neighbor
                currentCell = new Point(randomNeighbor.Location.X, randomNeighbor.Location.Y);
                // Update the new current cell's properties
                randomNeighbor.IsActive = true;
                randomNeighbor.HasVisitor = true;
                randomNeighbor.IsOnStack = true;
                visited.Add(randomNeighbor);

                // Commit the neighbor cell to the maze
                maze[currentCell.X, currentCell.Y] = randomNeighbor;
            }
            else
            {
                // Remove the current cell and set it's active flag to false;
                visited.RemoveAt(visited.Count - 1);
                maze[currentCell.X, currentCell.Y].IsActive = false;
                maze[currentCell.X, currentCell.Y].IsOnStack = false;

                // Check if the visited stack is empty. If not, set the last item to active
                if (visited.Count > 0)
                {
                    Cell newCurrent = visited[visited.Count - 1];
                    currentCell = new Point(newCurrent.Location.X, newCurrent.Location.Y);
                    maze[currentCell.X, currentCell.Y].IsActive = true;
                }
                else
                {
                    // We need to check if there are any unvisited cells that aren't locked. These would be cells
                    // that are entirely enclosed in locked cells and therefore skipped during the previous passes
                    List<Cell> untouchedCells = GetUnvisitedAndUnlockedCells();

                    // If the untouchedCells count is 0, we're finished. Otherwise, randomly pick one of them and do
                    // another pass.
                    if (untouchedCells.Count == 0)
                        mode = MazeMode.Finished;
                    else
                    {
                        Cell randomUntouched = untouchedCells[rnd.Next(0, untouchedCells.Count)];
                        // Make the current cell the neighbor
                        currentCell = new Point(randomUntouched.Location.X, randomUntouched.Location.Y);
                        randomUntouched.IsActive = true;
                        randomUntouched.HasVisitor = true;
                        randomUntouched.IsOnStack = true;
                        visited.Add(randomUntouched);
                    }
                    return;
                }
            }
        }

        private List<Cell> GetUnvisitedAndUnlockedCells()
        {
            List<Cell> untouchedCells = new List<Cell>();

            for (int i = 0; i < mazeSize.X; i++)
                for (int j = 0; j < mazeSize.Y; j++)
                    if (!maze[i, j].IsLocked && !maze[i, j].HasVisitor)
                        untouchedCells.Add(maze[i, j]);

            return untouchedCells;
        }

        private List<Cell> GetUnvisitedNeighbors(Cell currentCell)
        {
            List<Cell> neighbors = new List<Cell>();

            // Check for the neighbor to the North
            if(currentCell.Location.Y != 0)
            {
                Cell northNeighbor = maze[currentCell.Location.X,currentCell.Location.Y - 1];
                if (!northNeighbor.HasVisitor && !northNeighbor.IsLocked)
                    neighbors.Add(northNeighbor);
            }
            // Check for the neighbor to the East
            if (currentCell.Location.X < mazeSize.X - 1)
            {
                Cell eastNeighbor = maze[currentCell.Location.X + 1, currentCell.Location.Y];
                if (!eastNeighbor.HasVisitor && !eastNeighbor.IsLocked)
                    neighbors.Add(eastNeighbor);
            }
            // Check for the neighbor to the South
            if (currentCell.Location.Y < mazeSize.Y - 1)
            {
                Cell southNeighbor = maze[currentCell.Location.X, currentCell.Location.Y + 1];
                if (!southNeighbor.HasVisitor && !southNeighbor.IsLocked)
                    neighbors.Add(southNeighbor);
            }
            // Check for the neighbor to the West
            if (currentCell.Location.X != 0)
            {
                Cell westNeighbor = maze[currentCell.Location.X - 1, currentCell.Location.Y];
                if (!westNeighbor.HasVisitor && !westNeighbor.IsLocked)
                    neighbors.Add(westNeighbor);
            }

            return neighbors;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazeGenerator.Maze
{
    public class Cell
    {
        // Possible values for each room. Sum up the door values for the cell's total value
        // North wall = 1
        // East wall = 4
        // South wall = 2
        // West wall = 8

        private Point location;
        private int cellValue;
        private bool hasVisitor;
        private bool isActive;
        private bool isOnStack;
        private bool isLocked;

        public Point Location
        {
            get { return location; }
            set { location = value; }
        }

        public bool HasVisitor
        {
            get { return hasVisitor; }
            set { hasVisitor = value; }
        }

        public bool IsOnStack
        {
            get { return isOnStack; }
            set { isOnStack = value; }
        }

        public bool IsLocked
        {
            get { return isLocked; }
            set { isLocked = value; }
        }

        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }

        public int CellValue
        {
            get { return cellValue; }
            set { cellValue = value; }
        }

        public Cell(Point loc)
        {
            cellValue = 15;
            location = loc;
        }

        public Cell(int cVal, Point loc)
        {
            cellValue = cVal;
            location = loc;
        }
    }
}


using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNPCStats2.Common.Core
{

    public struct SmartRectangle
    {
        private Rectangle _rectangle;

        public int X
        {
            readonly get => _rectangle.X;
            set => _rectangle.X = value;
        }
        public int Y
        {
            readonly get => _rectangle.Y;
            set => _rectangle.Y = value;
        }
        public int Width
        {
            readonly get => _rectangle.Width;
            set => _rectangle.Width = value;
        }
        public int Height
        {
            readonly get => _rectangle.Height;
            set => _rectangle.Height = value;
        }

        public int Left
        {
            readonly get => _rectangle.X;
            set
            {
                _rectangle.Width += _rectangle.X - value;
                _rectangle.X = value;
            }
        }
        public int Right
        {
            readonly get => _rectangle.X + _rectangle.Width;
            set => _rectangle.Width = value - _rectangle.X;
        }
        public int Top
        {
            readonly get => _rectangle.Y;
            set
            {
                _rectangle.Height += _rectangle.Y - value;
                _rectangle.Y = value;
            }
        }
        public int Bottom
        {
            readonly get => _rectangle.Y + _rectangle.Height;
            set => _rectangle.Height = value - _rectangle.Y;
        }

        public Point TopLeft
        {
            readonly get => new Point(_rectangle.X, _rectangle.Y);
            set
            {
                Top = value.Y;
                Left = value.X;
            }
        }
        public Point TopRight
        {
            readonly get => new Point(_rectangle.X + _rectangle.Width, _rectangle.Y);
            set
            {
                Top = value.Y;
                Right = value.X;
            }
        }
        public Point BottomLeft
        {
            readonly get => new Point(_rectangle.X, _rectangle.Y + _rectangle.Height);
            set
            {
                Bottom = value.Y;
                Left = value.X;
            }
        }
        public Point BottomRight
        {
            readonly get => new Point(_rectangle.X + _rectangle.Width, _rectangle.Y + _rectangle.Height);
            set
            {
                Bottom = value.Y;
                Right = value.X;
            }
        }

        public void MoveTopLeft(Point point)
        {
            _rectangle.X = point.X;
            _rectangle.Y = point.Y;
        }

        public void MoveTopRight(Point point)
        {
            _rectangle.X = point.X - _rectangle.Width;
            _rectangle.Y = point.Y;
        }

        public void MoveBottomLeft(Point point)
        {
            _rectangle.X = point.X;
            _rectangle.Y = point.Y - _rectangle.Height;
        }

        public void MoveBottomRight(Point point)
        {
            _rectangle.X = point.X - _rectangle.Width;
            _rectangle.Y = point.Y - _rectangle.Height;
        }

        public readonly Point Center => TopLeft + new Point(_rectangle.Width / 2, _rectangle.Height / 2);

        public readonly int Area => _rectangle.Width * _rectangle.Height;

        public Point Size
        {
            readonly get => new Point(_rectangle.Width, _rectangle.Height);
            set
            {
                _rectangle.Width = value.X;
                _rectangle.Height = value.Y;
            }
        }
        public readonly bool HasNoSize => _rectangle.Width <= 0 || _rectangle.Height <= 0;

        public SmartRectangle Inflate(Point point) => Inflate(point.X, point.Y);
        public SmartRectangle Inflate(int x, int y)
        {
            _rectangle.Inflate(x, y);
            return this;
        }

        public SmartRectangle Offset(Point point) => Offset(point.X, point.Y);
        public SmartRectangle Offset(int x, int y)
        {
            _rectangle.Offset(x, y);
            return this;
        }

        public SmartRectangle Contains(Point point) => Contains(point.X, point.Y);
        public SmartRectangle Contains(int x, int y)
        {
            _rectangle.Contains(x, y);
            return this;
        }

        public bool Correct()
        {
            bool corrected = false;

            if (_rectangle.Width < 0)
            {
                _rectangle.X += _rectangle.Width;
                _rectangle.Width = -_rectangle.Width;
                corrected = true;
            }

            if (_rectangle.Height < 0)
            {
                _rectangle.Y += _rectangle.Height;
                _rectangle.Height = -_rectangle.Height;
                corrected = true;
            }

            return corrected;
        }

        public SmartRectangle()
        {
            _rectangle = Rectangle.Empty;
        }
        public SmartRectangle(int x = 0, int y = 0, int width = 0, int height = 0)
        {
            _rectangle = new Rectangle(x, y, width, height);
        }
        public SmartRectangle(Point start, Point end)
        {
            int x = Math.Min(start.X, end.X);
            int y = Math.Min(start.Y, end.Y);
            int width = Math.Abs(end.X - start.X);
            int height = Math.Abs(end.Y - start.Y);

            _rectangle = new Rectangle(x, y, width, height);
        }

        public SmartRectangle SetBounds(Point start, Point end)
        {
            if (end.X > start.X)
            {
                TopLeft = start;
                BottomRight = end;
            }
            else
            {
                TopRight = start;
                BottomLeft = end;
            }
            return this;
        }

        public SmartRectangle Flip(SmartRectangle reference, bool hori = false, bool vert = false)
        {
            if (hori)
            {
                _rectangle.X = reference.Right - (_rectangle.X + _rectangle.Width) + reference.Left;
            }
            if (vert)
            {
                _rectangle.Y = reference.Bottom - (_rectangle.Y + _rectangle.Height) + reference.Top;
            }
            return this;
        }


        public static implicit operator Rectangle(SmartRectangle smart)
        {
            return smart._rectangle;
        }

        public static implicit operator SmartRectangle(Rectangle rectangle)
        {
            return new SmartRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }

        public override string ToString()
        {
            return _rectangle.ToString();
        }
    }
}

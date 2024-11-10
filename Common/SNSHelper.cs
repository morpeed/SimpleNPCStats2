using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SimpleNPCStats2.Common.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace SimpleNPCStats2.Common
{
    public static class SNSHelper
    {
        public static UIElement SetRectangle(this UIElement element, Rectangle rectangle)
        {
            element.Left.Set(rectangle.X, 0);
            element.Top.Set(rectangle.Y, 0);
            element.Width.Set(rectangle.Width, 0);
            element.Height.Set(rectangle.Height, 0);
            element.Recalculate();
            return element;
        }
        public static UIElement SetRectangle(this UIElement element, int x, int y, int width, int height) => SetRectangle(element, new Rectangle(x, y, width, height));

        public static Rectangle GetRectangle(this UIElement element)
        {
            return new Rectangle((int)element.Left.Pixels, (int)element.Top.Pixels, (int)element.Width.Pixels, (int)element.Height.Pixels);
        }

        public static UIElement CopyRectangle(this UIElement element, UIElement other)
        {
            element.Left.Set(other.Left.Pixels, 0);
            element.Top.Set(other.Top.Pixels, 0);
            element.Width.Set(other.Width.Pixels, 0);
            element.Height.Set(other.Height.Pixels, 0);
            element.Recalculate();
            return element;
        }

        public static void SetSize(this UIElement element, float pixelsX, float pixelsY, float precentX, float precentY)
        {
            element.Width.Set(pixelsX, precentX);
            element.Height.Set(pixelsY, precentY);
        }

        public static void SetPosition(this UIElement element, float pixelsX, float pixelsY, float precentX, float precentY)
        {
            element.Left.Set(pixelsX, precentX);
            element.Top.Set(pixelsY, precentY);
        }

        public static void SetAlign(this UIElement element, float halign, float valign)
        {
            element.HAlign = halign;
            element.VAlign = valign;
        }

        public static void DebugDraw(this Rectangle rectangle, Color? color = null)
        {
            var drawColor = color ?? Color.White;
            drawColor *= 0.25f + (float)SNSHelper.BoundCos(Main.GlobalTimeWrappedHourly) * 0.25f;
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, drawColor);
        }

        public static double BoundCos(double num)
        {
            return (1 + Math.Cos(num)) / 2;
        }

        public static double DebugCos(float speed = 1) => BoundCos(Main.GlobalTimeWrappedHourly * speed);

        public static void DebugDrawBoundaries(this UIElement element)
        {
            DebugDraw(element.GetDimensions().ToRectangle(), Color.White * 0.5f);
            foreach (var e in element.Children)
            {
                DebugDraw(e.GetDimensions().ToRectangle(), Color.White * 0.5f);
            }
        }

        public static T CycleEnum<T>(this T currentEnum, int amount = 1, params T[] skipEnums) where T : Enum
        {
            var enumValues = Enum.GetValues(typeof(T));
            
            while (true)
            {
                int nextIndex = (Array.IndexOf(enumValues, currentEnum) + amount) % enumValues.Length;
                nextIndex = (nextIndex % enumValues.Length + enumValues.Length) % enumValues.Length;
                currentEnum = (T)enumValues.GetValue(nextIndex);

                if (!skipEnums.Contains(currentEnum))
                {
                    return currentEnum;
                }
            }
        }

        public static Vector2 Center(this Texture2D texture) => new Vector2(texture.Width / 2, texture.Height / 2);

        public static void SetPercentPadding(this UIElement element, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            element.Width.Percent = 1;
            element.Height.Percent = 1;

            element.Left.Pixels = left;
            element.Top.Pixels = top;

            element.Width.Pixels -= left + right;
            element.Height.Pixels -= top + bottom;
        }

        public const string LocalizationDirectory = "Mods.SimpleNPCStats2.";

        public static double RoundToEvery(double value, double roundTo) => Math.Round(value / roundTo) * roundTo;

        public static void Draw3x3Sprite(SpriteBatch spriteBatch, Texture2D texture, SmartRectangle destination, Point cornerSize, Color? color = null)
        {
            if (destination.HasNoSize)
            {
                return;
            }

            Color drawColor = color ?? Color.White;

            SmartRectangle textureBounds = texture.Bounds;
            SmartRectangle source = new SmartRectangle();
            SmartRectangle dest = new SmartRectangle();


            // Top Left
            source.SetBounds(Point.Zero, cornerSize);
            source.Width = Math.Min(destination.Width / 2, cornerSize.X);
            source.Height = Math.Min(destination.Height / 2, cornerSize.Y);
            dest.Size = source.Size;
            dest.MoveTopLeft(destination.TopLeft);
            spriteBatch.Draw(texture, dest, source, drawColor);

            //Top Right
            source.SetBounds(Point.Zero, cornerSize);
            source.Width = Math.Min((destination.Width + 1) / 2, cornerSize.X);
            source.Height = Math.Min(destination.Height / 2, cornerSize.Y);
            source.MoveTopRight(textureBounds.TopRight);
            dest.Size = source.Size;
            dest.MoveTopRight(destination.TopRight);
            spriteBatch.Draw(texture, dest, source, drawColor);

            //Bottom Left
            source.SetBounds(Point.Zero, cornerSize);
            source.Width = Math.Min(destination.Width / 2, cornerSize.X);
            source.Height = Math.Min((destination.Height + 1) / 2, cornerSize.Y);
            source.MoveBottomLeft(textureBounds.BottomLeft);
            dest.Size = source.Size;
            dest.MoveBottomLeft(destination.BottomLeft);
            spriteBatch.Draw(texture, dest, source, drawColor);

            //Bottom Right
            source.SetBounds(Point.Zero, cornerSize);
            source.Width = Math.Min((destination.Width + 1) / 2, cornerSize.X);
            source.Height = Math.Min((destination.Height + 1) / 2, cornerSize.Y);
            source.MoveBottomRight(textureBounds.BottomRight);
            dest.Size = source.Size;
            dest.MoveBottomRight(destination.BottomRight);
            spriteBatch.Draw(texture, dest, source, drawColor);

            bool drawHorizontalEdge = destination.Width - (cornerSize.X * 2) > 0;
            bool drawVerticalEdge = destination.Height - (cornerSize.Y * 2) > 0;

            if (drawHorizontalEdge)
            {
                //Top Middle
                source.SetBounds(new Point(cornerSize.X, 0), new Point(texture.Width - cornerSize.X, cornerSize.Y));
                source.Height = Math.Min(destination.Height / 2, cornerSize.Y);
                dest.TopLeft = new Point(destination.X + cornerSize.X, destination.Y);
                dest.Height = source.Height;
                dest.Width = destination.Width - cornerSize.X * 2;
                spriteBatch.Draw(texture, dest, source, drawColor);

                //Bottom Middle
                source.SetBounds(new Point(cornerSize.X, 0), new Point(texture.Width - cornerSize.X, cornerSize.Y));
                source.Height = Math.Min((destination.Height + 1) / 2, cornerSize.Y);
                dest.TopLeft = new Point(destination.X + cornerSize.X, destination.Y);
                dest.Height = source.Height;
                dest.Width = destination.Width - cornerSize.X * 2;
                source.Flip(textureBounds, vert: true);
                dest.Flip(destination, vert: true);
                spriteBatch.Draw(texture, dest, source, drawColor);
            }
            if (drawVerticalEdge)
            {
                // Left Middle
                source.SetBounds(new Point(0, cornerSize.Y), new Point(cornerSize.X, texture.Height - cornerSize.Y));
                source.Width = Math.Min(destination.Width / 2, cornerSize.X);
                dest.TopLeft = new Point(destination.X, destination.Y + cornerSize.Y);
                dest.Width = source.Width;
                dest.Height = destination.Height - cornerSize.Y * 2;
                spriteBatch.Draw(texture, dest, source, drawColor);

                // Right Middle
                source.SetBounds(new Point(0, cornerSize.Y), new Point(cornerSize.X, texture.Height - cornerSize.Y));
                source.Width = Math.Min((destination.Width + 1) / 2, cornerSize.X);
                dest.TopLeft = new Point(destination.X, destination.Y + cornerSize.Y);
                dest.Width = source.Width;
                dest.Height = destination.Height - cornerSize.Y * 2;
                source.Flip(textureBounds, hori: true);
                dest.Flip(destination, hori: true);
                spriteBatch.Draw(texture, dest, source, drawColor);

                if (drawHorizontalEdge)
                {
                    //Center
                    source.SetBounds(cornerSize, textureBounds.BottomRight - cornerSize);
                    dest.SetBounds(destination.TopLeft + cornerSize, destination.BottomRight - cornerSize);
                    spriteBatch.Draw(texture, dest, source, drawColor);
                }
            }
        }
    }
}

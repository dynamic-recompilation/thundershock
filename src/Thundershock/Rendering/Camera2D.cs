﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Rendering
{
    public class Camera2D : Camera
    {
        public AspectRatioMode AspectRatioMode { get; set; } = AspectRatioMode.ScaleVertically;

        public override Rectangle ViewportBounds
        {
            get
            {
                // TODO: make this not require the use of the fucking EntryPoint class.
                var screenSize = new Vector2(EntryPoint.CurrentApp.ScreenWidth, EntryPoint.CurrentApp.ScreenHeight);

                var aspectRatio = screenSize.X / screenSize.Y;

                var viewSize = new Vector2(ViewportWidth, ViewportHeight);

                if (AspectRatioMode == AspectRatioMode.ScaleHorizontally)
                {
                    viewSize.Y = viewSize.X * aspectRatio;
                }
                else if (AspectRatioMode == AspectRatioMode.ScaleVertically)
                {
                    viewSize.X = viewSize.Y * aspectRatio;
                }

                return new Rectangle(0, 0, (int) viewSize.X, (int) viewSize.Y);
            }
        }

        public override Matrix GetRenderTransform(GraphicsDevice gfx)
        {
            var scale2D = gfx.Viewport.Bounds.Size.ToVector2() / ViewportBounds.Size.ToVector2();
            
            var scale = new Vector3(scale2D.X, scale2D.Y, 1);
            
            var identity = Matrix.Identity;
            Matrix.CreateScale(ref scale, out identity);

            return identity;
        }
    }

    public enum AspectRatioMode
    {
        Ignore,
        ScaleHorizontally,
        ScaleVertically
    }
}
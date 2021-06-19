﻿using Thundershock.Core;
using Thundershock.Core.Rendering;
using System.Numerics;

namespace Thundershock.Gui.Elements
{
    public class Picture : Element
    {
        public Color Tint { get; set; } = Color.White;
        public Texture2D Image { get; set; }
        
        public bool Tile { get; set; }
        
        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            return Image?.Bounds.Size ?? Vector2.Zero;
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            if (Image == null)
                return;
            
            if (Tile)
            {
                for (var x = ContentRectangle.Left; x < ContentRectangle.Right; x += Image.Width)
                {
                    for (var y = ContentRectangle.Top; y < ContentRectangle.Bottom; y += Image.Height)
                    {
                        var rect = new Rectangle(x, y, Image.Width, Image.Height);
                        renderer.FillRectangle(rect, Image, Tint);
                    }
                }   
            }
            else
            {
                renderer.FillRectangle(ContentRectangle, Image, Tint);
            }
        }
    }
}
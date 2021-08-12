﻿using System;
using System.Numerics;
using FontStashSharp.Interfaces;

namespace Thundershock.Core.Rendering
{
    /// <summary>
    /// The Sprite Rocket is an extremely enhanced version of the MonoGame Sprite Batch with heaps more
    /// options for renderable polygons.
    /// </summary>
    public sealed class Renderer2D : IFontStashRenderer
    {
        private int[] _ibo = new int[128];
        private int _indexPointer;
        private int _batchPointer;
        private int _vertexPointer;
        private Vertex[] _vertexArray = new Vertex[128];
        private Effect _effect;
        private bool _running;
        private RenderItem[] _batch = new RenderItem[128];
        private Texture2D _blankTexture;
        private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
        private Renderer _renderer;

        public Matrix4x4 ProjectionMatrix
        {
            get => _projectionMatrix;
            set => _projectionMatrix = value;
        }
        
        public Renderer2D(GraphicsProcessor gpu)
        {
            _renderer = new Renderer(gpu);

            _blankTexture = new Texture2D(gpu, 1, 1, TextureFilteringMode.Point);
            _blankTexture.Upload(new byte[] {0xff, 0xff, 0xff, 0xff});
        }
        
        
        public void Begin(Matrix4x4? projection = null)
        {
            if (_running)
            {
                throw new InvalidOperationException("Cannot begin a new batch of sprites as the current batch has not been ended.");
            }
            
            _running = true;
            _batchPointer = 0;
            _indexPointer = 0;
            _vertexPointer = 0;
            _effect = null;
        }
        
        private RenderItem MakeRenderItem(Texture2D texture)
        {
            if (_running)
            {
                var tex = texture ?? _blankTexture;
                
                if (_batchPointer > 0)
                {
                    var last = _batch[_batchPointer - 1];
                    if (last.Texture == tex) return last;
                }

                var newBatchItem = new RenderItem(this);
                newBatchItem.Texture = tex;
                _batch[_batchPointer] = newBatchItem;

                _batchPointer++;
                if (_batchPointer >= _batch.Length)
                    Array.Resize(ref _batch, _batch.Length + 128);

                return newBatchItem;
            }
            else
            {
                throw new InvalidOperationException("You must call Begin() before you do this.");
            }
        }
        
        /// <summary>
        /// Ends the current batch and draws all polygons to the screen.
        /// </summary>
        /// <exception cref="InvalidOperationException">A batch hasn't begun yet.</exception>
        public void End()
        {
            if (_running)
            {
                if (_batchPointer > 0)
                {
                    _renderer.ProjectionMatrix = _projectionMatrix;
                    _renderer.Begin(_effect);

                    // Submit the vertex buffer. All vertices for every single batch item are in here.
                    // This is a massive optimization since now we don't need to do this on every draw call.
                    _renderer.UploadVertices(_vertexArray.AsSpan(0, _vertexPointer));

                    // Another optimization is to upload all batch indices right now. Each batch item will
                    // contain the information needed to only render the relevant triangles.
                    _renderer.UploadIndices(_ibo.AsSpan(0, _indexPointer));

                    for (var i = 0; i < _batchPointer; i++)
                    {
                        var item = _batch[i];
                        var tex = item.Texture;

                        var pCount = item.Triangles;

                        if (pCount >= 1)
                        {
                            _renderer.Textures[0] = tex;

                            _renderer.ProjectionMatrix = ProjectionMatrix;
                            _renderer.Draw(PrimitiveType.TriangleList, item.Start, pCount);
                        }
                    }

                    _renderer.End();

                    // Reset the vertex and index pointers to give the impression that we've cleared
                    // the buffers.  We don't actually clear the buffers because resizing the arrays
                    // is a bit slow and unnecessary.
                    _vertexPointer = 0;
                    _indexPointer = 0;
                    _batchPointer = 0;
                }
            }
            else
            {
                throw new InvalidOperationException("Cannot end the current batch as it has not yet begun.");
            }
            
            _running = false;
        }

        /// <summary>
        /// Draws a rectangular outline.
        /// </summary>
        /// <param name="bounds">The area to draw the outline in.</param>
        /// <param name="color">The color of the outline.</param>
        /// <param name="thickness">The width of each line.</param>
        public void DrawRectangle(Rectangle bounds, Color color, int thickness)
        {
            if (thickness < 1)
                return;

            if (color.A <= 0)
                return;

            if (bounds.IsEmpty)
                return;

            if (bounds.Width <= thickness * 2 || bounds.Height <= thickness * 2)
            {
                FillRectangle(bounds, color);
            }
            
            var left = new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height);
            var right = new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height);
            var top = new Rectangle(left.Right, bounds.Top, bounds.Width - (thickness * 2), thickness);
            var bottom = new Rectangle(top.Left, bounds.Bottom - thickness, top.Width, top.Height);
            
            FillRectangle(left, color);
            FillRectangle(top, color);
            FillRectangle(right, color);
            FillRectangle(bottom, color);
        }

        public void FillRectangle(Rectangle rect, Color color, Texture2D texture, Rectangle uv)
        {
            var batch = MakeRenderItem(texture);

            var tl = AddVertex(rect.Location, color, uv.Location);
            var tr = AddVertex(new Vector2(rect.Right, rect.Top), color, new Vector2(uv.Right, uv.Top));
            var bl = AddVertex(new Vector2(rect.Left, rect.Bottom), color, new Vector2(uv.Left, uv.Bottom));
            var br = AddVertex(new Vector2(rect.Right, rect.Bottom), color, new Vector2(uv.Right, uv.Bottom));
            
            batch.AddIndex(tr);
            batch.AddIndex(bl);
            batch.AddIndex(tl);
            
            batch.AddIndex(tr);
            batch.AddIndex(br);
            batch.AddIndex(bl);
        }
        
        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to draw.</param>
        /// <param name="color">THe color of the rectangle.</param>
        /// <param name="texture">The texture to fill the area with.</param>
        public void FillRectangle(Rectangle rect, Color color, Texture2D texture = null)
        {
            var renderItem = MakeRenderItem(texture);

            // add the 4 vertices
            var tl = AddVertex(new Vector2(rect.Left, rect.Top), color, TextureCoords.TopLeft);
            var tr = AddVertex(new Vector2(rect.Right, rect.Top), color, TextureCoords.TopRight);
            var bl = AddVertex(new Vector2(rect.Left, rect.Bottom), color, TextureCoords.BottomLeft);
            var br = AddVertex(new Vector2(rect.Right, rect.Bottom), color, TextureCoords.BottomRight);

            // firsst triangle
            renderItem.AddIndex(tr);
            renderItem.AddIndex(bl);
            renderItem.AddIndex(tl);

            // second triangle
            renderItem.AddIndex(tr);
            renderItem.AddIndex(br);
            renderItem.AddIndex(bl);

            // And that's how you draw a rectangle with two triangles!
        }
        
        /* Credit where credit's due
         * =========================
         *
         * The following code is from https://github.com/Jjagg/OpenWheels,
         * and has been adapted to work within SpriteRocket2D.
         */
        
        private void CreateLine(Vector2 p1, Vector2 p2, Color color, float lineWidth, out int v1, out int v2, out int v3, out int v4)
        {
            var d = Vector2.Normalize(p2 - p1);
            var dt = new Vector2(-d.Y, d.X) * (lineWidth / 2f);

            v1 = AddVertex(p1 + dt, color,TextureCoords.TopLeft);
            v2 = AddVertex(p1 - dt, color, TextureCoords.TopRight);
            v3 = AddVertex(p2 - dt, color, TextureCoords.BottomRight);
            v4 = AddVertex(p2 + dt, color, TextureCoords.BottomLeft);
        }
        
        private const float RightStartAngle = 0;
        private const float RightEndAngle = (float) (2 * Math.PI);

        /// <summary>
        /// Draws lines between two or more points, as if the game was playing Connect the Dots.
        /// </summary>
        /// <param name="points">A sequence of two or more points to draw lines between.</param>
        /// <param name="color">The color of each line.</param>
        /// <param name="lineWidth">The thickness of each line.</param>
        public void DrawLineStrip(ReadOnlySpan<Vector2> points, Color color, float lineWidth = 1)
        {
            if (points.Length < 2)
                return;

            var p1 = points[0];
            var p2 = points[1];

            CreateLine(p1, p2, color, lineWidth, out var i1, out var i2, out var i3, out var i4);

            var renderItem = MakeRenderItem(null);

            var i3Prev = i3;
            var i4Prev = i4;
            
            renderItem.AddIndex(i1);
            renderItem.AddIndex(i2);
            renderItem.AddIndex(i3);
            renderItem.AddIndex(i2);
            renderItem.AddIndex(i3);
            renderItem.AddIndex(i4);

            p1 = p2;
            
            for (var i = 2; i < points.Length; i++)
            {
                p2 = points[i];

                CreateLine(p1, p2, color, lineWidth, out i1, out i2, out i3, out i4);

                renderItem.AddIndex(i1);
                renderItem.AddIndex(i2);
                renderItem.AddIndex(i3);
                renderItem.AddIndex(i2);
                renderItem.AddIndex(i3);
                renderItem.AddIndex(i4);

                renderItem.AddIndex(i3Prev);
                renderItem.AddIndex(i4Prev);
                renderItem.AddIndex(i2);

                i3Prev = i3;
                i4Prev = i4;
                p1 = p2;
            }

        }
        
        private void FillTriangleFan(Vector2 center, ref Span<Vector2> vs, Color color, Texture2D texture)
        {
            var c = vs.Length;
            if (c < 2)
                throw new ArgumentException(@"Need at least 3 vertices for a triangle fan.", nameof(vs));

            var renderItem = MakeRenderItem(texture);

            var texWidth = texture?.Width ?? 1;
            var texHeight = texture?.Height ?? 1;
            var texDiameter = (float) Math.Min(texWidth, texHeight);
            var u = texDiameter / (float) texWidth;
            var v = texDiameter / (float) texHeight;

            var ul = (1f - u) / 2;
            var ut = (1f - v) / 2;

            var point5 = new Vector2(0.5f, 0.5f);
            var centerIndex = AddVertex(center, color, point5);
            
            var rs = Vector2.Distance(center, vs[0]);

            var imageRect = Rectangle.FromHalfExtents(center, rs);
            var uvRect = new Rectangle(ul, ut, u, v);

            var uv1 = Rectangle.MapVec2(vs[0], imageRect, uvRect);

            var v1 = AddVertex(vs[0], color, uv1);

            for (var i = 1; i < c; i++)
            {
                var uv = Rectangle.MapVec2(vs[i], imageRect, uvRect);
                var v2 = AddVertex(vs[i], color, uv);
                renderItem.AddIndex(centerIndex);
                renderItem.AddIndex(v1);
                renderItem.AddIndex(v2);
                v1 = v2;
            }
        }
        
        /// <summary>
        /// Draws a filled circle.
        /// </summary>
        /// <param name="center">The center point of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="color">The color to draw the circle with.</param>
        /// <param name="maxError">https://youtu.be/hQ3GW7lVBWY</param>
        public void FillCircle(Vector2 center, float radius, Color color, float maxError = 0.25f)
        {
            FillCircle(center, radius, color, null, maxError);
        }

        public void FillCircle(Vector2 center, float radius, Color color, Texture2D texture, float maxError = 0.000001f)
        {
            FillCircleSegment(center, radius, RightStartAngle, RightEndAngle, color, texture, maxError);
        }
        
        void IFontStashRenderer.Draw(object texture, Vector2 pos, System.Drawing.Rectangle? src, System.Drawing.Color color, float rotation, Vector2 origin, Vector2 scale,
            float depth)
        {
            var tsTexture = texture as Texture2D;

            // var rotate = Matrix4x4.CreateRotationZ(rotation);
            // var scaleMatrix = Matrix4x4.CreateScale(scale.X, scale.Y, 1);

            // var transform = scaleMatrix * rotate;

            pos -= origin;
            
            var tsColor = new Color(color.R, color.G, color.B, color.A);

            var tsRectangle = Rectangle.Unit;
            var tsDrawRect = new Rectangle(0, 0, tsTexture.Width, tsTexture.Height);
            
            if (src != null)
            {
                tsRectangle.X = (float) src?.Left / tsTexture.Width;
                tsRectangle.Width = (float) src?.Width / tsTexture.Width;
                
                tsRectangle.Y = (float) src?.Top / tsTexture.Height;
                tsRectangle.Height = (float) src?.Height / tsTexture.Height;

                tsDrawRect.Width = (float) src?.Width;
                tsDrawRect.Height = (float) src?.Height;
            }

            tsDrawRect.X = pos.X;
            tsDrawRect.Y = pos.Y;
            
            FillRectangle(tsDrawRect, tsColor, tsTexture, tsRectangle);
        }

        private static void CreateCircleSegment(Vector2 center, float radius, float step, float start, float end, ref Span<Vector2> result)
        {
            var i = 0;
            float theta;
            for (theta = start; theta < end; theta += step)
                result[i++] = new Vector2((float) (center.X + radius * Math.Cos(theta)), (float) (center.Y + radius * Math.Sin(theta)));

            if (theta != end)
                result[i] = center + new Vector2((float) (radius * Math.Cos(end)), (float) (radius * Math.Sin(end)));
        }
        
        private void FillCircleSegment(Vector2 center, float radius, float start, float end, Color color, Texture2D texture, float maxError)
        {
            if (radius <= 0)
                return;

            if (color.A <= 0)
                return;
            
            ComputeCircleSegments(radius, maxError, end - start, out var step, out var segments);

            if (segments <= 0)
                return;
            
            Span<Vector2> points = stackalloc Vector2[segments + 1];
            CreateCircleSegment(center, radius, step, start, end, ref points);
            
            FillTriangleFan(center, ref points, color, texture);
        }
        
        private void ComputeCircleSegments(float radius, float maxError, float range, out float step, out int segments)
        {
            var invErrRad = 1 - maxError / radius;
            step = (float) Math.Acos(2 * invErrRad * invErrRad - 1);
            segments = (int) (range / step + 0.999f);
        }
        
        private class RenderItem
        {
            private Renderer2D _renderer;
            private int _batchStart;
            private int _batchLength;

            public int Triangles => _batchLength / 3;

            public int Start => _batchStart;
            
            public Texture2D Texture { get; set; }
            
            public RenderItem(Renderer2D renderer)
            {
                _renderer = renderer;
                _batchStart = _renderer._indexPointer;
            }

            public void AddIndex(int index)
            {
                _renderer._ibo[_renderer._indexPointer] = index;
                _renderer._indexPointer++;
                if (_renderer._indexPointer >= _renderer._ibo.Length)
                    Array.Resize(ref _renderer._ibo, _renderer._ibo.Length + 128);
                _batchLength++;
            }
        }

        private int AddVertex(Vector2 position, Color color, Vector2 texCoord)
        {
            var vert = new Vertex(new Vector3(position, 0), color, texCoord);
            var ptr = _vertexPointer;
            _vertexArray[_vertexPointer] = vert;
            _vertexPointer++;

            if (_vertexPointer >= _vertexArray.Length)
            {
                Array.Resize(ref _vertexArray, _vertexArray.Length + 128);
            }
            
            return ptr;
        }
        
        private static class TextureCoords
        {
            public static Vector2 TopLeft => Vector2.Zero;
            public static Vector2 TopRight => new Vector2(1, 0);
            public static Vector2 BottomLeft => new Vector2(0, 1);
            public static Vector2 BottomRight => Vector2.One;
        }
    }
}
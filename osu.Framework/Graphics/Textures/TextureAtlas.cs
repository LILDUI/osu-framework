﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Drawing;
using osu.Framework.Graphics.OpenGL.Textures;
using OpenTK.Graphics.ES20;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Textures
{
    class TextureGLAtlas : TextureGLSingle
    {
        public TextureGLAtlas(int width, int height) : base(width, height, false)
        {
        }
    }

    class TextureGLAtlasWhite : TextureGLSub
    {
        public TextureGLAtlasWhite(TextureGLSingle parent) : base(new Rectangle(0, 0, 1, 1), parent)
        {
        }

        public override bool Bind()
        {
            //we can use the special white space from any atlas texture.
            if (GLWrapper.AtlasTextureIsBound)
                return true;

            return base.Bind();
        }
    }

    public class TextureAtlas
    {
        private List<Rectangle> subTextureBounds = new List<Rectangle>();
        private TextureGLSingle atlasTexture;

        private int atlasWidth;
        private int atlasHeight;

        private int currentY;

        private int mipmapLevels => (int)Math.Floor(Math.Log(atlasWidth) / Math.Log(2));

        public TextureAtlas(int width, int height)
        {
            atlasWidth = width;
            atlasHeight = height;
        }

        public void Reset()
        {
            subTextureBounds.Clear();
            currentY = 0;

            atlasTexture = new TextureGLSingle(atlasWidth, atlasHeight);

            using (var whiteTex = Add(2, 2))
            {
                //add an empty white rect to use for solid box drawing (shader optimisation).
                byte[] white = new byte[whiteTex.Width * whiteTex.Height * 4];
                for (int i = 0; i < white.Length; i++)
                    white[i] = 255;
                whiteTex.SetData(new TextureUpload(white));
            }
        }

        private Point findPosition(int width, int height)
        {
            // Super naive implementation only going from left to right.
            Point res = new Point(0, currentY);

            if (currentY + height > atlasHeight)
            {
                Reset();
                return new Point(0, 0);
            }

            int maxY = currentY;
            foreach (Rectangle bounds in subTextureBounds)
            {
                // +1 is required to prevent aliasing issues with sub-pixel positions while drawing. Bordering edged of other textures can show without it.
                res.X = Math.Max(res.X, bounds.Right + 4);
                maxY = Math.Max(maxY, bounds.Bottom);
            }

            if (res.X + width > atlasWidth)
            {
                // +1 is required to prevent aliasing issues with sub-pixel positions while drawing. Bordering edged of other textures can show without it.
                currentY = maxY + 4;
                subTextureBounds.Clear();
                res = findPosition(width, height);
            }

            return res;
        }

        internal Texture Add(int width, int height)
        {
            lock (this)
            {
                if (atlasTexture == null)
                    Reset();

                Point position = findPosition(width, height);
                Rectangle bounds = new Rectangle(position.X, position.Y, width, height);
                subTextureBounds.Add(bounds);

                return new Texture(new TextureGLSub(bounds, atlasTexture));
            }
        }

        internal Texture GetWhitePixel()
        {
            if (atlasTexture == null)
                Reset();

            return new Texture(new TextureGLAtlasWhite(atlasTexture));
        }
    }
}

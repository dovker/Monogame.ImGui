using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.ImGui.Data
{
    /// <summary>
    /// Contains the GUIRenderer's texture data element.
    /// </summary>
    public class TextureData
    {
        public int TextureID;
        public IntPtr? FontTextureID;
        public Dictionary<IntPtr, Texture2D> Loaded;

        public int GetTextureID()
        {
            return TextureID++;
        }

        public TextureData()
        {
            Loaded = new Dictionary<IntPtr, Texture2D>();
        }
    }
}

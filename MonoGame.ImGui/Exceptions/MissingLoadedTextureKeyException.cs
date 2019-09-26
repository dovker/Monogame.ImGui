using System;

namespace MonoGame.ImGui.Exceptions
{
    public class MissingLoadedTextureKeyException
        : InvalidOperationException
    {
        public override string Message
        {
            get { return string.Format("Could not find a texture with id {0}, please check your bindings", _texture_id); }
        }

        public MissingLoadedTextureKeyException(IntPtr texture_id)
        {
            _texture_id = texture_id;
        }

        private readonly IntPtr _texture_id;
    }
}

namespace MonoGame.ImGui.Exceptions; 

public class MissingLoadedTextureKeyException : InvalidOperationException {
    private readonly IntPtr _textureId;

    public MissingLoadedTextureKeyException(IntPtr textureId) {
        _textureId = textureId;
    }

    public override string Message => $"Could not find a texture with id {_textureId}, please check your bindings";
}
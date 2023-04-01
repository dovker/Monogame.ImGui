using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGui.Data;
using MonoGame.ImGui.Exceptions;
using MonoGame.ImGui.Utilities;

namespace MonoGame.ImGui; 

/// <summary>
///     Responsible for rendering the ImGui elements to the screen.
/// </summary>
public class ImGuiRenderer {
    public ImGuiRenderer(Game owner) {
        Owner = owner;
        _effect = new BasicEffect(owner.GraphicsDevice);
        _textureData = new TextureData();
        _rasterizerState = GenerateRasterizerState.Perform();
        _inputData = new InputData();
        _vertex = new VertexData();
        _index = new IndexData();
    }

    public Game Owner { get; }
    public GraphicsDevice GraphicsDevice => Owner.GraphicsDevice;


    public virtual void BeginLayout(GameTime gameTime) {
        ImGuiNET.ImGui.GetIO().DeltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
        _inputData.Update(Owner);

        ImGuiNET.ImGui.NewFrame();
    }

    public virtual void EndLayout() {
        ImGuiNET.ImGui.Render();
        RenderDrawData(ImGuiNET.ImGui.GetDrawData());
    }

    public virtual IntPtr BindTexture(Texture2D texture) {
        var id = new IntPtr(_textureData.GetNextTextureId());
        _textureData.Loaded.Add(id, texture);

        return id;
    }

    public virtual void UnbindTexture(IntPtr textureId) {
        _textureData.Loaded.Remove(textureId);
    }

    public ImGuiRenderer Initialize() {
        var context = ImGuiNET.ImGui.CreateContext();
        ImGuiNET.ImGui.SetCurrentContext(context);


        _inputData.Initialize(Owner);
        return this;
    }

    public virtual unsafe ImGuiRenderer RebuildFontAtlas() {
        // Get font texture from ImGui
        var io = ImGuiNET.ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out var width, out var height, out var bytesPerPixel);

        // Copy the data to a managed array
        var pixels = new byte[width * height * bytesPerPixel];
        Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);
        // Create and register the texture as an XNA texture
        var texture = new Texture2D(GraphicsDevice, width, height, false, SurfaceFormat.Color);
        texture.SetData(pixels);

        // Should a texture already have been build previously, unbind it first so it can be deallocated
        if (_textureData.FontTextureID.HasValue)
            UnbindTexture(_textureData.FontTextureID.Value);

        // Bind the new texture to an ImGui-friendly id
        _textureData.FontTextureID = BindTexture(texture);

        // Let ImGui know where to find the texture
        io.Fonts.SetTexID(_textureData.FontTextureID.Value);
        io.Fonts.ClearTexData(); // Clears CPU side texture data
        return this;
    }

    private void RenderDrawData(ImDrawDataPtr drawData) {
        // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers
        var lastViewport = GraphicsDevice.Viewport;
        var lastScissorRect = GraphicsDevice.ScissorRectangle;

        GraphicsDevice.BlendFactor = Color.White;
        GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        GraphicsDevice.RasterizerState = _rasterizerState;
        GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp; //ADD THIS LINE


        // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
        drawData.ScaleClipRects(ImGuiNET.ImGui.GetIO().DisplayFramebufferScale);

        // Setup projection
        GraphicsDevice.Viewport = new Viewport(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);

        UpdateBuffers(drawData);
        RenderCommandLists(drawData);

        // Restore modified state
        GraphicsDevice.Viewport = lastViewport;
        GraphicsDevice.ScissorRectangle = lastScissorRect;
    }

    private void RenderCommandLists(ImDrawDataPtr drawData) {
        GraphicsDevice.SetVertexBuffer(_vertex.Buffer);
        GraphicsDevice.Indices = _index.Buffer;

        var vertexOffset = 0;
        var indexOffset = 0;
        for (var i = 0; i < drawData.CmdListsCount; ++i) {
            var commandList = drawData.CmdListsRange[i];
            for (var commandIndex = 0; commandIndex < commandList.CmdBuffer.Size; ++commandIndex) {
                var drawCommand = commandList.CmdBuffer[commandIndex];

                if (!_textureData.Loaded.ContainsKey(drawCommand.TextureId))
                    throw new MissingLoadedTextureKeyException(drawCommand.TextureId);

                GraphicsDevice.ScissorRectangle = GenerateScissorRect(drawCommand);
                var effect = UpdateEffect(_textureData.Loaded[drawCommand.TextureId]);

                foreach (var pass in effect.CurrentTechnique.Passes) {
                    pass.Apply();
                    DrawPrimitives(vertexOffset, indexOffset, commandList, drawCommand);
                }

                indexOffset += (int) drawCommand.ElemCount;
            }

            vertexOffset += commandList.VtxBuffer.Size;
        }
    }

    private Rectangle GenerateScissorRect(ImDrawCmdPtr drawCommand) {
        return new Rectangle(
            (int) drawCommand.ClipRect.X,
            (int) drawCommand.ClipRect.Y,
            (int) (drawCommand.ClipRect.Z - drawCommand.ClipRect.X),
            (int) (drawCommand.ClipRect.W - drawCommand.ClipRect.Y));
    }

    private void DrawPrimitives(int vertexOffset, int indexOffset, ImDrawListPtr commandList, ImDrawCmdPtr drawCommand) {
#pragma warning disable CS0618

#pragma warning disable CS0618

        GraphicsDevice.DrawIndexedPrimitives(
            PrimitiveType.TriangleList, vertexOffset, 0,
            commandList.VtxBuffer.Size, indexOffset, (int) (drawCommand.ElemCount / 3));


#pragma warning restore CS0618
    }

    protected virtual Effect UpdateEffect(Texture2D texture) {
        var io = ImGuiNET.ImGui.GetIO();
        var displaySize = io.DisplaySize;

        const float offset = 0.0f;
        _effect.World = Matrix.Identity;
        _effect.View = Matrix.Identity;
        _effect.Projection = Matrix.CreateOrthographicOffCenter(offset, displaySize.X + offset, displaySize.Y + offset, offset, -1.0f, 1.0f);
        _effect.TextureEnabled = true;
        _effect.Texture = texture;
        _effect.VertexColorEnabled = true;

        return _effect;
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData) {
        if (drawData.TotalVtxCount == 0)
            return;

        if (drawData.TotalVtxCount > _vertex.BufferSize) {
            _vertex.Buffer?.Dispose();
            _vertex.BufferSize = (int) (drawData.TotalVtxCount * 1.5f);
            _vertex.Buffer = new VertexBuffer(GraphicsDevice, DrawVertDeclaration.Declaration, _vertex.BufferSize, BufferUsage.None);
            _vertex.Data = new byte[_vertex.BufferSize * DrawVertDeclaration.Size];
        }

        if (drawData.TotalIdxCount > _index.BufferSize) {
            _index.Buffer?.Dispose();

            _index.BufferSize = (int) (drawData.TotalIdxCount * 1.5f);
            _index.Buffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, _index.BufferSize, BufferUsage.None);
            _index.Data = new byte[_index.BufferSize * sizeof(ushort)];
        }

        var vertexOffset = 0;
        var indexOffset = 0;

        for (var i = 0; i < drawData.CmdListsCount; ++i) {
            var commands = drawData.CmdListsRange[i];
            fixed (void* vtxDstPtr = &_vertex.Data[vertexOffset * DrawVertDeclaration.Size])
            fixed (void* idxDstPtr = &_index.Data[indexOffset * sizeof(ushort)]) {
                Buffer.MemoryCopy((void*) commands.VtxBuffer.Data, vtxDstPtr, _vertex.Data.Length, commands.VtxBuffer.Size * DrawVertDeclaration.Size);
                Buffer.MemoryCopy((void*) commands.IdxBuffer.Data, idxDstPtr, _index.Data.Length, commands.IdxBuffer.Size * sizeof(ushort));
            }

            vertexOffset += commands.VtxBuffer.Size;
            indexOffset += commands.IdxBuffer.Size;
        }

        // Copy the managed byte arrays to the gpu vertex- and index buffers
        _vertex.Buffer.SetData(_vertex.Data, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
        _index.Buffer.SetData(_index.Data, 0, drawData.TotalIdxCount * sizeof(ushort));
    }

    private readonly IndexData _index;
    private readonly VertexData _vertex;
    private readonly InputData _inputData;
    private readonly TextureData _textureData;
    private readonly BasicEffect _effect;
    private readonly RasterizerState _rasterizerState;
}
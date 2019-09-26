using System;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGui.Data;
using MonoGame.ImGui.Exceptions;
using MonoGame.ImGui.Utilities;

namespace MonoGame.ImGui
{
    /// <summary>
    /// Responsible for rendering the ImGui elements to the screen.
    /// </summary>
    public class ImGUIRenderer
    {
        public Game Owner { get; private set; }
        public GraphicsDevice GraphicsDevice { get { return Owner.GraphicsDevice; } }


        public virtual void BeginLayout(GameTime gameTime)
        {
            ImGuiNET.ImGui.GetIO().DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _input_data.Update(GraphicsDevice);

            ImGuiNET.ImGui.NewFrame();
        }

        public virtual void EndLayout()
        {
            ImGuiNET.ImGui.Render();
            unsafe { RenderDrawData(ImGuiNET.ImGui.GetDrawData()); }
        }

        public virtual IntPtr BindTexture(Texture2D texture)
        {
            var id = new IntPtr(_texture_data.GetTextureID());
            _texture_data.Loaded.Add(id, texture);

            return id;
        }

        public virtual void UnbindTexture(IntPtr textureId)
        {
            _texture_data.Loaded.Remove(textureId);
        }

        public ImGUIRenderer(Game owner)
        {
            Owner = owner;
            _effect = new BasicEffect(owner.GraphicsDevice);
            _texture_data = new TextureData();
            _rasterizer_state = GenerateRasterizerState.Perform();
            _input_data = new InputData();
            _vertex = new VertexData();
            _index = new IndexData();

        }

        public ImGUIRenderer Initialize()
        {
            var context = ImGuiNET.ImGui.CreateContext();
            ImGuiNET.ImGui.SetCurrentContext(context);


            _input_data.Initialize(Owner);
            return this;
        }

        public virtual unsafe ImGUIRenderer RebuildFontAtlas()
        {
            // Get font texture from ImGui
            var io = ImGuiNET.ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

            // Copy the data to a managed array
            var pixels = new byte[width * height * bytesPerPixel];
            unsafe { Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length); }
            // Create and register the texture as an XNA texture
            var texture = new Texture2D(GraphicsDevice, width, height, false, SurfaceFormat.Color);
            texture.SetData(pixels);

            // Should a texture already have been build previously, unbind it first so it can be deallocated
            if (_texture_data.FontTextureID.HasValue)
                UnbindTexture(_texture_data.FontTextureID.Value);

            // Bind the new texture to an ImGui-friendly id
            _texture_data.FontTextureID = BindTexture(texture);

            // Let ImGui know where to find the texture
            io.Fonts.SetTexID(_texture_data.FontTextureID.Value);
            io.Fonts.ClearTexData(); // Clears CPU side texture data
            return this;
        }

        private void RenderDrawData(ImDrawDataPtr drawData)
        {
            // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers
            var last_viewport = GraphicsDevice.Viewport;
            var last_scissor_rect = GraphicsDevice.ScissorRectangle;

            GraphicsDevice.BlendFactor = Color.White;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.RasterizerState = _rasterizer_state;
            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp; //ADD THIS LINE


            // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
            drawData.ScaleClipRects(ImGuiNET.ImGui.GetIO().DisplayFramebufferScale);

            // Setup projection
            GraphicsDevice.Viewport = new Viewport(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);

            UpdateBuffers(drawData);
            RenderCommandLists(drawData);

            // Restore modified state
            GraphicsDevice.Viewport = last_viewport;
            GraphicsDevice.ScissorRectangle = last_scissor_rect;
        }

        private unsafe void RenderCommandLists(ImDrawDataPtr draw_data)
        {
            GraphicsDevice.SetVertexBuffer(_vertex.Buffer);
            GraphicsDevice.Indices = _index.Buffer;

            var vertex_offset = 0;
            var index_offset = 0;
            for (var i = 0; i < draw_data.CmdListsCount; ++i)
            {
                var command_list = draw_data.CmdListsRange[i];
                for (var command_index = 0; command_index < command_list.CmdBuffer.Size; ++command_index)
                {
                    var draw_command = command_list.CmdBuffer[command_index];

                    if (!_texture_data.Loaded.ContainsKey(draw_command.TextureId))
                        throw new MissingLoadedTextureKeyException(draw_command.TextureId);

                    GraphicsDevice.ScissorRectangle = GenerateScissorRect(draw_command);
                    var effect = UpdateEffect(_texture_data.Loaded[draw_command.TextureId]);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        DrawPrimitives(vertex_offset, index_offset, command_list, draw_command);
                    }

                    index_offset += (int)draw_command.ElemCount;
                }

                vertex_offset += command_list.VtxBuffer.Size;
            }
        }

        private Rectangle GenerateScissorRect(ImDrawCmdPtr draw_command)
        {
            return new Rectangle(
                (int)draw_command.ClipRect.X,
                (int)draw_command.ClipRect.Y,
                (int)(draw_command.ClipRect.Z - draw_command.ClipRect.X),
                (int)(draw_command.ClipRect.W - draw_command.ClipRect.Y));
        }

        private void DrawPrimitives(int vertex_offset, int index_offset, ImDrawListPtr command_list, ImDrawCmdPtr draw_command)
        {

#pragma warning disable CS0618

#pragma warning disable CS0618

            GraphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList, vertex_offset, 0,
                command_list.VtxBuffer.Size, index_offset, (int)((draw_command.ElemCount / 3)));


#pragma warning restore CS0618
        }

        protected virtual Effect UpdateEffect(Texture2D texture)
        {
            var io = ImGuiNET.ImGui.GetIO();
            var display_size = io.DisplaySize;

            const float offset = 0.5f;
            _effect.World = Matrix.Identity;
            _effect.View = Matrix.Identity;
            _effect.Projection = Matrix.CreateOrthographicOffCenter(offset, display_size.X + offset, display_size.Y + offset, offset, -1.0f, 1.0f);
            _effect.TextureEnabled = true;
            _effect.Texture = texture;
            _effect.VertexColorEnabled = true;

            return _effect;
        }

        private unsafe void UpdateBuffers(ImDrawDataPtr draw_data)
        {
            if (draw_data.TotalVtxCount == 0)
                return;

            if (draw_data.TotalVtxCount > _vertex.BufferSize)
            {
                _vertex.Buffer?.Dispose();
                _vertex.BufferSize = (int)(draw_data.TotalVtxCount * 1.5f);
                _vertex.Buffer = new VertexBuffer(GraphicsDevice, DrawVertDeclaration.Declaration, _vertex.BufferSize, BufferUsage.None);
                _vertex.Data = new byte[_vertex.BufferSize * DrawVertDeclaration.Size];
            }

            if (draw_data.TotalIdxCount > _index.BufferSize)
            {
                _index.Buffer?.Dispose();

                _index.BufferSize = (int)(draw_data.TotalIdxCount * 1.5f);
                _index.Buffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, _index.BufferSize, BufferUsage.None);
                _index.Data = new byte[_index.BufferSize * sizeof(ushort)];
            }

            var vertex_offset = 0;
            var index_offset = 0;

            for (var i = 0; i < draw_data.CmdListsCount; ++i)
            {
                var commands = draw_data.CmdListsRange[i];
                fixed (void* vtxDstPtr = &_vertex.Data[vertex_offset * DrawVertDeclaration.Size])
                fixed (void* idxDstPtr = &_index.Data[index_offset * sizeof(ushort)])
                {
                    Buffer.MemoryCopy((void*)commands.VtxBuffer.Data, vtxDstPtr, _vertex.Data.Length, commands.VtxBuffer.Size * DrawVertDeclaration.Size);
                    Buffer.MemoryCopy((void*)commands.IdxBuffer.Data, idxDstPtr, _index.Data.Length, commands.IdxBuffer.Size * sizeof(ushort));
                }

                vertex_offset += commands.VtxBuffer.Size;
                index_offset += commands.IdxBuffer.Size;
            }

            // Copy the managed byte arrays to the gpu vertex- and index buffers
            _vertex.Buffer.SetData(_vertex.Data, 0, draw_data.TotalVtxCount * DrawVertDeclaration.Size);
            _index.Buffer.SetData(_index.Data, 0, draw_data.TotalIdxCount * sizeof(ushort));
        }

        private readonly IndexData _index;
        private readonly VertexData _vertex;
        private readonly InputData _input_data;
        private readonly TextureData _texture_data;
        private readonly BasicEffect _effect;
        private readonly RasterizerState _rasterizer_state;
    }
}

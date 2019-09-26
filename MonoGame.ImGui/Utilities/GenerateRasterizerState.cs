using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.ImGui.Utilities
{
    /// <summary>
    /// Responsible for generating the default rasterizer state of the renderer.
    /// </summary>
    public static class GenerateRasterizerState
    {
        public static RasterizerState Perform()
        {
            return new RasterizerState
            {
                CullMode = CullMode.None,
                DepthBias = 0,
                FillMode = FillMode.Solid,
                MultiSampleAntiAlias = false,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0
            };
        }
    }
}

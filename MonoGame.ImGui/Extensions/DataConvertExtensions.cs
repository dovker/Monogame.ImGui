using NumVector2 = System.Numerics.Vector2;
using NumVector3 = System.Numerics.Vector3;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace MonoGame.ImGui.Extensions
{
    /// <summary>
    /// Extends different monogame data containers with numeric-based conversions.
    /// </summary>
    public static class DataConvertExtensions
    {
        public static XnaVector3 ToXnaVector3(this NumVector3 value)
        {
            return new XnaVector3(value.X, value.Y, value.Z);
        }

        public static XnaVector2 ToXnaVector2(this NumVector2 value)
        {
            return new XnaVector2(value.X, value.Y);
        }

        public static NumVector3 ToNumericVector3(this XnaVector3 value)
        {
            return new NumVector3(value.X, value.Y, value.Z);
        }

        public static NumVector2 ToNumericVector2(this XnaVector2 value)
        {
            return new NumVector2(value.X, value.Y);
        }
    }
}

using System;

namespace Raylib
{
    public static class RaylibVector2Extensions
    {
        // ============================= Vector2 Extensions ================================

        // ----------------------------- From raylib ---------------------------------------
        // Adding missing raymath functions to the Vector2 class
        public static float Angle(this Vector2 vec)
        {
            return MathF.Atan2(vec.y, vec.x);
        }

        public static float Angle(this Vector2 vec, Vector2 vector)
        {
            return Raylib.Vector2Angle(vec, vector);
        }

        public static float DotProduct(this Vector2 vec, Vector2 vector)
        {
            return Raylib.Vector2DotProduct(vec, vector);
        }

        public static float Distance(this Vector2 vec, Vector2 vector)
        {
            return Raylib.Vector2Distance(vec, vector);
        }

        public static Vector2 Normalize(this Vector2 vec)
        {
            return Raylib.Vector2Normalize(vec);
        }

        public static Vector2 Lerp(this Vector2 vec, Vector2 vector, float amount)
        {
            return Raylib.Vector2Lerp(vec, vector, amount);
        }

        public static Vector2 Rotate(this Vector2 vec, float rads)
        {
            Vector2 result = new Vector2( vec.x * MathF.Cos(rads) - vec.y * MathF.Sin(rads), vec.x * MathF.Sin(rads) + vec.y * MathF.Cos(rads) );
            return result;
        }

        // ----------------------------- New ---------------------------------------
        // Things I've found useful
        public static Rectangle ToRectangle(this Vector2 vec)
        {
            return new Rectangle(0,0,vec.x,vec.y);
        }
    }
}

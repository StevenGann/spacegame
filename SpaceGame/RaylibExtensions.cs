using System;
using System.Numerics;

namespace Raylib_cs
{
    public static class RaylibVector2Extensions
    {
        // ============================= Vector2 Extensions ================================

        // ----------------------------- From raylib ---------------------------------------
        // Adding missing raymath functions to the Vector2 class
        public static float Angle(this Vector2 vec)
        {
            return MathF.Atan2(vec.Y, vec.X);
        }

        /*public static float Angle(this Vector2 vec, Vector2 vector)
        {
            return Raylib_cs.Vector2Angle(vec, vector);
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
        }*/

        public static Vector2 Rotate(this Vector2 vec, float rads)
        {
            Vector2 result = new Vector2(vec.X * MathF.Cos(rads) - vec.Y * MathF.Sin(rads), vec.X * MathF.Sin(rads) + vec.Y * MathF.Cos(rads));
            return result;
        }

        // ----------------------------- New ---------------------------------------
        // Things I've found useful
        public static Rectangle ToRectangle(this Vector2 vec)
        {
            return new Rectangle(0, 0, vec.X, vec.Y);
        }

        // ============================= Color Extensions ================================
        // ----------------------------- New ---------------------------------------
        public static Color SetAlpha(this Color color, byte alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
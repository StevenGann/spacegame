using Raylib;
using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

using Color = Raylib.Color;
using Rectangle = Raylib.Rectangle;

namespace SpaceGame
{
    public class Hitbox
    {
        public List<Triangle> Triangles = null;
        public float Radius { get; set; } = 0;

        public Hitbox()
        {
            Triangles = new List<Triangle>();
        }

        public Hitbox(Vector2[] Vertices)
        {
            Triangles = new List<Triangle>();
            AddVertices(Vertices);
        }

        public Hitbox(Rectangle Box)
        {
            Triangles = new List<Triangle>();
            AddRectangle(Box);
        }

        public Hitbox(float Radius)
        {
            this.Radius = Radius;
        }

        public void AddVertices(Vector2[] Vertices)
        {
            for (int i = 0; i < Vertices.Length; i += 3)
            {
                Triangle t = new Triangle();
                t.A = Vertices[i];
                t.B = Vertices[i + 1];
                t.C = Vertices[i + 2];
                Triangles.Add(t);
            }
        }

        public void AddRectangle(Rectangle Box)
        {
            Vector2[] verts =
            {
                new Vector2(Box.x, Box.y),
                new Vector2(Box.x + Box.width, Box.y),
                new Vector2(Box.x, Box.y + Box.height),

                new Vector2(Box.x + Box.width, Box.y),
                new Vector2(Box.x +Box.width, Box.y +Box.height),
                new Vector2(Box.x, Box.y + Box.height),
            };
            AddVertices(verts);
        }

        public static Hitbox Automatic(TextureResource Texture, int Segments)
        {
            Hitbox result = new Hitbox();
            float top = -1;
            float bottom = -1;
            byte t = 1;
            using (SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = (SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>)SixLabors.ImageSharp.Image.Load(Texture.Path))
            {
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        if (image[x, y].A > t) { top = y; break; }
                    }
                    if (top >= 0) { break; }
                }

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        if (image[x, image.Height - (y + 1)].A > t) { bottom = image.Height - (y + 1); break; }
                    }
                    if (bottom >= 0) { break; }
                }

                float height = bottom - top;
                float increment = height / Segments;
                Vector2 offset = new Vector2(image.Width / 2, image.Height / 2) * -1;
                for (int i = 0; i < Segments; i++)
                {
                    int y = (int)Math.Round(top + increment * i);
                    float left = -1;
                    float right = -1;
                    float left_next = -1;
                    float right_next = -1;

                    for (int x = 0; x < image.Width; x++)
                    {
                        if (image[x, y].A > t) { left = x; break; }
                    }

                    for (int x = 0; x < image.Width; x++)
                    {
                        if (image[image.Width - (x + 1), y].A > t) { right = image.Width - (x + 1); break; }
                    }

                    for (int x = 0; x < image.Width; x++)
                    {
                        if (image[x, (int)Math.Round(y + increment)].A > t) { left_next = x; break; }
                    }

                    for (int x = 0; x < image.Width; x++)
                    {
                        if (image[image.Width - (x + 1), (int)Math.Round(y + increment)].A > t) { right_next = image.Width - (x + 1); break; }
                    }

                    Vector2[] verts = {
                        new Vector2(left, y) + offset,
                        new Vector2(right, y) + offset,
                        new Vector2(left_next, y + increment) + offset,

                        new Vector2(right, y) + offset,
                        new Vector2(right_next, y + increment) + offset,
                        new Vector2(left_next, y + increment) + offset,
                    };
                    result.AddVertices(verts);
                }
            }

            return result;
        }

        public bool CheckCollision(Vector2 Location, float Angle, Vector2 Point, float Scale)
        {
            if (Triangles != null)
            {
                foreach (Triangle tri in Triangles)
                {
                    if (Raylib.Raylib.CheckCollisionPointTriangle(
                        Point,
                        tri.A.Rotate(Angle) * Scale + Location,
                        tri.B.Rotate(Angle) * Scale + Location,
                        tri.C.Rotate(Angle) * Scale + Location
                        ))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return Raylib.Raylib.CheckCollisionPointCircle(Point, Location, Radius * Scale);
            }

            return false;
        }

        public bool CheckCollision(Vector2 Location, float Angle, Hitbox Other, Vector2 OtherLocation, float Scale, float OtherScale, float OtherAngle)
        {
            if (Other.Radius > 0) // Other is circle
            {
                if (Radius > 0) // Both are circles
                {
                    return Raylib.Raylib.CheckCollisionCircles(OtherLocation, Other.Radius * OtherScale, Location, Radius * Scale);
                }
                else //I'm a mesh and other is a circle
                {
                    foreach (Triangle tri in Triangles)
                    {
                        if (CheckCollisionCircleTriangle(
                        OtherLocation, Other.Radius,
                        tri.A.Rotate(Angle) * Scale + Location,
                        tri.B.Rotate(Angle) * Scale + Location,
                        tri.C.Rotate(Angle) * Scale + Location
                        ))
                        {
                            return true;
                        }
                    }
                }
            }
            else // Other is a mesh
            {
                if (Radius > 0) // I'm a circle and other is a mesh
                {
                    foreach (Triangle tri in Other.Triangles)
                    {
                        if (CheckCollisionCircleTriangle(
                        Location, Radius,
                        tri.A.Rotate(OtherAngle) * OtherScale + OtherLocation,
                        tri.B.Rotate(OtherAngle) * OtherScale + OtherLocation,
                        tri.C.Rotate(OtherAngle) * OtherScale + OtherLocation
                        ))
                        {
                            return true;
                        }
                    }
                }
                else // Both are meshes
                {
                    foreach (Triangle tri in Triangles)
                    {
                        foreach (Triangle otherTri in Other.Triangles)
                        {
                            if (CheckCollisionTriangleTriangle(
                            tri.A.Rotate(Angle) * Scale + Location,
                            tri.B.Rotate(Angle) * Scale + Location,
                            tri.C.Rotate(Angle) * Scale + Location,
                            otherTri.A.Rotate(OtherAngle) * OtherScale + OtherLocation,
                            otherTri.B.Rotate(OtherAngle) * OtherScale + OtherLocation,
                            otherTri.C.Rotate(OtherAngle) * OtherScale + OtherLocation
                            ))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool CheckCollisionTriangleTriangle(Vector2 a1, Vector2 a2, Vector2 a3, Vector2 b1, Vector2 b2, Vector2 b3)
        {
            if (Raylib.Raylib.CheckCollisionPointTriangle(a1, b1, b2, b3)) { return true; }
            if (Raylib.Raylib.CheckCollisionPointTriangle(a2, b1, b2, b3)) { return true; }
            if (Raylib.Raylib.CheckCollisionPointTriangle(a3, b1, b2, b3)) { return true; }
            return false;
        }

        private static bool CheckCollisionCircleTriangle(Vector2 center, float radius, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            if (Raylib.Raylib.CheckCollisionPointTriangle(center, p1, p2, p3)) { return true; }
            if (Raylib.Raylib.CheckCollisionPointTriangle(center + Vector2.UnitX * radius, p1, p2, p3)) { return true; }
            if (Raylib.Raylib.CheckCollisionPointTriangle(center - Vector2.UnitX * radius, p1, p2, p3)) { return true; }
            if (Raylib.Raylib.CheckCollisionPointTriangle(center + Vector2.UnitY * radius, p1, p2, p3)) { return true; }
            if (Raylib.Raylib.CheckCollisionPointTriangle(center - Vector2.UnitY * radius, p1, p2, p3)) { return true; }
            return false;
        }

        public void Draw(Vector2 Location, float Angle, float Scale)
        {
            Color col = new Color(255, 255, 255, 64);
            if (Triangles != null)
            {
                foreach (Triangle tri in Triangles)
                {
                    Raylib.Raylib.DrawLineEx(tri.A.Rotate(Angle) * Scale + Location, tri.B.Rotate(Angle) * Scale + Location, 2.5f, col);
                    Raylib.Raylib.DrawLineEx(tri.B.Rotate(Angle) * Scale + Location, tri.C.Rotate(Angle) * Scale + Location, 2.5f, col);
                    Raylib.Raylib.DrawLineEx(tri.C.Rotate(Angle) * Scale + Location, tri.A.Rotate(Angle) * Scale + Location, 2.5f, col);
                }
            }
            else
            {
                Raylib.Raylib.DrawCircleLines((int)Location.x, (int)Location.y, Radius * Scale, col);
            }
        }
    }

    public struct Triangle
    {
        public Vector2 A { get; set; }
        public Vector2 B { get; set; }
        public Vector2 C { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is Triangle)
            {
                Triangle o = (Triangle)obj;
                return A.x == o.A.x && A.y == o.A.y &&
                       B.x == o.B.x && B.y == o.B.y &&
                       C.x == o.C.x && C.y == o.C.y;
            }
            else return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode();
        }

        public override string ToString()
        {
#pragma warning disable CA1305 // Specify IFormatProvider
            return "[(" + A.x.ToString("N1") + "," + A.y.ToString("N1") +
                ") (" + B.x.ToString("N1") + "," + B.y.ToString("N1") +
                ") (" + C.x.ToString("N1") + "," + C.y.ToString("N1") + ")]";
#pragma warning restore CA1305 // Specify IFormatProvider
        }
    }
}
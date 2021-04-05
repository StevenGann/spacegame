using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SpaceGame
{
    public interface INetObject
    {
        public byte[] ToBytes();

        public void Update(byte[] Data);
    }

    public static class NetExtensions
    {
        public static byte GetByte(this bool a, int index)
        {
            return BitConverter.GetBytes(a)[index];
        }

        public static byte GetByte(this int a, int index)
        {
            return BitConverter.GetBytes(a)[index];
        }

        public static byte GetByte(this float a, int index)
        {
            return BitConverter.GetBytes(a)[index];
        }

        public static byte GetByte(this double a, int index)
        {
            return BitConverter.GetBytes(a)[index];
        }

        public static byte GetByte(this Vector2 a, int index)
        {
            if (index < 4)
            {
                return BitConverter.GetBytes(a.X)[index];
            }
            else
            {
                return BitConverter.GetBytes(a.Y)[index - 4];
            }
        }

        public static byte[] GetBytes(this string a)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(a);
            byte[] buffer = new byte[bytes.Length + 4];
            buffer[0] = bytes.Length.GetByte(0);
            buffer[1] = bytes.Length.GetByte(1);
            buffer[2] = bytes.Length.GetByte(2);
            buffer[3] = bytes.Length.GetByte(3);
            for (int i = 0; i < bytes.Length; i++)
            {
                buffer[4 + i] = bytes[i];
            }
            return buffer;
        }
    }
}
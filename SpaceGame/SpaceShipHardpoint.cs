﻿using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SpaceGame
{
    public class SpaceShipHardpoint : SpaceShip
    {
        public SpaceShip Parent { get; set; }
        public Vector2 Offset { get; set; }

        public override void Tick(double Delta)
        {
            if (!Active) { return; }

            base.Tick(Delta);
            Location = Parent.Location + Rotate(Offset, Parent.Angle);
            Velocity = Parent.Velocity;
            Behavior = Parent.Behavior;
            Faction = Parent.Faction;
            if (Behavior == Behaviors.Going) { Behavior = Behaviors.Idle; }
            Stance = Parent.Stance;
            CombatRange = Parent.CombatRange;
        }

        public static Vector2 Rotate(Vector2 v, double degrees)
        {
            float radians = (float)((degrees - 90) * (Math.PI / 180));
            float sin = MathF.Sin(radians);
            float cos = MathF.Cos(radians);

            float tx = v.X;
            float ty = v.Y;
            float rx = (cos * tx) - (sin * ty);
            float ry = (sin * tx) + (cos * ty);
            return new Vector2(rx, ry);
        }

        public static SpaceShipHardpoint FromXml(XmlResource Xml, SpaceShipHardpoint DstObject)
        {
            if (Xml == null) { throw new ArgumentNullException("Xml"); }
            SpaceShipHardpoint Result = DstObject;
            if (DstObject == null)
            {
                Result = new SpaceShipHardpoint();
            }
            Result = SpaceShip.FromXml(Xml, Result) as SpaceShipHardpoint;

            XmlNode obj = Xml.Xml.LastChild;

            string baseName = GetXmlText(obj, "Base", string.Empty);
            SpaceShipHardpoint baseObject = Result;
            if (!string.IsNullOrEmpty(baseName))
            {
                try
                {
                    baseObject = SpaceShipHardpoint.FromXml(ResourceManager.Get<XmlResource>(baseName), null);
                }
                catch (KeyNotFoundException e)
                {
                    baseObject = Result;
                    Console.WriteLine("XML Error: Failed to locate XML base " + baseName);
                }
            }

            string[] offsetRaw = GetXmlText(obj, "Offset", "0,0").Split(',');
            Result.Offset = new Vector2(float.Parse(offsetRaw[0]), float.Parse(offsetRaw[1]));
            Result.Texture = ResourceManager.Get<TextureResource>(GetXmlText(obj, "Texture", baseObject.Texture.Name));
            return Result;
        }

        private static double GetXmlValue(XmlNode Parent, string Name, double Default)
        {
            if (Parent.Attributes.Count > 0)
            {
                XmlAttributeCollection attributes = Parent.Attributes;
                foreach (XmlAttribute attribute in attributes)
                {
                    if (attribute.Name.ToUpperInvariant() == Name.ToUpperInvariant())
                    {
                        try
                        {
                            return double.Parse(attribute.InnerText);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("\nXMLParse Error");
                            Debug.WriteLine(e.Message);
                            Debug.WriteLine(attribute.OuterXml);
                        }
                    }
                }
            }

            if (Parent.HasChildNodes)
            {
                XmlNodeList children = Parent.ChildNodes;
                foreach (XmlNode node in children)
                {
                    if (node.Name.ToUpperInvariant() == Name.ToUpperInvariant())
                    {
                        try
                        {
                            string text = node.InnerText;
                            if (text.StartsWith("++"))
                            {
                                text = text.TrimStart('+');
                                return Default + double.Parse(text);
                            }
                            if (text.StartsWith("--"))
                            {
                                text = text.TrimStart('-');
                                return Default - double.Parse(text);
                            }
                            if (text.StartsWith("*"))
                            {
                                text = text.TrimStart('*');
                                return Default * double.Parse(text);
                            }
                            if (text.StartsWith("/"))
                            {
                                text = text.TrimStart('/');
                                return Default / double.Parse(text);
                            }
                            else
                            {
                                return double.Parse(text);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\nXMLParse Error");
                            Console.WriteLine(e.Message);
                            Console.WriteLine(node.OuterXml);
                            return Default;
                        }
                    }
                }
            }
            return Default;
        }

        private static string GetXmlText(XmlNode Parent, string Name, string Default)
        {
            if (Parent.Attributes.Count > 0)
            {
                XmlAttributeCollection attributes = Parent.Attributes;
                foreach (XmlAttribute attribute in attributes)
                {
                    if (attribute.Name.ToUpperInvariant() == Name.ToUpperInvariant())
                    {
                        return attribute.InnerText;
                    }
                }
            }

            if (Parent.HasChildNodes)
            {
                XmlNodeList children = Parent.ChildNodes;
                foreach (XmlNode node in children)
                {
                    if (node.Name.ToUpperInvariant() == Name.ToUpperInvariant())
                    {
                        return node.InnerText;
                    }
                }
            }

            return Default;
        }
    }
}
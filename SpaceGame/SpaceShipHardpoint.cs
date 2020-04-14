using Raylib;
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
            Stance = Parent.Stance;
        }

        public static Vector2 Rotate(Vector2 v, double degrees)
        {
            float radians = (float)((degrees - 90) * (Math.PI / 180));
            float sin = MathF.Sin(radians);
            float cos = MathF.Cos(radians);

            float tx = v.x;
            float ty = v.y;
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
            Result = SpaceShipHardpoint.FromXml(Xml, Result) as SpaceShipHardpoint;

            XmlNode obj = Xml.Xml.LastChild;

            string baseName = GetXmlText(obj, "Base", string.Empty);
            SpaceShipHardpoint baseObject = Result;
            if (!string.IsNullOrEmpty(baseName))
            {
                try
                {
                    baseObject = SpaceShipHardpoint.FromXml(ResourceManager.GetXml(baseName), null);
                }
                catch (KeyNotFoundException e)
                {
                    baseObject = Result;
                    Console.WriteLine("XML Error: Failed to locate XML base " + baseName);
                }
            }

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
                            Console.WriteLine("\nXMLParse Error");
                            Console.WriteLine(e.Message);
                            Console.WriteLine(attribute.OuterXml);
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
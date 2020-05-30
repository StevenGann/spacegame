using Raylib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SpaceGame
{
    public class SpaceShipUnit
    {
        public List<SpaceShip> Units { get; } = new List<SpaceShip>();
        public TextureResource UiImage { get; set; } = ResourceManager.GetTexture(@"thumbnail\unknown");
        public Formation Formation = null;

        public bool Selected
        {
            set
            {
                foreach (SpaceShip unit in Units)
                {
                    unit.Selected = value;
                }
            }
            get
            {
                foreach (SpaceShip unit in Units)
                {
                    if (unit.Selected)
                    {
                        Selected = true;
                        return true;
                    }
                }
                return false;
            }
        }

        public int Faction
        {
            set
            {
                foreach (SpaceShip unit in Units)
                {
                    unit.Faction = value;
                }
            }
            get
            {
                return Units[0].Faction;
            }
        }

        public bool Active
        {
            get
            {
                foreach (SpaceShip unit in Units)
                {
                    if (unit.Active)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public Vector2 Location
        {
            get
            {
                if (!locationValid)
                {
                    if (Units.Count > 1)
                    {
                        location = new Vector2(0, 0);
                        float count = 0;
                        foreach (SpaceShip unit in Units)
                        {
                            location += unit.Location;
                            count++;
                        }
                        location /= count;
                    }
                    else
                    {
                        location = Units[0].Location;
                    }
                    locationValid = true;
                }

                return location;
            }

            set
            {
                foreach (SpaceShip unit in Units)
                {
                    Random RNG = new Random();
                    unit.Location = value + new Vector2(RNG.Next(5) - RNG.Next(5), RNG.Next(5) - RNG.Next(5));
                }
            }
        }

        private Vector2 location = new Vector2();
        private bool locationValid = true;

        public SpaceShipUnit()
        {
        }

        public SpaceShipUnit(SpaceShip Ship)
        {
            Add(Ship);
        }

        public void Add(SpaceShip Ship)
        {
            Units.Add(Ship);
            Ship.Unit = this;
            if (Ship.Hardpoints != null)
            {
                foreach (SpaceShipHardpoint hp in Ship.Hardpoints)
                {
                    Add(hp);
                }
            }
        }

        public Vector2 DrawLocation
        {
            get
            {
                return (Location * GameManager.ViewScale) + GameManager.ViewOffset;
            }
        }

        public void Tick(double Delta)
        {
            locationValid = false;
        }

        public void Draw()
        {
            if (Units.Count > 1)
            {
                Color col = new Color(0, 255, 0, 32);
                Vector2 loc = DrawLocation;

                //Raylib.Raylib.DrawTextureEx(Units[0].Texture.Texture, loc - Units[0].TextureOffset * 0.5f, 0, 0.5f, new Color(0, 255, 0, 255));

                if (Selected)
                {
                    Raylib.Raylib.DrawCircle((int)loc.x, (int)loc.y, 10f, col);
                    Raylib.Raylib.DrawCircleLines((int)loc.x, (int)loc.y, 10f, col);
                    bool pruneFlag = false;
                    foreach (SpaceShip unit in Units)
                    {
                        if (unit.Active)
                        {
                            Raylib.Raylib.DrawLineEx(loc, unit.DrawLocation, 2.5f, col);
                        }
                        else
                        {
                            pruneFlag = true;
                        }
                    }
                    if (pruneFlag) { PruneUnits(); }
                }
            }
        }

        private void PruneUnits()
        {
            int count = 0;
            for (int i = 0; i < Units.Count; i++)
            {
                if (!Units[i].Active)
                {
                    Units.RemoveAt(i);
                    i--;
                    count++;
                }
            }
            System.GC.Collect();
        }

        public static SpaceShipUnit FromXml(XmlResource Xml, SpaceShipUnit DstObject)
        {
            if (Xml == null) { throw new ArgumentNullException("Xml"); }
            SpaceShipUnit Result = DstObject;
            if (DstObject == null)
            {
                Result = new SpaceShipUnit();
            }

            XmlNode obj = Xml.Xml.LastChild;

            string baseName = GetXmlText(obj, "Base", string.Empty);
            SpaceShipUnit baseObject = Result;

            if (!string.IsNullOrEmpty(baseName))
            {
                try
                {
                    baseObject = SpaceShipUnit.FromXml(ResourceManager.GetXml(baseName), null);
                }
                catch (KeyNotFoundException e)
                {
                    baseObject = Result;
                    Console.WriteLine("XML Error: Failed to locate XML base " + baseName);
                }
            }

            List<SpaceObject> units = GetXmlNested(obj, "ships", null);
            if (units != null && units.Count > 0)
            {
                Result.Units.Clear();
                foreach (SpaceObject o in units)
                {
                    SpaceShip ss = o as SpaceShip;
                    if (ss != null)
                    {
                        Result.Add(ss);
                    }
                }
            }

            Result.UiImage = ResourceManager.GetTexture(GetXmlText(obj, "image", baseObject.UiImage.Name));

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

        private static List<SpaceObject> GetXmlNested(XmlNode Parent, string Name, List<SpaceObject> Default)
        {
            if (Parent.HasChildNodes)
            {
                XmlNodeList children = Parent.ChildNodes;
                foreach (XmlNode node in children)
                {
                    if (node.Name.ToUpperInvariant() == Name.ToUpperInvariant())
                    {
                        List<SpaceObject> result = new List<SpaceObject>();
                        if (node.ChildNodes.Count > 0)
                        {
                            children = node.ChildNodes;
                            foreach (XmlNode childNode in children)
                            {
                                if (childNode.Name.ToUpperInvariant() == "spaceship".ToUpperInvariant())
                                {
                                    SpaceShip child = SpaceShip.FromXml(new XmlResource() { Xml = new XmlDocument() { InnerXml = childNode.OuterXml } }, null);
                                    result.Add(child);
                                }
                            }
                        }

                        return result;
                    }
                }
            }

            return Default;
        }
    }
}
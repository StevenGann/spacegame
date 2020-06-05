using Raylib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Xml;

namespace SpaceGame
{
    public class SpaceObject
    {
        public double Mass { get; set; } = 1.0;
        public Vector2 Location { get; set; } = new Vector2(0, 0);
        public double Depth { get; set; } = 0.0;
        public Vector2 Velocity { get; set; } = new Vector2(0, 0);
        public double AngularVelocity { get; set; } = 0.0;
        public double AngularAcceleration { get; set; } = 0.0;
        public double AngularDrag { get; set; } = 0.0;
        public Vector2 Acceleration { get; set; } = new Vector2(0, 0);
        public double Angle { get; set; } = 0.0; // Degrees
        public double Drag { get; set; } = 0.0;
        public TextureResource Texture { get; set; }
        public bool Active { get; set; } = true;
        public float Scale { get; set; } = 1.0f;
        public int Faction { get; set; } = 0;
        public Hitbox Hitbox { get; set; } = null;
        public string XmlSource { get; set; } = string.Empty;
        public dynamic tickScript;
        public dynamic drawScript;
        public dynamic spawnScript;
        public dynamic destroyScript;
        private bool initialized = false;

        public bool Selected
        {
            get { return selected; }
            set
            {
                if (value != selected)
                {
                    selected = value;
                    if (UiManager.Selected.Contains(this) && !selected)
                    {
                        UiManager.Selected.Remove(this);
                    }
                    else if (!UiManager.Selected.Contains(this) && selected)
                    {
                        UiManager.Selected.Add(this);
                    }
                }
            }
        }

        private bool selected = false;

        public Vector2 DrawLocation
        {
            get
            {
                return (Location * GameManager.ViewScale) + GameManager.ViewOffset;
            }
        }

        public Vector2 TextureOffset { get; set; } = new Vector2(0, 0);
        public bool Initialized { get; set; } = false;

        public virtual void Tick(double Delta)
        {
            if (!Active) { return; }

            if (!initialized)
            {
                if (spawnScript != null)
                {
                    spawnScript.Spawn(this);
                }
                initialized = true;
            }

            Velocity += Acceleration * (float)Delta;
            Location += Velocity * (float)Delta;
            Velocity *= (float)(1.0 - (Drag * Delta / Mass));
            AngularVelocity += AngularAcceleration * Delta;
            Angle += AngularVelocity * Delta;
            AngularVelocity *= (1.0 - (AngularDrag * Delta));

            if (tickScript != null)
            {
                tickScript.Tick(Delta, this);
            }
        }

        public virtual void Draw()
        {
            if (!Active) { return; }
            //Raylib.Raylib.DrawTextureEx(Texture.Texture, Location, (float)Angle, 1.0f, Color.WHITE);

            if (!Initialized)
            {
                if (Texture.Loaded == false)
                {
                    Debug.WriteLine("Loading " + Texture.Name);
                }
                TextureOffset = new Vector2(Texture.Texture.width / 2, Texture.Texture.height / 2);
                Initialized = true;
            }

            Vector2 loc = DrawLocation;

            if (Selected)
            {
                Raylib.Raylib.DrawCircleLines((int)loc.x, (int)loc.y, Scale * ((Texture.Texture.width + Texture.Texture.height) / 4f) * GameManager.ViewScale, Color.GREEN);
            }

            //Raylib.Raylib.DrawTextureEx(Texture.Texture, RotateAroundPoint(Location - TextureOffset, Location, Angle), (float)Angle, 1.0f, Color.WHITE);
            Raylib.Raylib.DrawTexturePro(
                Texture.Texture,
                new Rectangle(0, 0, Texture.Texture.width, Texture.Texture.height),
                new Rectangle(
                    loc.x,
                    loc.y,
                    Texture.Texture.width * Scale * GameManager.ViewScale,
                    Texture.Texture.height * Scale * GameManager.ViewScale),
                TextureOffset * Scale * GameManager.ViewScale,
                (float)Angle,
                Color.WHITE);

            if (drawScript != null)
            {
                drawScript.Draw(this);
            }

            if (Hitbox != null && Debug.Enabled && !Debug.ConsoleIsOpen && Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_F3)) { Hitbox.Draw(loc, (float)(Angle * 0.0174533), GameManager.ViewScale * Scale); }
        }

        public virtual void Destroy()
        {
            Active = false;
            if (destroyScript != null)
            {
                destroyScript.Destroy(this);
            }
        }

        public static SpaceObject FromXml(XmlResource Xml, SpaceObject DstObject)
        {
            if (Xml == null) { throw new ArgumentNullException(nameof(Xml)); }
            SpaceObject Result = DstObject;
            if (DstObject == null)
            {
                Result = new SpaceObject();
            }
            Result.XmlSource = Xml.Name;
            XmlNode obj = Xml.Xml.LastChild;

            string baseName = GetXmlText(obj, "Base", string.Empty);
            SpaceObject baseObject = Result;
            if (!string.IsNullOrEmpty(baseName))
            {
                try
                {
                    baseObject = SpaceObject.FromXml(ResourceManager.GetXml(baseName), new SpaceObject());
                }
                catch (KeyNotFoundException e)
                {
                    baseObject = Result;
                    Debug.WriteLine("XML Error: Failed to locate XML base " + baseName);
                }
            }

            Result.Mass = GetXmlValue(obj, "Mass", baseObject.Mass);
            Result.Depth = GetXmlValue(obj, "Depth", baseObject.Depth);
            Result.AngularDrag = GetXmlValue(obj, "AngularDrag", baseObject.AngularDrag);
            Result.Drag = GetXmlValue(obj, "Drag", baseObject.Drag);
            Result.Scale = (float)GetXmlValue(obj, "Scale", baseObject.Scale);
            try
            {
                Result.Texture = ResourceManager.GetTexture(GetXmlText(obj, "Texture", @"ui\error"));
            }
            catch (KeyNotFoundException e)
            {
                Result.Texture = ResourceManager.GetTexture(@"ui\error");
                Result.Scale = 1;
            }

            Result.tickScript = GetXmlScript(obj, "Tick", baseObject.tickScript);
            Result.drawScript = GetXmlScript(obj, "Draw", baseObject.drawScript);
            Result.spawnScript = GetXmlScript(obj, "Spawn", baseObject.spawnScript);
            Result.destroyScript = GetXmlScript(obj, "Destroy", baseObject.destroyScript);

            return Result;
        }

        public bool CheckCollision(Hitbox Other, Vector2 OtherLocation, float OtherScale, float OtherAngle)
        {
            if (Hitbox != null)
            {
                return Hitbox.CheckCollision(this.Location, (float)(Angle * 0.0174533), Other, OtherLocation, this.Scale, OtherScale, OtherAngle);
            }
            else
            {
                return false;
            }
        }

        public bool CheckCollision(Vector2 Point)
        {
            if (Hitbox != null)
            {
                return Hitbox.CheckCollision(this.Location, (float)(Angle * 0.0174533), Point, this.Scale);
            }
            else
            {
                return false;
            }
        }

        public static dynamic GetXmlScript(XmlNode Parent, string Name, dynamic Default)
        {
            if (Parent.HasChildNodes)
            {
                XmlNodeList children = Parent.ChildNodes;
                foreach (XmlNode node in children)
                {
                    if (node.Name.ToUpperInvariant() == Name.ToUpperInvariant() && node.Attributes.Count > 0)
                    {
                        XmlAttributeCollection attributes = node.Attributes;
                        foreach (XmlAttribute attribute in attributes)
                        {
                            if (attribute.Name.ToUpperInvariant() == "source".ToUpperInvariant())
                            {
                                ScriptResource script = ResourceManager.GetScript(attribute.Value);
                                script.Loaded = true;
                                return script.Script;
                            }
                        }
                    }
                }
            }

            string scriptText = GetXmlText(Parent, Name, string.Empty);
            if (scriptText != string.Empty)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                if (Debug.GetFlag("DangerousScripts") != 1)
                {
                    List<string> blacklist = GameManager.ScriptBlacklist;
                    foreach (string s in blacklist)
                    {
                        if (scriptText.Contains(s))
                        {
                            watch.Stop();
                            Debug.WriteLine("%WARNING%Did not compile script because it contained \"" + s + "\" and DangerousScripts cvar is disabled.");
                            Debug.WriteLine("%WARNING%If you trust this script, use \"SET DANGEROUSSCRIPTS 1\" in the console or config file.");
                            return Default;
                        }
                    }
                }

                dynamic result = CSScriptLib.CSScript.Evaluator.LoadMethod(scriptText);
                watch.Stop();
                Debug.WriteLine("%SUCCESS%Compiled script in " + watch.ElapsedMilliseconds + "ms");
                return result;
            }

            return Default;
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
                            Debug.WriteLine("\nXMLParse Error");
                            Debug.WriteLine(e.Message);
                            Debug.WriteLine(node.OuterXml);
                            return Default;
                        }
                    }
                }
            }
            return Default;
        }

        public void DrawMinimap(Vector2 Location)
        {
            if (!(this is SpaceEffect))
            {
                if (this is SpaceProjectile)
                {
                    Raylib.Raylib.DrawCircleV(Location, 1, GameManager.FactionColors[Faction].SetAlpha(32));
                }
                else
                {
                    Raylib.Raylib.DrawCircleV(Location, MathF.Sqrt(Texture.Texture.width * Scale), GameManager.FactionColors[Faction].SetAlpha(32));
                }
            }
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

        private static Vector2 RotateAroundPoint(Vector2 Position, Vector2 Center, double Angle)
        {
            //Note to self: System.Math operates in Radians
            double angle = (Angle) * (Math.PI / 180); // Convert to radians
            double rotatedX = Math.Cos(angle) * (Position.x - Center.x) - Math.Sin(angle) * (Position.y - Center.y) + Center.x;
            double rotatedY = Math.Sin(angle) * (Position.x - Center.x) + Math.Cos(angle) * (Position.y - Center.y) + Center.y;
            return new Vector2((float)rotatedX, (float)rotatedY);
        }
    }
}
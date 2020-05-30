using Raylib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SpaceGame
{
    internal class SpaceEffect : SpaceObject
    {
        public double Lifespan { get; set; } = 100;
        public int MaxParticles { get; set; } = 10;
        public int MinParticles { get; set; } = 1;
        public double ParticleSpawnRadius { get; set; } = 20;
        public double MaxParticleLife { get; set; } = 50;
        public double MinParticleLife { get; set; } = 10;
        public double MaxParticleScale { get; set; } = 2.0;
        public double MinParticleScale { get; set; } = 0.5;
        public double MaxParticleRotation { get; set; } = 5;
        public double MinParticleRotation { get; set; } = -5;
        public double MaxParticleAlpha { get; set; } = 1;
        public double MinParticleAlpha { get; set; } = 0.5;
        public double MaxParticleDirection { get; set; } = 360;
        public double MinParticleDirection { get; set; } = 0;
        public double MaxParticleAngle { get; set; } = 360;
        public double MinParticleAngle { get; set; } = 0;
        public double MaxParticleSpeed { get; set; } = 5;
        public double MinParticleSpeed { get; set; } = 0;
        public bool ParticleFade { get; set; } = true;
        public double ParticleDrag { get; set; } = 0.1;
        public List<SpaceEffect> Children { get; set; } = null;

        private Random RNG = new Random();
        private Queue<Particle> Particles = new Queue<Particle>();
        private Queue<Particle> buffer = new Queue<Particle>();

        public override void Tick(double Delta)
        {
            if (!Active) { return; }
            Lifespan -= Delta;
            if (Lifespan <= 0) { Active = false; }

            while (Particles.Count < RNG.Next(MinParticles, MaxParticles))
            {
                Particles.Enqueue(new Particle()
                {
                    Location = new Vector2((float)RngDouble(-ParticleSpawnRadius, ParticleSpawnRadius), (float)RngDouble(-ParticleSpawnRadius, ParticleSpawnRadius)),
                    Velocity = new Vector2((float)RngDouble(MinParticleSpeed, MaxParticleSpeed) - (float)RngDouble(MinParticleSpeed, MaxParticleSpeed), (float)RngDouble(MinParticleSpeed, MaxParticleSpeed) - (float)RngDouble(MinParticleSpeed, MaxParticleSpeed)),
                    Angle = (float)RngDouble(MinParticleAngle, MaxParticleAngle),
                    Scale = (float)RngDouble(MinParticleScale, MaxParticleScale),
                    Alpha = (float)RngDouble(MinParticleAlpha, MaxParticleAlpha),
                    Lifespan = (float)Math.Min(RngDouble(MinParticleLife, MaxParticleLife), Lifespan),
                });
            }

            while (Particles.Count > 0)
            {
                Particle p = Particles.Dequeue();
                if (p.Lifespan > p.Lifemax)
                {
                    p.Lifemax = p.Lifespan;
                }
                if (ParticleFade)
                {
                    p.Alpha = p.Lifespan / p.Lifemax;
                }
                p.Location += p.Velocity;
                p.Lifespan -= (float)Delta;
                if (p.Lifespan > 0)
                {
                    buffer.Enqueue(p);
                }
            }
            while (buffer.Count > 0)
            {
                Particles.Enqueue(buffer.Dequeue());
            }

            if (Children != null)
            {
                foreach (SpaceEffect child in Children)
                {
                    if (child.Location.x == 0)
                    {
                        child.Location = Location;
                        child.Velocity = Velocity;
                        GameManager.Add(child);
                    }
                    //child.Tick(Delta);
                }
            }

            base.Tick(Delta);
        }

        public override void Draw()
        {
            if (!Active) { return; }

            if (!Initialized)
            {
                if (Texture.Loaded == false)
                {
                    Debug.WriteLine("Loading " + Texture.Name);
                }
                TextureOffset = new Vector2(Texture.Texture.width / 2, Texture.Texture.height / 2);
                Initialized = true;
            }

            if (MinParticleAlpha < 1.0) { Raylib.Raylib.BeginBlendMode(BlendMode.BLEND_ADDITIVE); }
            while (Particles.Count > 0)
            {
                Particle p = Particles.Dequeue();
                DrawParticle(p);
                buffer.Enqueue(p);
            }

            if (MinParticleAlpha < 1.0) { Raylib.Raylib.EndBlendMode(); }

            while (buffer.Count > 0)
            {
                Particles.Enqueue(buffer.Dequeue());
            }

            /*if (Children != null)
            {
                foreach (SpaceEffect child in Children)
                {
                    child.Draw();
                }
            }*/
        }

        private void DrawParticle(Particle p)
        {
            Vector2 loc = DrawLocation + p.Location;
            Raylib.Raylib.DrawTexturePro(
                Texture.Texture,
                new Rectangle(0, 0, Texture.Texture.width, Texture.Texture.height),
                new Rectangle(loc.x, loc.y, Texture.Texture.width * p.Scale * GameManager.ViewScale, Texture.Texture.height * p.Scale * GameManager.ViewScale),
                TextureOffset * p.Scale * GameManager.ViewScale,
                p.Angle,
                new Color((byte)0xFF, (byte)0xFF, (byte)0xFF, (byte)(255 * p.Alpha)));
        }

        private double RngDouble(double Min, double Max)
        {
            return Min + (RNG.NextDouble() * (Max - Min));
        }

        private struct Particle
        {
            public Vector2 Location { get; set; }
            public Vector2 Velocity { get; set; }
            public float Angle { get; set; }
            public float Scale { get; set; }
            public float Alpha { get; set; }
            public float Lifespan { get; set; }
            public float Lifemax { get; set; }
        }

        public static SpaceEffect FromXml(XmlResource Xml, SpaceEffect DstObject)
        {
            if (Xml == null) { throw new ArgumentNullException("Xml"); }
            SpaceEffect Result = DstObject;
            if (DstObject == null)
            {
                Result = new SpaceEffect();
            }
            Result = SpaceObject.FromXml(Xml, Result) as SpaceEffect;

            XmlNode obj = Xml.Xml.LastChild;

            string baseName = GetXmlText(obj, "Base", string.Empty);
            SpaceEffect baseObject = Result;
            if (!string.IsNullOrEmpty(baseName))
            {
                try
                {
                    baseObject = SpaceEffect.FromXml(ResourceManager.GetXml(baseName), null);
                }
                catch (KeyNotFoundException e)
                {
                    baseObject = Result;
                    Debug.WriteLine("XML Error: Failed to locate XML base " + baseName);
                }
            }

            Result.Lifespan = GetXmlValue(obj, "Lifespan", baseObject.Lifespan);
            Result.MaxParticles = (int)GetXmlValue(obj, "MaxParticles", baseObject.MaxParticles);
            Result.MinParticles = (int)GetXmlValue(obj, "MinParticles", baseObject.MinParticles);
            Result.ParticleSpawnRadius = GetXmlValue(obj, "ParticleSpawnRadius", baseObject.ParticleSpawnRadius);
            Result.MaxParticleLife = GetXmlValue(obj, "MaxParticleLife", baseObject.MaxParticleLife);
            Result.MinParticleLife = GetXmlValue(obj, "MinParticleLife", baseObject.MinParticleLife);
            Result.MaxParticleScale = GetXmlValue(obj, "MaxParticleScale", baseObject.MaxParticleScale);
            Result.MinParticleScale = GetXmlValue(obj, "MinParticleScale", baseObject.MinParticleScale);
            Result.MaxParticleRotation = GetXmlValue(obj, "MaxParticleRotation", baseObject.MaxParticleRotation);
            Result.MinParticleRotation = GetXmlValue(obj, "MinParticleRotation", baseObject.MinParticleRotation);
            Result.MaxParticleAlpha = GetXmlValue(obj, "MaxParticleAlpha", baseObject.MaxParticleAlpha);
            Result.MinParticleAlpha = GetXmlValue(obj, "MinParticleAlpha", baseObject.MinParticleAlpha);
            Result.MaxParticleDirection = GetXmlValue(obj, "MaxParticleDirection", baseObject.MaxParticleDirection);
            Result.MinParticleDirection = GetXmlValue(obj, "MinParticleDirection", baseObject.MinParticleDirection);
            Result.MaxParticleAngle = GetXmlValue(obj, "MaxParticleAngle", baseObject.MaxParticleAngle);
            Result.MinParticleAngle = GetXmlValue(obj, "MinParticleAngle", baseObject.MinParticleAngle);
            Result.MaxParticleSpeed = GetXmlValue(obj, "MaxParticleSpeed", baseObject.MaxParticleSpeed);
            Result.MinParticleSpeed = GetXmlValue(obj, "MinParticleSpeed", baseObject.MinParticleSpeed);
            Result.ParticleFade = GetXmlBool(obj, "ParticleFade", baseObject.ParticleFade);
            Result.ParticleDrag = GetXmlValue(obj, "ParticleDrag", baseObject.ParticleDrag);
            Result.Children = GetXmlChildrenEffect(obj, "ChildEffect", baseObject.Children);

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

        private static List<SpaceEffect> GetXmlChildrenEffect(XmlNode Parent, string Name, List<SpaceEffect> Default)
        {
            int count = 0;
            List<SpaceEffect> result = new List<SpaceEffect>();
            if (Default != null)
            {
                foreach (SpaceEffect effect in Default)
                {
                    SpaceEffect newChild = new SpaceEffect();
                    result.Add(FromXml(ResourceManager.GetXml(effect.XmlSource), newChild));
                }
                count = Default.Count;
            }

            if (Parent.HasChildNodes)
            {
                XmlNodeList children = Parent.ChildNodes;
                foreach (XmlNode node in children)
                {
                    if (node.Name.ToUpperInvariant() == Name.ToUpperInvariant())
                    {
                        SpaceEffect newChild = new SpaceEffect();
                        result.Add(FromXml(ResourceManager.GetXml(node.InnerText), newChild));
                        count++;
                    }
                }
            }

            if (count > 0) { return result; }

            return null;
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

        private static bool GetXmlBool(XmlNode Parent, string Name, bool Default)
        {
            if (Parent.Attributes.Count > 0)
            {
                XmlAttributeCollection attributes = Parent.Attributes;
                foreach (XmlAttribute attribute in attributes)
                {
                    if (attribute.Name.ToUpperInvariant() == Name.ToUpperInvariant())
                    {
                        return attribute.InnerText.ToUpperInvariant() == "TRUE";
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
                        return node.InnerText.ToUpperInvariant() == "TRUE";
                    }
                }
            }

            return Default;
        }
    }
}
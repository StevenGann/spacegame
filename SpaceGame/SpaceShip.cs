using Raylib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Xml;

namespace SpaceGame
{
    public class SpaceShip : SpaceObject
    {
        public double Hull { get; set; } = 100;
        public double Shield { get; set; } = 100;
        public double MaxHull { get; set; } = 100;
        public double MaxShield { get; set; } = 100;
        public double ShieldRegen { get; set; } = 0.25;
        public Vector2 Goal { get; set; } = new Vector2(1000, 700);
        public SpaceObject Objective { get; set; }
        public double Throttle { get; set; } = 0.0;
        public double MaxThrust { get; set; } = 0.1;
        public double TurnSpeed { get; set; } = 0.25;
        public double RateOfFire { get; set; } = 10.0;
        public Behaviors Behavior { get; set; } = Behaviors.Idle;
        public Stances Stance { get; set; } = Stances.Defend;
        public int ShieldRebootProbability { get; set; } = 4;
        public float CombatRange { get; set; } = -1;
        public List<SpaceShipHardpoint> Hardpoints { get; set; } = null;
        public SpaceShipUnit Unit { get; set; } = null;
        private Random RNG = new Random();
        private double shotCooldown = 0.0;
        private double shotHeat = 0.0;
        private Vector2 attackOffset = new Vector2();

        public override void Tick(double Delta)
        {
            if (!Active) { return; }
            /*
            if (Location.x < 0) { Location = new Vector2(Location.x + 1920, Location.y); }
            if (Location.x > 1920) { Location = new Vector2(Location.x - 1920, Location.y); }
            if (Location.y < 0) { Location = new Vector2(Location.x, Location.y + 1200); }
            if (Location.y > 1200) { Location = new Vector2(Location.x, Location.y - 1200); }
            */
            if (Behavior == Behaviors.Idle)
            {
                Throttle = 0;
                if (Velocity.Length() > 0)
                {
                    Velocity = Velocity.Length() * 0.95f * Vector2.Normalize(Velocity);
                }
                AngularAcceleration = 0;

                if (Shield <= 0 && RNG.Next(100 * 60) < ShieldRebootProbability)
                {
                    Shield += 1;
                }

                if (Stance == Stances.Defend)
                {
                    if (CombatRange <= 0) { CombatRange = MathF.Min(Texture.Texture.width, Texture.Texture.height) * 10; }

                    if (Hardpoints == null) // If I've got hardpoints, let them deal with it.
                    {
                        SpaceObject[] nearTargets = GetTargets(Location, CombatRange);
                        if (nearTargets.Length > 0)
                        {
                            Objective = nearTargets[0];

                            if (Objective.Active == false)
                            {
                            }

                            float distance = Raylib.Raylib.Vector2Distance(Objective.Location, Location);
                            //if (RNG.Next(100) <= 200 / distance) { attackOffset = new Vector2(RNG.Next((int)(distance / 50)) - RNG.Next((int)(distance / 50)), RNG.Next((int)(distance / 50)) - RNG.Next((int)(distance / 50))); }
                            Goal = (Objective as SpaceShip).GetLead(1 + distance / 50);// + attackOffset;

                            double angleOffset = (AngleToPoint(this.Location, Goal) - Angle) + 90;
                            if (angleOffset > 180) { angleOffset -= 360; }
                            if (angleOffset < -180) { angleOffset += 360; }

                            if (angleOffset > 1) { AngularAcceleration = TurnSpeed * Delta; }
                            else if (angleOffset < -1) { AngularAcceleration = -TurnSpeed * Delta; }
                            else
                            {
                                AngularAcceleration = TurnSpeed * Math.Abs(Math.Pow(angleOffset, 2)) * angleOffset * Delta;
                                if (shotCooldown <= 0) { Shoot(); }
                            }
                        }
                    }
                }
            }
            else if (Behavior == Behaviors.Attacking)
            {
                if (Objective == this || Objective.Active == false) { Behavior = Behaviors.Idle; }

                float distance = Raylib.Raylib.Vector2Distance(Objective.Location, Location);
                if (RNG.Next(100) <= 200 / distance) { attackOffset = new Vector2(RNG.Next((int)(distance / 50)) - RNG.Next((int)(distance / 50)), RNG.Next((int)(distance / 50)) - RNG.Next((int)(distance / 50))); }

                if (Objective is SpaceShip)
                {
                    Goal = (Objective as SpaceShip).GetLead(1 + distance / 50) + attackOffset;
                }
                else
                {
                    Goal = Objective.Location + attackOffset;
                }

                double angleOffset = (AngleToPoint(this.Location, Goal) - Angle) + 90;
                if (angleOffset > 180) { angleOffset -= 360; }
                if (angleOffset < -180) { angleOffset += 360; }

                if (angleOffset > 1) { AngularAcceleration = TurnSpeed * Delta; }
                else if (angleOffset < -1) { AngularAcceleration = -TurnSpeed * Delta; }
                else
                {
                    AngularAcceleration = TurnSpeed * Math.Abs(Math.Pow(angleOffset, 2)) * angleOffset * Delta;
                    if (shotCooldown <= 0 && distance < CombatRange) { Shoot(); }
                }

                Throttle = Math.Pow(Math.Clamp((180 - angleOffset) / 180, 0.0, 1.0), 2);
            }
            else if (Behavior == Behaviors.Going)
            {
                double angleOffset = (AngleToPoint(this.Location, Goal) - Angle) + 90;
                if (angleOffset > 180) { angleOffset -= 360; }
                if (angleOffset < -180) { angleOffset += 360; }

                if (angleOffset > 1) { AngularAcceleration = TurnSpeed * Delta; }
                else if (angleOffset < -1) { AngularAcceleration = -TurnSpeed * Delta; }
                else
                {
                    AngularAcceleration = TurnSpeed * Math.Abs(Math.Pow(angleOffset, 2)) * angleOffset * Delta;
                }

                Throttle = Math.Pow(Math.Clamp((180 - angleOffset) / 180, 0.0, 1.0), 2);

                if (Raylib.Raylib.Vector2Distance(Location, Goal) < Velocity.Length() * 60 * 10)
                {
                    if (Raylib.Raylib.Vector2Distance(Location, Goal) < Texture.Texture.height)
                    {
                        Behavior = Behaviors.Idle;
                    }
                    else
                    {
                        Throttle *= (Raylib.Raylib.Vector2Distance(Location, Goal) / ((Velocity.Length() * 120) + 1)) * 0.75;
                    }
                }
            }

            double radians = (Angle - 90) * (Math.PI / 180);
            Vector2 thrust = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));
            thrust = thrust * (float)(MaxThrust * Throttle);
            Acceleration = thrust * (float)Delta / (float)Mass;

            // Nudge away from other ships
            SpaceObject[] neighbors = GetNeighbors(Location, Texture.Texture.width + (Velocity.Length() / 2));
            if (neighbors.Length > 0)
            {
                for (int i = 0; i < neighbors.Length; i++)
                {
                    Vector2 nudge = Location - neighbors[i].Location;
                    nudge = (Vector2.Normalize(nudge) * (Velocity.Length() + 0.1f)) / (Vector2.Distance(neighbors[i].Location, Location) * 2.0f + 0.001f);
                    //nudge += new Vector2((float)RNG.NextDouble(), (float)RNG.NextDouble()) / (Vector2.Distance(neighbors[i].Location, Location) + 0.1f) ;
                    Acceleration += nudge / (float)Mass;
                }
            }
            // Nudge towards ships in same unit
            if (Unit != null)
            {
                if (Unit.Units.Count > 2)
                {
                    /*for (int i = 0; i < Unit.Units.Count; i++)
                    {
                        if (Unit.Units[i] != this)
                        {
                            Vector2 nudge = Unit.Units[i].Location - Location;
                            nudge = (Vector2.Normalize(nudge) * (Velocity.Length() + 0.1f)) * (Vector2.Distance(Unit.Units[i].Location, Location) * 0.00001f);
                            Acceleration += nudge / (float)Mass;
                        }
                    }*/
                    if (Unit.Location != Location)
                    {
                        Vector2 nudge = Unit.Location - Location;
                        nudge = (Vector2.Normalize(nudge) * (Velocity.Length() + 0.1f)) * (Vector2.Distance(Unit.Location, Location) * 0.00001f);
                        Acceleration += nudge / (float)Mass;
                    }
                }
            }

            if (shotCooldown > 0) { shotCooldown -= 1 * Delta * (RNG.NextDouble() + RNG.NextDouble() + RNG.NextDouble()); }
            if (shotHeat > 0) { shotHeat -= 0.1 * Delta; }

            if (Shield > 0 && Shield < MaxShield) { Shield += ShieldRegen; }

            base.Tick(Delta);
        }

        public override void Draw()
        {
            if (!Active) { return; }
            if (Selected)
            {
                if (Behavior == Behaviors.Going)
                {
                    Color col = new Color(128, 128, 128, 16);
                    Raylib.Raylib.DrawLineEx(Location * GameManager.ViewScale + GameManager.ViewOffset,
                        Goal * GameManager.ViewScale + GameManager.ViewOffset,
                        2f, col);
                }
                else if (Behavior == Behaviors.Attacking)
                {
                    Color col = new Color(255, 0, 0, 16);
                    Raylib.Raylib.DrawLineEx(Location * GameManager.ViewScale + GameManager.ViewOffset,
                        Goal * GameManager.ViewScale + GameManager.ViewOffset
                        , 2f, col);
                }
            }

            base.Draw();

            Vector2 loc = DrawLocation;
            if (Active)
            {
                int barWidth = (int)(Math.Sqrt(Texture.Texture.width * Scale) * 5);
                int barHeight = 4;
                float barHalf = barWidth / 2f;
                float barOffset = Texture.Texture.width / 2f;

                if (Shield < MaxShield && Shield > 0)
                {
                    Raylib.Raylib.DrawRectangle((int)(loc.x - barHalf), (int)(loc.y + barOffset), (int)Math.Round(barWidth * (Shield / MaxShield)), barHeight, Color.GREEN);
                    Raylib.Raylib.DrawRectangleLines((int)(loc.x - barHalf), (int)(loc.y + barOffset), barWidth, barHeight, Color.DARKGREEN);
                }
                if (Hull < MaxHull && Hull > 0)
                {
                    Raylib.Raylib.DrawRectangle((int)(loc.x - barHalf), (int)(loc.y + barOffset) + barHeight + 1, (int)Math.Round(barWidth * (Hull / MaxHull)), barHeight, Color.RED);
                    Raylib.Raylib.DrawRectangleLines((int)(loc.x - barHalf), (int)(loc.y + barOffset) + barHeight + 1, barWidth, barHeight, Color.DARKPURPLE);
                }

                if (Stance == Stances.Defend)
                {
                    Raylib.Raylib.DrawCircleLines((int)loc.x, (int)loc.y, CombatRange * GameManager.ViewScale, new Color(255, 0, 0, 32));
                }
            }
        }

        public Vector2 GetLead(float LeadTicks)
        {
            return this.Location + ((this.Velocity + this.Acceleration) * LeadTicks);
        }

        public void Damage(double Amount)
        {
            if (Shield > Amount)
            {
                Shield -= Amount;
            }
            else
            {
                Hull -= Amount - Shield;
                Shield = 0;
            }

            if (Hull <= 0)
            {
                Destroy();
            }
        }

        private void Destroy()
        {
            if (Hardpoints != null)
            {
                foreach (SpaceShipHardpoint hp in Hardpoints)
                {
                    if (hp.Active) { hp.Destroy(); }
                }
            }

            var o = SpaceEffect.FromXml(ResourceManager.GetXml(@"effect\base_explosion"), null);
            o.Location = Location;
            o.Velocity = Velocity;
            GameManager.Add(o);
            Active = false;
        }

        public void Shoot()
        {
            double error = RNG.Next((int)Math.Sqrt(Math.Ceiling(Math.Clamp(shotHeat, 0, 100)))) - RNG.Next((int)Math.Sqrt(Math.Ceiling(Math.Clamp(shotHeat, 0, 100))));
            double radians = (Angle - 90 + error) * (Math.PI / 180);
            Vector2 heading = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));

            SpaceProjectile obj = new SpaceProjectile()
            {
                Texture = ResourceManager.GetTexture(@"projectile\proton+"),
                Angle = this.Angle,
                Velocity = heading * 50.0f,
                Location = this.Location + heading * 100.0f,//Location = RotateAroundPoint(this.Location - (this.TextureOffset), this.Location, this.Angle),
                Scale = 0.5f,
                Faction = this.Faction,
                Sender = this
            };

            obj.Hitbox = new Hitbox((obj.Texture.Texture.width + obj.Texture.Texture.height) / 4);
            //obj.Hitbox = Hitbox.Automatic(obj.Texture, 1);

            GameManager.Add(obj);

            shotCooldown = 60 / RateOfFire;
            shotHeat += 1;
        }

        private static double AngleToPoint(Vector2 A, Vector2 B)
        {
            float xDiff = B.x - A.x;
            float yDiff = B.y - A.y;
            return Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
        }

        protected static Vector2 RotateAroundPoint(Vector2 Position, Vector2 Center, double Angle)
        {
            //Note to self: System.Math operates in Radians
            double angle = (Angle) * (Math.PI / 180); // Convert to radians
            double rotatedX = Math.Cos(angle) * (Position.x - Center.x) - Math.Sin(angle) * (Position.y - Center.y) + Center.x;
            double rotatedY = Math.Sin(angle) * (Position.x - Center.x) + Math.Cos(angle) * (Position.y - Center.y) + Center.y;
            return new Vector2((float)rotatedX, (float)rotatedY);
        }

        private SpaceObject[] GetNeighbors(Vector2 Position, float Radius)
        {
            IEnumerable<SpaceObject> inSelection = GameManager.Instance.Objects.Where(o =>
                       o is SpaceShip &&
                       o is SpaceShipHardpoint == false &&
                       o.Faction == this.Faction &&
                       o.Active == true &&
                       o.Location != Position &&
                       Raylib.Raylib.Vector2Distance(o.Location, Position) < Radius
                    );

            return inSelection.ToArray();
        }

        private SpaceObject[] GetTargets(Vector2 Position, float Radius)
        {
            IEnumerable<SpaceObject> inSelection = GameManager.Instance.Objects.Where(o =>
                       o is SpaceShip &&
                       o.Faction != this.Faction &&
                       o.Active == true &&
                       Raylib.Raylib.Vector2Distance(o.Location, Position) < Radius
                    );

            return inSelection.ToArray();
        }

        public enum Behaviors
        {
            Idle,
            Going,
            Following,
            Attacking
        }

        public enum Stances
        {
            NoAttack,
            Defend,
            Attack,
            Hunt
        }

        public static SpaceShip FromXml(XmlResource Xml, SpaceShip DstObject)
        {
            if (Xml == null) { throw new ArgumentNullException("Xml"); }
            SpaceShip Result = DstObject;
            if (DstObject == null)
            {
                Result = new SpaceShip();
            }
            Result = SpaceObject.FromXml(Xml, Result) as SpaceShip;

            XmlNode obj = Xml.Xml.LastChild;

            string baseName = GetXmlText(obj, "Base", string.Empty);
            SpaceShip baseObject = Result;

            if (!string.IsNullOrEmpty(baseName))
            {
                try
                {
                    baseObject = SpaceShip.FromXml(ResourceManager.GetXml(baseName), null);
                }
                catch (KeyNotFoundException e)
                {
                    baseObject = Result;
                    Console.WriteLine("XML Error: Failed to locate XML base " + baseName);
                }
            }

            Result.Hull = GetXmlValue(obj, "Hull", baseObject.Hull);
            Result.Shield = GetXmlValue(obj, "Shield", baseObject.Shield);
            Result.MaxHull = GetXmlValue(obj, "MaxHull", baseObject.MaxHull);
            Result.MaxShield = GetXmlValue(obj, "MaxShield", baseObject.MaxShield);
            Result.ShieldRegen = GetXmlValue(obj, "ShieldRegen", baseObject.ShieldRegen);
            Result.MaxThrust = GetXmlValue(obj, "MaxThrust", baseObject.MaxThrust);
            Result.TurnSpeed = GetXmlValue(obj, "TurnSpeed", baseObject.TurnSpeed);
            Result.RateOfFire = GetXmlValue(obj, "RateOfFire", baseObject.RateOfFire);
            Result.ShieldRebootProbability = (int)GetXmlValue(obj, "ShieldRebootProbability", baseObject.ShieldRebootProbability);

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
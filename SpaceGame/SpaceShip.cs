﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
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
        private int RandomOffset = -1;
        private double shotCooldown = 0.0;
        private double shotHeat = 0.0;
        private Vector2 attackOffset = new Vector2();
        private bool isLeader = true;
        private SpaceShip leader = null;

        public override void Tick(double Delta)
        {
            if (!Active) { return; }

            isLeader = Unit == null || Unit.Formation == null || this == Unit.Leader;

            if (!isLeader)
            {
                leader = Unit.Leader;
                Stance = leader.Stance;
                Objective = leader.Objective;
                Goal = leader.Goal;
                Behavior = leader.Behavior;
            }

            if (isLeader || Behavior == Behaviors.Attacking)
            {
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
                                if (RandomOffset >= nearTargets.Length || RandomOffset < 0 || RNG.NextDouble() < 0.001)
                                {
                                    RandomOffset = RandomOffset = (int)Math.Floor(RNG.NextDouble() * nearTargets.Length);
                                }

                                Objective = nearTargets[RandomOffset];

                                float distance = Vector2.Distance(Objective.Location, Location);
                                if (RNG.Next(100) <= 200 / distance) { attackOffset = new Vector2(RNG.Next((int)(distance / 50)) - RNG.Next((int)(distance / 50)), RNG.Next((int)(distance / 50)) - RNG.Next((int)(distance / 50))); }
                                Goal = (Objective as SpaceShip).GetLead(1 + distance / 50) + attackOffset;

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

                    float distance = Vector2.Distance(Objective.Location, Location);
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
                    Vector2 goal = Goal;
                    /*if (Unit != null && Unit.Formation != null && this != Unit.Units[0])
                    {
                        goal = Unit.Formation.GetLocation(this, Texture.Texture.width * 0.5f);
                    }*/

                    double angleOffset = (AngleToPoint(this.Location, goal) - Angle) + 90;
                    if (angleOffset > 180) { angleOffset -= 360; }
                    if (angleOffset < -180) { angleOffset += 360; }

                    if (angleOffset > 1) { AngularAcceleration = TurnSpeed * Delta; }
                    else if (angleOffset < -1) { AngularAcceleration = -TurnSpeed * Delta; }
                    else
                    {
                        AngularAcceleration = TurnSpeed * Math.Abs(Math.Pow(angleOffset, 2)) * angleOffset * Delta;
                    }

                    Throttle = Math.Pow(Math.Clamp((180 - angleOffset) / 180, 0.0, 1.0), 2);

                    if (Vector2.Distance(Location, goal) < Velocity.Length() * 60 * 10)
                    {
                        if (Vector2.Distance(Location, goal) < Texture.Texture.height)
                        {
                            Behavior = Behaviors.Idle;
                        }
                        else
                        {
                            Throttle *= (Vector2.Distance(Location, goal) / ((Velocity.Length() * 120) + 1)) * 0.75;
                        }
                    }

                    if (Stance == Stances.Defend)
                    {
                        if (CombatRange <= 0) { CombatRange = MathF.Min(Texture.Texture.width, Texture.Texture.height) * 10; }

                        if (Hardpoints == null) // If I've got hardpoints, let them deal with it.
                        {
                            SpaceObject[] nearTargets = GetTargets(Location, MathF.Sqrt(Velocity.Length()) * 0.75f * CombatRange);
                            if (nearTargets.Length > 0)
                            {
                                if (RandomOffset >= nearTargets.Length || RandomOffset < 0 || RNG.NextDouble() < 0.001)
                                {
                                    RandomOffset = RandomOffset = (int)Math.Floor(RNG.NextDouble() * nearTargets.Length);
                                }

                                float distance = Vector2.Distance(nearTargets[RandomOffset].Location, Location);
                                if (RNG.Next(100) <= 200 / distance) { attackOffset = new Vector2(RNG.Next((int)(distance / 50)) - RNG.Next((int)(distance / 50)), RNG.Next((int)(distance / 50)) - RNG.Next((int)(distance / 50))); }
                                Vector2 incidentalGoal = (nearTargets[RandomOffset] as SpaceShip).GetLead(1 + distance / 50) + attackOffset;

                                angleOffset = (AngleToPoint(this.Location, incidentalGoal) - Angle) + 90;
                                if (angleOffset > 180) { angleOffset -= 360; }
                                if (angleOffset < -180) { angleOffset += 360; }

                                if (Math.Abs(angleOffset) < 5)
                                {
                                    //AngularAcceleration = TurnSpeed * Math.Abs(Math.Pow(angleOffset, 2)) * angleOffset * Delta;
                                    if (shotCooldown <= 0) { Shoot(); }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Vector2 goal = Unit.Formation.GetLocation(this, Texture.Texture.width * 0.5f);
                Vector2 screenGoal = UiManager.WorldToScreen(goal);
                Vector2 screenLocation = UiManager.WorldToScreen(Location);
                Debug.DrawLine((int)screenGoal.X, (int)screenGoal.Y, (int)screenLocation.X, (int)screenLocation.Y, new Color(255, 255, 255, 64));

                if (Vector2.Distance(Location, goal) > Texture.Texture.width)
                {
                    double angleOffset = (AngleToPoint(this.Location, goal) - Angle) + 90;
                    if (angleOffset > 180) { angleOffset -= 360; }
                    if (angleOffset < -180) { angleOffset += 360; }

                    if (angleOffset > 1) { AngularAcceleration = TurnSpeed * Delta; }
                    else if (angleOffset < -1) { AngularAcceleration = -TurnSpeed * Delta; }
                    else
                    {
                        AngularAcceleration = TurnSpeed * Math.Abs(Math.Pow(angleOffset, 2)) * angleOffset * Delta;
                    }

                    Throttle = Math.Pow(Math.Clamp((180 - angleOffset) / 180, 0.0, 1.0), 2);

                    if (Vector2.Distance(Location, goal) < Velocity.Length() * 60 * 10)
                    {
                        Throttle *= (Vector2.Distance(Location, goal) / ((Velocity.Length() * 120) + 1)) * 0.75;
                    }

                    //if (Behavior == Behaviors.Idle)
                    //{ Throttle = Throttle * 0.1; }
                }
                else
                {
                    Throttle = 0;
                }
            }

            double radians = (Angle - 90) * (Math.PI / 180);
            Vector2 thrust = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));
            thrust = thrust * (float)(MaxThrust * Throttle);
            Acceleration = thrust * (float)Delta / (float)Mass;

            // Nudge away from other ships
            SpaceObject[] neighbors = GetNeighbors(Location, Texture.Texture.width);
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
            /*
            if (!isLeader)
            {
                Vector2 nudge = Unit.Formation.GetLocation(this, Texture.Texture.width * 0.5f);
                nudge = nudge - Location;
                if (nudge.X != 0 || nudge.Y != 0)
                {
                    if (nudge.Length() < Texture.Texture.width)
                    {
                        double angleOffset = leader.Angle - Angle;
                        if (angleOffset > 180) { angleOffset -= 360; }
                        if (angleOffset < -180) { angleOffset += 360; }

                        if (angleOffset > 1) { AngularAcceleration = TurnSpeed * Delta; }
                        else if (angleOffset < -1) { AngularAcceleration = -TurnSpeed * Delta; }
                        else
                        {
                            AngularAcceleration += TurnSpeed * Math.Pow(angleOffset, 5) * 0.1f;
                        }
                    }
                    if (false)//Behavior == Behaviors.Idle)
                    {
                        nudge = Vector2.Normalize(nudge) * 0.01f;
                    }
                    else
                    {
                        nudge = Vector2.Normalize(nudge) * (Velocity.Length() * 0.1f + 1f) * 0.1f;
                    }

                    Velocity += (nudge * 1f) / (float)Mass;
                }
            }*/

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
                if (Behavior == Behaviors.Going && !(this is SpaceShipHardpoint))
                {
                    Color col = new Color(128, 128, 128, 16);
                    DrawLineEx(Location * GameManager.ViewScale + GameManager.ViewOffset,
                        Goal * GameManager.ViewScale + GameManager.ViewOffset,
                        2f, col);
                }
                else if (Behavior == Behaviors.Attacking)
                {
                    Color col = new Color(255, 0, 0, 16);
                    DrawLineEx(Location * GameManager.ViewScale + GameManager.ViewOffset,
                        Goal * GameManager.ViewScale + GameManager.ViewOffset
                        , 2f, col);
                }
            }

            Vector2 loc = DrawLocation;

            if (Unit != null && this == Unit.Leader)
            {
                DrawCircle((int)loc.X, (int)loc.Y, Texture.Texture.width * Scale * 0.25f, new Color(255, 0, 0, 128));
            }

            base.Draw();

            if (Active)
            {
                int barWidth = (int)(Math.Sqrt(Texture.Texture.width * Scale) * 5);
                int barHeight = 4;
                float barHalf = barWidth / 2f;
                float barOffset = Texture.Texture.width / 2f;

                if (Shield < MaxShield && Shield > 0)
                {
                    DrawRectangle((int)(loc.X - barHalf), (int)(loc.Y + barOffset), (int)Math.Round(barWidth * (Shield / MaxShield)), barHeight, Color.GREEN);
                    DrawRectangleLines((int)(loc.X - barHalf), (int)(loc.Y + barOffset), barWidth, barHeight, Color.DARKGREEN);
                }
                if (Hull < MaxHull && Hull > 0)
                {
                    DrawRectangle((int)(loc.X - barHalf), (int)(loc.Y + barOffset) + barHeight + 1, (int)Math.Round(barWidth * (Hull / MaxHull)), barHeight, Color.RED);
                    DrawRectangleLines((int)(loc.X - barHalf), (int)(loc.Y + barOffset) + barHeight + 1, barWidth, barHeight, Color.DARKPURPLE);
                }

                if (Debug.Enabled && !Debug.ConsoleIsOpen && IsKeyDown(KeyboardKey.KEY_F2))
                {
                    if (Stance == Stances.Defend)
                    {
                        if (Behavior == Behaviors.Idle)
                        {
                            DrawCircleLines((int)loc.X, (int)loc.Y, CombatRange * GameManager.ViewScale, new Color(255, 0, 0, 32));
                        }
                        else if (Behavior == Behaviors.Going)
                        {
                            DrawCircleLines((int)loc.X, (int)loc.Y, MathF.Sqrt(Velocity.Length()) * 0.75f * CombatRange * GameManager.ViewScale, new Color(255, 0, 0, 32));
                        }
                        else if (Behavior == Behaviors.Attacking)
                        {
                            DrawCircleLines((int)loc.X, (int)loc.Y, CombatRange * GameManager.ViewScale, new Color(255, 0, 0, 32));
                        }
                    }
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

        public override void Destroy()
        {
            if (Hardpoints != null)
            {
                foreach (SpaceShipHardpoint hp in Hardpoints)
                {
                    if (hp.Active) { hp.Destroy(); }
                }
            }

            var o = SpaceEffect.FromXml(ResourceManager.Get<XmlResource>(@"xml\effect\base_explosion"), null);
            o.Location = Location;
            o.Velocity = Velocity;
            GameManager.Add(o);

            base.Destroy();
        }

        public void Shoot()
        {
            double error = RNG.Next((int)Math.Sqrt(Math.Ceiling(Math.Clamp(shotHeat, 0, 100)))) - RNG.Next((int)Math.Sqrt(Math.Ceiling(Math.Clamp(shotHeat, 0, 100))));
            double radians = (Angle - 90 + error) * (Math.PI / 180);
            Vector2 heading = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));

            SpaceProjectile obj = new SpaceProjectile()
            {
                Texture = ResourceManager.Get<TextureResource>(@"images\projectile\proton+"),
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

            if (Hitbox == null && RNG.Next(100) < 10)
            {
                Hitbox = Hitbox.Automatic(Texture, (int)Math.Max(2, Scale * Texture.Texture.height / 8));
            }
        }

        private static double AngleToPoint(Vector2 A, Vector2 B)
        {
            float xDiff = B.X - A.X;
            float yDiff = B.Y - A.Y;
            return Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
        }

        protected static Vector2 RotateAroundPoint(Vector2 Position, Vector2 Center, double Angle)
        {
            //Note to self: System.Math operates in Radians
            double angle = (Angle) * (Math.PI / 180); // Convert to radians
            double rotatedX = Math.Cos(angle) * (Position.X - Center.X) - Math.Sin(angle) * (Position.Y - Center.Y) + Center.X;
            double rotatedY = Math.Sin(angle) * (Position.X - Center.X) + Math.Cos(angle) * (Position.Y - Center.Y) + Center.Y;
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
                       Vector2.Distance(o.Location, Position) < Radius
                    );

            return inSelection.ToArray();
        }

        private SpaceObject[] GetTargets(Vector2 Position, float Radius)
        {
            IEnumerable<SpaceObject> inSelection = GameManager.Instance.Objects.Where(o =>
                       o is SpaceShip &&
                       o.Faction != this.Faction &&
                       o.Active == true &&
                       Vector2.Distance(o.Location, Position) < Radius
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
                    baseObject = SpaceShip.FromXml(ResourceManager.Get<XmlResource>(baseName), null);
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
            Result.Texture = ResourceManager.Get<TextureResource>(GetXmlText(obj, "Texture", baseObject.Texture.Name));
            //Result.Hitbox = Hitbox.Automatic(Result.Texture, (int)Math.Max(2, Result.Scale * Result.Texture.Texture.height / 8));

            List<SpaceObject> hardpoints = GetXmlNested(obj, "Hardpoints", null);
            if (hardpoints != null && hardpoints.Count > 0)
            {
                Result.Hardpoints = new List<SpaceShipHardpoint>();
                foreach (SpaceObject o in hardpoints)
                {
                    SpaceShipHardpoint hp = o as SpaceShipHardpoint;
                    if (hp != null)
                    {
                        hp.Parent = Result;
                        hp.Depth += Result.Depth;
                        hp.Scale *= Result.Scale;
                        Result.Hardpoints.Add(hp);
                    }
                }
                Debug.WriteLine("Loaded hardpoints");
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
                                if (childNode.Name.ToUpperInvariant() == "hardpoint".ToUpperInvariant())
                                {
                                    SpaceShipHardpoint child = SpaceShipHardpoint.FromXml(new XmlResource() { Xml = new XmlDocument() { InnerXml = childNode.OuterXml } }, null);
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
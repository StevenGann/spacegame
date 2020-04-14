using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Raylib;

namespace SpaceGame
{
    internal class GameManager
    {
        public static GameManager Instance { get; set; } = null;
        public List<SpaceObject> Objects { get; set; } = new List<SpaceObject>();
        public List<SpaceShipUnit> Units { get; set; } = new List<SpaceShipUnit>();
        public static int PruneLimit = 100;
        public static TextureResource Background = ResourceManager.GetTexture(@"environment\space\stars_big");
        public static float ViewScale { get; set; } = 0.5f;
        public static Vector2 ViewOffset { get; set; } = new Vector2(0, 0);
        public static bool Paused { get; set; } = false;
        private static float panBoost = 0;

        private static Queue<SpaceObject> bufferObjects = new Queue<SpaceObject>();

        public static void Add(SpaceObject Object)
        {
            Instantiate();
            if (!Instance.Objects.Contains(Object))
            {
                //Instance.Objects.Add(Object);
                bufferObjects.Enqueue(Object);
            }
            //if (Instance.Objects.Count > PruneLimit) { Prune(); }
        }

        public static void Add(SpaceShipUnit Unit)
        {
            Instantiate();
            if (!Instance.Units.Contains(Unit))
            {
                Instance.Units.Add(Unit);
            }
            foreach (SpaceShip ship in Unit.Units)
            {
                Add(ship);
                if (ship.Hardpoints != null)
                {
                    foreach (SpaceShipHardpoint hp in ship.Hardpoints)
                    {
                        Add(hp);
                    }
                }
            }
        }

        public static void Tick(double Delta)
        {
            if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE)) { Paused = !Paused; }

            // =============================Camera Controls========================================

            float scrollAmount = Raylib.Raylib.GetMouseWheelMove();
            float a = (float)Math.Abs(scrollAmount) * ViewScale * 100.0f;
            if (scrollAmount > 0)
            {
                ViewScale *= 1.1f;
                if (ViewScale > 2) { ViewScale = 2; }
                else { ViewOffset -= new Vector2(a, a); }
            }
            if (scrollAmount < 0)
            {
                ViewScale *= 0.9f;
                if (ViewScale < 0.5) { ViewScale = 0.5f; }
                else { ViewOffset += new Vector2(a, a); }
            }

            if (Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_W)) { ViewOffset += Vector2.UnitY * (5f + panBoost); }
            if (Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_S)) { ViewOffset += Vector2.UnitY * -(5f + panBoost); }
            if (Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_A)) { ViewOffset += Vector2.UnitX * (5f + panBoost); }
            if (Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_D)) { ViewOffset += Vector2.UnitX * -(5f + panBoost); }
            if (!Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_W) &&
               !Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_S) &&
               !Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_A) &&
               !Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_D))
            {
                panBoost *= 0.90f;
            }
            else
            {
                panBoost += 0.25f;
                panBoost = Math.Clamp(panBoost, 0, 10);
            }
            if (!Paused || Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT) || Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT))
            {
                // ==================================Collisons=========================================
                while (bufferObjects.Count > 0)
                {
                    Instance.Objects.Add(bufferObjects.Dequeue());
                }

                IEnumerable<SpaceObject> projectiles = Instance.Objects.Where(o =>
                    o is SpaceProjectile &&
                    o.Active == true
                );

                IEnumerable<SpaceObject> ships = Instance.Objects.Where(o =>
                    o is SpaceShip &&
                    o.Active == true
                );

                foreach (SpaceObject projectile in projectiles)
                {
                    foreach (SpaceObject ship in ships)
                    {
                        if (projectile.Faction != ship.Faction &&
                            //Raylib.Raylib.Vector2Distance(projectile.Location, ship.Location) < ship.Texture.Texture.width / 2 &&
                            ship.CheckCollision(projectile.Hitbox, projectile.Location, projectile.Scale, (float)projectile.Angle) &&
                            (projectile as SpaceProjectile).Sender != ship
                            )
                        {
                            (ship as SpaceShip).Damage((projectile as SpaceProjectile).Damage);

                            var o = SpaceEffect.FromXml(ResourceManager.GetXml(@"effect\base_impact"), null);
                            o.Location = projectile.Location;
                            o.Velocity = ship.Velocity;
                            GameManager.Add(o);

                            projectile.Active = false;
                        }
                    }
                }
                // ====================================================================================

                foreach (SpaceShipUnit unit in Instance.Units)
                {
                    unit.Tick(Delta);
                }

                for (int i = 0; i < Instance.Objects.Count; i++)
                {
                    Instance.Objects[i].Tick(Delta);
                }

                if (Instance.Objects.Count > PruneLimit) { Prune(); }
            }
        }

        public static void Draw()
        {
            DrawBackground();

            /*for (int i = 0; i < Instance.Objects.Count; i++)
            {
                Instance.Objects[i].Draw();
            }*/

            foreach (SpaceShipUnit unit in Instance.Units)
            {
                unit.Draw();
            }

            IEnumerable<SpaceObject> query = from obj in Instance.Objects
                                             orderby obj.Depth
                                             select obj;
            foreach (SpaceObject obj in query)
            {
                obj.Draw();
            }
        }

        private static Vector2 bgOffset = new Vector2(-32, -32);

        private static void DrawBackground()
        {
            //Raylib.Raylib.DrawTexture(Background.Texture, (int)ViewOffset.x, (int)ViewOffset.y, Raylib.Color.WHITE);
            Raylib.Raylib.DrawTextureEx(Background.Texture, (ViewOffset / 50f) + bgOffset, 0, 0.5f, Color.WHITE);
        }

        public static void Instantiate()
        {
            if (Instance == null)
            {
                Instance = new GameManager();
            }
        }

        public static void Prune()
        {
            int count = 0;
            for (int i = 0; i < Instance.Objects.Count; i++)
            {
                if (!Instance.Objects[i].Active)
                {
                    Instance.Objects[i].Texture.Users--;
                    Instance.Objects.RemoveAt(i);
                    i--;
                    count++;
                }
            }
            PruneLimit = (int)Math.Floor(Instance.Objects.Count * 2.0);

            Console.WriteLine("Pruned " + count + " objects\t" + PruneLimit);
            System.GC.Collect();
        }
    }
}
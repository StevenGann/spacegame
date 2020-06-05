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
        public static float MapSize = 20000;
        public static Dictionary<int, Color> FactionColors = new Dictionary<int, Color>();
        private static float panBoost = 0;

        public static List<string> ScriptBlacklist
        {
            get
            {
                List<string> newList = new List<string>();
                foreach (string s in scriptBlacklist)
                {
                    newList.Add(s);
                }
                return newList;
            }
        }

        private static List<string> scriptBlacklist = new List<string>();

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
            if (Debug.Enabled)
            {
                if (!Debug.ConsoleIsOpen)
                {
                    if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE)) { Paused = !Paused; }
                }
            }

            // =============================Camera Controls========================================

            float scrollAmount = Raylib.Raylib.GetMouseWheelMove();
            float a = (float)Math.Abs(scrollAmount) * ViewScale * 100.0f;
            a = 0;
            const float maxZoom = 4;
            const float minZoom = 0.1f;
            if (scrollAmount > 0)
            {
                ViewScale *= 1.1f;
                if (ViewScale > maxZoom) { ViewScale = maxZoom; }
                else { ViewOffset -= new Vector2(a, a); }
            }
            if (scrollAmount < 0)
            {
                ViewScale *= 0.9f;
                if (ViewScale < minZoom) { ViewScale = minZoom; }
                else { ViewOffset += new Vector2(a, a); }
            }

            if (!Debug.ConsoleIsOpen)
            {
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
            }

            if (!Paused || ((Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT) || Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT)) && !Debug.ConsoleIsOpen))
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

        private static Vector2 bgOffset = new Vector2(-128, -128);

        private static void DrawBackground()
        {
            //Raylib.Raylib.DrawTexture(Background.Texture, (int)ViewOffset.x, (int)ViewOffset.y, Raylib.Color.WHITE);
            Raylib.Raylib.DrawTextureEx(Background.Texture, (ViewOffset / 50f) + bgOffset, 0, 0.75f, Color.WHITE);
        }

        public static void Instantiate()
        {
            if (Instance == null)
            {
                Instance = new GameManager();
                FactionColors.Add(0, Color.WHITE);
                FactionColors.Add(1, Color.SKYBLUE);
                FactionColors.Add(2, Color.RED);
                FactionColors.Add(3, Color.ORANGE);
                FactionColors.Add(4, Color.PURPLE);

                Debug.RegisterCommand("list-objects", CommandListObjects, "Lists all SpaceObjects tracked by the GameManager");
                Debug.RegisterCommand("list-units", CommandListUnits, "Lists all SpaceUnits tracked by the GameManager");
                Debug.RegisterCommand("spawn-unit", CommandSpawnUnit, "Spawns a unit in the center of the current view");

                scriptBlacklist.Add("IO");
                scriptBlacklist.Add("Net");
                scriptBlacklist.Add("unsafe");
                scriptBlacklist.Add("IntPtr");

                Debug.ExecuteBatch("startup.bat");
            }
        }

        private static int CommandListUnits(List<string> arg)
        {
            for (int i = 0; i < GameManager.Instance.Units.Count; i += 0)
            {
                string line = "";
                for (int j = 0; j < 8; j++)
                {
                    if ((i + j) < GameManager.Instance.Units.Count)
                    {
                        line += (i + j).ToString() + ":[" + GameManager.Instance.Units[i + j].Units[0].XmlSource + "]\t";
                    }
                }

                Debug.WriteLine(line);
                i += 8;
            }

            return 0;
        }

        private static int CommandListObjects(List<string> arg)
        {
            for (int i = 0; i < GameManager.Instance.Objects.Count; i += 0)
            {
                string line = "";
                for (int j = 0; j < 8; j++)
                {
                    if ((i + j) < GameManager.Instance.Objects.Count)
                    {
                        line += (i + j).ToString() + ":[" + "]\t";
                    }
                }

                Debug.WriteLine(line);
                i += 8;
            }

            return 0;
        }

        private static int CommandSpawnUnit(List<string> arg)
        {
            if (arg == null || arg.Count < 1)
            {
                Debug.WriteLine("%WARNING%Usage: \"spawn-unit <xml name>\"");
                return 1;
            }

            SpaceShipUnit newUnit = new SpaceShipUnit();
            for (int i = 0; i < 7; i++)
            {
                SpaceShip newShip = SpaceShip.FromXml(ResourceManager.GetXml(arg[0]), null);
                newShip.Location = new Vector2(0, 0);
                newShip.Faction = 2;
                newShip.Hitbox = new Hitbox(newShip.Texture.Texture.width);
                newUnit.Add(newShip);
            }
            newUnit.UiImage = ResourceManager.GetTexture(@"thumbnail\barb");
            GameManager.Add(newUnit);

            return 0;
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

            Debug.WriteLine("Pruned " + count + " objects\t" + PruneLimit);
            if (Debug.GetFlag("force_gc") > 0)
            {
                System.GC.Collect();
            }
        }
    }
}
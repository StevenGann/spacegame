using System;
using System.Collections.Generic;
using Raylib;
using static Raylib.Raylib;

namespace SpaceGame
{
    internal class Program
    {
        private static string ResourcesPath = "";

        private static void Main(string[] args)
        {
            InitWindow(1920, 1080, "SpaceGame");
            SetConfigFlags(ConfigFlag.FLAG_VSYNC_HINT | ConfigFlag.FLAG_FULLSCREEN_MODE | ConfigFlag.FLAG_MSAA_4X_HINT);
            Random RNG = new Random();
            ResourcesPath = System.IO.Directory.GetCurrentDirectory() + @"\..\..\resources\";
            ResourceManager.Load(ResourcesPath);

            SpaceShip objPlayer = new SpaceShip()
            {
                Texture = ResourceManager.GetTexture(@"ship\kar ik vot 349"),
                Drag = 0.07,
                AngularDrag = 0.2,
                Depth = -1000,
                MaxShield = 1000,
                Shield = 1000,
                MaxHull = 5000,
                Hull = 5000,
                TurnSpeed = 0.05,
                MaxThrust = 0.07,
                Mass = 10,
                Scale = 2f,
                Location = new Vector2(300, 300),
                Faction = 1
            };
            objPlayer.Hardpoints = new List<SpaceShipHardpoint>();
            Vector2[] hps = {
                new Vector2(200, 75),
                new Vector2(150, 75),
                new Vector2(100, 75),
                new Vector2(50,  75),
                new Vector2(0,   75),
                new Vector2(-50, 75),
                new Vector2(-100, 75),
                new Vector2(-150, 75),
                new Vector2(-200, 75),
                new Vector2(200,  -75),
                new Vector2(150,  -75),
                new Vector2(100,  -75),
                new Vector2(50,   -75),
                new Vector2(0,    -75),
                new Vector2(-50,  -75),
                new Vector2(-100, -75),
                new Vector2(-150, -75),
                new Vector2(-200, -75),
            };
            foreach (Vector2 vec in hps)
            {
                var hp = new SpaceShipHardpoint()
                {
                    Scale = objPlayer.Scale,
                    Parent = objPlayer,
                    Depth = objPlayer.Depth + 1,
                    Texture = ResourceManager.GetTexture(@"hardpoint\blaster turret"),
                    Offset = vec,
                    Faction = objPlayer.Faction,
                    Drag = 1,
                    AngularDrag = 1,
                    TurnSpeed = 1,
                    RateOfFire = 2
                };
                //hp.Hitbox = Hitbox.Automatic(hp.Texture, 2);
                hp.Hitbox = new Hitbox(hp.Texture.Texture.height / 2);
                objPlayer.Hardpoints.Add(hp);
            }
            objPlayer.Hitbox = Hitbox.Automatic(objPlayer.Texture, (int)Math.Floor(objPlayer.Texture.Texture.height / 32.0));
            //objPlayer.Hitbox = new Hitbox();
            //objPlayer.Hitbox.AddRectangle(new Rectangle(-120, 30, 240, 200));
            //objPlayer.Hitbox.AddRectangle(new Rectangle(-50, -240, 100, 300));
            SpaceShipUnit unitPlayer = new SpaceShipUnit(objPlayer);
            //GameManager.Add(objPlayer);
            GameManager.Add(unitPlayer);

            for (int j = 0; j < 10; j++)
            {
                SpaceShipUnit unitEnemy = new SpaceShipUnit();
                for (int i = 0; i < 7; i++)
                {
                    SpaceShip objEnemy = SpaceShip.FromXml(ResourceManager.GetXml(@"ship\test_fighter"), null);
                    /*objEnemy = new SpaceShip()
                    {
                        Texture = ResourceManager.GetTexture(@"ship\barb"),
                        Drag = 0.01,
                        AngularDrag = 0.1,
                        RateOfFire = 1.0,
                        Scale = 0.25f,
                        Depth = RNG.NextDouble() * RNG.Next(-100, 100)
                    };*/
                    objEnemy.Location = new Vector2(900 - (i * 60), 900 - (j * 60));
                    objEnemy.Faction = 2;
                    objEnemy.Hitbox = new Hitbox(objEnemy.Texture.Texture.width);
                    unitEnemy.Add(objEnemy);
                }
                GameManager.Add(unitEnemy);
            }

            UiManager.Instantiate();
            double TargetFps = 60;
            SetTargetFPS((int)TargetFps);
            while (!Raylib.Raylib.WindowShouldClose())
            {
                double fps = Math.Clamp(Raylib.Raylib.GetFPS(), 25, 1000);
                double delta = TargetFps / fps;

                //if (Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_SPACE)) { delta = 1; }

                GameManager.Tick(delta);

                UiManager.Tick(delta);

                BeginDrawing();

                //ClearBackground(Color.BLACK);

                GameManager.Draw();

                UiManager.Draw();

                //DrawFPS(10, 10);

                Debug.Draw();

                EndDrawing();

                ResourceManager.Cull();
            }

            CloseWindow();
        }
    }
}
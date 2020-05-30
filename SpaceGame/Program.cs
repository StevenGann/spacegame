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

            SpaceShip objPlayer = SpaceShip.FromXml(ResourceManager.GetXml(@"ship\base_cruiser"), null);
            /*= new SpaceShip()
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
            Location = new Vector2(900, 300),
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
                //Drag = 1,
                AngularDrag = 1,
                TurnSpeed = 1,
                RateOfFire = 2
            };
            //hp.Hitbox = Hitbox.Automatic(hp.Texture, 2);
            hp.Hitbox = new Hitbox(hp.Texture.Texture.height / 2);
            objPlayer.Hardpoints.Add(hp);
        }*/
            objPlayer.Faction = 1;
            objPlayer.Location = new Vector2(1000, 300);
            objPlayer.Hitbox = Hitbox.Automatic(objPlayer.Texture, (int)Math.Floor(objPlayer.Texture.Texture.height / 32.0));
            //objPlayer.Hitbox = new Hitbox();
            //objPlayer.Hitbox.AddRectangle(new Rectangle(-120, 30, 240, 200));
            //objPlayer.Hitbox.AddRectangle(new Rectangle(-50, -240, 100, 300));
            SpaceShipUnit unitPlayer = new SpaceShipUnit(objPlayer);
            unitPlayer.UiImage = ResourceManager.GetTexture(@"thumbnail\kar ik vot 349");
            //GameManager.Add(objPlayer);
            GameManager.Add(unitPlayer);

            for (int j = 0; j < 10; j++)
            {
                SpaceShipUnit unitEnemy = SpaceShipUnit.FromXml(ResourceManager.GetXml(@"unit\base_fighter_squadron"), null);
                /*new SpaceShipUnit();
            for (int i = 0; i < 7; i++)
            {
                SpaceShip objEnemy = SpaceShip.FromXml(ResourceManager.GetXml(@"ship\test_fighter"), null);
                objEnemy.Location = new Vector2(900 - (i * 60), 900 - (j * 60));
                objEnemy.Faction = 2;
                objEnemy.Hitbox = new Hitbox(objEnemy.Texture.Texture.width);
                unitEnemy.Add(objEnemy);
            }
            unitEnemy.UiImage = ResourceManager.GetTexture(@"thumbnail\barb");
            */
                unitEnemy.Location = new Vector2(900, 900 - (j * 60));
                unitEnemy.Formation = new Formation();
                GameManager.Add(unitEnemy);
            }

            SpaceStructure station = new SpaceStructure()
            {
                Texture = ResourceManager.GetTexture(@"planet\station2"),
                Location = new Vector2(1500, 500),
                Scale = 2.0f,
                Hitbox = Hitbox.Automatic(ResourceManager.GetTexture(@"planet\station2"), 6),
                Faction = 2,
                MaxHull = 1000,
                Hull = 1000,
                MaxShield = 1000,
                Shield = 1000
            };
            GameManager.Add(station);

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
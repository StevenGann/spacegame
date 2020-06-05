using System;
using System.Collections.Generic;
using System.ComponentModel;
using Raylib;
using static Raylib.Raylib;

namespace SpaceGame
{
    internal class Program
    {
        private static string ResourcesPath = "";

        private static void Main(string[] args)
        {
            ResourcesPath = System.IO.Directory.GetCurrentDirectory() + @"\..\..\resources\";

            /*
            SetTargetFPS(120);
            InitWindow(800, 600, "Loading");
            BackgroundWorker loadingWorker = new BackgroundWorker();
            loadingWorker.DoWork += LoadingWorker_DoWork;
            loadingWorker.RunWorkerAsync();

            TextureResource bkg = null;
            FontResource fnt = null;
            Color bkgColor = Color.WHITE;
            Color foreColor = Color.BLACK;
            while (loadingWorker.IsBusy)
            {
                if (ResourceManager.Instance != null && (bkg == null || fnt == null))
                {
                    try { bkg = ResourceManager.GetTexture("_menu\\loading"); } catch { }
                    try { fnt = ResourceManager.GetFont("Perfect_DOS_VGA_437_Win"); } catch { }
                }

                BeginDrawing();
                Raylib.Raylib.ClearBackground(bkgColor);
                if (bkg != null)
                {
                    Raylib.Raylib.DrawTexturePro(bkg.Texture, new Rectangle(0, 0, bkg.Texture.width, bkg.Texture.height), new Rectangle(0, 0, 800, 600), Vector2.Zero, 0.0f, Color.WHITE);
                    bkgColor = Color.BLACK;
                    foreColor = Color.WHITE;
                }

                if (fnt != null) { Raylib.Raylib.DrawTextEx(fnt.Font, "Loading", new Vector2(20, 20), 16, 4, foreColor); }
                else { Raylib.Raylib.DrawText("Loading", 20, 20, 16, foreColor); }
                if (ResourceManager.Instance != null)
                {
                    if (ResourceManager.Instance.Xml.Count > 0)
                    {
                        string txt = ResourceManager.Instance.Xml.Count + " XML files";
                        if (fnt != null) { Raylib.Raylib.DrawTextEx(fnt.Font, txt, new Vector2(20, 50), 16, 4, foreColor); }
                        else { Raylib.Raylib.DrawText(txt, 20, 50, 16, foreColor); }
                    }
                    if (ResourceManager.Instance.Fonts.Count > 0)
                    {
                        string txt = ResourceManager.Instance.Fonts.Count + " fonts";
                        if (fnt != null) { Raylib.Raylib.DrawTextEx(fnt.Font, txt, new Vector2(20, 80), 16, 4, foreColor); }
                        else { Raylib.Raylib.DrawText(txt, 20, 80, 16, foreColor); }
                    }
                    if (ResourceManager.Instance.Sounds.Count > 0)
                    {
                        string txt = ResourceManager.Instance.Sounds.Count + " sounds";
                        if (fnt != null) { Raylib.Raylib.DrawTextEx(fnt.Font, txt, new Vector2(20, 110), 16, 4, foreColor); }
                        else { Raylib.Raylib.DrawText(txt, 20, 110, 16, foreColor); }
                    }
                    if (ResourceManager.Instance.Scripts.Count > 0)
                    {
                        string txt = ResourceManager.Instance.Scripts.Count + " scripts";
                        if (fnt != null) { Raylib.Raylib.DrawTextEx(fnt.Font, txt, new Vector2(20, 140), 16, 4, foreColor); }
                        else { Raylib.Raylib.DrawText(txt, 20, 110, 16, foreColor); }
                    }
                    if (ResourceManager.Instance.Textures.Count > 0)
                    {
                        string txt = ResourceManager.Instance.Textures.Count + " textures";
                        if (fnt != null) { Raylib.Raylib.DrawTextEx(fnt.Font, txt, new Vector2(20, 170), 16, 4, foreColor); }
                        else { Raylib.Raylib.DrawText(txt, 20, 110, 16, foreColor); }
                    }
                }

                string line = Debug.ConsoleBuffer;
                if (fnt != null) { Raylib.Raylib.DrawTextEx(fnt.Font, line, new Vector2(20, 580), 16, 4, foreColor); }
                else { Raylib.Raylib.DrawText(line, 20, 580, 16, foreColor); }
                EndDrawing();
            }
            CloseWindow();
            */

            ResourceManager.Load(ResourcesPath);
            GameManager.Instantiate();

            InitWindow((int)Math.Max(1024, Debug.GetFlag("ScreenWidth")), (int)Math.Max(768, Debug.GetFlag("ScreenHeight")), "SpaceGame");
            SetConfigFlags(ConfigFlag.FLAG_VSYNC_HINT | ConfigFlag.FLAG_MSAA_4X_HINT | ((Debug.GetFlag("Fullscreen") == 1) ? (ConfigFlag.FLAG_FULLSCREEN_MODE) : 0));
            UiManager.Instantiate();

            for (int j = 0; j < 1; j++)
            {
                SpaceShipUnit unitEnemy = SpaceShipUnit.FromXml(ResourceManager.GetXml(@"unit\base_fighter_squadron"), null);
                unitEnemy.Location = new Vector2(900, 900 - (j * 60));
                unitEnemy.Formation = new Formation();
                GameManager.Add(unitEnemy);
            }
            /*
            SpaceShip objPlayer = SpaceShip.FromXml(ResourceManager.GetXml(@"ship\base_cruiser"), null);
            objPlayer.Faction = 1;
            objPlayer.Location = new Vector2(1000, 300);
            objPlayer.Hitbox = Hitbox.Automatic(objPlayer.Texture, (int)Math.Floor(objPlayer.Texture.Texture.height / 32.0));

            SpaceShipUnit unitPlayer = new SpaceShipUnit(objPlayer);
            unitPlayer.UiImage = ResourceManager.GetTexture(@"thumbnail\kar ik vot 349");
            GameManager.Add(unitPlayer);

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
            */

            double TargetFps = 60;
            SetTargetFPS((int)TargetFps);
            while (!Raylib.Raylib.WindowShouldClose())
            {
                double fps = Math.Clamp(Raylib.Raylib.GetFPS(), 25, 1000);
                double delta = TargetFps / fps;

                GameManager.Tick(delta);

                UiManager.Tick(delta);

                BeginDrawing();

                GameManager.Draw();

                UiManager.Draw();

                Debug.Draw();

                EndDrawing();

                ResourceManager.Cull();
            }

            CloseWindow();
        }

        private static void LoadingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ResourceManager.Load(ResourcesPath);
            GameManager.Instantiate();
        }
    }
}
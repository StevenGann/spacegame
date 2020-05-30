using Raylib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Vector2 = Raylib.Vector2;

namespace SpaceGame
{
    internal class UiManager
    {
        public static UiManager Instance;
        public static Vector2 DownLocation { get; set; }
        public static Vector2 UpLocation { get; set; }
        public static Rectangle SelectionRectangle { get; set; }
        public static MouseStates MouseState { get; set; } = MouseStates.Idle;
        public static List<SpaceObject> Selected { get; set; } = new List<SpaceObject>();
        public static List<SpaceShipUnit> SelectedUnits { get; set; } = new List<SpaceShipUnit>();
        public static int ScreenWidth = 1920;
        public static int ScreenHeight = 1080;
        public const float MinimapSize = 512;
        public static RenderTexture2D MinimapTexture = Raylib.Raylib.LoadRenderTexture((int)MinimapSize, (int)MinimapSize);
        private static Texture2D panelTexture = ResourceManager.GetTexture(@"ui\ninepatch_button").Texture;
        private static Texture2D selectTexture = ResourceManager.GetTexture(@"ui\selection_box").Texture;
        private static List<Rectangle> Keepouts = new List<Rectangle>();

        private static NPatchInfo npi = new NPatchInfo()
        {
            sourceRec = new Rectangle(0, 0, 64, 64),
            top = 16,
            bottom = 16,
            left = 16,
            right = 16
        };

        private static Random RNG = new Random();
        private static Vector2 downLocationScreen = new Vector2();

        public static void Tick(double Delta)
        {
            bool inKeepout = InKeepout(Raylib.Raylib.GetMousePosition());
            //if (inKeepout) { Debug.WriteOverlay("In Keepout"); }

            //Debug.WriteOverlay(MouseState.ToString());
            if (Raylib.Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON) && MouseState == MouseStates.Idle)
            {
                downLocationScreen = Raylib.Raylib.GetMousePosition();
                DownLocation = Raylib.Raylib.GetMousePosition() / GameManager.ViewScale - GameManager.ViewOffset / GameManager.ViewScale;
            }
            else if (Raylib.Raylib.IsMouseButtonDown(MouseButton.MOUSE_LEFT_BUTTON) && !inKeepout)
            {
                Vector2 mousePos = Raylib.Raylib.GetMousePosition() / GameManager.ViewScale - GameManager.ViewOffset / GameManager.ViewScale;
                if (Raylib.Raylib.Vector2Distance(mousePos, DownLocation) > 64 && !InKeepout(downLocationScreen))
                {
                    MouseState = MouseStates.Dragging;
                    SelectionRectangle = RecFromVec(DownLocation, mousePos);
                }
            }
            else if (Raylib.Raylib.IsMouseButtonReleased(MouseButton.MOUSE_LEFT_BUTTON))
            {
                UpLocation = Raylib.Raylib.GetMousePosition() / GameManager.ViewScale - GameManager.ViewOffset / GameManager.ViewScale;
                if (MouseState == MouseStates.Dragging)
                {
                    MouseState = MouseStates.DragReleased;
                }
                else
                {
                    GetSelection();
                    MouseState = MouseStates.Idle;
                }
            }
            else if (MouseState == MouseStates.DragReleased)
            {
                SelectionRectangle = RecFromVec(DownLocation, UpLocation);
                GetSelection();
                MouseState = MouseStates.Idle;
            }

            if (Raylib.Raylib.IsMouseButtonReleased(MouseButton.MOUSE_RIGHT_BUTTON) && MouseState == MouseStates.Idle)
            {
                if (Selected.Count != 0)
                {
                    IEnumerable<SpaceObject> inSelection = GameManager.Instance.Objects.Where(o =>
                           o is SpaceShip &&
                           o.Active == true &&
                           Raylib.Raylib.Vector2Distance(o.Location, Raylib.Raylib.GetMousePosition() / GameManager.ViewScale - GameManager.ViewOffset / GameManager.ViewScale) < MathF.Max(o.TextureOffset.x, o.TextureOffset.y)
                        );

                    if (inSelection.Any())
                    {
                        foreach (SpaceObject obj in Selected)
                        {
                            SpaceObject target = inSelection.First();
                            foreach (SpaceObject spaceobj in inSelection)
                            {
                                if (spaceobj is SpaceShipHardpoint)
                                {
                                    target = spaceobj;
                                    break;
                                }
                            }
                            (obj as SpaceShip).Objective = target;
                            (obj as SpaceShip).Behavior = SpaceShip.Behaviors.Attacking;
                        }
                    }
                    else
                    {
                        int count = 1;
                        foreach (SpaceObject obj in Selected)
                        {
                            (obj as SpaceShip).Objective = null;
                            //(obj as SpaceShip).Goal = Raylib.Raylib.GetMousePosition();
                            Vector2 mouse = Raylib.Raylib.GetMousePosition() / GameManager.ViewScale - GameManager.ViewOffset / GameManager.ViewScale;
                            (obj as SpaceShip).Goal = new Vector2(mouse.x + RNG.Next(count) - RNG.Next(count), mouse.y + RNG.Next(count) - RNG.Next(count));
                            (obj as SpaceShip).Behavior = SpaceShip.Behaviors.Going;
                            count += 5;
                        }
                    }
                }
            }
            Raylib.Raylib.BeginTextureMode(MinimapTexture);
            Texture2D bkgTex = ResourceManager.GetTexture(@"_menu\haze+").Texture;
            Raylib.Raylib.DrawTexturePro(bkgTex, new Rectangle(0, 0, bkgTex.width, bkgTex.height), new Rectangle(0, 0, MinimapSize, MinimapSize), Vector2.Zero, 0.0f, Color.WHITE);
            float scaleFactor = MinimapSize / GameManager.MapSize;
            Vector2 origin = new Vector2(0, MinimapSize);

            Debug.WriteOverlay("View Scale: " + GameManager.ViewScale);
            Raylib.Raylib.BeginBlendMode(BlendMode.BLEND_ADDITIVE);
            Vector2 TL = ScreenToWorld(new Vector2(0, ScreenHeight)) * scaleFactor;
            Vector2 BR = ScreenToWorld(new Vector2(ScreenWidth, 0)) * scaleFactor;
            Raylib.Raylib.DrawRectangle((int)TL.x, (int)(MinimapSize - TL.y), (int)(BR.x - TL.x), (int)(TL.y - BR.y), new Color(0, 255, 0, 16));

            foreach (SpaceObject obj in GameManager.Instance.Objects)
            {
                if (obj.Active)
                {
                    Vector2 pos = (obj.Location * scaleFactor);
                    pos = new Vector2(pos.x, MinimapSize - pos.y);
                    //Raylib.Raylib.DrawCircleV(pos, 4, GameManager.FactionColors[obj.Faction].SetAlpha(32));// TODO: objects draw themselves
                    obj.DrawMinimap(pos);
                }
            }
            Raylib.Raylib.EndBlendMode();
            Raylib.Raylib.EndTextureMode();
        }

        private static bool InKeepout(Vector2 point)
        {
            foreach (Rectangle rect in Keepouts)
            {
                if (Raylib.Raylib.CheckCollisionPointRec(point, rect))
                {
                    return true;
                }
            }
            return false;
        }

        private static Rectangle AddKeepout(Rectangle keepout)
        {
            Keepouts.Add(keepout);
            return keepout;
        }

        public static void Draw()
        {
            Keepouts.Clear();

            if (MouseState == MouseStates.Dragging)
            {
                Rectangle selrec = new Rectangle(
                    SelectionRectangle.x * GameManager.ViewScale + GameManager.ViewOffset.x,
                    SelectionRectangle.y * GameManager.ViewScale + GameManager.ViewOffset.y,
                    SelectionRectangle.width * GameManager.ViewScale,
                    SelectionRectangle.height * GameManager.ViewScale
                    );
                Raylib.Raylib.DrawTextureNPatch(selectTexture, npi, selrec, new Vector2(0, 0), 0, Color.WHITE);
            }

            int margin = 4;
            float mapTrayWidth = 300;
            float mapTrayHeight = 300;
            float unitTrayWidth = ScreenWidth - (mapTrayWidth + 2 * margin);
            float unitTrayHeight = 150;
            Raylib.Raylib.DrawTextureNPatch(panelTexture, npi, AddKeepout(new Rectangle(0, ScreenHeight - mapTrayHeight, mapTrayWidth, mapTrayHeight)), new Vector2(0, 0), 0, Color.WHITE);
            Raylib.Raylib.DrawTextureNPatch(panelTexture, npi, AddKeepout(new Rectangle(mapTrayWidth + margin, ScreenHeight - unitTrayHeight, unitTrayWidth, unitTrayHeight)), new Vector2(0, 0), 0, Color.WHITE);
            float unitHeight = 80;
            float unitWidth = 75;

            Vector2 startPos = new Vector2(3 * margin + mapTrayWidth, (ScreenHeight - unitTrayHeight) + 2 * margin);
            int barWidth = (int)(unitWidth - margin * 2);
            int barHeight = 8;
            float barHalf = barWidth / 2f;
            float barOffset = unitTrayHeight - (barHeight * 4 + margin * 2);
            foreach (SpaceShipUnit unit in SelectedUnits)
            {
                if (unit != null && unit.Units.Count > 0)
                {
                    Raylib.Raylib.DrawTextureNPatch(selectTexture, npi, new Rectangle(startPos.x - margin, startPos.y - margin, unitWidth + (2 * margin), unitTrayHeight - (2 * margin)), new Vector2(0, 0), 0, Color.WHITE);
                    Texture2D tex = unit.UiImage.Texture;
                    float scalar = MathF.Min(MathF.Min(unitWidth / tex.width, unitHeight / tex.height), 1f);
                    Vector2 offset = new Vector2((unitWidth - (tex.width * scalar)) / 2f, (unitHeight - (tex.height * scalar)) / 2f);
                    Raylib.Raylib.DrawTextureEx(tex, startPos + offset, 0, scalar, Color.WHITE);

                    Raylib.Raylib.DrawRectangle((int)(startPos.x + margin), (int)(startPos.y + barOffset), (int)Math.Round(barWidth * (unit.Units[0].Shield / unit.Units[0].MaxShield)), barHeight, Color.GREEN);
                    Raylib.Raylib.DrawRectangleLines((int)(startPos.x + margin), (int)(startPos.y + barOffset), barWidth, barHeight, Color.DARKGREEN);
                    Raylib.Raylib.DrawRectangle((int)(startPos.x + margin), (int)(startPos.y + barOffset) + barHeight + 1, (int)Math.Round(barWidth * (unit.Units[0].Hull / unit.Units[0].MaxHull)), barHeight, Color.RED);
                    Raylib.Raylib.DrawRectangleLines((int)(startPos.x + margin), (int)(startPos.y + barOffset) + barHeight + 1, barWidth, barHeight, Color.DARKPURPLE);

                    startPos += new Vector2(unitWidth + (2 * margin), 0);
                }
            }

            Vector2 mapOrigin = new Vector2(margin, (ScreenHeight - mapTrayHeight) + margin);
            Raylib.Raylib.DrawTexturePro(MinimapTexture.texture, new Rectangle(0, 0, MinimapSize, MinimapSize), new Rectangle(mapOrigin.x, mapOrigin.y, 300 - 2 * margin, 300 - 2 * margin), Vector2.Zero, 0.0f, Color.WHITE);

            if (Debug.Enabled && !Debug.ConsoleIsOpen && Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_F4))
            {
                Color c = new Color(255, 0, 0, 255);
                foreach (Rectangle rec in Keepouts)
                {
                    Raylib.Raylib.DrawRectangleLinesEx(rec, 2, c);
                }
            }
        }

        public static void GetSelection()
        {
            if (MouseState == MouseStates.DragReleased)
            {
                IEnumerable<SpaceObject> inSelection = GameManager.Instance.Objects.Where(o =>
                       o is SpaceShip &&
                       o.Active == true &&
                       o.Location.x > SelectionRectangle.x &&
                       o.Location.y > SelectionRectangle.y &&
                       o.Location.x < SelectionRectangle.x + SelectionRectangle.width &&
                       o.Location.y < SelectionRectangle.y + SelectionRectangle.height
                    );
                if (!inSelection.Any()) { return; }
                if (!Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
                {
                    ClearSelection();
                }

                foreach (SpaceObject o in inSelection)
                {
                    o.Selected = true;
                    Selected.Add(o);
                }
                UpdateSelectedUnits();
            }
            else if (MouseState == MouseStates.Idle)
            {
                IEnumerable<SpaceObject> inSelection = GameManager.Instance.Objects.Where(o =>
                       o is SpaceShip &&
                       o.Active == true &&
                       Raylib.Raylib.Vector2Distance(o.Location, UpLocation) < MathF.Max(o.TextureOffset.x, o.TextureOffset.y)
                    );
                if (!inSelection.Any())
                {
                    ClearSelection();
                    return;
                }
                if (!Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
                {
                    ClearSelection();
                }

                SpaceObject ob = inSelection.First();
                ob.Selected = true;
                Selected.Add(ob);
                UpdateSelectedUnits();
            }
        }

        public static void ClearSelection()
        {
            var temp = Selected.ToArray();
            Selected.Clear();

            for (int i = 0; i < temp.Length; i++)
            {
                temp[i].Selected = false;
            }
            UpdateSelectedUnits();
        }

        public static void UpdateSelectedUnits()
        {
            SelectedUnits.Clear();
            foreach (SpaceObject s in Selected)
            {
                SpaceShip ship = s as SpaceShip;
                if (ship != null)
                {
                    if (!SelectedUnits.Contains(ship.Unit))
                    {
                        SelectedUnits.Add(ship.Unit);
                    }
                }
            }
            Debug.WriteLine("Synced unit selection");
        }

        public static Vector2 ScreenToWorld(Vector2 Point)
        {
            return Point / GameManager.ViewScale - GameManager.ViewOffset / GameManager.ViewScale;
        }

        public static Vector2 WorldToScreen(Vector2 Point)
        {
            return (Point * GameManager.ViewScale) + GameManager.ViewOffset;
        }

        public static Rectangle ScreenToWorld(Rectangle Box)
        {
            Vector2 TopLeft = ScreenToWorld(new Vector2(Box.x, Box.y));
            Vector2 BottomRight = ScreenToWorld(new Vector2(Box.x + Box.width, Box.y + Box.height));
            return new Rectangle()
            {
                x = TopLeft.x,
                y = TopLeft.y,
                width = BottomRight.x - TopLeft.x,
                height = BottomRight.y - TopLeft.y
            };
        }

        public static Rectangle WorldToScreen(Rectangle Box)
        {
            Vector2 TopLeft = WorldToScreen(new Vector2(Box.x, Box.y));
            Vector2 BottomRight = WorldToScreen(new Vector2(Box.x + Box.width, Box.y + Box.height));
            return new Rectangle()
            {
                x = TopLeft.x,
                y = TopLeft.y,
                width = BottomRight.x - TopLeft.x,
                height = BottomRight.y - TopLeft.y
            };
        }

        public static void Instantiate()
        {
            if (Instance == null)
            {
                Instance = new UiManager();
            }
        }

        public enum MouseStates
        {
            Idle,
            Dragging,
            DragReleased,
        }

        private static Rectangle RecFromVec(Vector2 A, Vector2 B)
        {
            return new Rectangle(
                MathF.Min(A.x, B.x),
                MathF.Min(A.y, B.y),
                MathF.Abs(A.x - B.x),
                MathF.Abs(A.y - B.y)
                );
        }
    }
}
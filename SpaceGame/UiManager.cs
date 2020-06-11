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

        public static bool mousePressed;
        public static bool mouseReleased;
        public static bool mouseUp;
        public static bool mouseDown;
        public static bool rightPressed;
        public static bool rightReleased;
        public static bool rightUp;
        public static bool rightDown;
        public static Vector2 mousePosition;

        private static Texture2D panelTexture = ResourceManager.GetTexture(@"ui\ninepatch_button").Texture;
        private static Texture2D selectTexture = ResourceManager.GetTexture(@"ui\selection_box").Texture;

        private static bool initialized = false;

        private static List<IUiElement> UiElements = new List<IUiElement>();

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

        private static UiTray unitsTray;

        public static void Tick(double Delta)
        {
            if (!initialized)
            {
                UiMinimap map = new UiMinimap();
                map.Bounds = new Rectangle(0, ScreenHeight - 300, 300, 300);
                UiElements.Add(map);

                unitsTray = new UiTray();
                unitsTray.Bounds = new Rectangle(300, ScreenHeight - 150, ScreenWidth - 300, 150);
                UiElements.Add(unitsTray);

                initialized = true;
            }

            mousePressed = Raylib.Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON);
            mouseReleased = Raylib.Raylib.IsMouseButtonReleased(MouseButton.MOUSE_LEFT_BUTTON);
            mouseUp = Raylib.Raylib.IsMouseButtonUp(MouseButton.MOUSE_LEFT_BUTTON);
            mouseDown = Raylib.Raylib.IsMouseButtonDown(MouseButton.MOUSE_LEFT_BUTTON);
            rightPressed = Raylib.Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON);
            rightReleased = Raylib.Raylib.IsMouseButtonReleased(MouseButton.MOUSE_RIGHT_BUTTON);
            rightUp = Raylib.Raylib.IsMouseButtonUp(MouseButton.MOUSE_RIGHT_BUTTON);
            rightDown = Raylib.Raylib.IsMouseButtonDown(MouseButton.MOUSE_RIGHT_BUTTON);
            mousePosition = Raylib.Raylib.GetMousePosition();
            bool inKeepout = InUi(mousePosition);

            if (inKeepout) { Debug.WriteOverlay("In UI"); }

            if (mousePressed && MouseState == MouseStates.Idle)
            {
                downLocationScreen = mousePosition;
                DownLocation = ScreenToWorld(mousePosition);
            }
            else if (Raylib.Raylib.IsMouseButtonDown(MouseButton.MOUSE_LEFT_BUTTON) && !inKeepout)
            {
                Vector2 mousePos = ScreenToWorld(mousePosition);
                if (Raylib.Raylib.Vector2Distance(mousePos, DownLocation) > 64 && !InUi(downLocationScreen))
                {
                    MouseState = MouseStates.Dragging;
                    SelectionRectangle = RecFromVec(DownLocation, mousePos);
                }
            }
            else if (mouseReleased)
            {
                UpLocation = ScreenToWorld(mousePosition);
                if (MouseState == MouseStates.Dragging)
                {
                    MouseState = MouseStates.DragReleased;
                }
                else
                {
                    if (!inKeepout) { GetSelection(); }
                    MouseState = MouseStates.Idle;
                }
            }
            else if (MouseState == MouseStates.DragReleased)
            {
                SelectionRectangle = RecFromVec(DownLocation, UpLocation);
                GetSelection();
                MouseState = MouseStates.Idle;
            }

            if (rightReleased && MouseState == MouseStates.Idle)
            {
                if (SelectedUnits.Count != 0)
                {
                    IEnumerable<SpaceObject> inSelection = GameManager.Instance.Objects.Where(o =>
                           o is SpaceShip &&
                           o.Active == true &&
                           Raylib.Raylib.Vector2Distance(o.Location, ScreenToWorld(mousePosition)) < MathF.Max(o.TextureOffset.x, o.TextureOffset.y)
                        );

                    if (inSelection.Any())
                    {
                        foreach (SpaceShipUnit unit in SelectedUnits)
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
                            if (unit.Leader != null)
                            {
                                unit.Leader.Objective = target;
                                unit.Leader.Behavior = SpaceShip.Behaviors.Attacking;
                            }
                        }
                    }
                    else
                    {
                        foreach (SpaceShipUnit unit in SelectedUnits)
                        {
                            if (unit != null && unit.Leader != null)
                            {
                                unit.Leader.Objective = null;
                                //(obj as SpaceShip).Goal = Raylib.Raylib.GetMousePosition();
                                Vector2 mouse = ScreenToWorld(mousePosition);
                                unit.Leader.Goal = mouse;
                                unit.Leader.Behavior = SpaceShip.Behaviors.Going;
                            }
                        }
                    }
                }
            }

            foreach (IUiElement element in UiElements)
            {
                if (Raylib.Raylib.CheckCollisionPointRec(mousePosition, element.Bounds))
                {
                    if (mousePressed) { element.MousePressed(); }
                    else if (mouseReleased) { element.MouseReleased(); }
                    else if (mouseDown) { element.MouseDown(); }
                    else { element.MouseHover(); }
                }
                element.Tick();
            }
        }

        private static bool InUi(Vector2 point)
        {
            foreach (IUiElement element in UiElements)
            {
                if (Raylib.Raylib.CheckCollisionPointRec(point, element.Bounds))
                {
                    return true;
                }
            }
            return false;
        }

        public static void Draw()
        {
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

            foreach (IUiElement element in UiElements)
            {
                element.Draw();
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
            unitsTray.Children.Clear();
            foreach (SpaceObject s in Selected)
            {
                SpaceShip ship = s as SpaceShip;
                if (ship != null)
                {
                    if (!SelectedUnits.Contains(ship.Unit))
                    {
                        SelectedUnits.Add(ship.Unit);
                        UiTrayUnit trayUnit = new UiTrayUnit
                        {
                            Unit = ship.Unit,
                            Bounds = new Rectangle(0, 0, 75, 100)
                        };
                        unitsTray.Children.Add(trayUnit);
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

    public interface IUiElement
    {
        Rectangle Bounds { get; set; }

        void MouseHover();

        void MousePressed();

        void MouseDown();

        void MouseReleased();

        void Tick();

        void Draw();
    }

    public class UiMinimap : IUiElement
    {
        public Rectangle Bounds { get; set; } = new Rectangle(0, 0, 300, 300);
        private const float MinimapSize = 512;
        private const float margin = 4f;
        private static RenderTexture2D MinimapTexture = Raylib.Raylib.LoadRenderTexture((int)MinimapSize, (int)MinimapSize);
        private static Texture2D panelTexture = ResourceManager.GetTexture(@"ui\ninepatch_button").Texture;

        private static NPatchInfo npi = new NPatchInfo()
        {
            sourceRec = new Rectangle(0, 0, 64, 64),
            top = 16,
            bottom = 16,
            left = 16,
            right = 16
        };

        public void MousePressed()
        {
        }

        public void MouseDown()
        {
            float scaleFactor = -1f * GameManager.ViewScale * GameManager.MapSize / Bounds.width;
            Vector2 worldPos = (UiManager.mousePosition - new Vector2(Bounds.x + margin, Bounds.y + margin)) * scaleFactor;
            GameManager.ViewOffset = worldPos + new Vector2(UiManager.ScreenWidth / 2f, UiManager.ScreenHeight / 2f);
        }

        public void MouseHover()
        {
        }

        public void MouseReleased()
        {
        }

        public void Tick()
        {
            Raylib.Raylib.BeginTextureMode(MinimapTexture);
            Texture2D bkgTex = ResourceManager.GetTexture(@"_menu\haze+").Texture;
            Raylib.Raylib.DrawTexturePro(bkgTex, new Rectangle(0, 0, bkgTex.width, bkgTex.height), new Rectangle(0, 0, MinimapSize, MinimapSize), Vector2.Zero, 0.0f, Color.WHITE);
            float scaleFactor = MinimapSize / GameManager.MapSize;

            Debug.WriteOverlay("View Scale: " + GameManager.ViewScale);
            Raylib.Raylib.BeginBlendMode(BlendMode.BLEND_ADDITIVE);
            Vector2 TL = UiManager.ScreenToWorld(new Vector2(0, UiManager.ScreenHeight)) * scaleFactor;
            Vector2 BR = UiManager.ScreenToWorld(new Vector2(UiManager.ScreenWidth, 0)) * scaleFactor;
            Raylib.Raylib.DrawRectangle((int)TL.x, (int)(MinimapSize - TL.y), (int)(BR.x - TL.x), (int)(TL.y - BR.y), new Color(0, 255, 0, 16));
            Color c = new Color(64, 255, 64, 64);
            float t = 2.5f * (MinimapSize / 300f);
            Raylib.Raylib.DrawLineEx(new Vector2(TL.x, MinimapSize - TL.y), new Vector2(TL.x, MinimapSize - BR.y), t, c);
            Raylib.Raylib.DrawLineEx(new Vector2(BR.x, MinimapSize - TL.y), new Vector2(BR.x, MinimapSize - BR.y), t, c);
            Raylib.Raylib.DrawLineEx(new Vector2(TL.x, MinimapSize - BR.y), new Vector2(BR.x, MinimapSize - BR.y), t, c);
            Raylib.Raylib.DrawLineEx(new Vector2(TL.x, MinimapSize - TL.y), new Vector2(BR.x, MinimapSize - TL.y), t, c);

            foreach (SpaceObject obj in GameManager.Instance.Objects)
            {
                if (obj.Active)
                {
                    Vector2 pos = (obj.Location * scaleFactor);
                    pos = new Vector2(pos.x, MinimapSize - pos.y);
                    obj.DrawMinimap(pos);
                }
            }
            Raylib.Raylib.EndBlendMode();
            Raylib.Raylib.EndTextureMode();
        }

        public void Draw()
        {
            Raylib.Raylib.DrawTextureNPatch(panelTexture, npi, new Rectangle(Bounds.x, Bounds.y, Bounds.width, Bounds.height), new Vector2(0, 0), 0, Color.WHITE);

            Raylib.Raylib.DrawTexturePro(MinimapTexture.texture, new Rectangle(0, 0, MinimapSize, MinimapSize), new Rectangle(Bounds.x + margin, Bounds.y + margin, Bounds.width - (2 * margin), Bounds.height - (2 * margin)), Vector2.Zero, 0.0f, Color.WHITE);
        }
    }

    public class UiTray : IUiElement
    {
        public Rectangle Bounds { get; set; } = new Rectangle(0, 0, 300, 300);
        public List<IUiElement> Children = new List<IUiElement>();
        private const float margin = 4f;
        private static Texture2D panelTexture = ResourceManager.GetTexture(@"ui\ninepatch_button").Texture;

        private static NPatchInfo npi = new NPatchInfo()
        {
            sourceRec = new Rectangle(0, 0, 64, 64),
            top = 16,
            bottom = 16,
            left = 16,
            right = 16
        };

        public void MousePressed()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Raylib.Raylib.CheckCollisionPointRec(UiManager.mousePosition, Children[i].Bounds))
                {
                    Children[i].MousePressed();
                }
            }
        }

        public void MouseDown()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Raylib.Raylib.CheckCollisionPointRec(UiManager.mousePosition, Children[i].Bounds))
                {
                    Children[i].MouseDown();
                }
            }
        }

        public void MouseHover()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Raylib.Raylib.CheckCollisionPointRec(UiManager.mousePosition, Children[i].Bounds))
                {
                    Children[i].MouseHover();
                }
            }
        }

        public void MouseReleased()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Raylib.Raylib.CheckCollisionPointRec(UiManager.mousePosition, Children[i].Bounds))
                {
                    Children[i].MouseReleased();
                }
            }
        }

        public void Tick()
        {
            float offset = margin;
            foreach (IUiElement child in Children)
            {
                child.Bounds = new Rectangle(Bounds.x + offset, Bounds.y + margin, child.Bounds.width, Bounds.height - (margin * 2));
                offset += child.Bounds.width;
            }

            foreach (IUiElement child in Children)
            {
                child.Tick();
            }
        }

        public void Draw()
        {
            Raylib.Raylib.DrawTextureNPatch(panelTexture, npi, new Rectangle(Bounds.x, Bounds.y, Bounds.width, Bounds.height), new Vector2(0, 0), 0, Color.WHITE);
            foreach (IUiElement child in Children)
            {
                child.Draw();
            }
        }
    }

    public class UiTrayUnit : IUiElement
    {
        public Rectangle Bounds { get; set; } = new Rectangle(0, 0, 300, 300);
        public SpaceShipUnit Unit { get; set; }
        private const float margin = 4f;
        private static Texture2D panelTexture = ResourceManager.GetTexture(@"ui\ninepatch_button").Texture;

        private static NPatchInfo npi = new NPatchInfo()
        {
            sourceRec = new Rectangle(0, 0, 64, 64),
            top = 16,
            bottom = 16,
            left = 16,
            right = 16
        };

        public void MousePressed()
        {
            UiManager.ClearSelection();
            Unit.Selected = true;
            UiManager.UpdateSelectedUnits();
        }

        public void MouseDown()
        {
        }

        public void MouseHover()
        {
        }

        public void MouseReleased()
        {
        }

        public void Tick()
        {
        }

        public void Draw()
        {
            if (Unit == null || Unit.Units.Count == 0) { return; }
            Raylib.Raylib.DrawTextureNPatch(panelTexture, npi, new Rectangle(Bounds.x, Bounds.y, Bounds.width, Bounds.height), new Vector2(0, 0), 0, Color.WHITE);

            float unitHeight = 80;
            float unitWidth = 75;
            int barWidth = (int)(unitWidth - margin * 2);
            int barHeight = 8;
            float barOffset = Bounds.height - (barHeight * 4 + margin * 2);

            Texture2D tex = Unit.UiImage.Texture;
            float scalar = MathF.Min(MathF.Min(unitWidth / tex.width, unitHeight / tex.height), 1f);
            Vector2 offset = new Vector2((unitWidth - (tex.width * scalar)) / 2f, (unitHeight - (tex.height * scalar)) / 2f);
            Raylib.Raylib.DrawTextureEx(tex, new Vector2(Bounds.x, Bounds.y) + offset, 0, scalar, Color.WHITE);

            Raylib.Raylib.DrawRectangle((int)(Bounds.x + margin), (int)(Bounds.y + barOffset), (int)Math.Round(barWidth * (Unit.Units[0].Shield / Unit.Units[0].MaxShield)), barHeight, Color.GREEN);
            Raylib.Raylib.DrawRectangleLines((int)(Bounds.x + margin), (int)(Bounds.y + barOffset), barWidth, barHeight, Color.DARKGREEN);
            Raylib.Raylib.DrawRectangle((int)(Bounds.x + margin), (int)(Bounds.y + barOffset) + barHeight + 1, (int)Math.Round(barWidth * (Unit.Units[0].Hull / Unit.Units[0].MaxHull)), barHeight, Color.RED);
            Raylib.Raylib.DrawRectangleLines((int)(Bounds.x + margin), (int)(Bounds.y + barOffset) + barHeight + 1, barWidth, barHeight, Color.DARKPURPLE);
        }
    }
}
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceGame
{
    public static class UiManager
    {
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

        private static Texture2D selectTexture = ResourceManager.Get<TextureResource>(@"images\ui\selection_box").Texture;

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

        private static Vector2 downLocationScreen = new Vector2();

        private static UiUnitTray unitsTray;

        public static void Tick(double Delta)
        {
            Debug.WriteOverlay("Tick: " + Delta.ToString());
            if (!initialized)
            {
                UiMinimap map = new UiMinimap
                {
                    Bounds = new Rectangle(0, ScreenHeight - 300, 300, 300)
                };
                UiElements.Add(map);

                unitsTray = new UiUnitTray
                {
                    Bounds = new Rectangle(300, ScreenHeight - 150, ScreenWidth - 300, 150)
                };
                UiElements.Add(unitsTray);

                UiReinforcementTray reinforcementTray = new UiReinforcementTray
                {
                    Bounds = new Rectangle(0, 150, 300, 300)
                };
                UiElements.Add(reinforcementTray);

                initialized = true;
            }

            mousePressed = IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON);
            mouseReleased = IsMouseButtonReleased(MouseButton.MOUSE_LEFT_BUTTON);
            mouseUp = IsMouseButtonUp(MouseButton.MOUSE_LEFT_BUTTON);
            mouseDown = IsMouseButtonDown(MouseButton.MOUSE_LEFT_BUTTON);
            rightPressed = IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON);
            rightReleased = IsMouseButtonReleased(MouseButton.MOUSE_RIGHT_BUTTON);
            rightUp = IsMouseButtonUp(MouseButton.MOUSE_RIGHT_BUTTON);
            rightDown = IsMouseButtonDown(MouseButton.MOUSE_RIGHT_BUTTON);
            mousePosition = GetMousePosition();
            bool inKeepout = InUi(mousePosition);

            if (inKeepout) { Debug.WriteOverlay("In UI"); }

            if (mousePressed && MouseState == MouseStates.Idle)
            {
                downLocationScreen = mousePosition;
                DownLocation = ScreenToWorld(mousePosition);
            }
            else if (IsMouseButtonDown(MouseButton.MOUSE_LEFT_BUTTON) && !inKeepout)
            {
                Vector2 mousePos = ScreenToWorld(mousePosition);
                if (Vector2.Distance(mousePos, DownLocation) > 64 && !InUi(downLocationScreen))
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

            if (rightReleased && MouseState == MouseStates.Idle && SelectedUnits.Count != 0)
            {
                IEnumerable<SpaceObject> inSelection = GameManager.Instance.Objects.Where(o =>
                       o is SpaceShip &&
                       o.Active &&
                        Vector2.Distance(o.Location, ScreenToWorld(mousePosition)) < MathF.Max(o.TextureOffset.X, o.TextureOffset.Y)
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
                        if (unit?.Leader != null)
                        {
                            unit.Leader.Objective = null;
                            //(obj as SpaceShip).Goal =  GetMousePosition();
                            Vector2 mouse = ScreenToWorld(mousePosition);
                            unit.Leader.Goal = mouse;
                            unit.Leader.Behavior = SpaceShip.Behaviors.Going;
                        }
                    }
                }
            }

            foreach (IUiElement element in UiElements)
            {
                if (CheckCollisionPointRec(mousePosition, element.Bounds))
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
                if (CheckCollisionPointRec(point, element.Bounds))
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
                    (SelectionRectangle.x * GameManager.ViewScale) + GameManager.ViewOffset.X,
                    (SelectionRectangle.y * GameManager.ViewScale) + GameManager.ViewOffset.Y,
                    SelectionRectangle.width * GameManager.ViewScale,
                    SelectionRectangle.height * GameManager.ViewScale
                    );
                DrawTextureNPatch(selectTexture, npi, selrec, new Vector2(0, 0), 0, Color.WHITE);
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
                       o.Active &&
                       o.Location.X > SelectionRectangle.x &&
                       o.Location.Y > SelectionRectangle.y &&
                       o.Location.X < SelectionRectangle.x + SelectionRectangle.width &&
                       o.Location.Y < SelectionRectangle.y + SelectionRectangle.height
                    );
                if (!inSelection.Any()) { return; }
                if (!IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
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
                       o.Active &&
                        Vector2.Distance(o.Location, UpLocation) < MathF.Max(o.TextureOffset.X, o.TextureOffset.Y)
                    );
                if (!inSelection.Any())
                {
                    ClearSelection();
                    return;
                }
                if (!IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
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
                if ((s is SpaceShip ship) && ship != null && !SelectedUnits.Contains(ship.Unit))
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
            Debug.WriteLine("Synced unit selection");
        }

        public static Vector2 ScreenToWorld(Vector2 Point)
        {
            return (Point / GameManager.ViewScale) - (GameManager.ViewOffset / GameManager.ViewScale);
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
                x = TopLeft.X,
                y = TopLeft.Y,
                width = BottomRight.X - TopLeft.X,
                height = BottomRight.Y - TopLeft.Y
            };
        }

        public static Rectangle WorldToScreen(Rectangle Box)
        {
            Vector2 TopLeft = WorldToScreen(new Vector2(Box.x, Box.y));
            Vector2 BottomRight = WorldToScreen(new Vector2(Box.x + Box.width, Box.y + Box.height));
            return new Rectangle()
            {
                x = TopLeft.X,
                y = TopLeft.Y,
                width = BottomRight.X - TopLeft.X,
                height = BottomRight.Y - TopLeft.Y
            };
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
                MathF.Min(A.X, B.X),
                MathF.Min(A.Y, B.Y),
                MathF.Abs(A.X - B.X),
                MathF.Abs(A.Y - B.Y)
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
        private static RenderTexture2D MinimapTexture = LoadRenderTexture((int)MinimapSize, (int)MinimapSize);
        private static Texture2D panelTexture = ResourceManager.Get<TextureResource>(@"images\ui\ninepatch_button").Texture;
        private static Texture2D bkgTex = ResourceManager.Get<TextureResource>(@"images\_menu\haze+").Texture;

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
            BeginTextureMode(MinimapTexture);

            DrawTexturePro(bkgTex, new Rectangle(0, 0, bkgTex.width, bkgTex.height), new Rectangle(0, 0, MinimapSize, MinimapSize), Vector2.Zero, 0.0f, Color.WHITE);
            float scaleFactor = MinimapSize / GameManager.MapSize;

            Debug.WriteOverlay("View Scale: " + GameManager.ViewScale);
            BeginBlendMode(BlendMode.BLEND_ADDITIVE);
            Vector2 TL = UiManager.ScreenToWorld(new Vector2(0, UiManager.ScreenHeight)) * scaleFactor;
            Vector2 BR = UiManager.ScreenToWorld(new Vector2(UiManager.ScreenWidth, 0)) * scaleFactor;
            DrawRectangle((int)TL.X, (int)(MinimapSize - TL.Y), (int)(BR.X - TL.X), (int)(TL.Y - BR.Y), new Color(0, 255, 0, 16));
            Color c = new Color(64, 255, 64, 64);
            const float t = 2.5f * (MinimapSize / 300f);
            DrawLineEx(new Vector2(TL.X, MinimapSize - TL.Y), new Vector2(TL.X, MinimapSize - BR.Y), t, c);
            DrawLineEx(new Vector2(BR.X, MinimapSize - TL.Y), new Vector2(BR.X, MinimapSize - BR.Y), t, c);
            DrawLineEx(new Vector2(TL.X, MinimapSize - BR.Y), new Vector2(BR.X, MinimapSize - BR.Y), t, c);
            DrawLineEx(new Vector2(TL.X, MinimapSize - TL.Y), new Vector2(BR.X, MinimapSize - TL.Y), t, c);

            foreach (SpaceObject obj in GameManager.Instance.Objects)
            {
                if (obj.Active)
                {
                    Vector2 pos = (obj.Location * scaleFactor);
                    pos = new Vector2(pos.X, MinimapSize - pos.Y);
                    obj.DrawMinimap(pos);
                }
            }
            EndBlendMode();
            EndTextureMode();
        }

        public void Draw()
        {
            DrawTextureNPatch(panelTexture, npi, new Rectangle(Bounds.x, Bounds.y, Bounds.width, Bounds.height), new Vector2(0, 0), 0, Color.WHITE);

            DrawTexturePro(MinimapTexture.texture, new Rectangle(0, 0, MinimapSize, MinimapSize), new Rectangle(Bounds.x + margin, Bounds.y + margin, Bounds.width - (2 * margin), Bounds.height - (2 * margin)), Vector2.Zero, 0.0f, Color.WHITE);
        }
    }

    public class UiReinforcementTray : IUiElement
    {
        public Rectangle Bounds
        {
            get
            {
                if (IsOpen) { return bounds; }
                else { return new Rectangle(bounds.x - margin, bounds.y + margin, openTexture.width, openTexture.height); }
            }
            set { bounds = value; }
        }

        private Rectangle bounds = new Rectangle(0, 0, 300, 300);
        public List<IUiElement> Children = new List<IUiElement>();
        public bool IsOpen = true;
        private const float margin = 4f;
        private static Texture2D panelTexture = ResourceManager.Get<TextureResource>(@"images\ui\ninepatch_button").Texture;
        private static Texture2D closedTexture = ResourceManager.Get<TextureResource>(@"images\ui\collapsed").Texture;
        private static Texture2D openTexture = ResourceManager.Get<TextureResource>(@"images\ui\expanded").Texture;

        private static NPatchInfo npi = new NPatchInfo()
        {
            sourceRec = new Rectangle(0, 0, 64, 64),
            top = 16,
            bottom = 16,
            left = 16,
            right = 16
        };

        public void MouseDown()
        {
        }

        public void MouseHover()
        {
        }

        public void MousePressed()
        {
        }

        public void MouseReleased()
        {
            if (IsOpen && CheckCollisionPointRec(UiManager.mousePosition, new Rectangle(bounds.x + bounds.width - openTexture.width - margin, bounds.y + margin, openTexture.width, openTexture.height)))
            {
                IsOpen = false;
            }
            if (!IsOpen && CheckCollisionPointRec(UiManager.mousePosition, new Rectangle(bounds.x - margin, bounds.y + margin, openTexture.width, openTexture.height)))
            {
                IsOpen = true;
            }
        }

        public void Tick()
        {
        }

        public void Draw()
        {
            if (IsOpen)
            {
                DrawTextureNPatch(panelTexture, npi, new Rectangle(bounds.x - npi.left, bounds.y, bounds.width + npi.left, bounds.height), new Vector2(0, 0), 0, Color.WHITE);
                DrawTexture(openTexture, (int)(bounds.x + bounds.width - openTexture.width - margin), (int)(bounds.y + margin), Color.WHITE);
                foreach (IUiElement child in Children)
                {
                    child.Draw();
                }
            }
            else
            {
                DrawTextureNPatch(panelTexture, npi, new Rectangle(bounds.x - npi.left, bounds.y, npi.left + npi.right, bounds.height), new Vector2(0, 0), 0, Color.WHITE);
                DrawTexture(closedTexture, (int)(bounds.x - margin), (int)(bounds.y + margin), Color.WHITE);
            }
        }
    }

    public class UiUnitTray : IUiElement
    {
        public Rectangle Bounds { get; set; } = new Rectangle(0, 0, 300, 300);
        public List<IUiElement> Children = new List<IUiElement>();
        private const float margin = 4f;
        private static Texture2D panelTexture = ResourceManager.Get<TextureResource>(@"images\ui\ninepatch_button").Texture;

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
                if (CheckCollisionPointRec(UiManager.mousePosition, Children[i].Bounds))
                {
                    Children[i].MousePressed();
                }
            }
        }

        public void MouseDown()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (CheckCollisionPointRec(UiManager.mousePosition, Children[i].Bounds))
                {
                    Children[i].MouseDown();
                }
            }
        }

        public void MouseHover()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (CheckCollisionPointRec(UiManager.mousePosition, Children[i].Bounds))
                {
                    Children[i].MouseHover();
                }
            }
        }

        public void MouseReleased()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (CheckCollisionPointRec(UiManager.mousePosition, Children[i].Bounds))
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
            DrawTextureNPatch(panelTexture, npi, new Rectangle(Bounds.x, Bounds.y, Bounds.width, Bounds.height), new Vector2(0, 0), 0, Color.WHITE);
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
        private static Texture2D panelTexture = ResourceManager.Get<TextureResource>(@"images\ui\ninepatch_button").Texture;

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
            DrawTextureNPatch(panelTexture, npi, new Rectangle(Bounds.x, Bounds.y, Bounds.width, Bounds.height), new Vector2(0, 0), 0, Color.WHITE);

            const float unitHeight = 80;
            const float unitWidth = 75;
            const int barWidth = (int)(unitWidth - (margin * 2));
            const int barHeight = 8;
            float barOffset = Bounds.height - ((barHeight * 4) + (margin * 2));

            Texture2D tex = Unit.UiImage.Texture;
            float scalar = MathF.Min(MathF.Min(unitWidth / tex.width, unitHeight / tex.height), 1f);
            Vector2 offset = new Vector2((unitWidth - (tex.width * scalar)) / 2f, (unitHeight - (tex.height * scalar)) / 2f);
            DrawTextureEx(tex, new Vector2(Bounds.x, Bounds.y) + offset, 0, scalar, Color.WHITE);

            DrawRectangle((int)(Bounds.x + margin), (int)(Bounds.y + barOffset), (int)Math.Round(barWidth * (Unit.Units[0].Shield / Unit.Units[0].MaxShield)), barHeight, Color.GREEN);
            DrawRectangleLines((int)(Bounds.x + margin), (int)(Bounds.y + barOffset), barWidth, barHeight, Color.DARKGREEN);
            DrawRectangle((int)(Bounds.x + margin), (int)(Bounds.y + barOffset) + barHeight + 1, (int)Math.Round(barWidth * (Unit.Units[0].Hull / Unit.Units[0].MaxHull)), barHeight, Color.RED);
            DrawRectangleLines((int)(Bounds.x + margin), (int)(Bounds.y + barOffset) + barHeight + 1, barWidth, barHeight, Color.DARKPURPLE);
        }
    }
}
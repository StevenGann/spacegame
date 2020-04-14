using Raylib;
using System;
using System.Collections.Generic;
using System.Text;
using static Raylib.Raylib;

namespace SpaceGame
{
    public class Debug
    {
        /// <summary>
        /// Enables are disables most functionality of the Debug class.
        /// </summary>
        public static bool Enabled { get; set; }

        /// <summary>
        /// Opens or closes the in-game console, if Enabled.
        /// </summary>
        public static bool ConsoleIsOpen { get; set; }

        private static Debug instance;
        private Queue<DrawTextJob> textJobs = new Queue<DrawTextJob>();
        private Queue<DrawShapeJob> shapeJobs = new Queue<DrawShapeJob>();
        private Queue<string> overlayLines = new Queue<string>();
        private Stack<string> consoleLines = new Stack<string>();
        private Stack<string> consoleLinesBuffer = new Stack<string>();
        private int consoleMaxLines = 64;
        private int consoleFontSize = 16;
        private Font font;
        private Color backgroundColor = new Color(0, 0, 0, 128);
        private Color backgroundColorAlt = new Color(0, 0, 64, 128);
        private Color foregroundColor = new Color(255, 255, 255, 255);
        private Color outlineColor = new Color(255, 255, 255, 128);
        private int consoleSize;
        private int consoleMaxSize = 256;
        private bool initialized;

        /// <summary>
        /// Default constructor.
        /// Sets Enable to true if compiled with DEBUG flag.
        /// </summary>
        public Debug()
        {
#if DEBUG
            Debug.Enabled = true;
#endif
            consoleLines.Push("Console initialized.");
        }

        /// <summary>
        /// Adds a line of text to the in-game console pane and echos to System.Console.
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        public static void WriteLine(string text)
        {
            if (text == null) { return; }

            if (text.StartsWith("%", StringComparison.Ordinal))
            {
                string s = "";
                string[] tokens = text.Split('%');
                if (tokens.Length > 2)
                {
                    for (int i = 2; i < tokens.Length; i++)
                    {
                        s += ((i > 2) ? "%" : "") + tokens[i];
                    }
                }
                else
                {
                    s = text;
                }
                Console.WriteLine(s);
            }
            else
            {
                Console.WriteLine(text);
            }
            if (instance == null) { instance = new Debug(); }
            lock (instance.consoleLines)
            {
                instance.consoleLines.Push(text);
            }
        }

        /// <summary>
        /// Queues a line of text to be rendered in the debug overlay during the next frame.
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        public static void WriteOverlay(string text)
        {
            if (instance == null) { instance = new Debug(); }
            if (!Enabled) { return; }
            lock (instance.overlayLines)
            {
                instance.overlayLines.Enqueue(text);
            }
        }

        /// <summary>
        /// Queues a line of text to be drawn on the screen at the specified location during the next frame.
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        /// <param name="x">X screen coordinate to draw at</param>
        /// <param name="y">Y screen coordinate to draw at</param>
        /// <param name="size">Size of text</param>
        public static void DrawText(string text, int x, int y, int size)
        {
            if (instance == null) { instance = new Debug(); }
            if (!Enabled) { return; }
            DrawTextJob j = new DrawTextJob
            {
                Text = text,
                Size = size,
                X = x,
                Y = y
            };
            lock (instance.textJobs)
            {
                instance.textJobs.Enqueue(j);
            }
        }

        public static void DrawLine(int x1, int y1, int x2, int y2, Color color)
        {
            if (instance == null) { instance = new Debug(); }
            if (!Enabled) { return; }
            DrawShapeJob j = new DrawShapeJob
            {
                ShapeType = Shapes.Line,
                A = (int)Math.Clamp(x1, 1, 1920 - 1),
                B = (int)Math.Clamp(y1, 1, 1080 - 1),
                C = (int)Math.Clamp(x2, 1, 1920 - 1),
                D = (int)Math.Clamp(y2, 1, 1080 - 1),
            };
            lock (instance.shapeJobs)
            {
                instance.shapeJobs.Enqueue(j);
            }
        }

        public static void DrawRectangle(int x, int y, int w, int h, Color color)
        {
            if (instance == null) { instance = new Debug(); }
            if (!Enabled) { return; }
            DrawShapeJob j = new DrawShapeJob
            {
                ShapeType = Shapes.Rectangle,
                A = x,
                B = y,
                C = w,
                D = h,
                Color = color,
            };
            lock (instance.shapeJobs)
            {
                instance.shapeJobs.Enqueue(j);
            }
        }

        /// <summary>
        /// Draws the in-game console, debug overlay, and any text queued by the DrawText() method.
        /// Also manages keyboard handling for enabling or disabling the Debug interface and interacting with the in-game console.
        /// </summary>
        public static void Draw()
        {
            if (instance == null) { instance = new Debug(); }
            Enabled = true;
            if (IsKeyPressed(KeyboardKey.KEY_F1))
            {
                if (Enabled)
                {
                    Enabled = false;
                    ConsoleIsOpen = false;
                }
                else
                {
                    Enabled = true;
                }
            }
            if (IsKeyPressed(KeyboardKey.KEY_GRAVE) && Enabled)
            {
                ConsoleIsOpen = !ConsoleIsOpen;
            }
            if (instance == null) { return; }
            if (!Enabled)
            {
                if (instance.consoleLines.Count > instance.consoleMaxLines * 2)
                {
                    for (int i = 0; i < instance.consoleMaxLines; i++)
                    {
                        instance.consoleLinesBuffer.Push(instance.consoleLines.Pop());
                    }
                    instance.consoleLines.Clear();
                    while (instance.consoleLinesBuffer.Count > 0)
                    {
                        instance.consoleLines.Push(instance.consoleLinesBuffer.Pop());
                    }
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    Debug.WriteLine("Trimmed debug console");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                }
            }
            if (!instance.initialized)
            {
                instance.font = ResourceManager.GetFont("Perfect_DOS_VGA_437_Win").Font;
                instance.initialized = true;
            }

            // ------------------------------------------------------------------------------------
            Debug.WriteOverlay("FPS: " + Convert.ToString(Raylib.Raylib.GetFPS(), null));

            Vector2 p = new Vector2();

            lock (instance.shapeJobs)
            {
                while (instance.shapeJobs.Count > 0)
                {
                    DrawShapeJob j = instance.shapeJobs.Dequeue();
                    if (j.ShapeType == Shapes.Line)
                    {
                        Raylib.Raylib.DrawLine(j.A, j.B, j.C, j.D, j.Color);
                    }
                    else if (j.ShapeType == Shapes.Rectangle)
                    {
                        Raylib.Raylib.DrawRectangle(j.A, j.B, j.C, j.D, j.Color);
                    }
                }
            }

            if (instance.consoleSize > 0)
            {
                DrawRectangle(0, 0, GetScreenWidth(), instance.consoleSize, instance.backgroundColor);
                DrawLineEx(new Vector2(0, instance.consoleSize + 1), new Vector2(GetScreenWidth(), instance.consoleSize + 1), 2, Color.WHITE);

                lock (instance.consoleLines)
                {
                    p.x = 4;
                    p.y = instance.consoleSize - instance.consoleFontSize;
                    int count = instance.consoleLines.Count;
                    for (int i = 0; i < Math.Min(instance.consoleMaxLines, count); i++)
                    {
                        string line = instance.consoleLines.Pop();
                        string originalLine = line;
                        string tag = "";
                        if (line.StartsWith("%", StringComparison.Ordinal))
                        {
                            string[] tokens = line.Split('%');
                            if (tokens.Length > 2)
                            {
                                line = "";
                                for (int j = 2; j < tokens.Length; j++)
                                {
                                    line += ((j > 2) ? "%" : "") + tokens[j];
                                }

                                tag = tokens[1].ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                            }
                        }

                        if (p.y > -instance.consoleFontSize)
                        {
                            if (!string.IsNullOrEmpty(tag))
                            {
                                Color c = instance.backgroundColorAlt;
                                if (tag == "SUCCESS") { c = new Color(0, 255, 0, 64); }
                                else if (tag == "WARNING") { c = new Color(255, 255, 0, 64); }
                                else if (tag == "ERROR") { c = new Color(255, 0, 0, 64); }
                                DrawRectangle((int)p.x - 2, (int)p.y + 1, GetScreenWidth() - 2, instance.consoleFontSize - 1, c);
                            }
                            DrawTextEx(instance.font, line, p, instance.consoleFontSize, 1.0f, instance.foregroundColor);
                        }

                        p.y -= instance.consoleFontSize;
                        instance.consoleLinesBuffer.Push(originalLine);
                    }
                    instance.consoleLines.Clear();
                    while (instance.consoleLinesBuffer.Count > 0)
                    {
                        instance.consoleLines.Push(instance.consoleLinesBuffer.Pop());
                    }
                }
            }

            if (ConsoleIsOpen && instance.consoleSize < instance.consoleMaxSize) { instance.consoleSize += 8; }
            if (!ConsoleIsOpen && instance.consoleSize > 0) { instance.consoleSize -= 8; }

            lock (instance.textJobs)
            {
                while (instance.textJobs.Count > 0)
                {
                    DrawTextJob j = instance.textJobs.Dequeue();
                    if (j.Y > instance.consoleSize)
                    {
                        p.x = j.X - 1;
                        p.y = j.Y - 1;
                        DrawTextEx(instance.font, j.Text, p, j.Size, 1.0f, instance.outlineColor);
                        p.x = j.X + 1;
                        p.y = j.Y + 1;
                        DrawTextEx(instance.font, j.Text, p, j.Size, 1.0f, instance.outlineColor);
                        p.x = j.X - 1;
                        p.y = j.Y + 1;
                        DrawTextEx(instance.font, j.Text, p, j.Size, 1.0f, instance.outlineColor);
                        p.x = j.X + 1;
                        p.y = j.Y - 1;
                        DrawTextEx(instance.font, j.Text, p, j.Size, 1.0f, instance.outlineColor);

                        p.x = j.X;
                        p.y = j.Y;
                        DrawTextEx(instance.font, j.Text, p, j.Size, 1.0f, Color.BLACK);
                    }
                }
            }

            lock (instance.overlayLines)
            {
                p.y = instance.consoleSize + (instance.consoleFontSize * 0.25f);
                p.x = 4;
                while (instance.overlayLines.Count > 0)
                {
                    string line = instance.overlayLines.Dequeue();
                    DrawRectangle((int)p.x - 2, (int)p.y - 1, (int)((line.Length * instance.consoleFontSize * 0.66f) - 2), instance.consoleFontSize - 1, instance.outlineColor);
                    DrawTextEx(instance.font, line, p, instance.consoleFontSize, 1.0f, Color.BLACK);
                    p.y += instance.consoleFontSize;
                }
            }
        }

        private struct DrawTextJob
        {
            public string Text;
            public int Size;
            public int X;
            public int Y;
        }

        private struct DrawShapeJob
        {
            public Shapes ShapeType;
            public int A;
            public int B;
            public int C;
            public int D;
            public Color Color;
        }

        private enum Shapes
        {
            Line,
            Circle,
            Rectangle
        }
    }
}
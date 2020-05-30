using Raylib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public static Dictionary<string, Tuple<Func<List<string>, int>, string>> Commands { get; } = new Dictionary<string, Tuple<Func<List<string>, int>, string>>();
        public static Dictionary<string, double> Flags { get; } = new Dictionary<string, double>();

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
        private static string terminalBuffer = "";
        private static int terminalCursor = 0;
        private static int blinkCounter = 0;
        private static bool showCursor = true;

        /// <summary>
        /// Default constructor.
        /// Sets Enable to true if compiled with DEBUG flag.
        /// </summary>
        public Debug()
        {
#if DEBUG
            Debug.Enabled = true;
#endif
            Commands.Clear();

            Commands.Add("help", new Tuple<Func<List<string>, int>, string>(CommandHelp, "Lists all commands with their descriptions"));
            Commands.Add("flags", new Tuple<Func<List<string>, int>, string>(CommandFlags, "Lists all debug flags and their values>"));
            Commands.Add("set", new Tuple<Func<List<string>, int>, string>(CommandSet, "Use: \"set <name> <value>\" Sets a debug flag <name> to <value>"));
            Commands.Add("get", new Tuple<Func<List<string>, int>, string>(CommandGet, "Use: \"set <name>\" Prints a debug flag <name>"));

            consoleLines.Push("Console initialized.");
        }

        public static void RegisterCommand(string Name, Func<List<string>, int> Function, string Help)
        {
            if (instance == null) { instance = new Debug(); }
            Commands.Add(Name.ToLower(), new Tuple<Func<List<string>, int>, string>(Function, Help));
        }

        public static void SetFlag(string Name, double Value)
        {
            if (Flags.ContainsKey(Name.ToUpperInvariant()))
            {
                Flags[Name.ToUpperInvariant()] = Value;
            }
            else
            {
                Flags.Add(Name.ToUpperInvariant(), Value);
            }
        }

        public static double GetFlag(string Name)
        {
            if (Flags.ContainsKey(Name.ToUpperInvariant()))
            {
                return Flags[Name.ToUpperInvariant()];
            }
            else
            {
                Flags.Add(Name.ToUpperInvariant(), 0);
                return 0;
            }
        }

        /// <summary>
        /// Adds a line of text to the in-game console pane and echos to System.Console.
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        public static void WriteLine(string text)
        {
            if (text == null) { return; }

            if (text.Contains("\n"))
            {
                string[] tokens = text.Split("\n");
                foreach (string line in tokens)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        WriteLine(line.Trim());
                    }
                }
                return;
            }

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
                    Debug.WriteLine("Trimmed debug console");
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
                UpdateTerminal();
                //Debug.WriteOverlay(terminalBuffer.Insert(terminalCursor, showCursor ? "_" : " "));

                DrawRectangle(0, 0, GetScreenWidth(), instance.consoleSize, instance.backgroundColor);
                DrawLineEx(new Vector2(0, instance.consoleSize + 1), new Vector2(GetScreenWidth(), instance.consoleSize + 1), 2, Color.WHITE);
                DrawLineEx(new Vector2(0, instance.consoleSize - instance.consoleFontSize - 1), new Vector2(GetScreenWidth(), instance.consoleSize - instance.consoleFontSize - 1), 1, Color.GRAY);
                DrawTextEx(instance.font, terminalBuffer.Insert(terminalCursor, showCursor ? "_" : " "), new Vector2(4, instance.consoleSize - instance.consoleFontSize - 1), instance.consoleFontSize, 1.0f, instance.foregroundColor);

                lock (instance.consoleLines)
                {
                    p.x = 4;
                    p.y = instance.consoleSize - (instance.consoleFontSize * 2) - 2;
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

        private static void UpdateTerminal()
        {
            if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_A)) { insertLetter("A"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_B)) { insertLetter("B"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_C)) { insertLetter("C"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_D)) { insertLetter("D"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_E)) { insertLetter("E"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_F)) { insertLetter("F"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_G)) { insertLetter("G"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_H)) { insertLetter("H"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_I)) { insertLetter("I"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_J)) { insertLetter("J"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_K)) { insertLetter("K"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_L)) { insertLetter("L"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_M)) { insertLetter("M"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_N)) { insertLetter("N"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_O)) { insertLetter("O"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_P)) { insertLetter("P"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_Q)) { insertLetter("Q"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_R)) { insertLetter("R"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_S)) { insertLetter("S"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_T)) { insertLetter("T"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_U)) { insertLetter("U"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_V)) { insertLetter("V"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_W)) { insertLetter("W"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_X)) { insertLetter("X"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_Y)) { insertLetter("Y"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_Z)) { insertLetter("Z"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE)) { insertLetter(" "); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_ONE)) { insertLetter("1", "!"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_TWO)) { insertLetter("2", "@"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_THREE)) { insertLetter("3", "#"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_FOUR)) { insertLetter("4", "$"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_FIVE)) { insertLetter("5", "%"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_SIX)) { insertLetter("6", "^"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_SEVEN)) { insertLetter("7", "&"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_EIGHT)) { insertLetter("8", "*"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_NINE)) { insertLetter("9", "("); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_ZERO)) { insertLetter("0", ")"); }
            //else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_GRAVE)) { insertLetter("`", "~"); } //Backtick closes the terminal, so don't use it
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_MINUS)) { insertLetter("-", "_"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_EQUAL)) { insertLetter("=", "+"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT_BRACKET)) { insertLetter("[", "{"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT_BRACKET)) { insertLetter("]", "}"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSLASH)) { insertLetter("\\", "|"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_SEMICOLON)) { insertLetter(";", ":"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_APOSTROPHE)) { insertLetter("'", "\""); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_COMMA)) { insertLetter(",", "<"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_PERIOD)) { insertLetter(".", ">"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_SLASH)) { insertLetter("/", "?"); }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE) || (Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_BACKSPACE) && ((blinkCounter % 5) == 0)))
            {
                blinkCounter = 0;
                if (terminalCursor > 0)
                {
                    terminalCursor -= 1;
                    terminalBuffer = terminalBuffer.Remove(terminalCursor, 1);
                }
            }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_DELETE))
            {
                terminalCursor = 0;
                terminalBuffer = "";
            }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT) || (Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_LEFT) && ((blinkCounter % 10) == 0)))
            {
                blinkCounter = 0;
                if (terminalCursor > 0)
                {
                    terminalCursor -= 1;
                }
            }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT) || (Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT) && ((blinkCounter % 10) == 0)))
            {
                blinkCounter = 0;
                if (terminalCursor < terminalBuffer.Length)
                {
                    terminalCursor += 1;
                }
            }
            else if (Raylib.Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
            {
                if (!string.IsNullOrWhiteSpace(terminalBuffer))
                {
                    WriteLine(terminalBuffer);
                    ExecuteString(terminalBuffer);
                }
                terminalCursor = 0;
                terminalBuffer = "";
            }

            blinkCounter++;
            if ((blinkCounter % 30) == 0) { showCursor = !showCursor; }
        }

        private static void insertLetter(string Letter)
        {
            if (Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT))
            {
                terminalBuffer = terminalBuffer.Insert(terminalCursor, Letter.ToUpperInvariant());
            }
            else
            {
                terminalBuffer = terminalBuffer.Insert(terminalCursor, Letter.ToLower());
            }
            terminalCursor += 1;
        }

        private static void insertLetter(string Letter, string Shifted)
        {
            if (Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT))
            {
                if (string.IsNullOrEmpty(Shifted)) { return; }
                terminalBuffer = terminalBuffer.Insert(terminalCursor, Shifted);
            }
            else
            {
                if (string.IsNullOrEmpty(Letter)) { return; }
                terminalBuffer = terminalBuffer.Insert(terminalCursor, Letter);
            }
            terminalCursor += 1;
        }

        private static void ExecuteString(string Input)
        {
            try
            {
                string[] tokens = Regex.Split(Input, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                if (tokens.Length > 0)
                {
                    string command = tokens[0].ToLowerInvariant();
                    if (Commands.ContainsKey(command))
                    {
                        List<string> parameters = null;

                        if (tokens.Length > 1)
                        {
                            parameters = new List<string>();
                            for (int i = 1; i < tokens.Length; i++)
                            {
                                parameters.Add(tokens[i]);
                            }
                        }

                        int result = Commands[command].Item1(parameters);
                        if (result != 0)
                        {
                            throw new Exception("Command \"" + command + "\" returned a non-zero status " + result.ToString());
                        }
                    }
                    else
                    {
                        throw new Exception("Command not found: " + command);
                    }
                }
            }
            catch (Exception e)
            {
                WriteLine("%ERROR%[CONSOLE ERROR]");
                WriteLine("%ERROR%" + e.Message);
            }
        }

        private int CommandHelp(List<string> arg)
        {
            WriteLine("Console Commands:");

            foreach (string command in Commands.Keys)
            {
                //Tuple<Func<List<string>, int>, string>
                string description = Commands[command].Item2;
                WriteLine(command.ToLower() + " :\t\t" + description);
            }

            return 0;
        }

        private int CommandGet(List<string> arg)
        {
            if (arg == null || arg.Count != 1) { Debug.WriteLine("%WARNING%Usage: \"get <name>\""); return 1; }
            double value = GetFlag(arg[0]);
            WriteLine(arg[0].ToUpperInvariant() + " = " + value.ToString());
            return 0;
        }

        private int CommandSet(List<string> arg)
        {
            if (arg == null || arg.Count != 2) { Debug.WriteLine("%WARNING%Usage: \"set <name> <value>\""); return 1; }

            try
            {
                double value = double.Parse(arg[1]);
                SetFlag(arg[0], value);
                WriteLine(arg[0].ToUpperInvariant() + " = " + GetFlag(arg[0]).ToString());
            }
            catch { Debug.WriteLine("%ERROR%Failed to parse \"" + arg[1] + "\""); return 1; }
            return 0;
        }

        private int CommandFlags(List<string> arg)
        {
            if (Flags.Keys.Count == 0)
            {
                WriteLine("No flags have been created");
                return 0;
            }

            List<string> names = Flags.Keys.ToList<string>();
            foreach (string name in names)
            {
                WriteLine(name + "\t = " + Flags[name].ToString());
            }
            return 0;
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
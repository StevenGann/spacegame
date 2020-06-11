using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SpaceGame
{
    public class ResourceManager
    {
        public static ResourceManager Instance { get; set; } = null;
        public Dictionary<string, TextureResource> Textures = new Dictionary<string, TextureResource>();
        public Dictionary<string, XmlResource> Xml = new Dictionary<string, XmlResource>();
        public Dictionary<string, FontResource> Fonts = new Dictionary<string, FontResource>();
        public Dictionary<string, SoundResource> Sounds = new Dictionary<string, SoundResource>();
        public Dictionary<string, ScriptResource> Scripts = new Dictionary<string, ScriptResource>();
        private static string imagesFolder = @"images\";
        private static string xmlFolder = @"xml\";
        private static string fontsFolder = @"fonts\";
        private static string soundsFolder = @"sounds\";
        private static string scriptsFolder = @"scripts\";
        private static string tempPath = "";

        public static TextureResource GetTexture(string Id)
        {
            if (Instance.Textures.ContainsKey(Id.ToUpperInvariant()))
            {
                Instance.Textures[Id.ToUpperInvariant()].Users++;
                return Instance.Textures[Id.ToUpperInvariant()];
            }
            else
            {
                string message = "Cannot find TextureResource " + Id.ToUpperInvariant();
#if DEBUG
                throw new KeyNotFoundException(message);
#else
                Debug.WriteLine(message);
                return null;
#endif
            }
        }

        public static XmlResource GetXml(string Id)
        {
            if (Instance.Xml.ContainsKey(Id.ToUpperInvariant()))
            {
                Instance.Xml[Id.ToUpperInvariant()].Users++;
                return Instance.Xml[Id.ToUpperInvariant()];
            }
            else
            {
                string message = "Cannot find XmlResource " + Id.ToUpperInvariant();
#if DEBUG
                throw new KeyNotFoundException(message);
#else
                Debug.WriteLine(message);
                return null;
#endif
            }
        }

        public static FontResource GetFont(string Id)
        {
            if (Instance.Fonts.ContainsKey(Id.ToUpperInvariant()))
            {
                Instance.Fonts[Id.ToUpperInvariant()].Users++;
                return Instance.Fonts[Id.ToUpperInvariant()];
            }
            else
            {
                string message = "Cannot find FontResource " + Id.ToUpperInvariant();
#if DEBUG
                throw new KeyNotFoundException(message);
#else
                Debug.WriteLine(message);
                return null;
#endif
            }
        }

        public static SoundResource GetSound(string Id)
        {
            if (Instance.Sounds.ContainsKey(Id.ToUpperInvariant()))
            {
                Instance.Sounds[Id.ToUpperInvariant()].Users++;
                return Instance.Sounds[Id.ToUpperInvariant()];
            }
            else
            {
                string message = "Cannot find SoundResource " + Id.ToUpperInvariant();
#if DEBUG
                throw new KeyNotFoundException(message);
#else
                Debug.WriteLine(message);
                return null;
#endif
            }
        }

        public static ScriptResource GetScript(string Id)
        {
            if (Instance.Scripts.ContainsKey(Id.ToUpperInvariant()))
            {
                Instance.Scripts[Id.ToUpperInvariant()].Users++;
                return Instance.Scripts[Id.ToUpperInvariant()];
            }
            else
            {
                string message = "Cannot find ScriptResource " + Id.ToUpperInvariant();
#if DEBUG
                throw new KeyNotFoundException(message);
#else
                Debug.WriteLine(message);
                return null;
#endif
            }
        }

        public static void Load(string BasePath)
        {
            Instantiate();
            Debug.WriteLine("Loading XML...");
            tempPath = BasePath + xmlFolder;
            ProcessDirectory(tempPath, ".xml", typeof(XmlResource));

            Debug.WriteLine("Loading Fonts...");
            tempPath = BasePath + fontsFolder;
            ProcessDirectory(tempPath, ".ttf", typeof(FontResource));

            Debug.WriteLine("Loading Sounds...");
            tempPath = BasePath + soundsFolder;
            ProcessDirectory(tempPath, ".wav", typeof(SoundResource));
            ProcessDirectory(tempPath, ".mp3", typeof(SoundResource));
            ProcessDirectory(tempPath, ".ogg", typeof(SoundResource));

            Debug.WriteLine("Loading Scripts...");
            tempPath = BasePath + scriptsFolder;
            ProcessDirectory(tempPath, ".cs", typeof(ScriptResource));

            Debug.WriteLine("Loading Textures...");
            tempPath = BasePath + imagesFolder;
            ProcessDirectory(tempPath, ".png", typeof(TextureResource));
            ProcessDirectory(tempPath, ".jpg", typeof(TextureResource));
        }

        public static void Instantiate()
        {
            if (Instance == null)
            {
                Instance = new ResourceManager();
            }
        }

        private static void ProcessDirectory(string TargetDirectory, string Extension, Type AssetType)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = System.IO.Directory.GetFiles(TargetDirectory);
            foreach (string fileName in fileEntries)
            {
                if (string.Equals(System.IO.Path.GetExtension(fileName), Extension, StringComparison.InvariantCultureIgnoreCase))
                {
                    string fn = fileName.Substring(tempPath.Length, fileName.Length - tempPath.Length).ToUpperInvariant();
                    fn = fn.Substring(0, fn.Length - Extension.Length);
                    Debug.WriteLine("Name: " + fn);

                    if (AssetType == typeof(TextureResource))
                    {
                        TextureResource r = new TextureResource()
                        {
                            Name = fn,
                            Path = fileName,
                        };
                        Instance.Textures.Add(r.Name, r);
                    }
                    else if (AssetType == typeof(XmlResource))
                    {
                        XmlResource r = new XmlResource()
                        {
                            Name = fn,
                            Path = fileName,
                        };
                        Instance.Xml.Add(r.Name, r);
                    }
                    else if (AssetType == typeof(FontResource))
                    {
                        FontResource r = new FontResource()
                        {
                            Name = fn,
                            Path = fileName,
                        };
                        Instance.Fonts.Add(r.Name, r);
                    }
                    else if (AssetType == typeof(SoundResource))
                    {
                        SoundResource r = new SoundResource()
                        {
                            Name = fn,
                            Path = fileName,
                        };
                        Instance.Sounds.Add(r.Name, r);
                    }
                    else if (AssetType == typeof(ScriptResource))
                    {
                        ScriptResource r = new ScriptResource()
                        {
                            Name = fn,
                            Path = fileName,
                        };
                        Instance.Scripts.Add(r.Name, r);
                    }
                }
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(TargetDirectory);
            subdirectoryEntries = subdirectoryEntries.Reverse().ToArray<string>();
            foreach (string subdirectory in subdirectoryEntries)
            {
                ProcessDirectory(subdirectory, Extension, AssetType);
            }
        }

        public static void Cull()
        {
            /*foreach (KeyValuePair<string, TextureResource> kvp in Instance.Textures)
            {
                if (kvp.Value.Loaded && kvp.Value.Users <= 0)
                {
                    Debug.WriteLine("Unloading " + kvp.Value.Name);
                    kvp.Value.Loaded = false;
                }
            }*/
        }
    }

    public class TextureResource : IResource
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int Users { get; set; }
        private Raylib.Texture2D texture;
        private bool loaded = false;

        public bool Loaded
        {
            get { return loaded; }
            set
            {
                if (value)
                {
                    texture = Raylib.Raylib.LoadTexture(Path);
                    Raylib.Raylib.SetTextureFilter(texture, Raylib.TextureFilterMode.FILTER_POINT);
                    Raylib.Raylib.GenTextureMipmaps(ref texture);
                }
                else
                {
                    Raylib.Raylib.UnloadTexture(texture);
                }
                loaded = value;
            }
        }

        public Raylib.Texture2D Texture
        {
            get
            {
                if (!Loaded) { Loaded = true; }
                return texture;
            }
            set
            {
                loaded = true;
                texture = value;
            }
        }
    }

    public class SoundResource : IResource
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int Users { get; set; }
        private bool loaded = false;
        private Raylib.Sound sound;

        public bool Loaded
        {
            get { return loaded; }
            set
            {
                if (value)
                {
                    sound = Raylib.Raylib.LoadSound(Path);
                }
                else
                {
                }
                loaded = value;
            }
        }

        public Raylib.Sound Sound
        {
            get
            {
                if (!Loaded) { Loaded = true; }
                return sound;
            }
            set
            {
                loaded = true;
                sound = value;
            }
        }
    }

    public class XmlResource : IResource
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int Users { get; set; }
        private bool loaded = false;
        private XmlDocument xml = null;

        public bool Loaded
        {
            get { return loaded; }
            set
            {
                if (value)
                {
                    xml = new XmlDocument();
                    xml.Load(Path);
                    loaded = true;
                }
                else
                {
                    xml = null;
                }
                loaded = value;
            }
        }

        public XmlDocument Xml
        {
            get
            {
                if (!Loaded)
                {
                    Loaded = true;
                }
                return xml;
            }
            set
            {
                xml = value;
                loaded = true;
            }
        }
    }

    public class FontResource : IResource
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int Users { get; set; }
        private bool loaded = false;
        private Raylib.Font font = default;

        public bool Loaded
        {
            get { return loaded; }
            set
            {
                if (value)
                {
                    font = Raylib.Raylib.LoadFont(Path);
                    loaded = true;
                }
                else
                {
                    font = default;
                }
                loaded = value;
            }
        }

        public Raylib.Font Font
        {
            get
            {
                if (!Loaded)
                {
                    Loaded = true;
                }
                return font;
            }
            set
            {
                font = value;
                loaded = true;
            }
        }
    }

    public class ScriptResource : IResource
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Error { get => error; }

        public string Text
        {
            get
            {
                //if (!loaded) { loaded = true; }
                scriptText = System.IO.File.ReadAllText(Path);
                return scriptText;
            }
        }

        public int Users { get; set; }
        private bool loaded = false;
        private dynamic script = null;
        private string scriptText = "";
        private bool error = false;

        public bool Loaded
        {
            get { return loaded; }
            set
            {
                if (value)
                {
                    if (!loaded && !error)
                    {
                        if (string.IsNullOrWhiteSpace(scriptText))
                        {
                            scriptText = System.IO.File.ReadAllText(Path);
                        }
                        if (scriptText != string.Empty)
                        {
                            var watch = System.Diagnostics.Stopwatch.StartNew();

                            if (Debug.GetFlag("DangerousScripts") != 1)
                            {
                                List<string> blacklist = GameManager.ScriptBlacklist;
                                foreach (string s in blacklist)
                                {
                                    if (scriptText.Contains(s))
                                    {
                                        watch.Stop();
                                        Debug.WriteLine("%WARNING%Did not compile script because it contained \"" + s + "\" and DangerousScripts cvar is disabled.");
                                        Debug.WriteLine("%WARNING%If you trust this script, use \"SET DANGEROUSSCRIPTS 1\" in the console or config file.");
                                        script = null;
                                    }
                                }
                            }
                            string scriptHeader = @"using System; using SpaceGame;";
                            try
                            {
                                script = CSScriptLib.CSScript.Evaluator.LoadMethod(scriptHeader + "\n" + scriptText);
                                watch.Stop();
                                Debug.WriteLine("%SUCCESS%Compiled script in " + watch.ElapsedMilliseconds + "ms");
                            }
                            catch (Exception e)
                            {
                                watch.Stop();
                                Debug.WriteLine("%ERROR%Script compilation error. " + e.Message);
                                loaded = false;
                                error = true;
                                return;
                            }
                        }

                        loaded = true;
                    }
                }
                else
                {
                    script = null;
                }
                loaded = value;
            }
        }

        public dynamic Script
        {
            get
            {
                if (!Loaded)
                {
                    Loaded = true;
                }
                return script;
            }
            set
            {
                script = value;
                loaded = true;
            }
        }
    }
}
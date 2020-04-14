using System;
using System.Collections.Generic;
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
        private static string imagesFolder = @"images\";
        private static string xmlFolder = @"xml\";
        private static string fontsFolder = @"fonts\";
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
                Console.WriteLine(message);
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
                Console.WriteLine(message);
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
                Console.WriteLine(message);
                return null;
#endif
            }
        }

        public static void Load(string BasePath)
        {
            Instantiate();
            Console.WriteLine("Loading Textures...");
            tempPath = BasePath + imagesFolder;
            ProcessDirectory(tempPath, ".png", typeof(TextureResource));
            Console.WriteLine("Loading XML...");
            tempPath = BasePath + xmlFolder;
            ProcessDirectory(tempPath, ".xml", typeof(XmlResource));
            Console.WriteLine("Loading Fonts...");
            tempPath = BasePath + fontsFolder;
            ProcessDirectory(tempPath, ".ttf", typeof(FontResource));
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
                if (System.IO.Path.GetExtension(fileName).ToUpperInvariant() == Extension.ToUpperInvariant())
                {
                    string fn = fileName.Substring(tempPath.Length, fileName.Length - tempPath.Length).ToUpperInvariant();
                    fn = fn.Substring(0, fn.Length - Extension.Length);
                    Console.Write("Name: ");
                    Console.WriteLine(fn);

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
                }
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(TargetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                ProcessDirectory(subdirectory, Extension, AssetType);
            }
        }

        public static void Cull()
        {
            Random RNG = new Random();
            foreach (KeyValuePair<string, TextureResource> kvp in Instance.Textures)
            {
                if (kvp.Value.Loaded && kvp.Value.Users <= 0)
                {
                    Console.WriteLine("Unloading " + kvp.Value.Name);
                    kvp.Value.Loaded = false;
                }
            }
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
}
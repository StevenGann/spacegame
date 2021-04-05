/*
 * Copyright (c) 2020 Orade Technologies, LLC
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Raylib_cs.Raylib;

namespace SpaceGame
{
    public static class ResourceManager
    {
        public static string cacheDirectory = @"..\cache\";

        private static Dictionary<Type, string[]> RegisteredTypes = new Dictionary<Type, string[]>()
        {
            {typeof(FontResource), new string[]{".ttf"} },
            {typeof(SoundResource), new string[]{".wav", ".mp3", ".ogg"} },
            {typeof(TextureResource),new string[]{".png", ".jpg", ".bmp"} },
        };

        private static Dictionary<Type, Dictionary<string, IResource>> Resources = new Dictionary<Type, Dictionary<string, IResource>>();
        private static string tempPath = "";

        /// <summary>
        /// Unloads resources that are not currently being used.
        /// Invoke periodically but ONLY when you are certain no other parts of the program are actively using the resources.
        /// Currently only supports the TextureResource type.
        /// </summary>
        public static void Cull()
        {
            foreach (KeyValuePair<string, IResource> kvp in Resources[typeof(TextureResource)])
            {
                if (kvp.Value is TextureResource && kvp.Value.Loaded && kvp.Value.Users <= 0)
                {
                    //Debug.WriteLine("Unloading " + kvp.Value.Name);
                    Console.WriteLine("Unloading " + kvp.Value.Name);
                    kvp.Value.Loaded = false;
                }
            }
        }

        /// <summary>
        /// Fetches an IResource object of the specified ID, if found.
        /// The ID is case-insensetive.
        /// </summary>
        /// <typeparam name="T">Type of resource, must implement IResource</typeparam>
        /// <param name="Id">The resource's directory relative to the base search path, extension removed and sanitized of special characters.</param>
        /// <returns>Desired resource, if found.</returns>
        public static T Get<T>(string Id) where T : class, IResource
        {
            Dictionary<string, IResource> dict = null;
            if (Resources.ContainsKey(typeof(T)))
            {
                dict = Resources[typeof(T)];
            }

            if (dict?.ContainsKey(Id.ToUpperInvariant()) == true)
            {
                dict[Id.ToUpperInvariant()].Users++;
                T result = dict[Id.ToUpperInvariant()] as T;
                return result;
            }
            else
            {
                string message = "Cannot find " + typeof(T) + " " + Id.ToUpperInvariant();
#if DEBUG
                throw new KeyNotFoundException(message);
#else
                Debug.WriteLine(message);
                //Console.WriteLine(message);
                return null;
#endif
            }
        }

        /// <summary>
        /// Iterates over every subdirectory and file, recursively, in the specified path and catalogues any recognized resource it finds.
        /// Note that these resources are not actually loaded at this time, but lazy-loaded when they are requested later.
        /// </summary>
        /// <param name="BasePath">Path to begin searching from.</param>
        public static void Load(string BasePath)
        {
            if (Directory.Exists(Path.GetFullPath(cacheDirectory)))
            {
                Directory.Delete(Path.GetFullPath(cacheDirectory), true);
            }

            LoadArchives(BasePath, ".zip");

            foreach (KeyValuePair<Type, string[]> rt in RegisteredTypes)
            {
                foreach (string extension in rt.Value)
                {
                    tempPath = BasePath;
                    ProcessDirectory(BasePath, extension, rt.Key);
                }
            }
        }

        public static void LoadArchives(string BasePath, string ArchiveExtension)
        {
            string[] fileEntries = System.IO.Directory.GetFiles(BasePath);
            foreach (string fileName in fileEntries)
            {
                if (string.Equals(System.IO.Path.GetExtension(fileName), ArchiveExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    Debug.WriteLine("Found resource archive: " + fileName[BasePath.Length..]);

                    using ZipFile zf = new ZipFile(fileName);
                    foreach (ZipEntry zipEntry in zf)
                    {
                        foreach (KeyValuePair<Type, string[]> rt in RegisteredTypes)
                        {
                            foreach (string extension in rt.Value)
                            {
                                if (zipEntry.Name.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    Debug.WriteLine(zipEntry.Name);
                                    string fn = zipEntry.Name.ToUpperInvariant();
                                    fn = fn.Substring(0, fn.Length - extension.Length);
                                    fn = fn.Replace('/', '\\');
                                    Debug.WriteLine("Indexed " + rt.Key.Name + " from archive: " + fn);
                                    //Console.WriteLine("Name: " + fn);

                                    IResource resource = rt.Key.GetConstructor(Type.EmptyTypes).Invoke(null) as IResource;
                                    if (!Resources.ContainsKey(rt.Key))
                                    {
                                        Resources.Add(rt.Key, new Dictionary<string, IResource>());
                                    }
                                    Dictionary<string, IResource> dict = Resources[rt.Key];
                                    resource.Name = fn;
                                    resource.Path = "ZIP{" + fileName + "}" + zipEntry.Name.Replace('/', '\\');
                                    dict[fn] = resource;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static string GetRealPath(string ResourcePath)
        {
            if (!(ResourcePath.Contains('{') && ResourcePath.Contains('}')))
            {
                return ResourcePath;
            }
            else
            {
                string[] segments = ResourcePath.Split(new char[] { '{', '}' });
                if (segments[0] == "ZIP")
                {
                    string zipPath = segments[1];
                    string assetPath = segments[2];

                    string cachedPath = Path.GetFullPath(cacheDirectory + assetPath);

                    if (!File.Exists(cachedPath))
                    {
                        using ZipFile zf = new ZipFile(zipPath);
                        foreach (ZipEntry zipEntry in zf)
                        {
                            if (assetPath == zipEntry.Name.Replace('/', '\\'))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(cachedPath));
                                var buffer = new byte[4096];
                                using var zipStream = zf.GetInputStream(zipEntry);
                                using Stream fsOutput = File.Create(cachedPath);
                                StreamUtils.Copy(zipStream, fsOutput, buffer);
                            }
                        }
                    }
                    return cachedPath;
                }
            }

            throw new NotSupportedException("Resource source not recognized");
        }

        /*

         */

        /// <summary>
        /// Register a resource type and the file extensions it may include.
        /// The specified Type must implement the IResource interface
        /// </summary>
        /// <param name="Type">Type of resource, must implement IResource</param>
        /// <param name="Extensions">Array containing all file extensions this resource type may have, including the ".", i.e. ".bmp" or ".xml". Case insensetive.</param>
        public static void Register(Type Type, string[] Extensions)
        {
            if (!Type.GetInterfaces().Contains(typeof(IResource)))
            {
                string message = Type.Name + " must implement IResource";
#if DEBUG
                throw new KeyNotFoundException(message);
#else
                Debug.WriteLine(message);
                return;
#endif
            }

            RegisteredTypes[Type] = Extensions;
        }

        private static void ProcessDirectory(string TargetDirectory, string Extension, Type AssetType)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = System.IO.Directory.GetFiles(TargetDirectory);
            foreach (string fileName in fileEntries)
            {
                if (string.Equals(System.IO.Path.GetExtension(fileName), Extension, StringComparison.InvariantCultureIgnoreCase))
                {
                    string fn = fileName[tempPath.Length..].ToUpperInvariant();
                    fn = fn.Substring(0, fn.Length - Extension.Length);
                    Debug.WriteLine("Indexed " + AssetType.Name + ": " + fn);
                    //Console.WriteLine("Name: " + fn);

                    IResource resource = AssetType.GetConstructor(Type.EmptyTypes).Invoke(null) as IResource;
                    if (!Resources.ContainsKey(AssetType))
                    {
                        Resources.Add(AssetType, new Dictionary<string, IResource>());
                    }
                    Dictionary<string, IResource> dict = Resources[AssetType];
                    resource.Name = fn;
                    resource.Path = fileName;
                    dict[fn] = resource;
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

        private struct ResourceType
        {
            public string[] Extensions { get; set; }
            public Type Type { get; set; }
        }
    }

    public class FontResource : IResource
    {
        private Font font = default;
        private bool loaded = false;

        public Font Font
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

        public bool Loaded
        {
            get { return loaded; }
            set
            {
                if (value)
                {
                    font = LoadFont(Path);
                    loaded = true;
                }
                else
                {
                    font = default;
                }
                loaded = value;
            }
        }

        public string Name { get; set; }

        public string Path
        {
            get
            {
                return ResourceManager.GetRealPath(path);
            }
            set { path = value; }
        }

        private string path;
        public int Users { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class SoundResource : IResource
    {
        private bool loaded = false;
        private Sound sound;

        public bool Loaded
        {
            get { return loaded; }
            set
            {
                if (value)
                {
                    sound = LoadSound(Path);
                }
                else
                {
                }
                loaded = value;
            }
        }

        public string Name { get; set; }

        public string Path
        {
            get
            {
                return ResourceManager.GetRealPath(path);
            }
            set { path = value; }
        }

        private string path;

        public Sound Sound
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

        public int Users { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class TextureResource : IResource
    {
        private bool loaded = false;
        private Texture2D texture;

        public bool Loaded
        {
            get { return loaded; }
            set
            {
                if (value)
                {
                    texture = LoadTexture(Path);
                    SetTextureFilter(texture, TextureFilterMode.FILTER_POINT);
                    GenTextureMipmaps(ref texture);
                }
                else
                {
                    UnloadTexture(texture);
                }
                loaded = value;
            }
        }

        public string Name { get; set; }

        public string Path
        {
            get
            {
                return ResourceManager.GetRealPath(path);
            }
            set { path = value; }
        }

        private string path;

        public Texture2D Texture
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

        public int Users { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
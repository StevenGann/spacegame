using System;
using System.Collections.Generic;
using System.Text;

namespace SpaceGame
{
    public class ScriptResource : IResource
    {
        private bool error = false;
        private bool loaded = false;
        private dynamic script = null;
        private string scriptText = "";
        public bool Error { get => error; }

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
                        if (!string.IsNullOrEmpty(scriptText))
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
                            const string scriptHeader = "using System; using SpaceGame;";
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

        public string Text
        {
            get
            {
                scriptText = System.IO.File.ReadAllText(Path);
                return scriptText;
            }
        }

        public int Users { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SpaceGame
{
    public class XmlResource : IResource
    {
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
}
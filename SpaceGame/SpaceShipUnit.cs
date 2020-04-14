using Raylib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpaceGame
{
    public class SpaceShipUnit
    {
        public List<SpaceShip> Units { get; set; } = new List<SpaceShip>();

        public bool Selected
        {
            set
            {
                foreach (SpaceShip unit in Units)
                {
                    unit.Selected = value;
                }
            }
            get
            {
                foreach (SpaceShip unit in Units)
                {
                    if (unit.Selected)
                    {
                        Selected = true;
                        return true;
                    }
                }
                return false;
            }
        }

        public int Faction
        {
            set
            {
                foreach (SpaceShip unit in Units)
                {
                    unit.Faction = value;
                }
            }
            get
            {
                return Units[0].Faction;
            }
        }

        public bool Active
        {
            get
            {
                foreach (SpaceShip unit in Units)
                {
                    if (unit.Active)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public Vector2 Location
        {
            get
            {
                if (!locationValid)
                {
                    if (Units.Count > 1)
                    {
                        location = new Vector2(0, 0);
                        float count = 0;
                        foreach (SpaceShip unit in Units)
                        {
                            location += unit.Location;
                            count++;
                        }
                        location /= count;
                    }
                    else
                    {
                        location = Units[0].Location;
                    }
                    locationValid = true;
                }

                return location;
            }
        }

        private Vector2 location = new Vector2();
        private bool locationValid = true;

        public SpaceShipUnit()
        {
        }

        public SpaceShipUnit(SpaceShip Ship)
        {
            Add(Ship);
        }

        public void Add(SpaceShip Ship)
        {
            Units.Add(Ship);
            Ship.Unit = this;
            if (Ship.Hardpoints != null)
            {
                foreach (SpaceShipHardpoint hp in Ship.Hardpoints)
                {
                    Add(hp);
                }
            }
        }

        public Vector2 DrawLocation
        {
            get
            {
                return (Location * GameManager.ViewScale) + GameManager.ViewOffset;
            }
        }

        public void Tick(double Delta)
        {
            locationValid = false;
        }

        public void Draw()
        {
            if (Units.Count > 1)
            {
                Color col = new Color(0, 255, 0, 32);
                Vector2 loc = DrawLocation;

                //Raylib.Raylib.DrawTextureEx(Units[0].Texture.Texture, loc - Units[0].TextureOffset * 0.5f, 0, 0.5f, new Color(0, 255, 0, 255));

                if (Selected)
                {
                    Raylib.Raylib.DrawCircle((int)loc.x, (int)loc.y, 10f, col);
                    Raylib.Raylib.DrawCircleLines((int)loc.x, (int)loc.y, 10f, col);
                    bool pruneFlag = false;
                    foreach (SpaceShip unit in Units)
                    {
                        if (unit.Active)
                        {
                            Raylib.Raylib.DrawLineEx(loc, unit.DrawLocation, 2.5f, col);
                        }
                        else
                        {
                            pruneFlag = true;
                        }
                    }
                    if (pruneFlag) { PruneUnits(); }
                }
            }
        }

        private void PruneUnits()
        {
            int count = 0;
            for (int i = 0; i < Units.Count; i++)
            {
                if (!Units[i].Active)
                {
                    Units.RemoveAt(i);
                    i--;
                    count++;
                }
            }
            System.GC.Collect();
        }
    }
}
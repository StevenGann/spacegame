using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpaceGame
{
    public class Formation
    {
        private List<Vector2> slots = new List<Vector2>();

        public Formation()
        {
            slots.Add(new Vector2(0, 0));
            slots.Add(new Vector2(-1.5f, 1.0f));
            slots.Add(new Vector2(1.5f, 1.0f));
            slots.Add(new Vector2(-3.0f, 2.0f));
            slots.Add(new Vector2(3.0f, 2.0f));
            slots.Add(new Vector2(-4.5f, 3.0f));
            slots.Add(new Vector2(4.5f, 3.0f));
            slots.Add(new Vector2(-6.0f, 4.0f));
            slots.Add(new Vector2(6.0f, 4.0f));
        }

        public Vector2 GetLocation(SpaceShip Ship, float Scalar)
        {
            SpaceShipUnit unit = Ship.Unit;
            if (unit == null || unit.Units.Count == 1 || unit.Units[0] == Ship)
            {
                return Ship.Location;
            }

            int index = 0;
            while (index < unit.Units.Count && index <= slots.Count)
            {
                if (unit.Units[index] == Ship) { break; }
                index++;
            }

            if (index < slots.Count)
            {
                return unit.Units[0].Location + (slots[index].Rotate((float)unit.Units[0].Angle * DEG2RAD) * Scalar);
            }

            return Ship.Location;
        }
    }
}
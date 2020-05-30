using System;
using System.Collections.Generic;
using System.Text;

namespace SpaceGame
{
    internal class SpaceStructure : SpaceShip
    {
        public override void Tick(double Delta)
        {
            MaxThrust = 0;
            TurnSpeed = 0;
            base.Tick(Delta);
        }

        public override void Draw()
        {
            base.Draw();
        }
    }
}
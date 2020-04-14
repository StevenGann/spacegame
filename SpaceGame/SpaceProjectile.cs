using System;
using System.Collections.Generic;
using System.Text;

namespace SpaceGame
{
    public class SpaceProjectile : SpaceObject
    {
        public SpaceObject Sender { get; set; }
        public double Damage { get; set; } = 5;
        public double ShieldDamage { get; set; } = 5;
        public double Lifetime { get; set; } = 120;

        public override void Tick(double Delta)
        {
            if (Lifetime <= 0)
            {
                this.Active = false;
            }

            Lifetime -= 1;

            base.Tick(Delta);
        }
    }
}

public void Spawn(SpaceObject Entity)
{
	Console.WriteLine("Spawned " + Entity.GetHashCode().ToString());
}

public void Tick(double Delta, SpaceObject Entity)
{
  int x = (int)Entity.DrawLocation.x;
  int y = (int)Entity.DrawLocation.y;
  int s = 16;
  int m = 4;
  SpaceShip entity = Entity as SpaceShip;
  //Debug.DrawText(entity.Behavior.ToString(), x, y, s);
  //Debug.DrawText(entity.Stance.ToString(), x, y - (s + m), s);
  //Debug.DrawText("Hello!", (int)Entity.DrawLocation.x, (int)Entity.DrawLocation.y, 16);
  //Console.WriteLine("Ticked " + Entity.GetHashCode().ToString());
}

public void Draw(SpaceObject Entity)
{ 
  //Console.WriteLine("Drew " + Entity.GetHashCode().ToString());
}

public void Destroy(SpaceObject Entity)
{
  Console.WriteLine("Destroyed " + Entity.GetHashCode().ToString());
}
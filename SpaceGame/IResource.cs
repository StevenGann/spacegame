namespace SpaceGame
{
    public interface IResource
    {
        bool Loaded { get; set; }
        string Name { get; set; }
        string Path { get; set; }
        int Users { get; set; }
    }
}
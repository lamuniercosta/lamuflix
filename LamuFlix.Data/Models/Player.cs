namespace LamuFlix.Data.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string Formats { get; set; } = null!;
    }
}

namespace LamuFlix.Data.Models
{
    // ReSharper disable NullableWarningSuppressionIsUsed
    // EF Core entity materialization
    public class Temp
    {
        public int FilmeId { get; set; }
        public string Title { get; set; } = null!;
        public string ImdbId { get; set; } = null!;
        public string RealTitle { get; set; } = null!;
    }
    // ReSharper restore NullableWarningSuppressionIsUsed
}

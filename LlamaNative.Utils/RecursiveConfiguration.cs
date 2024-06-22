namespace LlamaNative.Utils
{
    public class RecursiveConfiguration<TConfiguration>
    {
        public required TConfiguration Configuration { get; set; }

        public Dictionary<string, string> Resources { get; set; } = [];
    }
}

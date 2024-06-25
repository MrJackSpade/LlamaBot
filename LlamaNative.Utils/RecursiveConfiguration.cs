namespace LlamaNative.Utils
{
    public class RecursiveConfiguration<TConfiguration>
    {
        public TConfiguration Configuration { get; set; }

        public Dictionary<string, string> Resources { get; set; } = [];
    }
}
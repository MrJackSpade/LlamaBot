namespace LlamaNative.Tokens.Models
{
    [Flags]
    public enum TokenMask
    {
        Undefined = 0,

        User = 1,

        Bot = 2,

        Prompt = 4,

        Template = 8,
    }
}
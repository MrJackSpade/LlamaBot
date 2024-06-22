using LlamaNative.Models;

namespace LlamaNative.Sampling.Interfaces
{
    public interface ITokenSelector

    {
        int SampleNext(SampleContext sampleContext);
    }
}
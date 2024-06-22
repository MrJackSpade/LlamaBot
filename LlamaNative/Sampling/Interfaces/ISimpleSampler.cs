using LlamaNative.Models;

namespace LlamaNative.Sampling.Interfaces
{
    public interface ISimpleSampler
    {
        public void SampleNext(SampleContext context);
    }
}
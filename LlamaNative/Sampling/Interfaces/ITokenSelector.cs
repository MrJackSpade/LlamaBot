using LlamaNative.Models;

namespace LlamaNative.Sampling.Interfaces
{
    /// <summary>
    /// Non-generic interface for token selection with per-request settings.
    /// </summary>
    public interface ITokenSelector
    {
        /// <summary>
        /// Selects the next token given the sample context and settings.
        /// </summary>
        int SampleNext(SampleContext sampleContext, object settings);

        /// <summary>
        /// Validates that the settings object is the correct type for this sampler.
        /// Throws ArgumentException if invalid.
        /// </summary>
        void ValidateSettings(object settings);
    }

    /// <summary>
    /// Strongly-typed generic interface for token selection.
    /// Implementers should implement this interface with their specific settings type.
    /// </summary>
    public interface ITokenSelector<T> : ITokenSelector
    {
        /// <summary>
        /// Selects the next token given the sample context and strongly-typed settings.
        /// </summary>
        int SampleNext(SampleContext sampleContext, T settings);

        // Default implementation bridges to generic
        int ITokenSelector.SampleNext(SampleContext sampleContext, object settings)
        {
            return SampleNext(sampleContext, (T)settings);
        }

        void ITokenSelector.ValidateSettings(object settings)
        {
            if (settings is not T)
            {
                throw new ArgumentException($"Expected settings of type {typeof(T).Name}, but received {settings?.GetType().Name ?? "null"}");
            }
        }
    }
}
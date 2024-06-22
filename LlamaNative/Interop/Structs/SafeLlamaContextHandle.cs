namespace LlamaNative.Interop.Structs
{
    public class SafeLlamaContextHandle : SafeLlamaHandleBase
    {
        private readonly SafeLlamaModelHandle _model;

        public SafeLlamaContextHandle(nint contextPtr, SafeLlamaModelHandle model, Action<nint> free)
            : base(contextPtr, free)
        {
            _model = model;
        }

        protected SafeLlamaContextHandle(Action<nint> free) : base(free)
        {
        }
    }
}
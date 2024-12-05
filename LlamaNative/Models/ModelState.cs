using LlamaNative.Decode.Collections;
using LlamaNative.Decode.Interfaces;
using LlamaNative.Decode.Utils;
using LlamaNative.Interop.Structs;
using LlamaNative.Tokens.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;

namespace LlamaNative.Models
{
    public class ModelState
    {
        private readonly PointerArray _buffer;

        private readonly PointerArraySynchronizer _synchronizer;

        public uint AvailableBuffer => Settings.NCtx - _buffer.Pointer;

        public SafeContextHandle ContextHandle { get; }

        public KvCacheState KvCache { get; set; }

        public SafeModelHandle ModelHandle { get; }

        public ContextParams Settings { get; }

        public ModelState(KvCacheState kvCache, ContextParams settings, SafeContextHandle contextHandle, SafeModelHandle modelHandle)
        {
            _synchronizer = new(
                new KvCacheShifter(settings.NThreads, settings.NBatch, contextHandle, modelHandle),
                Token.Null
            );
            ContextHandle = contextHandle;
            KvCache = kvCache;
            ModelHandle = modelHandle;
            Settings = settings;

            _buffer = new PointerArray(Settings.NCtx);
            _buffer.Fill(Token.Null);
        }

        public void AppendToken(Token token)
        {
            this.AppendToken(new SequencedToken(token, [0]));
        }

        public void AppendToken(SequencedToken token)
        {
            _buffer[_buffer.Pointer++] = token;
        }

        public void ClearBuffer()
        {
            _buffer.Clear();
        }

        public void SetBufferPointer(uint startIndex)
        {
            _buffer.Pointer = startIndex;
        }

        public void Sync()
        {
            _synchronizer.Sync(this.KvCache, _buffer);
        }

        public void Sync(ModelState modelState)
        {
            _buffer.Clear();

            for (uint i = 0; i < modelState._buffer.Length; i++)
            {
                this.AppendToken(modelState._buffer[i]);
            }

            _buffer.Pointer = modelState._buffer.Pointer;

            _synchronizer.Sync(this.KvCache, _buffer);
        }
    }
}
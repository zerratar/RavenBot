using System;
using System.Diagnostics;

namespace RavenBot.Core.Memory
{
    public class ValuePointer<T> where T : struct
    {
        private readonly DeepPointer pointer;

        public ValuePointer(string module, Int64 base_, params Int64[] offsets)
        {
            this.pointer = new DeepPointer(module, base_, offsets);
        }

        public ValuePointer(DeepPointer pointer)
        {
            this.pointer = pointer;
        }

        public ValuePointer<T> Offset(Int64 offset0, params Int64[] offsets)
        {
            return new ValuePointer<T>(this.pointer.Offset(offset0, offsets));
        }

        public bool WriteValue(Process process, T value)
        {
            return pointer.DerefOffsets(process, out var ptr) && process.WriteValue(ptr, value);
        }

        public T ReadValue(Process process)
        {
            if (pointer.Deref<T>(process, out T val))
                return val;

            return default(T);
        }

        public ProcessBoundValuePointer<T> BindToProcess(Process process)
        {
            return new ProcessBoundValuePointer<T>(this, process);
        }
    }
}

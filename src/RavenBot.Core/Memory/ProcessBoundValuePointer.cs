using System.Diagnostics;

namespace RavenBot.Core.Memory
{
    public class ProcessBoundValuePointer<T> where T : struct
    {
        private readonly ValuePointer<T> ptr;
        private readonly Process process;

        public ProcessBoundValuePointer(ValuePointer<T> ptr, Process process)
        {
            this.ptr = ptr;
            this.process = process;
        }

        public bool WriteValue(T value)
        {
            return ptr.WriteValue(process, value);
        }

        public T ReadValue()
        {
            return ptr.ReadValue(process);
        }
    }
}
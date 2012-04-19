using System;
using BananaHook.Infrastructure.PInvoke;

namespace BananaHook.Infrastructure
{
    public class Patch : IDisposable
    {
        private readonly MemoryPageProtector _protector;
        private readonly IMemory _memory;
        private readonly IntPtr _targetAddress;
        private readonly byte[] _replaceWith;
        private byte[] _original;

        public Patch(IMemory memory, IntPtr targetAddress, byte[] replaceWith)
        {
            _protector = new MemoryPageProtector(new Win32Implementation(), targetAddress, (IntPtr)replaceWith.Length);
            _memory = memory;
            _targetAddress = targetAddress;
            _replaceWith = replaceWith;
        }

        public bool IsApplied { get; private set; }

        public void Apply()
        {
            if (IsApplied) return;

            _protector.ExecuteWithProtection(MemoryProtectionConstraints.PAGE_EXECUTE_READWRITE, () =>
            {
                _original = _memory.ReadBytes(_targetAddress, _replaceWith.Length);
                _memory.WriteBytes(_targetAddress, _replaceWith);
            });
            IsApplied = true;
        }

        public void Remove()
        {
            if (!IsApplied) return;

            _protector.ExecuteWithProtection(MemoryProtectionConstraints.PAGE_EXECUTE_READWRITE, () => 
                _memory.WriteBytes(_targetAddress, _original));

            IsApplied = false;
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            Remove();
        }

        ~Patch()
        {
            Dispose();
        }

        #endregion
    }
}
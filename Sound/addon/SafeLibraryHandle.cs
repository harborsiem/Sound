using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.LibraryLoader;

#pragma warning disable CA1416

namespace SystemX.Addon {
    internal sealed unsafe class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid {
        public SafeLibraryHandle(IntPtr preexistingHandle) : base(true) {
            base.SetHandle(preexistingHandle);
        }

        [System.Security.SecurityCritical]
        protected override bool ReleaseHandle() {
            return PInvoke.FreeLibrary((HMODULE)handle);
        }

        public static SafeLibraryHandle LoadLibraryEx(string libFilename, int flags) {
            fixed (char* plibFilename = libFilename)
                return new SafeLibraryHandle(PInvoke.LoadLibraryEx(new PCWSTR(plibFilename), HANDLE.Null, (LOAD_LIBRARY_FLAGS)flags));
        }
    }
}

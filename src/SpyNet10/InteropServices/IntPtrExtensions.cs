using System.Runtime.InteropServices;

namespace SpyNet10.InteropServices;

public static class IntPtrExtensions
{
    public static T ToStructure<T>(this IntPtr p) => (T)Marshal.PtrToStructure(p, typeof(T));
}
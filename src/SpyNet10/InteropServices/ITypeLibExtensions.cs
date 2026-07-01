using System.Runtime.InteropServices.ComTypes;

namespace SpyNet10.InteropServices;

public static class ITypeLibExtensions
{
    public static Guid GetGuid(this ITypeLib typeLib)
    {
        TYPELIBATTR attr;
        IntPtr p = IntPtr.Zero;

        typeLib.GetLibAttr(out p);

        try
        {
            attr = p.ToStructure<TYPELIBATTR>();
            return attr.guid;
        }
        finally
        {
            typeLib.ReleaseTLibAttr(p);
        }
    }

    public static TYPELIBATTR GetLibAttr(this ITypeLib typeLib)
    {
        TYPELIBATTR attr;
        IntPtr p = IntPtr.Zero;

        typeLib.GetLibAttr(out p);

        try
        {
            attr = p.ToStructure<TYPELIBATTR>();
        }
        finally
        {
            typeLib.ReleaseTLibAttr(p);
        }

        return attr;
    }

    public static Version GetVersion(this ITypeLib typeLib)
    {
        TYPELIBATTR attr;
        IntPtr p = IntPtr.Zero;

        typeLib.GetLibAttr(out p);

        try
        {
            attr = p.ToStructure<TYPELIBATTR>();
            return new Version(attr.wMajorVerNum, attr.wMinorVerNum);
        }
        finally
        {
            typeLib.ReleaseTLibAttr(p);
        }
    }
}

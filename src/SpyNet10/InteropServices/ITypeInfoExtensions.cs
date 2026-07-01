using System.Runtime.InteropServices.ComTypes;

namespace SpyNet10.InteropServices;

public static class ITypeInfoExtensions
{
    public static Guid GetGuid(this ITypeInfo typeInfo)
    {
        TYPEATTR attr;
        IntPtr p = IntPtr.Zero;

        typeInfo.GetTypeAttr(out p);

        try
        {
            attr = p.ToStructure<TYPEATTR>();
            return attr.guid;
        }
        finally
        {
            typeInfo.ReleaseTypeAttr(p);
        }
    }

    public static Version GetVersion(this ITypeInfo typeInfo)
    {
        TYPEATTR attr;
        IntPtr p = IntPtr.Zero;

        typeInfo.GetTypeAttr(out p);

        try
        {
            attr = p.ToStructure<TYPEATTR>();
            return new Version(attr.wMajorVerNum, attr.wMinorVerNum);
        }
        finally
        {
            typeInfo.ReleaseTypeAttr(p);
        }
    }
}

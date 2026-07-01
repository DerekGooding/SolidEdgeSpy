using SpyNet10.Extensions;

namespace SpyNet10.InteropServices;

public class ComImplementedTypeInfo
{
    public ComImplementedTypeInfo(ComTypeInfo comTypeInfo, System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS implTypeFlags)
    {
        ComTypeInfo = comTypeInfo;
        ImplementedTypeFlags = implTypeFlags;
    }

    public ComTypeInfo ComTypeInfo { get; }
    public System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS ImplementedTypeFlags { get; }

    public bool IsDefault => ImplementedTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FDEFAULT);
    public bool IsSource => ImplementedTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FSOURCE);
    public bool IsRestricted => ImplementedTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FRESTRICTED);
    public bool IsDefaultVTable => ImplementedTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FDEFAULTVTABLE);
}
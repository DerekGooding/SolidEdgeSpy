using SpyNet10.Extensions;

namespace SpyNet10.InteropServices;

public class ComImplementedTypeInfo
{
    private ComTypeInfo _comTypeInfo = null;
    private System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS _implTypeFlags = default(System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS);

    public ComImplementedTypeInfo(ComTypeInfo comTypeInfo, System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS implTypeFlags)
    {
        _comTypeInfo = comTypeInfo;
        _implTypeFlags = implTypeFlags;
    }

    public ComTypeInfo ComTypeInfo => _comTypeInfo;
    public System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS ImplementedTypeFlags => _implTypeFlags;

    public bool IsDefault => _implTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FDEFAULT);
    public bool IsSource => _implTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FSOURCE);
    public bool IsRestricted => _implTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FRESTRICTED);
    public bool IsDefaultVTable => _implTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FDEFAULTVTABLE);
}
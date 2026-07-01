using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace SpyNet10.InteropServices;

[ComImport()]
[Guid(NativeMethods.IID_IDispatch)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDispatch
{
    int GetTypeInfoCount();

    ITypeInfo GetTypeInfo(int iTInfo, int lcid);

    [PreserveSig]
    int GetIDsOfNames
    (
        ref Guid riid,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)]
        string[] rgsNames,
        int cNames,
        int lcid,
        [MarshalAs(UnmanagedType.LPArray)]
        int[] rgDispId
    );

    [PreserveSig]
    int Invoke(
        int dispIdMember,
        ref Guid riid,
        int lcid,
        INVOKEKIND wFlags,
        ref DISPPARAMS pDispParams,
        out Variant pvarResult,
        ref EXCEPINFO pExcepInfo,
        out uint puArgErr);
}
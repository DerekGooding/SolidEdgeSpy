using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace SpyNet10.InteropServices;

public static class ComHelper
{
    public static Type VariantTypeToType(VarEnum variantType)
    {
        switch (variantType)
        {
            case VarEnum.VT_EMPTY:
            case VarEnum.VT_NULL:
                return null;

            case VarEnum.VT_I2:
                return typeof(short);

            case VarEnum.VT_I4:
            case VarEnum.VT_INT:
                return typeof(int);

            case VarEnum.VT_R4:
                return typeof(float);

            case VarEnum.VT_R8:
                return typeof(double);

            case VarEnum.VT_CY:
                return typeof(decimal);

            case VarEnum.VT_DATE:
                return typeof(DateTime);

            case VarEnum.VT_BSTR:
            case VarEnum.VT_LPSTR:
            case VarEnum.VT_LPWSTR:
                return typeof(string);

            case VarEnum.VT_DISPATCH:
                return typeof(object);

            case VarEnum.VT_ERROR:
            case VarEnum.VT_HRESULT:
                return typeof(int);

            case VarEnum.VT_BOOL:
                return typeof(bool);

            case VarEnum.VT_VARIANT:
            case VarEnum.VT_UNKNOWN:
                return typeof(object);

            case VarEnum.VT_DECIMAL:
                break;

            case VarEnum.VT_I1:
                return typeof(sbyte);

            case VarEnum.VT_UI1:
                return typeof(byte);

            case VarEnum.VT_UI2:
                return typeof(ushort);

            case VarEnum.VT_UI4:
            case VarEnum.VT_UINT:
                return typeof(uint);

            case VarEnum.VT_I8:
                return typeof(long);

            case VarEnum.VT_UI8:
                return typeof(ulong);

            case VarEnum.VT_VOID:
                break;

            case VarEnum.VT_PTR:
                break;

            case VarEnum.VT_SAFEARRAY:
                break;

            case VarEnum.VT_CARRAY:
                break;

            case VarEnum.VT_USERDEFINED:
                break;

            case VarEnum.VT_RECORD:
                break;

            case VarEnum.VT_FILETIME:
                break;

            case VarEnum.VT_BLOB:
                break;

            case VarEnum.VT_STREAM:
                break;

            case VarEnum.VT_STORAGE:
                break;

            case VarEnum.VT_STREAMED_OBJECT:
                break;

            case VarEnum.VT_STORED_OBJECT:
                break;

            case VarEnum.VT_BLOB_OBJECT:
                break;

            case VarEnum.VT_CF:
                break;

            case VarEnum.VT_CLSID:
                return typeof(Guid);

            case VarEnum.VT_VECTOR:
                break;

            case VarEnum.VT_ARRAY:
                break;

            case VarEnum.VT_BYREF:
                break;
        }

        return null;
    }

    public static string TypeDescToString(TYPEDESC typeDesc, ITypeInfo typeInfo)
    {
        ITypeInfo refTypeInfo = null;
        var variantType = (VarEnum)typeDesc.vt;
        TYPEDESC ptrTypeDesc;

        switch (variantType)
        {
            case VarEnum.VT_PTR:
                ptrTypeDesc = typeDesc.lpValue.ToStructure<TYPEDESC>();
                return TypeDescToString(ptrTypeDesc, typeInfo);

            case VarEnum.VT_USERDEFINED:
                //int href = (int)(typeDesc.lpValue.ToInt64() & int.MaxValue);
                //typeInfo.GetRefTypeInfo(href, out refTypeInfo);
                var fixedTypeInfo = (IFixedTypeInfo)typeInfo;
                fixedTypeInfo.GetRefTypeInfo(typeDesc.lpValue, out refTypeInfo);
                var refComTypeInfo = ComTypeManager.Instance.FromITypeInfo(refTypeInfo);
                return refComTypeInfo.FullName;

            case VarEnum.VT_SAFEARRAY:
                var s = TypeDescToString(typeDesc.lpValue.ToStructure<TYPEDESC>(), typeInfo);
                return string.Format("{0}[]", s);

            case VarEnum.VT_VOID:
                return "void";

            default:
                var t = VariantTypeToType(variantType);
                if (t != null)
                {
                    return t.Name;
                }
                break;
        }

        return "";
    }
}
using System.Runtime.InteropServices.ComTypes;

namespace SpyNet10.InteropServices;

public class ComTypeLibrary
{
    private string _fileName;
    private ITypeLib _typeLib;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private int _helpContext = 0;
    private string _helpFile = string.Empty;
    private TYPELIBATTR _typeLibAttr;
    private List<ComTypeInfo> _typeInfos = null;

    public ComTypeLibrary(ITypeLib typeLib)
    {
        _typeLib = typeLib;
        _typeLib.GetDocumentation(-1, out _name, out _description, out _helpContext, out _helpFile);
        _typeLibAttr = _typeLib.GetLibAttr();

        _fileName = NativeMethods.QueryPathOfRegTypeLib(_typeLibAttr.guid, _typeLibAttr.wMajorVerNum, _typeLibAttr.wMinorVerNum, _typeLibAttr.lcid);
        _fileName = _fileName.Trim(['\0']);

        _fileName = Path.GetFullPath(_fileName);
    }

    public string Name => _name;
    public string Description => _description;
    public int HelpContext => _helpContext;
    public string HelpFile => _helpFile;
    public string Filename => _fileName;

    public ITypeLib GetITypeLib() => _typeLib;

    public Guid Guid => _typeLibAttr.guid;
    public int Lcid => _typeLibAttr.lcid;
    public SYSKIND Syskind => _typeLibAttr.syskind;
    public LIBFLAGS Libflags => _typeLibAttr.wLibFlags;
    public Version Version => new(_typeLibAttr.wMajorVerNum, _typeLibAttr.wMinorVerNum);

    public static ComTypeLibrary FromRegistry(Guid guid, short wVerMajor, short wVerMinor) => new(NativeMethods.LoadRegTypeLib(guid, wVerMajor, wVerMinor));

    public ComTypeInfo[] ComTypeInfos
    {
        get
        {
            if (_typeInfos == null)
            {
                LoadTypes();
            }

            return _typeInfos.ToArray();
        }
    }

    public ComAliasInfo[] Typedefs => ComTypeInfos.Where(entity => entity is ComAliasInfo).Cast<ComAliasInfo>().ToArray();

    public ComCoClassInfo[] CoClasses => ComTypeInfos.Where(entity => entity is ComCoClassInfo).Cast<ComCoClassInfo>().ToArray();

    public ComDispatchInfo[] Dispinterfaces => ComTypeInfos.Where(entity => entity is ComDispatchInfo).Cast<ComDispatchInfo>().ToArray();

    public ComEnumInfo[] Enums => ComTypeInfos.Where(entity => entity is ComEnumInfo).Cast<ComEnumInfo>().ToArray();

    public ComInterfaceInfo[] Interfaces => ComTypeInfos.Where(entity => entity is ComInterfaceInfo).Cast<ComInterfaceInfo>().ToArray();

    public ComModuleInfo[] Modules => ComTypeInfos.Where(entity => entity is ComModuleInfo).Cast<ComModuleInfo>().ToArray();

    public ComRecordInfo[] Structs => ComTypeInfos.Where(entity => entity is ComRecordInfo).Cast<ComRecordInfo>().ToArray();

    public ComUnionInfo[] Unions => ComTypeInfos.Where(entity => entity is ComUnionInfo).Cast<ComUnionInfo>().ToArray();

    public override string ToString() => _name;

    private void LoadTypes()
    {
        _typeInfos = new List<ComTypeInfo>();

        var count = _typeLib.GetTypeInfoCount();

        for (var i = 0; i < count; i++)
        {
            ITypeInfo typeInfo = null;
            _typeLib.GetTypeInfo(i, out typeInfo);

            var pTypeAttr = IntPtr.Zero;
            typeInfo.GetTypeAttr(out pTypeAttr);
            var typeAttr = pTypeAttr.ToStructure<TYPEATTR>();

            switch (typeAttr.typekind)
            {
                case System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_ALIAS:
                    _typeInfos.Add(new ComAliasInfo(this, typeInfo, pTypeAttr));
                    break;

                case System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_COCLASS:
                    _typeInfos.Add(new ComCoClassInfo(this, typeInfo, pTypeAttr));
                    break;

                case System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_DISPATCH:
                    _typeInfos.Add(new ComDispatchInfo(this, typeInfo, pTypeAttr));
                    break;

                case System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_ENUM:
                    _typeInfos.Add(new ComEnumInfo(this, typeInfo, pTypeAttr));
                    break;

                case System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_INTERFACE:
                    _typeInfos.Add(new ComInterfaceInfo(this, typeInfo, pTypeAttr));
                    break;
                //case System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_MAX:
                //    _typeInfos.Add(new ComMaxInfo(this, typeInfo, pTypeAttr));
                //    break;
                case System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_MODULE:
                    _typeInfos.Add(new ComModuleInfo(this, typeInfo, pTypeAttr));
                    break;

                case System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_RECORD:
                    _typeInfos.Add(new ComRecordInfo(this, typeInfo, pTypeAttr));
                    break;

                case System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_UNION:
                    _typeInfos.Add(new ComUnionInfo(this, typeInfo, pTypeAttr));
                    break;
            }
        }

        _typeInfos.Sort(delegate (ComTypeInfo a, ComTypeInfo b)
        {
            return a.Name.CompareTo(b.Name);
        });
    }

    public static ComTypeLibrary FromObject(object o) => FromIDispatch((IDispatch)o);

    public static ComTypeLibrary FromIDispatch(IDispatch dispatch)
    {
        ITypeLib typeLib = null;
        var typeInfo = dispatch.GetTypeInfo();
        var index = 0;

        typeInfo.GetContainingTypeLib(out typeLib, out index);

        return new ComTypeLibrary(typeLib);
    }
}
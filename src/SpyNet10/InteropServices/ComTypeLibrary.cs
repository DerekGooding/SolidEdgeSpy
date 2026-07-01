using System.Runtime.InteropServices.ComTypes;

namespace SpyNet10.InteropServices;

public class ComTypeLibrary
{
    private readonly ITypeLib _typeLib;
    private readonly string _name = string.Empty;
    private readonly string _description = string.Empty;
    private readonly int _helpContext;
    private readonly string _helpFile = string.Empty;
    private TYPELIBATTR _typeLibAttr;
    private List<ComTypeInfo> _typeInfos;

    public ComTypeLibrary(ITypeLib typeLib)
    {
        _typeLib = typeLib;
        _typeLib.GetDocumentation(-1, out _name, out _description, out _helpContext, out _helpFile);
        _typeLibAttr = _typeLib.GetLibAttr();

        Filename = NativeMethods.QueryPathOfRegTypeLib(_typeLibAttr.guid, _typeLibAttr.wMajorVerNum, _typeLibAttr.wMinorVerNum, _typeLibAttr.lcid);
        Filename = Filename.Trim(['\0']);

        Filename = Path.GetFullPath(Filename);
    }

    public string Name => _name;
    public string Description => _description;
    public int HelpContext => _helpContext;
    public string HelpFile => _helpFile;
    public string Filename { get; }

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

            return [.. _typeInfos];
        }
    }

    public ComAliasInfo[] Typedefs => [.. ComTypeInfos.Where(entity => entity is ComAliasInfo).Cast<ComAliasInfo>()];

    public ComCoClassInfo[] CoClasses => [.. ComTypeInfos.Where(entity => entity is ComCoClassInfo).Cast<ComCoClassInfo>()];

    public ComDispatchInfo[] Dispinterfaces => [.. ComTypeInfos.Where(entity => entity is ComDispatchInfo).Cast<ComDispatchInfo>()];

    public ComEnumInfo[] Enums => [.. ComTypeInfos.Where(entity => entity is ComEnumInfo).Cast<ComEnumInfo>()];

    public ComInterfaceInfo[] Interfaces => [.. ComTypeInfos.Where(entity => entity is ComInterfaceInfo).Cast<ComInterfaceInfo>()];

    public ComModuleInfo[] Modules => [.. ComTypeInfos.Where(entity => entity is ComModuleInfo).Cast<ComModuleInfo>()];

    public ComRecordInfo[] Structs => [.. ComTypeInfos.Where(entity => entity is ComRecordInfo).Cast<ComRecordInfo>()];

    public ComUnionInfo[] Unions => [.. ComTypeInfos.Where(entity => entity is ComUnionInfo).Cast<ComUnionInfo>()];

    public override string ToString() => _name;

    private void LoadTypes()
    {
        _typeInfos = [];

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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace SpyNet10.InteropServices;

public delegate void ComTypeLibrarySelectedHandler(object sender, ComTypeLibrary comTypeLibrary);

public delegate void ComTypeInfoSelectedHandler(object sender, ComTypeInfo comTypeInfo);

public sealed class ComTypeManager
{
    private readonly List<ComTypeLibrary> _typeLibraries = [];

    private static volatile ComTypeManager _instance;
    private static readonly object _syncRoot = new();

    public event ComTypeLibrarySelectedHandler ComTypeLibrarySelected;

    public event ComTypeInfoSelectedHandler ComTypeInfoSelected;

    private ComTypeManager()
    { }

    public static ComTypeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_syncRoot)
                {
                    if (_instance == null)
                        _instance = new ComTypeManager();
                }
            }

            return _instance;
        }
    }

    private ComTypeLibrary GetComTypeLibrary(ITypeLib typeLib)
    {
        var typeLibGuid = typeLib.GetGuid();
        var typeLibVersion = typeLib.GetVersion();

        var comTypeLibrary = _typeLibraries.Where(
            x => x.Guid.Equals(typeLibGuid)).Where(
            x => x.Version.Equals(typeLibVersion)
            ).FirstOrDefault();

        if (comTypeLibrary == null)
        {
            comTypeLibrary = new ComTypeLibrary(typeLib);
            _typeLibraries.Add(comTypeLibrary);
            _typeLibraries.Sort(delegate (ComTypeLibrary a, ComTypeLibrary b)
            {
                return a.Name.CompareTo(b.Name);
            });
        }

        return comTypeLibrary;
    }

    public ComTypeLibrary LoadRegTypeLib(Guid guid, Version version)
    {
        ComTypeLibrary comTypeLibrary = null;

        try
        {
            var typeLib = NativeMethods.LoadRegTypeLib(guid, (short)version.Major, (short)version.Minor);
            comTypeLibrary = GetComTypeLibrary(typeLib);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }

        return comTypeLibrary;
    }

    public ComTypeLibrary LoadRegTypeLib(Guid guid, short wVerMajor, short wVerMinor) => LoadRegTypeLib(guid, new Version(wVerMajor, wVerMinor));

    public ComTypeInfo FromITypeInfo(ITypeInfo typeInfo)
    {
        if (typeInfo == null) return null;

        ITypeLib typeLib = null;
        var index = 0;
        typeInfo.GetContainingTypeLib(out typeLib, out index);

        var comTypeLibrary = GetComTypeLibrary(typeLib);

        var typeName = Marshal.GetTypeInfoName(typeInfo);

        if (comTypeLibrary != null)
        {
            return comTypeLibrary.ComTypeInfos.Where(
                x => x.Name.Equals(typeName)).FirstOrDefault();
        }

        return null;
    }

    public ComTypeInfo FromIDispatch(IDispatch dispatch)
    {
        if (dispatch == null) return null;

        return FromITypeInfo(dispatch.GetTypeInfo());
    }

    public ComTypeInfo LookupUserDefined(TYPEDESC typeDesc, ComTypeInfo comTypeInfo)
    {
        if (comTypeInfo == null) return null;

        ITypeInfo refTypeInfo = null;
        var variantType = (VarEnum)typeDesc.vt;

        if (variantType == VarEnum.VT_USERDEFINED)
        {
            var fixedTypeInfo = (IFixedTypeInfo)comTypeInfo.GetITypeInfo();
            fixedTypeInfo.GetRefTypeInfo(typeDesc.lpValue, out refTypeInfo);
            return ComTypeManager.Instance.FromITypeInfo(refTypeInfo);
        }

        return null;
    }

    public ComTypeLibrary[] ComTypeLibraries => [.. _typeLibraries];

    public bool HasComType(string fullName)
    {
        var tokens = fullName.Split(['.']);

        if (tokens.Length == 2)
        {
            var comTypeLibrary = _typeLibraries.Where(x => x.Name.Equals(tokens[0])).FirstOrDefault();
            if (comTypeLibrary != null)
            {
                var comTypeInfo = comTypeLibrary.ComTypeInfos.Where(x => x.Name.Equals(tokens[1])).FirstOrDefault();
                if (comTypeInfo != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void LookupAndSelect(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var tokens = name.Split(['.']);

        ComTypeLibrary comTypeLibrary = null;
        ComTypeInfo comTypeInfo = null;

        if (tokens.Length == 1)
        {
            comTypeLibrary = _typeLibraries.Where(x => x.Name.Equals(tokens[0])).FirstOrDefault();
            if (comTypeLibrary != null)
            {
                if (ComTypeLibrarySelected != null)
                {
                    ComTypeLibrarySelected(this, comTypeLibrary);
                }
            }
        }
        else if (tokens.Length == 2)
        {
            comTypeLibrary = _typeLibraries.Where(x => x.Name.Equals(tokens[0])).FirstOrDefault();
            if (comTypeLibrary != null)
            {
                comTypeInfo = comTypeLibrary.ComTypeInfos.Where(x => x.Name.Equals(tokens[1])).FirstOrDefault();
                if ((comTypeInfo != null) && (ComTypeInfoSelected != null))
                {
                    ComTypeInfoSelected(this, comTypeInfo);
                }
            }
        }
    }
}
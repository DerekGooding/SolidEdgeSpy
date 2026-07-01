using SpyNet10.Extensions;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace SpyNet10.InteropServices;

public class ComTypeInfo
{
    protected ComTypeLibrary _comTypeLibrary;
    protected ITypeInfo _typeInfo;
    protected IntPtr _pTypeAttr;
    protected TYPEATTR _typeAttr;
    protected string _name = string.Empty;
    protected string _description = string.Empty;
    protected int _helpContext = 0;
    protected string _helpFile = string.Empty;
    protected List<ComMemberInfo> _members = null;
    protected List<ComImplementedTypeInfo> _implementedTypes = null;

    public ComTypeInfo(ComTypeLibrary comTypeLibrary, ITypeInfo typeInfo, IntPtr pTypeAttr)
    {
        _comTypeLibrary = comTypeLibrary;
        _typeInfo = typeInfo;
        _pTypeAttr = pTypeAttr;
        _typeAttr = _pTypeAttr.ToStructure<TYPEATTR>();
        _typeInfo.GetDocumentation(-1, out _name, out _description, out _helpContext, out _helpFile);
    }

    public ComImplementedTypeInfo[] ImplementedTypes
    {
        get
        {
            if (_implementedTypes == null)
            {
                _implementedTypes = new List<ComImplementedTypeInfo>();

                for (var i = 0; i < _typeAttr.cImplTypes; i++)
                {
                    var flags = default(IMPLTYPEFLAGS);
                    _typeInfo.GetImplTypeFlags(i, out flags);

                    ITypeInfo refTypeInfo = null;
                    var href = 0;
                    _typeInfo.GetRefTypeOfImplType(i, out href);
                    _typeInfo.GetRefTypeInfo(href, out refTypeInfo);

                    var comTypeInfo = ComTypeManager.Instance.FromITypeInfo(refTypeInfo);

                    _implementedTypes.Add(new ComImplementedTypeInfo(comTypeInfo, flags));
                }
            }

            return _implementedTypes.ToArray();
        }
    }

    public ComMemberInfo[] Members
    {
        get
        {
            if (_members == null) LoadMembers();

            return _members.ToArray();
        }
    }

    public ComFunctionInfo[] Methods => Members.OfType<ComFunctionInfo>().Where(function => function.InvokeKind == System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_FUNC).ToArray();

    public ComPropertyInfo[] Properties
    {
        get
        {
            var list = new List<ComPropertyInfo>();
            var dictionary = new Dictionary<string, List<ComFunctionInfo>>();

            var functions = Members.OfType<ComFunctionInfo>().Where(
                x => x.InvokeKind != System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_FUNC).ToArray();

            foreach (var function in functions)
            {
                if (!dictionary.ContainsKey(function.Name))
                {
                    dictionary.Add(function.Name, new List<ComFunctionInfo>());
                }

                dictionary[function.Name].Add(function);
            }

            var enumerator = dictionary.GetEnumerator();

            while (enumerator.MoveNext())
            {
                list.Add(new ComPropertyInfo(this, enumerator.Current.Value.ToArray()));
            }

            return list.ToArray();
        }
    }

    public ComVariableInfo[] Variables => Members.OfType<ComVariableInfo>().ToArray();

    public ComFunctionInfo[] GetMethods(bool includeInherited)
    {
        var list = new List<ComFunctionInfo>();

        list.AddRange(Methods);

        if (includeInherited)
        {
            list.AddRange(GetInheritedMethods(this));
        }

        list.Sort(delegate (ComFunctionInfo a, ComFunctionInfo b)
        {
            return a.Name.CompareTo(b.Name);
        });

        return list.GroupBy(x => x.Name).Select(s => s.First()).ToArray();
    }

    public ComPropertyInfo[] GetProperties(bool includeInherited)
    {
        var list = new List<ComPropertyInfo>();

        list.AddRange(Properties);

        if (includeInherited)
        {
            list.AddRange(GetInheriteProperties(this));
        }

        list.Sort(delegate (ComPropertyInfo a, ComPropertyInfo b)
        {
            return a.Name.CompareTo(b.Name);
        });

        return list.GroupBy(x => x.Name).Select(s => s.First()).ToArray();
    }

    private ComFunctionInfo[] GetInheritedMethods(ComTypeInfo comTypeInfo)
    {
        var list = new List<ComFunctionInfo>();

        for (var i = 0; i < comTypeInfo.ImplementedTypes.Length; i++)
        {
            var comImplementedTypeInfo = comTypeInfo.ImplementedTypes[i];

            if (comImplementedTypeInfo.IsSource == false)
            {
                foreach (var comFunctionInfo in comImplementedTypeInfo.ComTypeInfo.Methods)
                {
                    if (list.FirstOrDefault(x => x.Name.Equals(comFunctionInfo.Name)) == null)
                    {
                        list.Add(comFunctionInfo);
                    }
                }

                list.AddRange(GetInheritedMethods(comImplementedTypeInfo.ComTypeInfo));
            }
        }

        list.Sort(delegate (ComFunctionInfo a, ComFunctionInfo b)
        {
            return a.Name.CompareTo(b.Name);
        });

        return list.ToArray();
    }

    private ComPropertyInfo[] GetInheriteProperties(ComTypeInfo comTypeInfo)
    {
        var list = new List<ComPropertyInfo>();

        for (var i = 0; i < comTypeInfo.ImplementedTypes.Length; i++)
        {
            var comImplementedTypeInfo = comTypeInfo.ImplementedTypes[i];

            if (comImplementedTypeInfo.IsSource == false)
            {
                foreach (var comPropertyInfo in comImplementedTypeInfo.ComTypeInfo.Properties)
                {
                    if (list.FirstOrDefault(x => x.Name.Equals(comPropertyInfo.Name)) == null)
                    {
                        list.Add(comPropertyInfo);
                    }
                }

                list.AddRange(GetInheriteProperties(comImplementedTypeInfo.ComTypeInfo));
            }
        }

        list.Sort(delegate (ComPropertyInfo a, ComPropertyInfo b)
        {
            return a.Name.CompareTo(b.Name);
        });

        return list.ToArray();
    }

    public bool IsAlias => _typeAttr.typekind == System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_ALIAS;
    public bool IsCoClass => _typeAttr.typekind == System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_COCLASS;
    public bool IsDispatch => _typeAttr.typekind == System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_DISPATCH;
    public bool IsEnum => _typeAttr.typekind == System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_ENUM;
    public bool IsInterface => _typeAttr.typekind == System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_INTERFACE;
    public bool IsMax => _typeAttr.typekind == System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_MAX;
    public bool IsModule => _typeAttr.typekind == System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_MODULE;
    public bool IsRecord => _typeAttr.typekind == System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_RECORD;
    public bool IsUnion => _typeAttr.typekind == System.Runtime.InteropServices.ComTypes.TYPEKIND.TKIND_UNION;
    public bool IsAppObject => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FAPPOBJECT);
    public bool IsCanCreate => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FCANCREATE);
    public bool IsLicensed => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FLICENSED);
    public bool IsPredeclid => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FPREDECLID);
    public bool IsHidden => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FHIDDEN);
    public bool IsControl => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FCONTROL);
    public bool IsDual => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FDUAL);
    public bool IsNonExtensible => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FNONEXTENSIBLE);
    public bool IsOleAutomation => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FOLEAUTOMATION);
    public bool IsRestricted => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FRESTRICTED);
    public bool IsAggregatable => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FAGGREGATABLE);
    public bool IsReplaceable => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FREPLACEABLE);
    public bool IsDispatchable => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FDISPATCHABLE);
    public bool IsReverseBind => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FREVERSEBIND);
    public bool IsProxy => _typeAttr.wTypeFlags.IsSet(System.Runtime.InteropServices.ComTypes.TYPEFLAGS.TYPEFLAG_FPROXY);

    private void LoadMembers()
    {
        _members = new List<ComMemberInfo>();

        for (short i = 0; i < _typeAttr.cFuncs; i++)
        {
            var pFuncDesc = IntPtr.Zero;

            _typeInfo.GetFuncDesc(i, out pFuncDesc);
            var comFunctionInfo = new ComFunctionInfo(this, pFuncDesc);

            _members.Add(comFunctionInfo);
        }

        /* Note that these are not always enum constants.  Some properties show up as VARDESC's. */
        for (short i = 0; i < _typeAttr.cVars; i++)
        {
            VARDESC varDesc;
            var p = IntPtr.Zero;

            _typeInfo.GetVarDesc(i, out p);
            object constantValue = null;

            try
            {
                varDesc = p.ToStructure<VARDESC>();

                if (varDesc.varkind == VARKIND.VAR_CONST)
                {
                    constantValue = Marshal.GetObjectForNativeVariant(varDesc.desc.lpvarValue);
                }
            }
            finally
            {
                _typeInfo.ReleaseVarDesc(p);
            }

            var comVariableInfo = new ComVariableInfo(this, varDesc, constantValue);
            _members.Add(comVariableInfo);
        }

        _members.Sort(delegate (ComMemberInfo a, ComMemberInfo b)
        {
            return a.Name.CompareTo(b.Name);
        });
    }

    //private void LoadFunctions()
    //{
    //    _functions = new List<ComFunctionInfo>();

    //    for (short i = 0; i < _typeAttr.cFuncs; i++)
    //    {
    //        System.Runtime.InteropServices.ComTypes.FUNCDESC funcDesc = _typeInfo.GetFuncDesc(i);

    //        ComFunctionInfo comMethodInfo = new ComFunctionInfo(this, funcDesc);
    //        _functions.Add(comMethodInfo);
    //    }

    //    _functions.Sort(delegate(ComFunctionInfo a, ComFunctionInfo b)
    //    {
    //        return a.Name.CompareTo(b.Name);
    //    });
    //}

    //private void LoadVariables()
    //{
    //    _variables = new List<ComVariableInfo>();

    //    /* Note that these are not always enum constants.  Some properties show up as VARDESC's. */
    //    for (short i = 0; i < _typeAttr.cVars; i++)
    //    {
    //        System.Runtime.InteropServices.ComTypes.VARDESC varDesc;
    //        IntPtr p = IntPtr.Zero;

    //        _typeInfo.GetVarDesc(i, out p);
    //        object constantValue = null;

    //        try
    //        {
    //            varDesc = p.ToVARDESC();

    //            if (varDesc.varkind == VARKIND.VAR_CONST)
    //            {
    //                constantValue = Marshal.GetObjectForNativeVariant(varDesc.desc.lpvarValue);
    //            }
    //        }
    //        finally
    //        {
    //            _typeInfo.ReleaseTypeAttr(p);
    //        }

    //        ComVariableInfo comVariableInfo = new ComVariableInfo(this, varDesc, constantValue);
    //        _variables.Add(comVariableInfo);
    //    }

    //    _variables.Sort(delegate(ComVariableInfo a, ComVariableInfo b)
    //    {
    //        return a.Name.CompareTo(b.Name);
    //    });
    //}

    public ComTypeLibrary ComTypeLibrary => _comTypeLibrary;
    public string Name => _name;
    public string FullName => string.Format("{0}.{1}", _comTypeLibrary.Name, _name);
    public string Description => _description;

    public ITypeInfo GetITypeInfo() => _typeInfo;

    public Guid Guid => _typeAttr.guid;
    public Version Version => new(_typeAttr.wMajorVerNum, _typeAttr.wMinorVerNum);

    public bool IsCollection
    {
        get
        {
            var newEnum = Members.OfType<ComFunctionInfo>().Where(x => x.DispId == NativeMethods.DISPID_NEWENUM).FirstOrDefault();
            return newEnum == null ? false : true;
        }
    }

    public override string ToString() => FullName;
}

public class ComAliasInfo : ComTypeInfo
{
    public ComAliasInfo(ComTypeLibrary parent, ITypeInfo typeInfo, IntPtr pTypeAttr)
        : base(parent, typeInfo, pTypeAttr)
    {
    }
}

public class ComCoClassInfo : ComTypeInfo
{
    private List<ComFunctionInfo> _events = null;

    public ComCoClassInfo(ComTypeLibrary parent, ITypeInfo typeInfo, IntPtr pTypeAttr)
        : base(parent, typeInfo, pTypeAttr)
    {
    }

    public ComFunctionInfo[] Events
    {
        get
        {
            if (_events == null)
            {
                _events = new List<ComFunctionInfo>();

                for (var i = 0; i < ImplementedTypes.Length; i++)
                {
                    var comImplementedTypeInfo = ImplementedTypes[i];

                    if (comImplementedTypeInfo.IsSource)
                    {
                        foreach (var comFunctionInfo in comImplementedTypeInfo.ComTypeInfo.Methods)
                        {
                            if (comFunctionInfo.IsRestricted == false)
                            {
                                if (_events.FirstOrDefault(x => x.Name.Equals(comFunctionInfo.Name)) == null)
                                {
                                    _events.Add(comFunctionInfo);
                                }
                            }
                        }
                    }
                }

                _events.Sort(delegate (ComFunctionInfo a, ComFunctionInfo b)
                {
                    return a.Name.CompareTo(b.Name);
                });
            }

            return _events.ToArray();
        }
    }
}

public class ComDispatchInfo : ComTypeInfo
{
    public ComDispatchInfo(ComTypeLibrary parent, ITypeInfo typeInfo, IntPtr pTypeAttr)
        : base(parent, typeInfo, pTypeAttr)
    {
    }
}

public class ComEnumInfo : ComTypeInfo
{
    public ComEnumInfo(ComTypeLibrary parent, ITypeInfo typeInfo, IntPtr pTypeAttr)
        : base(parent, typeInfo, pTypeAttr)
    {
    }
}

public class ComInterfaceInfo : ComTypeInfo
{
    public ComInterfaceInfo(ComTypeLibrary parent, ITypeInfo typeInfo, IntPtr pTypeAttr)
        : base(parent, typeInfo, pTypeAttr)
    {
    }
}

public class ComMaxInfo : ComTypeInfo
{
    public ComMaxInfo(ComTypeLibrary parent, ITypeInfo typeInfo, IntPtr pTypeAttr)
        : base(parent, typeInfo, pTypeAttr)
    {
    }
}

public class ComModuleInfo : ComTypeInfo
{
    public ComModuleInfo(ComTypeLibrary parent, ITypeInfo typeInfo, IntPtr pTypeAttr)
        : base(parent, typeInfo, pTypeAttr)
    {
    }
}

public class ComRecordInfo : ComTypeInfo
{
    public ComRecordInfo(ComTypeLibrary parent, ITypeInfo typeInfo, IntPtr pTypeAttr)
        : base(parent, typeInfo, pTypeAttr)
    {
    }
}

public class ComUnionInfo : ComTypeInfo
{
    public ComUnionInfo(ComTypeLibrary parent, ITypeInfo typeInfo, IntPtr pTypeAttr)
        : base(parent, typeInfo, pTypeAttr)
    {
    }
}
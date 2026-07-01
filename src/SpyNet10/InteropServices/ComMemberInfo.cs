using SpyNet10.Extensions;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace SpyNet10.InteropServices;

public abstract class ComMemberInfo
{
    internal ComTypeInfo _comTypeInfo;
    internal string _name = string.Empty;
    internal string _description = string.Empty;
    internal int _helpContext;
    internal string _helpFile = string.Empty;

    public ComMemberInfo(ComTypeInfo comTypeInfo) => _comTypeInfo = comTypeInfo;

    public string Name => _name;
    public string Description => _description;
    public ComTypeInfo ComTypeInfo => _comTypeInfo;

    public override string ToString() => _name;
}

public class ComFunctionInfo : ComMemberInfo
{
    private readonly IntPtr _pFuncDesc = IntPtr.Zero;
    private FUNCDESC _funcDesc;
    private List<ComParameterInfo> _parameters = [];

    public ComFunctionInfo(ComTypeInfo parent, IntPtr pFuncDesc)
        : base(parent)
    {
        _pFuncDesc = pFuncDesc;
        _funcDesc = pFuncDesc.ToStructure<FUNCDESC>();
        _comTypeInfo.GetITypeInfo().GetDocumentation(_funcDesc.memid, out _name, out _description, out _helpContext, out _helpFile);

        if (_description == null) _description = string.Empty;
        if (_helpFile == null) _helpFile = string.Empty;

        LoadParameters();
    }

    public ComParameterInfo ReturnParameter { get; private set; }

    public ComParameterInfo[] Parameters
    {
        get
        {
            if (_parameters == null)
            {
                LoadParameters();
            }

            return [.. _parameters];
        }
    }

    public FUNCFLAGS FunctionFlags => (FUNCFLAGS)_funcDesc.wFuncFlags;

    public int DispId => _funcDesc.memid;
    public INVOKEKIND InvokeKind => _funcDesc.invkind;

    public bool IsBindable => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FBINDABLE);
    public bool IsDefaultBind => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FDEFAULTBIND);
    public bool IsDefaultCollectionElemement => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FDEFAULTCOLLELEM);
    public bool IsDisplayBindable => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FDISPLAYBIND);
    public bool IsHidden => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FHIDDEN);
    public bool IsImmediateBindable => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FIMMEDIATEBIND);
    public bool IsNonBrowsable => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FNONBROWSABLE);
    public bool IsReplaceable => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FREPLACEABLE);
    public bool IsRequestEdit => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FREQUESTEDIT);
    public bool IsRestricted => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FRESTRICTED);
    public bool IsSource => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FSOURCE);
    public bool IsUiDefault => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FUIDEFAULT);
    public bool SupportsGetLastError => FunctionFlags.IsSet(System.Runtime.InteropServices.ComTypes.FUNCFLAGS.FUNCFLAG_FUSESGETLASTERROR);

    private void LoadParameters()
    {
        _parameters = [];

        var rgBstrNames = new string[_funcDesc.cParams + 1];
        var pcNames = 0;
        _comTypeInfo.GetITypeInfo().GetNames(_funcDesc.memid, rgBstrNames, rgBstrNames.Length, out pcNames);

        var pElemDesc = _funcDesc.lprgelemdescParam;

        ReturnParameter = new ComParameterInfo(this, rgBstrNames[0], _funcDesc.elemdescFunc);

        if (_funcDesc.cParams > 0)
        {
            for (var cParams = 0; cParams < _funcDesc.cParams; cParams++)
            {
                var elemDesc = (ELEMDESC)Marshal.PtrToStructure(pElemDesc, typeof(ELEMDESC));
                _parameters.Add(new ComParameterInfo(this, rgBstrNames[cParams + 1], elemDesc));
                pElemDesc = new IntPtr(pElemDesc.ToInt64() + Marshal.SizeOf(typeof(ELEMDESC)));
            }
        }
        else
        {
            //list.Add(new ElemDesc(this, m_funcDesc.elemdescFunc, rgBstrNames[0], -1));
        }

        //m_parameters = list.ToArray();
    }

    public FUNCDESC FuncDesc => this._funcDesc;

    public override string ToString() => Name;

    public string ToString(bool includeParameters)
    {
        if (!includeParameters) return ToString();

        var sb = new StringBuilder();

        sb.Append(_name);

        if (_parameters.Count > 0)
        {
            sb.Append('(');

            foreach (var parameter in _parameters)
            {
                sb.Append(parameter.Name);
                sb.Append(", ");
            }

            sb.Remove(sb.Length - 2, 2);

            sb.Append(')');
        }
        else
        {
            sb.Append("()");
        }

        return sb.ToString();
    }
}

public class ComPropertyInfo : ComMemberInfo
{
    private readonly List<ComFunctionInfo> _functions = [];

    public ComPropertyInfo(ComTypeInfo parent, ComFunctionInfo[] functions)
        : base(parent)
    {
        _functions.AddRange(functions);

        _name = functions[0].Name;
        _description = functions[0].Description;
    }

    public ComFunctionInfo GetFunction => _functions.Where(x => x.InvokeKind == System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYGET).FirstOrDefault();

    public ComFunctionInfo SetFunction => _functions.Where(
                x => x.InvokeKind != System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_FUNC).Where(
                x => x.InvokeKind != System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYGET)
                .FirstOrDefault();

    public bool IsReadOnly => ((GetFunction != null) && (SetFunction == null));

    public bool IsWriteOnly => ((SetFunction != null) && (GetFunction == null));

    public bool IsReadWrite => ((SetFunction != null) && (GetFunction != null));

    public bool GetFunctionHasParameters
    {
        get
        {
            var getComFunctionInfo = GetFunction;

            if (getComFunctionInfo != null)
            {
                return getComFunctionInfo.Parameters.Length > 0;
            }

            return false;
        }
    }
}

public class ComVariableInfo : ComMemberInfo
{
    private VARDESC _varDesc;

    public ComVariableInfo(ComTypeInfo parent, VARDESC varDesc, object constantValue)
        : base(parent)
    {
        _varDesc = varDesc;
        _comTypeInfo.GetITypeInfo().GetDocumentation(_varDesc.memid, out _name, out _description, out _helpContext, out _helpFile);
        ConstantValue = constantValue;
        if (_description == null) _description = string.Empty;
        if (_helpFile == null) _helpFile = string.Empty;
    }

    public object ConstantValue { get; }
    public VARDESC VariableDescription => _varDesc;
    public VARKIND VariableKind => _varDesc.varkind;
    public VARFLAGS VariableFlags => (VARFLAGS)_varDesc.wVarFlags;

    public bool IsBindable => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FBINDABLE);
    public bool IsDefaultBind => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FDEFAULTBIND);
    public bool IsDefaultCollectionElemement => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FDEFAULTCOLLELEM);
    public bool IsDisplayBindable => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FDISPLAYBIND);
    public bool IsHidden => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FHIDDEN);
    public bool IsImmediateBindable => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FIMMEDIATEBIND);
    public bool IsNonBrowsable => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FNONBROWSABLE);
    public bool IsReadOnly => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FREADONLY);
    public bool IsReplaceable => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FREPLACEABLE);
    public bool IsRequestEdit => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FREQUESTEDIT);
    public bool IsRestricted => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FRESTRICTED);
    public bool IsSource => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FSOURCE);
    public bool IsUiDefault => VariableFlags.IsSet(System.Runtime.InteropServices.ComTypes.VARFLAGS.VARFLAG_FUIDEFAULT);
}

public class ComParameterInfo
{
    private ELEMDESC _elemDesc;

    public ComParameterInfo(ComFunctionInfo comFunctionInfo, string name, ELEMDESC elemDesc)
    {
        ComFunctionInfo = comFunctionInfo;
        Name = name;
        _elemDesc = elemDesc;
    }

    public ComFunctionInfo ComFunctionInfo { get; }
    public string Name { get; }
    public ELEMDESC ELEMDESC => _elemDesc;
    public VarEnum VariantType => (VarEnum)_elemDesc.tdesc.vt;

    public bool IsIn => _elemDesc.desc.paramdesc.wParamFlags.IsSet(System.Runtime.InteropServices.ComTypes.PARAMFLAG.PARAMFLAG_FIN);
    public bool IsOut => _elemDesc.desc.paramdesc.wParamFlags.IsSet(System.Runtime.InteropServices.ComTypes.PARAMFLAG.PARAMFLAG_FOUT);
    public bool IsLcid => _elemDesc.desc.paramdesc.wParamFlags.IsSet(System.Runtime.InteropServices.ComTypes.PARAMFLAG.PARAMFLAG_FLCID);
    public bool IsRetval => _elemDesc.desc.paramdesc.wParamFlags.IsSet(System.Runtime.InteropServices.ComTypes.PARAMFLAG.PARAMFLAG_FRETVAL);
    public bool IsOptional => _elemDesc.desc.paramdesc.wParamFlags.IsSet(System.Runtime.InteropServices.ComTypes.PARAMFLAG.PARAMFLAG_FOPT);
    public bool HasDefault => _elemDesc.desc.paramdesc.wParamFlags.IsSet(System.Runtime.InteropServices.ComTypes.PARAMFLAG.PARAMFLAG_FHASDEFAULT);
    public bool HasCustomData => _elemDesc.desc.paramdesc.wParamFlags.IsSet(System.Runtime.InteropServices.ComTypes.PARAMFLAG.PARAMFLAG_FHASCUSTDATA);

    public override string ToString() => Name;
}
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SpyNet10.InteropServices;

public class ComPtr : SafeHandle, ICustomTypeDescriptor
{
    private PropertyDescriptorCollection _propertyDescriptorCollection;

    public ComPtr()
        : base(IntPtr.Zero, true)
    {
    }

    public ComPtr(IntPtr pUnk)
        : this()
    {
        if (!pUnk.Equals(IntPtr.Zero))
        {
            Marshal.AddRef(pUnk);
        }

        this.SetHandle(pUnk);
    }

    public ComPtr(ComPtr p)
        : this()
    {
        if ((p != null) && (!p.IsInvalid))
        {
            this.SetHandle(p.handle);
            Marshal.AddRef(p.handle);
        }
    }

    #region Operators

    public static implicit operator IntPtr(ComPtr p) => p.handle;

    public static implicit operator ComPtr(IntPtr p) => new(p);

    public static bool operator true(ComPtr p)
    {
        if (p == null) return false;
        return !p.IsInvalid;
    }

    public static bool operator false(ComPtr p)
    {
        if (p == null) return true;
        return p.IsInvalid;
    }

    #endregion Operators

    #region Overrides

    public override bool IsInvalid => this.handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            try
            {
                var count = Marshal.Release(this.handle);
            }
            catch
            {
                GlobalExceptionHandler.HandleException();
            }
            finally
            {
                SetHandle(IntPtr.Zero);
            }
        }

        return true;
    }

    public override string ToString() => this.handle.ToString();

    #endregion Overrides

    #region Properties

    public int RefCount
    {
        get
        {
            if (this.IsInvalid) return 0;

            try
            {
                Marshal.AddRef(this.handle);
                return Marshal.Release(this.handle);
            }
            catch
            {
            }

            return 0;
        }
    }

    public bool IsDispatch => Is<IDispatch>();

    #endregion Properties

    #region Methods

    public static ComPtr FromRCW(object rcw)
    {
        var pUnk = IntPtr.Zero;

        try
        {
            pUnk = Marshal.GetIUnknownForObject(rcw);
            var count = Marshal.Release(pUnk);
        }
        catch
        {
        }
        finally
        {
        }

        return new ComPtr(pUnk);
    }

    public bool Is<T>()
    {
        if (this.IsInvalid) return false;

        var riid = typeof(T).GUID;
        var p = IntPtr.Zero;

        try
        {
            if (MarshalEx.Succeeded(Marshal.QueryInterface(this.handle, ref riid, out p)))
            {
                var count = Marshal.Release(p);
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    public object TryGetFirstAvailableProperty(string[] propertyNames)
    {
        object value = null;
        var comType = TryGetComTypeInfo();

        if (comType != null)
        {
            foreach (var propertyName in propertyNames)
            {
                var comPropertyInfo = comType.Properties.Where(x => x.Name.Equals(propertyName)).FirstOrDefault();

                if ((comPropertyInfo != null) && (comPropertyInfo.GetFunction != null))
                {
                    if (MarshalEx.Succeeded(TryInvokePropertyGet(comPropertyInfo.GetFunction.DispId, out value)))
                    {
                        break;
                    }
                }
            }
        }

        return value;
    }

    public int TryGetItemCount()
    {
        IDispatch dispatch = null;
        object count = 0;

        try
        {
            dispatch = TryGetUniqueRCW<IDispatch>();
            if (MarshalEx.Succeeded(dispatch.InvokePropertyGet("Count", out count)))
            {
                return count is int ? (int)count : -1;
            }
        }
        catch
        {
        }
        finally
        {
            CleanupUniqueRCW(dispatch);
        }

        return -1;
    }

    public ComTypeInfo TryGetComTypeInfo()
    {
        ComTypeInfo comTypeInfo = null;
        IDispatch dispatch = null;

        try
        {
            dispatch = TryGetUniqueRCW<IDispatch>();

            if (dispatch != null)
            {
                comTypeInfo = ComTypeManager.Instance.FromIDispatch(dispatch);
            }
        }
        catch
        {
        }
        finally
        {
            ComPtr.CleanupUniqueRCW(dispatch);
        }

        return comTypeInfo;
    }

    public System.Runtime.InteropServices.ComTypes.ITypeInfo TryGetTypeInfo()
    {
        System.Runtime.InteropServices.ComTypes.ITypeInfo typeInfo = null;

        IDispatch dispatch = null;

        try
        {
            dispatch = TryGetUniqueRCW<IDispatch>();

            if (dispatch != null)
            {
                typeInfo = dispatch.GetTypeInfo();
            }
        }
        catch
        {
        }
        finally
        {
            ComPtr.CleanupUniqueRCW(dispatch);
        }

        return typeInfo;
    }

    public object TryGetRCW()
    {
        if (!IsInvalid)
        {
            return Marshal.GetObjectForIUnknown(this.handle);
        }

        return null;
    }

    public object TryGetUniqueRCW()
    {
        if (!IsInvalid)
        {
            return Marshal.GetUniqueObjectForIUnknown(this.handle);
        }

        return null;
    }

    public T TryGetUniqueRCW<T>()
    {
        object rcw = null;

        try
        {
            if (Is<T>())
            {
                rcw = TryGetUniqueRCW();
            }
        }
        catch
        {
        }

        return (T)rcw;
    }

    public int TryInvokeMethod(int dispId, object[] args, out object returnValue)
    {
        var hr = NativeMethods.S_OK;
        IDispatch dispatch = null;
        returnValue = null;

        try
        {
            if (IsDispatch)
            {
                dispatch = TryGetUniqueRCW<IDispatch>();

                if (dispatch != null)
                {
                    if (MarshalEx.Succeeded(hr = dispatch.InvokeMethod(dispId, args, out returnValue)))
                    {
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            ComPtr.CleanupUniqueRCW(dispatch);
        }

        return hr;
    }

    public int TryInvokeMethod(string name, object[] args, out object returnValue)
    {
        var hr = NativeMethods.S_OK;
        IDispatch dispatch = null;
        returnValue = null;

        try
        {
            if (IsDispatch)
            {
                dispatch = TryGetUniqueRCW<IDispatch>();

                if (dispatch != null)
                {
                    if (MarshalEx.Succeeded(hr = dispatch.InvokeMethod(name, args, out returnValue)))
                    {
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            ComPtr.CleanupUniqueRCW(dispatch);
        }

        return hr;
    }

    public int TryInvokePropertyGet(int dispId, out object value)
    {
        var hr = NativeMethods.S_OK;
        IDispatch dispatch = null;
        value = null;

        try
        {
            if (IsDispatch)
            {
                dispatch = TryGetUniqueRCW<IDispatch>();

                if (dispatch != null)
                {
                    if (MarshalEx.Succeeded(hr = dispatch.InvokePropertyGet(dispId, out value)))
                    {
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            ComPtr.CleanupUniqueRCW(dispatch);
        }

        return hr;
    }

    public int TryInvokePropertyGet(string propertyName, out object value)
    {
        var hr = NativeMethods.S_OK;
        IDispatch dispatch = null;
        value = null;

        try
        {
            if (IsDispatch)
            {
                dispatch = TryGetUniqueRCW<IDispatch>();

                if (dispatch != null)
                {
                    if (MarshalEx.Succeeded(hr = dispatch.InvokePropertyGet(propertyName, out value)))
                    {
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            ComPtr.CleanupUniqueRCW(dispatch);
        }

        return hr;
    }

    public int TryInvokePropertySet(string propertyName, object value)
    {
        var hr = NativeMethods.S_OK;
        IDispatch dispatch = null;

        try
        {
            if (IsDispatch)
            {
                dispatch = TryGetUniqueRCW<IDispatch>();

                if (dispatch != null)
                {
                    if (MarshalEx.Succeeded(hr = dispatch.InvokePropertySet(propertyName, value)))
                    {
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            ComPtr.CleanupUniqueRCW(dispatch);
        }

        return hr;
    }

    public bool TryIsCollection()
    {
        var comTypeInfo = TryGetComTypeInfo();

        if (comTypeInfo != null)
        {
            return comTypeInfo.IsCollection;
        }

        return false;
    }

    public static void CleanupUniqueRCW(object rcw)
    {
        try
        {
            if (rcw != null)
            {
                var count = Marshal.FinalReleaseComObject(rcw);
            }
        }
        catch
        {
        }
    }

    #endregion Methods

    #region "TypeDescriptor Implementation"

    /// <summary>
    /// Get Class Name
    /// </summary>
    /// <returns>String</returns>
    public string GetClassName() => TypeDescriptor.GetClassName(this, true);

    /// <summary>
    /// GetAttributes
    /// </summary>
    /// <returns>AttributeCollection</returns>
    public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);

    /// <summary>
    /// GetComponentName
    /// </summary>
    /// <returns>String</returns>
    public string GetComponentName() => TypeDescriptor.GetComponentName(this, true);

    /// <summary>
    /// GetConverter
    /// </summary>
    /// <returns>TypeConverter</returns>
    public TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);

    /// <summary>
    /// GetDefaultEvent
    /// </summary>
    /// <returns>EventDescriptor</returns>
    public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);

    /// <summary>
    /// GetDefaultProperty
    /// </summary>
    /// <returns>PropertyDescriptor</returns>
    public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);

    /// <summary>
    /// GetEditor
    /// </summary>
    /// <param name="editorBaseType">editorBaseType</param>
    /// <returns>object</returns>
    public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);

    public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);

    public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);

    public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
    {
        if (_propertyDescriptorCollection == null)
        {
            _propertyDescriptorCollection = new PropertyDescriptorCollection([], true);

            var comTypeInfo = TryGetComTypeInfo();

            if (comTypeInfo != null)
            {
                var list = new List<ComPtrPropertyDescriptor>();

                foreach (var comPropertyInfo in comTypeInfo.GetProperties(true))
                {
                    var getComFunctionInfo = comPropertyInfo.GetFunction;
                    var bReadOnly = comPropertyInfo.SetFunction == null ? true : false;

                    if (getComFunctionInfo != null)
                    {
                        var variantType = getComFunctionInfo.ReturnParameter.VariantType;

                        switch (variantType)
                        {
                            case VarEnum.VT_PTR:
                            case VarEnum.VT_DISPATCH:
                            case VarEnum.VT_UNKNOWN:
                                continue;
                            case VarEnum.VT_SAFEARRAY:
                                continue;
                        }

                        // Special case. MailSession is a PITA property that causes modal dialog.
                        if (comPropertyInfo.Name.Equals("MailSession"))
                        {
                            var comPtrProperty = new ComPtrProperty(comPropertyInfo.Name, comPropertyInfo.Description, 0, typeof(int), variantType, true);
                            list.Add(new ComPtrPropertyDescriptor(ref comPtrProperty, comPropertyInfo, attributes));
                            continue;
                        }

                        object value = null;
                        if (MarshalEx.Succeeded(TryInvokePropertyGet(getComFunctionInfo.DispId, out value)))
                        {
                            var propertyType = typeof(object);

                            if (value != null)
                            {
                                propertyType = value.GetType();
                            }
                            else
                            {
                                bReadOnly = true;
                            }

                            var comPtrProperty = new ComPtrProperty(comPropertyInfo.Name, comPropertyInfo.Description, value, propertyType, variantType, bReadOnly);
                            list.Add(new ComPtrPropertyDescriptor(ref comPtrProperty, comPropertyInfo, attributes));
                        }
                    }
                }

#if DEBUG
                var refCountProperty = new ComPtrProperty("[RefCount]", "", this.RefCount, typeof(int), VarEnum.VT_I4, true);
                list.Add(new ComPtrPropertyDescriptor(ref refCountProperty, null, attributes));
#endif
                _propertyDescriptorCollection = new PropertyDescriptorCollection(list.ToArray());
            }
        }

        return _propertyDescriptorCollection;
    }

    public PropertyDescriptorCollection GetProperties() => TypeDescriptor.GetProperties(this, true);

    public object GetPropertyOwner(PropertyDescriptor pd) => this;

    #endregion "TypeDescriptor Implementation"
}

public class ComPtrProperty
{
    public ComPtrProperty(string sName, string description, object value, Type type, VarEnum variantType, bool bReadOnly)
    {
        Name = sName;
        Description = description;
        Value = value;
        Type = type;
        VariantType = variantType;
        ReadOnly = bReadOnly;
    }

    public string Name { get; } = string.Empty;
    public string Description { get; } = string.Empty;
    public Type Type { get; }
    public VarEnum VariantType { get; }
    public bool ReadOnly { get; }

    public object Value { get; set; }
}

public class ComPtrPropertyDescriptor : PropertyDescriptor
{
    private readonly ComPtrProperty _comPtrProperty;

    public ComPtrPropertyDescriptor(ref ComPtrProperty comPtrProperty, ComPropertyInfo comPropertyInfo, Attribute[] attrs)
        : base(comPtrProperty.Name, attrs)
    {
        _comPtrProperty = comPtrProperty;
        ComPropertyInfo = comPropertyInfo;
    }

    #region PropertyDescriptor specific

    public override bool CanResetValue(object component) => false;

    public override Type ComponentType => null;

    public override object GetValue(object component)
    {
#if DEBUG
        if (ComPropertyInfo == null)
        {
            if (_comPtrProperty.Name.Equals("[RefCount]"))
            {
                return ((ComPtr)component).RefCount;
            }
        }
#endif

        try
        {
            if (_comPtrProperty.Name.Equals("MailSession")) return _comPtrProperty.Value;
        }
        catch
        {
        }


        if (component is ComPtr p)
        {
            object value = null;
            if (MarshalEx.Succeeded(p.TryInvokePropertyGet(_comPtrProperty.Name, out value)))
            {
                return value;
            }
        }

        return _comPtrProperty.Value;
    }

    public override string Description => _comPtrProperty.Description;

    public override string Category => string.Empty;

    public override string DisplayName => _comPtrProperty.Name;

    public override bool IsReadOnly => _comPtrProperty.ReadOnly;

    public override void ResetValue(object component)
    { }

    public override bool ShouldSerializeValue(object component) => false;

    public override void SetValue(object component, object value)
    {
        if (component is ComPtr p)
        {
            if (MarshalEx.Succeeded(p.TryInvokePropertySet(_comPtrProperty.Name, value)))
            {
                _comPtrProperty.Value = value;
            }
        }
    }

    public override Type PropertyType => _comPtrProperty.Type;

    #endregion PropertyDescriptor specific

    public ComPropertyInfo ComPropertyInfo { get; }
    public VarEnum VariantType => _comPtrProperty.VariantType;

    public override string ToString() => string.Format("{0} [{1}]", _comPtrProperty.Name, _comPtrProperty.VariantType);
}
using SpyNet10.InteropServices;
using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;

namespace SpyNet10.Forms;

public partial class GlobalParameterBrowser : UserControl
{
    public GlobalParameterBrowser() => InitializeComponent();

    private void buttonRefresh_Click(object sender, EventArgs e)
    {
        Cursor.Current = Cursors.WaitCursor;

        RefreshGlobalParameters();

        Cursor.Current = Cursors.Default;
    }

    private void textBoxSearch_TextAccepted(object sender, EventArgs e) => buttonRefresh_Click(sender, e);

    public void RefreshGlobalParameters()
    {
        ComPtr pApplication = IntPtr.Zero;

        if (MarshalEx.Succeeded(MarshalEx.GetActiveObject("SolidEdge.Application", out pApplication)))
        {
            SelectedObject = new GlobalParameterInfo(pApplication, this.textBoxSearch.Text);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object SelectedObject
    {
        get => propertyGrid.SelectedObject;
        set
        {
            var globalParameterInfo = propertyGrid.SelectedObject as GlobalParameterInfo;

            globalParameterInfo?.Dispose();

            propertyGrid.SelectedObject = value;
        }
    }
}

public class GlobalParameterInfo : ICustomTypeDescriptor, IDisposable
{
    private readonly ComPtr _pApplication = IntPtr.Zero;
    private readonly List<SolidEdgeFramework.ApplicationGlobalConstants> _colorGlobalConstants = [];
    private readonly string _filter;

    public GlobalParameterInfo(ComPtr pApplication, string filter)
    {
        _pApplication = pApplication;
        _filter = filter;

        try
        {
            var type = typeof(SolidEdgeFramework.ApplicationGlobalConstants);
            var enumNames = type.GetEnumNames();
            var enumValues = type.GetEnumValues();

            // Build list of global constants that represent color using the constant name.
            for (var i = 0; i < enumNames.Length; i++)
            {
                if (enumNames[i].Contains("Color"))
                {
                    _colorGlobalConstants.Add((SolidEdgeFramework.ApplicationGlobalConstants)enumValues.GetValue(i));
                }
            }

            // These don't have "Color" in their name but are colors. Add manually to list.
            _colorGlobalConstants.Add(SolidEdgeFramework.ApplicationGlobalConstants.seApplicationGlobalInside);
            _colorGlobalConstants.Add(SolidEdgeFramework.ApplicationGlobalConstants.seApplicationGlobalInsideOutside);
        }
        catch
        {
        }
    }

    #region "TypeDescriptor Implementation"

    public string GetClassName() => TypeDescriptor.GetClassName(this, true);

    public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);

    public string GetComponentName() => TypeDescriptor.GetComponentName(this, true);

    public TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);

    public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);

    public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);

    public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);

    public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);

    public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);

    public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
    {
        if ((_pApplication == null) || (_pApplication.IsInvalid)) return new PropertyDescriptorCollection([]);

        var list = new List<GlobalParameterPropertyDescriptor>();

        try
        {
            var comTypeLibrary = ComTypeManager.Instance.ComTypeLibraries.Where(x => x.Name.Equals("SolidEdgeFramework")).FirstOrDefault();

            if (comTypeLibrary != null)
            {
                var enumInfo = comTypeLibrary.Enums.Where(x => x.Name.Equals("ApplicationGlobalConstants")).FirstOrDefault();

                foreach (var variableInfo in enumInfo.Variables)
                {
                    if (!string.IsNullOrEmpty(_filter))
                    {
                        if (variableInfo.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) == -1)
                        {
                            continue;
                        }
                    }

                    var globalConst = (SolidEdgeFramework.ApplicationGlobalConstants)variableInfo.ConstantValue;

                    // There is a known bug where seApplicationGlobalOpenAsReadOnly3DFile causes SE to display the read-only icon on
                    // files after GetGlobalParameter() is called.
                    if (globalConst.Equals(SolidEdgeFramework.ApplicationGlobalConstants.seApplicationGlobalOpenAsReadOnly3DFile))
                    {
                        continue;
                    }

                    try
                    {
                        object[] args = [globalConst, new VariantWrapper(null)];
                        object returnValue = null;

                        if (MarshalEx.Succeeded(_pApplication.TryInvokeMethod("GetGlobalParameter", args, out returnValue)))
                        {
                            if (args[1] != null)
                            {
                                var propertyType = args[1].GetType();

                                var name = variableInfo.Name.Replace("seApplicationGlobal", string.Empty);
                                var description = new StringBuilder();
                                description.AppendLine(variableInfo.Description);
                                description.AppendLine(string.Format("Application.GetGlobalParameter({0}.{1}, out value)", enumInfo.FullName, variableInfo.Name));

                                var property = new GlobalParameterProperty(name, description.ToString(), args[1], propertyType, true);

                                list.Add(new GlobalParameterPropertyDescriptor(ref property, attributes));

                                try
                                {
                                    if (_colorGlobalConstants.Contains(globalConst))
                                    {
                                        var color = Color.Empty;

                                        if (args[1] is int)
                                        {
                                            var rgb = BitConverter.GetBytes((int)args[1]);
                                            color = Color.FromArgb(255, rgb[0], rgb[1], rgb[2]);
                                        }
                                        else if (args[1] is uint)
                                        {
                                            var rgb = BitConverter.GetBytes((uint)args[1]);
                                            color = Color.FromArgb(255, rgb[0], rgb[1], rgb[2]);
                                        }
                                        else
                                        {
#if DEBUG
                                            //System.Diagnostics.Debugger.Break();
#endif
                                        }

                                        if (!color.IsEmpty)
                                        {
                                            description = new StringBuilder();
                                            description.AppendLine(property.Description);
                                            description.AppendLine("byte[] rgb = BitConverter.GetBytes((int)value)");
                                            description.AppendLine("Color color = Color.FromArgb(255, rgb[0], rgb[1], rgb[2]");

                                            property = new GlobalParameterProperty(string.Format("{0} (converted to color)", property.Name), description.ToString(), color, color.GetType(), true);

                                            list.Add(new GlobalParameterPropertyDescriptor(ref property, attributes));
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                    catch
                    {
                        GlobalExceptionHandler.HandleException();
                    }
                }
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }

        return new PropertyDescriptorCollection(list.ToArray());
    }

    public PropertyDescriptorCollection GetProperties() => TypeDescriptor.GetProperties(this, true);

    public object GetPropertyOwner(PropertyDescriptor pd) => this;

    #endregion "TypeDescriptor Implementation"

    public void Dispose()
    {
        if (_pApplication != null)
        {
            _pApplication.Dispose();
        }
    }
}

public class GlobalParameterProperty
{
    public GlobalParameterProperty(string sName, string description, object value, Type type, bool bReadOnly)
    {
        Name = sName;
        Description = description;
        Value = value;
        Type = type;
        ReadOnly = bReadOnly;
    }

    public Type Type { get; }

    public bool ReadOnly { get; }

    public string Name { get; } = string.Empty;
    public string Description { get; } = string.Empty;

    public object Value { get; set; }
}

public class GlobalParameterPropertyDescriptor : PropertyDescriptor
{
    private readonly GlobalParameterProperty _property;

    public GlobalParameterPropertyDescriptor(ref GlobalParameterProperty property, Attribute[] attrs)
        : base(property.Name, attrs) => _property = property;

    #region PropertyDescriptor specific

    public override bool CanResetValue(object component) => false;

    public override Type ComponentType => null;

    public override object GetValue(object component) => _property.Value;

    public override string Description => _property.Description;

    public override string Category => string.Empty;

    public override string DisplayName => _property.Name;

    public override bool IsReadOnly => _property.ReadOnly;

    public override void ResetValue(object component)
    {
    }

    public override bool ShouldSerializeValue(object component) => false;

    public override void SetValue(object component, object value) => _property.Value = value;

    public override Type PropertyType => _property.Type;

    #endregion PropertyDescriptor specific
}
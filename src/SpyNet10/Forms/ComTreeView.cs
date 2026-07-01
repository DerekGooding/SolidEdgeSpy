using SpyNet10.InteropServices;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;

namespace SpyNet10.Forms;

public class ComTreeView : TreeView
{
    private const int TV_FIRST = 0x1100;
    private const int TVM_SETEXTENDEDSTYLE = TV_FIRST + 44;
    private const int TVS_EX_DOUBLEBUFFER = 0x0004;
    private const int NODE_STRING_PADDING = 10;

    public const int ObjectImageIndex = 0;
    public const int NullObjectImageIndex = 1;
    public const int PropertyImageIndex = 2;
    public const int MethodImageIndex = 3;
    public const int CollectionImageIndex = 4;
    public const int NullCollectionImageIndex = 5;
    public const int ClosedFolderImageIndex = 6;
    public const int OpenFolderImageIndex = 7;
    public const int EventImageIndex = 8;

    internal VisualStyleRenderer OpenedRenderer = null;
    internal VisualStyleRenderer ClosedRenderer = null;
    internal VisualStyleRenderer ItemHoverRenderer = null;
    internal VisualStyleRenderer ItemSelectedRenderer = null;
    internal VisualStyleRenderer LostFocusSelectedRenderer = null;

    private bool _showNullObjects = true;
    private bool _showEmptyCollections = true;
    private bool _showProperties = false;
    private bool _showMethods = false;

    public ComTreeView()
        : base()
    {
        // Enable default double buffering processing
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

        DrawMode = TreeViewDrawMode.OwnerDrawAll;
        ShowLines = false;
        FullRowSelect = true;
        ItemHeight = 20;
        HotTracking = true;

        SetupImageList();
        SetupVisualStyleRenderers();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        var Style = 0;

        if (DoubleBuffered)
        {
            Style |= TVS_EX_DOUBLEBUFFER;
            NativeMethods.SendMessage(Handle, TVM_SETEXTENDEDSTYLE, (IntPtr)TVS_EX_DOUBLEBUFFER, (IntPtr)Style);
        }
    }

    protected override void OnAfterCollapse(TreeViewEventArgs e)
    {
        base.OnAfterCollapse(e);

        CleanupAndRemoveNodes(e.Node.Nodes);
        e.Node.Nodes.Add("...");

        Cursor.Current = Cursors.Default;
    }

    protected override void OnAfterExpand(TreeViewEventArgs e)
    {
        base.OnAfterExpand(e);

        Cursor.Current = Cursors.Default;
    }

    protected override void OnBeforeCollapse(TreeViewCancelEventArgs e)
    {
        Cursor.Current = Cursors.WaitCursor;

        base.OnBeforeCollapse(e);

        if (e.Node.ImageIndex == OpenFolderImageIndex)
        {
            e.Node.ImageIndex = ClosedFolderImageIndex;
            e.Node.SelectedImageIndex = e.Node.ImageIndex;
        }
    }

    protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
    {
        Cursor.Current = Cursors.WaitCursor;

        try
        {
            if (e.Node.ImageIndex == ClosedFolderImageIndex)
            {
                e.Node.ImageIndex = OpenFolderImageIndex;
                e.Node.SelectedImageIndex = e.Node.ImageIndex;
            }

            var comPtrTreeNode = e.Node as ComPtrTreeNode;

            if (comPtrTreeNode != null)
            {
                var childNodes = GetChildren(comPtrTreeNode);
                CleanupAndRemoveNodes(comPtrTreeNode.Nodes);
                comPtrTreeNode.Nodes.AddRange(childNodes);
            }
        }
        catch
        {
        }

        base.OnBeforeExpand(e);
    }

    //protected override void OnMouseUp(MouseEventArgs e)
    //{
    //    base.OnMouseUp(e);

    //    try
    //    {
    //        if (e.Button == System.Windows.Forms.MouseButtons.Right)
    //        {
    //            TreeViewHitTestInfo hitTestInfo = this.HitTest(e.Location);
    //            HandleRightClick(hitTestInfo.Node, e.Location);
    //        }
    //    }
    //    catch
    //    {
    //    }
    //}

    protected override void OnNodeMouseClick(TreeNodeMouseClickEventArgs e) => SelectedNode = e.Node;

    protected override void OnDrawNode(DrawTreeNodeEventArgs e)
    {
        if (e.Node.IsVisible == false) return;

        var node = e.Node as ComTreeNode;

        if (node == null) return;

        var baseFont = Font;
        var captionFontStyle = FontStyle.Regular;
        var captionColor = ForeColor;
        var caption2Color = Color.Green;
        var textFormatFlags = TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;

        var rectNode = node.Bounds;
        var rectExpander = new Rectangle(Indent * node.Level, rectNode.Y, ItemHeight, ItemHeight);
        var rectImage = new Rectangle(rectExpander.X + rectExpander.Width, rectNode.Y, ItemHeight, ItemHeight);

        rectImage.X = rectNode.X - rectImage.Width;
        rectExpander.X = rectNode.X - rectImage.Width - rectExpander.Width;

        DrawNodeExpander(node, e.Graphics, rectExpander);

        var rectSelected = rectImage;
        rectSelected.Width = e.Bounds.Width - rectImage.X;

        DrawNodeRectangle(e, rectSelected);

        Image nodeImage = null;

        if (ImageList != null)
        {
            var imageIndex = node.ImageIndex;
            if ((imageIndex == -1) && (ImageList.Images.Count > 0))
            {
                imageIndex = 0;
            }

            if (imageIndex >= 0)
            {
                nodeImage = ImageList.Images[imageIndex];
            }
        }

        if (nodeImage != null)
        {
            var z = ItemHeight - rectImage.Height;
            e.Graphics.DrawImage(nodeImage, rectImage.X + 2, rectImage.Y + 2);
        }

        var comPtrTreeNode = node as ComPtrTreeNode;

        if (comPtrTreeNode != null)
        {
            if (node is ComPtrTreeNode)
            {
                if (comPtrTreeNode.GetFunctionHasParameters)
                {
                    captionColor = Color.DarkGray;
                }
            }

            if ((comPtrTreeNode != null) && (comPtrTreeNode.CollectionCount > 0))
            {
                captionFontStyle = FontStyle.Bold;
            }
        }

        var rectCaption = rectNode;

        using (var captionFont = new System.Drawing.Font(baseFont, captionFontStyle))
        {
            rectCaption.Width = TextRenderer.MeasureText(node.Caption, captionFont).Width;
            TextRenderer.DrawText(e.Graphics, node.Caption, captionFont, rectCaption, captionColor, textFormatFlags);

            //Rectangle rectCaption2 = rectCaption;

            if (!string.IsNullOrWhiteSpace(node.Value))
            {
                var value = string.Format("[{0}]", node.Value);
                rectCaption.X += rectCaption.Width;
                rectCaption.Width = TextRenderer.MeasureText(value, captionFont).Width;
                TextRenderer.DrawText(e.Graphics, value, captionFont, rectCaption, captionColor, textFormatFlags);
            }
        }

        if (!string.IsNullOrWhiteSpace(node.TypeFullName))
        {
            rectCaption.X += rectCaption.Width;
            rectCaption.Width = TextRenderer.MeasureText(node.TypeFullName, baseFont).Width;
            TextRenderer.DrawText(e.Graphics, node.TypeFullName, baseFont, rectCaption, caption2Color, textFormatFlags);
        }
    }

    //private void HandleRightClick(TreeNode node, Point p)
    //{
    //    if (node == null) return;

    //    if (node is ComPtrTreeNode)
    //    {
    //        HandleRightClick((ComPtrTreeNode)node, p);
    //    }
    //}

    //private void HandleRightClick(ComPtrTreeNode comPtrTreeNode, Point p)
    //{
    //    if (comPtrTreeNode == null) return;
    //    if (comPtrTreeNode.ComFunctionInfo == null) return;

    //    ComFunctionInfo getFunction = comPtrTreeNode.ComFunctionInfo;

    //    if (comPtrTreeNode.ComPtr.IsInvalid)
    //    {
    //        if (getFunction.Parameters.Length == 1)
    //        {
    //            ComParameterInfo returnParameter = getFunction.ReturnParameter;
    //            ComParameterInfo firstParameter = getFunction.Parameters[0];

    //            ComTypeInfo cti = ComTypeManager.Instance.LookupUserDefined(firstParameter.ELEMDESC.tdesc, getFunction.ComTypeInfo);

    //            if ((cti != null) && (cti.IsEnum))
    //            {
    //                ContextMenu menu = new ContextMenu();
    //                MenuItem invokeMenuItem = new MenuItem("Invoke");

    //                foreach (ComVariableInfo comVariableInfo in cti.Variables)
    //                {
    //                    MenuItem menuItem = new MenuItem(comVariableInfo.Name);
    //                    menuItem.Click += menuItem_Click;
    //                    menuItem.Tag = new object[] { comPtrTreeNode, comVariableInfo };
    //                    invokeMenuItem.MenuItems.Add(menuItem);
    //                }

    //                menu.MenuItems.Add(invokeMenuItem);
    //                menu.Show(this, p);
    //            }
    //        }
    //    }
    //}

    //void menuItem_Click(object sender, EventArgs e)
    //{
    //    MenuItem menuItem = sender as MenuItem;
    //    object[] args = menuItem.Tag as object[];

    //    if (args.Length == 2)
    //    {
    //        ComPtrTreeNode comPtrTreeNode = args[0] as ComPtrTreeNode;
    //        ComVariableInfo comVariableInfo = args[1] as ComVariableInfo;

    //        if ((comPtrTreeNode != null) && (comVariableInfo != null))
    //        {
    //        }
    //    }
    //}

    private void DrawNodeExpander(ComTreeNode node, Graphics graphics, Rectangle rect)
    {
        if (node.IsExpanded)
        {
            graphics.DrawImage(Resources.Collapse_16x16, rect.X + 2, rect.Y + 2);
        }
        else
        {
            if (node.Nodes.Count > 0)
            {
                graphics.DrawImage(Resources.Expand_16x16, rect.X + 2, rect.Y + 2);
            }
        }
    }

    private void DrawNodeRectangle(DrawTreeNodeEventArgs e, Rectangle rect)
    {
        var fillColor = Color.Empty;
        var borderColor = Color.Empty;

        if ((e.State & TreeNodeStates.Selected) != 0)
        {
            if (Focused)
            {
                if (ItemSelectedRenderer != null)
                {
                    ItemSelectedRenderer.DrawBackground(e.Graphics, rect);
                }
                else
                {
                    fillColor = Color.FromArgb(203, 232, 246);
                    borderColor = Color.FromArgb(38, 160, 218);

                    e.Graphics.FillRectangle(new SolidBrush(fillColor), rect);
                    ControlPaint.DrawFocusRectangle(e.Graphics, rect);
                }
            }
            else
            {
                if (LostFocusSelectedRenderer != null)
                {
                    LostFocusSelectedRenderer.DrawBackground(e.Graphics, rect);
                }
                else
                {
                    fillColor = Color.FromArgb(247, 247, 247);
                    borderColor = Color.FromArgb(222, 222, 222);

                    e.Graphics.FillRectangle(new SolidBrush(fillColor), rect);
                    ControlPaint.DrawFocusRectangle(e.Graphics, rect);
                }
            }
        }
        else if ((e.State & TreeNodeStates.Hot) != 0)
        {
            if (ItemHoverRenderer != null)
            {
                ItemHoverRenderer.DrawBackground(e.Graphics, rect);
            }
            else
            {
                fillColor = Color.FromArgb(229, 243, 251);
                borderColor = Color.FromArgb(112, 192, 231);

                e.Graphics.FillRectangle(new SolidBrush(fillColor), rect);
                ControlPaint.DrawFocusRectangle(e.Graphics, rect);
            }
        }
    }

    private ComTreeNode[] GetChildren(ComPtrTreeNode node)
    {
        if (node == null) return [];

        ComTreeNode[] childNodes = [];

        try
        {
            childNodes = GetChildren(node.ComPtr);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }

        var filteredComTreeNodes = new List<ComTreeNode>();

        foreach (var childNode in childNodes)
        {
            var comPtrTreeNode = childNode as ComPtrTreeNode;
            var comPropertyTreeNode = childNode as ComPropertyTreeNode;

            if (comPtrTreeNode != null)
            {
                if ((comPtrTreeNode.ComPtr.IsInvalid) && (_showNullObjects == false))
                {
                    continue;
                }

                if (comPtrTreeNode.IsCollection)
                {
                    if ((comPtrTreeNode.IsEmptyCollection) && (_showEmptyCollections == false))
                    {
                        continue;
                    }
                }
            }
            else if (comPropertyTreeNode != null)
            {
                if (_showProperties == false)
                {
                    continue;
                }
            }

            filteredComTreeNodes.Add(childNode);
        }

        SetImageIndex(filteredComTreeNodes.ToArray());

        return filteredComTreeNodes.ToArray();
    }

    private ComTreeNode[] GetChildren(ComPtr comPtr)
    {
        if (comPtr == null) return [];

        var comTypeInfo = comPtr.TryGetComTypeInfo();

        if (comTypeInfo == null) return [];

        var childNodes = new List<ComTreeNode>();

        try
        {
            foreach (var comPropertyInfo in comTypeInfo.Properties)
            {
                // Special case. MailSession is a PITA property that causes modal dialog.
                if (comPropertyInfo.Name.Equals("MailSession"))
                {
                    continue;
                }

                var comTreeNode = GetChild(comPtr, comPropertyInfo);

                if (comTreeNode != null)
                {
                    if ((comTreeNode is ComPropertyTreeNode) && (_showProperties == false))
                    {
                        continue;
                    }

                    childNodes.Add(comTreeNode);
                }
            }

            if (comPtr.TryIsCollection())
            {
                var collectionChildNodes = new List<ComTreeNode>();
                var count = comPtr.TryGetItemCount();
                var foundCount = 0;

                try
                {
                    var comFunctionInfo = comTypeInfo.Methods.Where(x => x.Name.Equals("Item", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    if (comFunctionInfo != null)
                    {
                        object returnValue = null;

                        // Solid Edge is supposed to be 1 based index.
                        for (var i = 1; i <= count; i++)
                        {
                            returnValue = null;
                            if (MarshalEx.Succeeded(comPtr.TryInvokeMethod("Item", [i], out returnValue)))
                            {
                                var pItem = returnValue as ComPtr;
                                if ((pItem != null) && (pItem.IsInvalid == false))
                                {
                                    var comPtrItemTreeNode = new ComPtrItemTreeNode((ComPtr)returnValue, comFunctionInfo);
                                    comPtrItemTreeNode.Caption = string.Format("{0}({1})", comFunctionInfo.Name, i);
                                    comPtrItemTreeNode.Nodes.Add("...");
                                    collectionChildNodes.Add(comPtrItemTreeNode);
                                    foundCount++;
                                }
                            }
                        }

                        try
                        {
                            // Some collections are 0 based.
                            // Application->Customization->RibbonBarThemes seems to be 0 based.
                            if (foundCount == (count - 1))
                            {
                                returnValue = null;
                                if (MarshalEx.Succeeded(comPtr.TryInvokeMethod("Item", [0], out returnValue)))
                                {
                                    var pItem = returnValue as ComPtr;
                                    if ((pItem != null) && (pItem.IsInvalid == false))
                                    {
                                        var comPtrItemTreeNode = new ComPtrItemTreeNode((ComPtr)returnValue, comFunctionInfo);
                                        comPtrItemTreeNode.Caption = string.Format("{0}({1})", comFunctionInfo.Name, 0);
                                        comPtrItemTreeNode.Nodes.Add("...");
                                        collectionChildNodes.Insert(0, comPtrItemTreeNode);
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    GlobalExceptionHandler.HandleException();
                }

                childNodes.AddRange(collectionChildNodes.ToArray());
            }

            if (_showMethods)
            {
                foreach (var comFunctionInfo in comTypeInfo.GetMethods(true))
                {
                    if (comFunctionInfo.IsRestricted) continue;

                    var comMethodTreeNode = new ComMethodTreeNode(comFunctionInfo);
                    childNodes.Add(comMethodTreeNode);
                }
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }

        return childNodes.ToArray();
    }

    private ComTreeNode GetChild(ComPtr comPtr, ComPropertyInfo comPropertyInfo)
    {
        if (comPtr == null) return null;
        if (comPropertyInfo == null) return null;
        if (comPtr.IsInvalid) return null;

        var getFunctionInfo = comPropertyInfo.GetFunction;

        if (getFunctionInfo == null) return null;
        if (getFunctionInfo.IsRestricted) return null;

        ComTreeNode comTreeNode = null;
        object propertyValue = null;

        if (getFunctionInfo.Parameters.Length == 0)
        {
            try
            {
                comPtr.TryInvokePropertyGet(getFunctionInfo.DispId, out propertyValue);
            }
            catch
            {
                GlobalExceptionHandler.HandleException();
            }

            if (propertyValue == null)
            {
                switch (getFunctionInfo.ReturnParameter.VariantType)
                {
                    case VarEnum.VT_DISPATCH:
                    case VarEnum.VT_PTR:
                    case VarEnum.VT_ARRAY:
                    case VarEnum.VT_UNKNOWN:
                        propertyValue = new ComPtr(IntPtr.Zero);
                        break;
                }
            }

            if (propertyValue is ComPtr)
            {
                comTreeNode = new ComPtrTreeNode(comPropertyInfo, (ComPtr)propertyValue);

                if (((ComPtr)propertyValue).IsInvalid == false)
                {
                    comTreeNode.Nodes.Add(string.Empty);
                }
            }
            else
            {
                comTreeNode = new ComPropertyTreeNode(comPropertyInfo, propertyValue);
            }
        }
        else
        {
            switch (getFunctionInfo.ReturnParameter.VariantType)
            {
                case VarEnum.VT_DISPATCH:
                case VarEnum.VT_PTR:
                case VarEnum.VT_ARRAY:
                case VarEnum.VT_UNKNOWN:
                    comTreeNode = new ComPtrTreeNode(comPropertyInfo, new ComPtr());
                    break;

                default:
                    comTreeNode = new ComPropertyTreeNode(comPropertyInfo, null);
                    break;
            }
        }

        return comTreeNode;
    }

    private void SetImageIndex(ComTreeNode[] comTreeNodes)
    {
        foreach (var comTreeNode in comTreeNodes)
        {
            SetImageIndex(comTreeNode);
        }
    }

    private void SetImageIndex(ComTreeNode comTreeNode)
    {
        var comPtrTreeNode = comTreeNode as ComPtrTreeNode;
        var comMethodTreeNode = comTreeNode as ComMethodTreeNode;
        var comPropertyTreeNode = comTreeNode as ComPropertyTreeNode;
        var comPtrItemTreeNode = comTreeNode as ComPtrItemTreeNode;

        if (comPtrTreeNode != null)
        {
            comPtrTreeNode.ImageIndex = comPtrTreeNode.ComPtr.IsInvalid ? NullObjectImageIndex : ObjectImageIndex;

            if (comPtrTreeNode.IsCollection)
            {
                comPtrTreeNode.ImageIndex = comPtrTreeNode.IsEmptyCollection ? NullCollectionImageIndex : CollectionImageIndex;
            }
        }
        else if (comMethodTreeNode != null)
        {
            comMethodTreeNode.ImageIndex = MethodImageIndex;
        }
        else if (comPropertyTreeNode != null)
        {
            comPropertyTreeNode.ImageIndex = PropertyImageIndex;
        }
        else if (comPtrItemTreeNode != null)
        {
            comPtrItemTreeNode.ImageIndex = comPtrTreeNode.ComPtr.IsInvalid ? NullObjectImageIndex : ObjectImageIndex;
        }

        comTreeNode.SelectedImageIndex = comTreeNode.ImageIndex;
    }

    public ComTreeNode AddRootNode(ComPtr p, string caption)
    {
        var comObjectRootTreeNode = new ComPtrTreeNode(caption, p);

        comObjectRootTreeNode.Nodes.Add("...");
        Nodes.Add(comObjectRootTreeNode);
        SelectedNode = comObjectRootTreeNode;
        comObjectRootTreeNode.Expand();

        return comObjectRootTreeNode;
    }

    public void CleanupAndRemoveNodes(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            CleanupAndRemoveNodes(node.Nodes);

            var comObjectTreeNode = node as ComPtrTreeNode;

            if ((comObjectTreeNode != null) && (comObjectTreeNode.ComPtr.IsInvalid == false))
            {
                comObjectTreeNode.ComPtr.Dispose();
            }
        }

        nodes.Clear();
    }

    private void ReExpandNodeUp(TreeNode treeNode)
    {
        if (treeNode != null)
        {
            if (treeNode.IsExpanded)
            {
                treeNode.Collapse();
                treeNode.Expand();
                SelectedNode = treeNode;

                OnAfterSelect(new TreeViewEventArgs(SelectedNode));
            }
            else
            {
                ReExpandNodeUp(treeNode.Parent);
            }
        }
    }

    private void SetupImageList()
    {
        ImageList = new ImageList();
        ImageList.ColorDepth = ColorDepth.Depth32Bit;
        ImageList.ImageSize = new Size(16, 16);
        ImageList.Images.Add(Resources.ComTreeItemBlue_16x16);
        ImageList.Images.Add(Resources.ComTreeItemGray_16x16);
        ImageList.Images.Add(Resources.Property_16x16);
        ImageList.Images.Add(Resources.Method_16x16);
        ImageList.Images.Add(Resources.ComTreeItemsBlue_16x16);
        ImageList.Images.Add(Resources.ComTreeItemsGray_16x16);
        ImageList.Images.Add(Resources.FolderClosed_16x16);
        ImageList.Images.Add(Resources.FolderOpen_16x16);
        ImageList.Images.Add(Resources.Event_16x16);
    }

    private void SetupVisualStyleRenderers()
    {
        try
        {
            if (System.Windows.Forms.Application.RenderWithVisualStyles)
            {
                OpenedRenderer = new VisualStyleRenderer("Explorer::TreeView", 2, 2);
                ClosedRenderer = new VisualStyleRenderer("Explorer::TreeView", 2, 1);
                ItemHoverRenderer = new VisualStyleRenderer("Explorer::TreeView", 1, 2);
                ItemSelectedRenderer = new VisualStyleRenderer("Explorer::TreeView", 1, 3);
                LostFocusSelectedRenderer = new VisualStyleRenderer("Explorer::TreeView", 1, 5);
                //Selectedx2Renderer = new VisualStyleRenderer("Explorer::TreeView", 1, 6);
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    #region Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowNullObjects
    {
        get => _showNullObjects;
        set
        {
            _showNullObjects = value;

            ReExpandNodeUp(SelectedNode);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowEmptyCollections
    {
        get => _showEmptyCollections;
        set
        {
            _showEmptyCollections = value;

            ReExpandNodeUp(SelectedNode);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowProperties
    {
        get => _showProperties;
        set
        {
            _showProperties = value;

            ReExpandNodeUp(SelectedNode);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowMethods
    {
        get => _showMethods;
        set
        {
            _showMethods = value;

            ReExpandNodeUp(SelectedNode);
        }
    }

    #endregion Properties
}

public class ComTreeNode : TreeNode
{
    private string _caption = string.Empty;
    private string _value = string.Empty;
    private string _typeFullName = string.Empty;

    public ComTreeNode()
        : base()
    {
    }

    public ComTreeNode(string caption)
        : base(caption) => Caption = caption;

    private void UpdateNodeText()
    {
        var spacer = new string(' ', 10);

        Text = _caption;

        if (!string.IsNullOrWhiteSpace(_value))
        {
            Text = string.Format("{0}{1}{2}", Text, spacer, _value);
        }

        if (!string.IsNullOrWhiteSpace(_typeFullName))
        {
            Text = string.Format("{0}{1}{2}", Text, spacer, _typeFullName);
        }
    }

    public string Caption
    {
        get => _caption;
        set
        {
            _caption = value;
            UpdateNodeText();
        }
    }

    public virtual string Value
    {
        get => _value;
        set
        {
            _value = value;
            UpdateNodeText();
        }
    }

    public string TypeFullName
    {
        get => _typeFullName;
        set
        {
            _typeFullName = value;
            UpdateNodeText();
        }
    }
}

public class ComPtrTreeNode : ComTreeNode
{
    private ComPtr _comPtr = IntPtr.Zero;
    private ComPropertyInfo _comPropertyInfo = null;
    protected ComFunctionInfo _comFunctionInfo = null;
    private bool _getFunctionHasParameters = false;
    protected bool _isCollection = false;
    public int _collectionCount = -1;

    public ComPtrTreeNode(string caption, ComPtr comPtr)
        : base(caption)
    {
        if (comPtr != null)
        {
            _comPtr = comPtr;

            if (_comPtr.IsInvalid == false)
            {
                var comTypeInfo = _comPtr.TryGetComTypeInfo();
                if (comTypeInfo != null)
                {
                    TypeFullName = comTypeInfo.FullName;
                }
                else
                {
                    TypeFullName = "IUnknown";
                }

                _isCollection = _comPtr.TryIsCollection();

                if (_isCollection)
                {
                    _collectionCount = _comPtr.TryGetItemCount();
                }

                string[] propertyNames = ["Name", "Caption", "StyleName", "ID", "Count", "Environment", "Description", "CommandString"];
                var value = comPtr.TryGetFirstAvailableProperty(propertyNames);

                if (value != null)
                {
                    Value = value.ToString();
                }
            }
        }
    }

    public ComPtrTreeNode(ComPropertyInfo comPropertyInfo, ComPtr comPtr)
        : this(comPropertyInfo.Name, comPtr)
    {
        _comPropertyInfo = comPropertyInfo;

        if ((_comPropertyInfo != null) && (_comPropertyInfo.GetFunction != null))
        {
            _comFunctionInfo = _comPropertyInfo.GetFunction;
            _getFunctionHasParameters = _comPropertyInfo.GetFunctionHasParameters;
        }
    }

    public ComPtr ComPtr => _comPtr;
    public ComPropertyInfo ComPropertyInfo => _comPropertyInfo;
    public ComFunctionInfo ComFunctionInfo => _comFunctionInfo;
    public bool GetFunctionHasParameters => _getFunctionHasParameters;
    public bool IsCollection => _isCollection;
    public bool IsEmptyCollection => (_isCollection) && (_collectionCount <= 0);
    public int CollectionCount => _collectionCount;
}

public class ComMethodTreeNode : ComTreeNode
{
    private ComFunctionInfo _comFunctionInfo = null;

    public ComMethodTreeNode(ComFunctionInfo comFunctionInfo)
        : base(comFunctionInfo.Name) => _comFunctionInfo = comFunctionInfo;

    public ComFunctionInfo ComFunctionInfo => _comFunctionInfo;
}

public class ComPropertyTreeNode : ComTreeNode
{
    private ComPropertyInfo _comPropertyInfo = null;
    private object _value;

    public ComPropertyTreeNode(ComPropertyInfo comPropertyInfo, object value)
        : base(comPropertyInfo.Name)
    {
        _comPropertyInfo = comPropertyInfo;

        if (value != null)
        {
            Value = value.ToString();
            TypeFullName = value.GetType().Name;
        }
    }

    public override string Value
    {
        get
        {
            var comPtrTreeNode = this.Parent as ComPtrTreeNode;

            if ((comPtrTreeNode != null) && (comPtrTreeNode.ComPtr.IsInvalid == false))
            {
                var getComFunctionInfo = _comPropertyInfo.GetFunction;

                if (getComFunctionInfo != null)
                {
                    object value = null;

                    if (MarshalEx.Succeeded(comPtrTreeNode.ComPtr.TryInvokePropertyGet(getComFunctionInfo.DispId, out value)))
                    {
                        _value = value;
                    }
                }
            }

            return base.Value;
        }

        set => base.Value = value;
    }

    public ComPropertyInfo ComPropertyInfo => _comPropertyInfo;
}

public class ComPtrItemTreeNode : ComPtrTreeNode
{
    public ComPtrItemTreeNode(ComPtr comPtr, ComFunctionInfo comFunctionInfo)
        : base(comFunctionInfo.Name, comPtr) => _comFunctionInfo = comFunctionInfo;
}
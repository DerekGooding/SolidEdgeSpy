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

    internal VisualStyleRenderer _openedRenderer;
    internal VisualStyleRenderer _closedRenderer;
    internal VisualStyleRenderer _itemHoverRenderer;
    internal VisualStyleRenderer _itemSelectedRenderer;
    internal VisualStyleRenderer _lostFocusSelectedRenderer;

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


            if (e.Node is ComPtrTreeNode comPtrTreeNode)
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
        if (!e.Node.IsVisible) return;


        if (e.Node is not ComTreeNode node) return;

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


        if (node is ComPtrTreeNode comPtrTreeNode)
        {
            if (node is ComPtrTreeNode && comPtrTreeNode.GetFunctionHasParameters)
            {
                captionColor = Color.DarkGray;
            }

            if (comPtrTreeNode?.CollectionCount > 0)
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

    private static void DrawNodeExpander(ComTreeNode node, Graphics graphics, Rectangle rect)
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
                if (_itemSelectedRenderer != null)
                {
                    _itemSelectedRenderer.DrawBackground(e.Graphics, rect);
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
                if (_lostFocusSelectedRenderer != null)
                {
                    _lostFocusSelectedRenderer.DrawBackground(e.Graphics, rect);
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
            if (_itemHoverRenderer != null)
            {
                _itemHoverRenderer.DrawBackground(e.Graphics, rect);
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
            if (childNode is ComPtrTreeNode comPtrTreeNode)
            {
                if ((comPtrTreeNode.ComPtr.IsInvalid) && (!ShowNullObjects))
                {
                    continue;
                }

                if (comPtrTreeNode.IsCollection)
                {
                    if ((comPtrTreeNode.IsEmptyCollection) && (!ShowEmptyCollections))
                    {
                        continue;
                    }
                }
            }
            else if (childNode is ComPropertyTreeNode comPropertyTreeNode)
            {
                if (!ShowProperties)
                {
                    continue;
                }
            }

            filteredComTreeNodes.Add(childNode);
        }

        SetImageIndex([.. filteredComTreeNodes]);

        return [.. filteredComTreeNodes];
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
                    if ((comTreeNode is ComPropertyTreeNode) && (!ShowProperties))
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
                                if ((returnValue is ComPtr pItem) && (!pItem.IsInvalid))
                                {
                                    var comPtrItemTreeNode = new ComPtrItemTreeNode((ComPtr)returnValue, comFunctionInfo)
                                    {
                                        Caption = string.Format("{0}({1})", comFunctionInfo.Name, i)
                                    };
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
                                    if ((returnValue is ComPtr pItem) && (!pItem.IsInvalid))
                                    {
                                        var comPtrItemTreeNode = new ComPtrItemTreeNode((ComPtr)returnValue, comFunctionInfo)
                                        {
                                            Caption = string.Format("{0}({1})", comFunctionInfo.Name, 0)
                                        };
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

            if (ShowMethods)
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

        return [.. childNodes];
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

                if (!((ComPtr)propertyValue).IsInvalid)
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
            comTreeNode = getFunctionInfo.ReturnParameter.VariantType switch
            {
                VarEnum.VT_DISPATCH or VarEnum.VT_PTR or VarEnum.VT_ARRAY or VarEnum.VT_UNKNOWN => new ComPtrTreeNode(comPropertyInfo, new ComPtr()),
                _ => new ComPropertyTreeNode(comPropertyInfo, null),
            };
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

        if (comPtrTreeNode != null)
        {
            comPtrTreeNode.ImageIndex = comPtrTreeNode.ComPtr.IsInvalid ? NullObjectImageIndex : ObjectImageIndex;

            if (comPtrTreeNode.IsCollection)
            {
                comPtrTreeNode.ImageIndex = comPtrTreeNode.IsEmptyCollection ? NullCollectionImageIndex : CollectionImageIndex;
            }
        }
        else if (comTreeNode is ComMethodTreeNode comMethodTreeNode)
        {
            comMethodTreeNode.ImageIndex = MethodImageIndex;
        }
        else if (comTreeNode is ComPropertyTreeNode comPropertyTreeNode)
        {
            comPropertyTreeNode.ImageIndex = PropertyImageIndex;
        }
        else if (comTreeNode is ComPtrItemTreeNode comPtrItemTreeNode)
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


            if ((node is ComPtrTreeNode comObjectTreeNode) && (!comObjectTreeNode.ComPtr.IsInvalid))
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
        ImageList = new ImageList
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(16, 16)
        };
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
                _openedRenderer = new VisualStyleRenderer("Explorer::TreeView", 2, 2);
                _closedRenderer = new VisualStyleRenderer("Explorer::TreeView", 2, 1);
                _itemHoverRenderer = new VisualStyleRenderer("Explorer::TreeView", 1, 2);
                _itemSelectedRenderer = new VisualStyleRenderer("Explorer::TreeView", 1, 3);
                _lostFocusSelectedRenderer = new VisualStyleRenderer("Explorer::TreeView", 1, 5);
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
    public bool ShowNullObjects { get; set
        {
            field = value;

            ReExpandNodeUp(SelectedNode);
        } } = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowEmptyCollections { get; set
        {
            field = value;

            ReExpandNodeUp(SelectedNode);
        } } = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowProperties { get; set
        {
            field = value;

            ReExpandNodeUp(SelectedNode);
        } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowMethods { get; set
        {
            field = value;

            ReExpandNodeUp(SelectedNode);
        } }

    #endregion Properties
}

public class ComTreeNode : TreeNode
{
    private string _value = string.Empty;

    public ComTreeNode()
        : base()
    {
    }

    public ComTreeNode(string caption)
        : base(caption) => Caption = caption;

    private void UpdateNodeText()
    {
        var spacer = new string(' ', 10);

        Text = Caption;

        if (!string.IsNullOrWhiteSpace(_value))
        {
            Text = string.Format("{0}{1}{2}", Text, spacer, _value);
        }

        if (!string.IsNullOrWhiteSpace(TypeFullName))
        {
            Text = string.Format("{0}{1}{2}", Text, spacer, TypeFullName);
        }
    }

    public string Caption { get; set
        {
            field = value;
            UpdateNodeText();
        } } = string.Empty;

    public virtual string Value
    {
        get => _value;
        set
        {
            _value = value;
            UpdateNodeText();
        }
    }

    public string TypeFullName { get; set
        {
            field = value;
            UpdateNodeText();
        } } = string.Empty;
}

public class ComPtrTreeNode : ComTreeNode
{
    protected ComFunctionInfo? _comFunctionInfo;
    protected bool _isCollection;
    public int _collectionCount = -1;

    public ComPtrTreeNode(string caption, ComPtr comPtr)
        : base(caption)
    {
        if (comPtr != null)
        {
            ComPtr = comPtr;

            if (!ComPtr.IsInvalid)
            {
                var comTypeInfo = ComPtr.TryGetComTypeInfo();
                if (comTypeInfo != null)
                {
                    TypeFullName = comTypeInfo.FullName;
                }
                else
                {
                    TypeFullName = "IUnknown";
                }

                _isCollection = ComPtr.TryIsCollection();

                if (_isCollection)
                {
                    _collectionCount = ComPtr.TryGetItemCount();
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
        ComPropertyInfo = comPropertyInfo;

        if (ComPropertyInfo?.GetFunction != null)
        {
            _comFunctionInfo = ComPropertyInfo.GetFunction;
            GetFunctionHasParameters = ComPropertyInfo.GetFunctionHasParameters;
        }
    }

    public ComPtr ComPtr { get; } = IntPtr.Zero;
    public ComPropertyInfo? ComPropertyInfo { get; }
    public ComFunctionInfo? ComFunctionInfo => _comFunctionInfo;
    public bool GetFunctionHasParameters { get; }
    public bool IsCollection => _isCollection;
    public bool IsEmptyCollection => (_isCollection) && (_collectionCount <= 0);
    public int CollectionCount => _collectionCount;
}

public class ComMethodTreeNode : ComTreeNode
{
    public ComMethodTreeNode(ComFunctionInfo comFunctionInfo)
        : base(comFunctionInfo.Name) => ComFunctionInfo = comFunctionInfo;

    public ComFunctionInfo ComFunctionInfo { get; }
}

public class ComPropertyTreeNode : ComTreeNode
{
    private object _value;

    public ComPropertyTreeNode(ComPropertyInfo comPropertyInfo, object value)
        : base(comPropertyInfo.Name)
    {
        ComPropertyInfo = comPropertyInfo;

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
            if ((Parent is ComPtrTreeNode comPtrTreeNode) && (!comPtrTreeNode.ComPtr.IsInvalid))
            {
                var getComFunctionInfo = ComPropertyInfo.GetFunction;

                if (getComFunctionInfo != null && MarshalEx.Succeeded(comPtrTreeNode.ComPtr.TryInvokePropertyGet(getComFunctionInfo.DispId, out var value)))
                {
                    _value = value;
                }
            }

            return base.Value;
        }

        set => base.Value = value;
    }

    public ComPropertyInfo ComPropertyInfo { get; }
}

public class ComPtrItemTreeNode : ComPtrTreeNode
{
    public ComPtrItemTreeNode(ComPtr comPtr, ComFunctionInfo comFunctionInfo)
        : base(comFunctionInfo.Name, comPtr) => _comFunctionInfo = comFunctionInfo;
}
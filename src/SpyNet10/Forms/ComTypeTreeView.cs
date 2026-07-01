using SpyNet10.InteropServices;
using System.ComponentModel;
using System.Windows.Forms.VisualStyles;

namespace SpyNet10.Forms;

public class ComTypeTreeView : TreeView
{
    public event EventHandler FilterChanged;

    private const int TV_FIRST = 0x1100;
    private const int TVM_SETBKCOLOR = TV_FIRST + 29;
    private const int TVM_SETEXTENDEDSTYLE = TV_FIRST + 44;
    private const int TVS_EX_DOUBLEBUFFER = 0x0004;
    private const int NODE_STRING_PADDING = 10;

    public const int ComTypeNodeNamespaceImageIndex = 0;
    public const int ComTypeNodeClassImageIndex = 1;
    public const int ComTypeNodeInterfaceImageIndex = 2;
    public const int ComTypeNodeEnumImageIndex = 3;
    public const int ComTypeNodeStructureImageIndex = 4;
    public const int ComTypeNodeAliasImageIndex = 5;
    public const int ComTypeNodeMethodImageIndex = 6;
    public const int ComTypeNodePropertyImageIndex = 7;
    public const int ComTypeNodeConstantImageIndex = 8;

    internal VisualStyleRenderer OpenedRenderer = null;
    internal VisualStyleRenderer ClosedRenderer = null;
    internal VisualStyleRenderer ItemHoverRenderer = null;
    internal VisualStyleRenderer ItemSelectedRenderer = null;
    internal VisualStyleRenderer LostFocusSelectedRenderer = null;
    //internal VisualStyleRenderer Selectedx2Renderer = null;

    private string _filter;

    public ComTypeTreeView()
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

    //protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
    //{
    //    Cursor.Current = Cursors.WaitCursor;
    //    BeginUpdate();

    //    if (e.Node.ImageIndex == ComTreeNodeClosedFolderImageIndex)
    //    {
    //        e.Node.ImageIndex = ComTreeNodeOpenFolderImageIndex;
    //        e.Node.SelectedImageIndex = e.Node.ImageIndex;
    //    }

    //    ComTypeTreeNode node = e.Node as ComTypeTreeNode;

    //    if (node != null)
    //    {
    //        if ((node.ComObject != null) || (node.Enumerable != null))
    //        {
    //            ComTypeTreeNode[] childNodes = BuildChildNodes(node);
    //            node.Nodes.Clear();
    //            node.Nodes.AddRange(childNodes);
    //        }
    //    }

    //    EndUpdate();
    //    Cursor.Current = Cursors.Default;
    //}

    protected override void OnDrawNode(DrawTreeNodeEventArgs e)
    {
        if (e.Node.IsVisible == false) return;

        var node = e.Node as ComTypeTreeNode;

        if (node == null) return;

        var baseFont = Font;
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

        var rectCaption = rectNode;

        rectCaption.Width = TextRenderer.MeasureText(node.Text, baseFont).Width;
        TextRenderer.DrawText(e.Graphics, node.Text, baseFont, rectCaption, captionColor, textFormatFlags);
    }

    private void DrawNodeExpander(ComTypeTreeNode node, Graphics graphics, Rectangle rect)
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

    private void SetupImageList()
    {
        ImageList = new ImageList();
        ImageList.ColorDepth = ColorDepth.Depth32Bit;
        ImageList.ImageSize = new Size(16, 16);
        ImageList.Images.Add(Resources.Namespace_16x16);
        ImageList.Images.Add(Resources.Class_16x16);
        ImageList.Images.Add(Resources.Interface_16x16);
        ImageList.Images.Add(Resources.Enum_16x16);
        ImageList.Images.Add(Resources.Structure_16x16);
        ImageList.Images.Add(Resources.Alias_16x16);
        ImageList.Images.Add(Resources.Method_16x16);
        ImageList.Images.Add(Resources.Property_16x16);
        ImageList.Images.Add(Resources.Constant_16x16);
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

    public void RefreshNodes()
    {
        Cursor.Current = Cursors.WaitCursor;

        if (string.IsNullOrWhiteSpace(Filter))
        {
            var nodes = new List<ComTypeTreeNode>();

            try
            {
                foreach (var comTypeLibrary in ComTypeManager.Instance.ComTypeLibraries)
                {
                    var comTypeLibraryTreeNode = new ComTypeLibraryTreeNode(comTypeLibrary);
                    nodes.Add(comTypeLibraryTreeNode);
                    RefreshNode(comTypeLibraryTreeNode);
                }
            }
            catch
            {
                GlobalExceptionHandler.HandleException();
            }

            Nodes.Clear();
            Nodes.AddRange(nodes.ToArray());
        }
        else
        {
            RefreshNodesFiltered();
        }

        Cursor.Current = Cursors.Default;
    }

    private void RefreshNode(ComTypeTreeNode node)
    {
        SetImageIndex(node);

        if (node is ComTypeLibraryTreeNode)
        {
            var comTypeLibraryTreeNode = (ComTypeLibraryTreeNode)node;
            var comTypeLibrary = comTypeLibraryTreeNode.ComTypeLibrary;

            foreach (var comTypeInfo in comTypeLibrary.ComTypeInfos)
            {
                if (comTypeInfo.IsHidden) continue;

                var comTypeInfoTreeNode = new ComTypeInfoTreeNode(comTypeInfo);
                comTypeLibraryTreeNode.Nodes.Add(comTypeInfoTreeNode);
                RefreshNode(comTypeInfoTreeNode);
            }
        }
        else if (node is ComTypeInfoTreeNode)
        {
            var comTypeInfoTreeNode = (ComTypeInfoTreeNode)node;
            var comTypeInfo = comTypeInfoTreeNode.ComTypeInfo;

            foreach (var comImplementedTypeInfo in comTypeInfo.ImplementedTypes)
            {
                var impComTypeInfoTreeNode = new ComTypeInfoTreeNode(comImplementedTypeInfo.ComTypeInfo);
                comTypeInfoTreeNode.Nodes.Add(impComTypeInfoTreeNode);
                RefreshNode(impComTypeInfoTreeNode);
            }
        }
    }

    private void RefreshNodesFiltered()
    {
        var list = new List<ComTypeTreeNode>();
        ComTypeTreeNode[] filterednodes = [];
        var iFilter = 0;

        try
        {
            if (int.TryParse(_filter, out iFilter) == false)
            {
                foreach (var comTypeLibrary in ComTypeManager.Instance.ComTypeLibraries)
                {
                    foreach (var comTypeInfo in comTypeLibrary.ComTypeInfos)
                    {
                        if (comTypeInfo.IsHidden) continue;

                        list.Add(new ComTypeInfoTreeNode(comTypeInfo));

                        foreach (var comFunctionInfo in comTypeInfo.Methods)
                        {
                            list.Add(new ComFunctionInfoTreeNode(comFunctionInfo));
                        }

                        foreach (var comPropertyInfo in comTypeInfo.Properties)
                        {
                            list.Add(new ComPropertyInfoTreeNode(comPropertyInfo));
                        }

                        foreach (var comVariableInfo in comTypeInfo.Variables)
                        {
                            list.Add(new ComVariableInfoTreeNode(comVariableInfo));
                        }
                    }
                }

                filterednodes = list.Where(x => x.Text.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            }
            else
            {
                foreach (var comTypeLibrary in ComTypeManager.Instance.ComTypeLibraries)
                {
                    foreach (var comTypeInfo in comTypeLibrary.ComTypeInfos)
                    {
                        if (comTypeInfo.IsHidden) continue;

                        foreach (var comVariableInfo in comTypeInfo.Variables)
                        {
                            list.Add(new ComVariableInfoTreeNode(comVariableInfo));
                        }
                    }
                }

                filterednodes = list.OfType<ComVariableInfoTreeNode>().Where(x => iFilter.Equals(x.ComVariableInfo.ConstantValue)).ToArray();
            }

            foreach (var comTypeTreeNode in filterednodes)
            {
                comTypeTreeNode.Text = comTypeTreeNode.FullText;
                SetImageIndex(comTypeTreeNode);
            }
        }
        catch
        {
        }

        Nodes.Clear();
        Nodes.AddRange(filterednodes);
    }

    private void SetImageIndex(ComTypeTreeNode[] comTypeTreeNodes)
    {
        foreach (var comTypeTreeNode in comTypeTreeNodes)
        {
            SetImageIndex(comTypeTreeNode);
        }
    }

    private void SetImageIndex(ComTypeTreeNode comTypeTreeNode)
    {
        var comTypeLibraryTreeNode = comTypeTreeNode as ComTypeLibraryTreeNode;
        var comTypeInfoTreeNode = comTypeTreeNode as ComTypeInfoTreeNode;
        var comFunctionInfoTreeNode = comTypeTreeNode as ComFunctionInfoTreeNode;
        var comPropertyInfoTreeNode = comTypeTreeNode as ComPropertyInfoTreeNode;
        var comVariableInfoTreeNode = comTypeTreeNode as ComVariableInfoTreeNode;

        if (comTypeLibraryTreeNode != null)
        {
            comTypeTreeNode.ImageIndex = ComTypeNodeNamespaceImageIndex;
        }
        else if (comTypeInfoTreeNode != null)
        {
            var comTypeInfo = comTypeInfoTreeNode.ComTypeInfo;

            if (comTypeInfo.IsCoClass)
            {
                comTypeTreeNode.ImageIndex = ComTypeNodeClassImageIndex;
            }
            else if (comTypeInfo.IsDispatch)
            {
                comTypeTreeNode.ImageIndex = ComTypeNodeInterfaceImageIndex;
            }
            else if (comTypeInfo.IsInterface)
            {
                comTypeTreeNode.ImageIndex = ComTypeNodeInterfaceImageIndex;
            }
            else if (comTypeInfo.IsEnum)
            {
                comTypeTreeNode.ImageIndex = ComTypeNodeEnumImageIndex;
            }
            else if (comTypeInfo.IsAlias)
            {
                comTypeTreeNode.ImageIndex = ComTypeNodeAliasImageIndex;
            }
            else if (comTypeInfo.IsRecord)
            {
                comTypeTreeNode.ImageIndex = ComTypeNodeStructureImageIndex;
            }
        }
        else if (comFunctionInfoTreeNode != null)
        {
            comTypeTreeNode.ImageIndex = ComTypeNodeMethodImageIndex;
        }
        else if (comPropertyInfoTreeNode != null)
        {
            comTypeTreeNode.ImageIndex = ComTypeNodePropertyImageIndex;
        }
        else if (comVariableInfoTreeNode != null)
        {
            comVariableInfoTreeNode.ImageIndex = ComTypeNodeConstantImageIndex;
        }

        comTypeTreeNode.SelectedImageIndex = comTypeTreeNode.ImageIndex;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            RefreshNodes();

            if (FilterChanged != null)
            {
                FilterChanged(this, new EventArgs());
            }
        }
    }

    public bool IsFiltered => string.IsNullOrWhiteSpace(_filter) == false;
}

public class ComTypeTreeNode : TreeNode
{
    private string _fullText = string.Empty;

    public ComTypeTreeNode()
        : base()
    {
    }

    public string FullText
    {
        get => _fullText; set => _fullText = value;
    }
}

public class ComTypeLibraryTreeNode : ComTypeTreeNode
{
    private ComTypeLibrary _comTypeLibrary;

    public ComTypeLibraryTreeNode(ComTypeLibrary comTypeLibrary)
        : base()
    {
        _comTypeLibrary = comTypeLibrary;
        Text = _comTypeLibrary.Name;
        FullText = _comTypeLibrary.Name;
    }

    public ComTypeLibrary ComTypeLibrary => _comTypeLibrary;
}

public class ComTypeInfoTreeNode : ComTypeTreeNode
{
    private ComTypeInfo _comTypeInfo;

    public ComTypeInfoTreeNode(ComTypeInfo comTypeInfo)
        : base()
    {
        _comTypeInfo = comTypeInfo;
        Text = _comTypeInfo.Name;
        FullText = _comTypeInfo.FullName;
    }

    public ComTypeInfo ComTypeInfo => _comTypeInfo;
}

public class ComFunctionInfoTreeNode : ComTypeTreeNode
{
    private ComFunctionInfo _comFunctionInfo;

    public ComFunctionInfoTreeNode(ComFunctionInfo comFunctionInfo)
        : base()
    {
        _comFunctionInfo = comFunctionInfo;
        Text = _comFunctionInfo.Name;
        FullText = string.Format("{0}.{1}", _comFunctionInfo.ComTypeInfo.FullName, _comFunctionInfo.Name);
    }

    public ComFunctionInfo ComFunctionInfo => _comFunctionInfo;
}

public class ComPropertyInfoTreeNode : ComTypeTreeNode
{
    private ComPropertyInfo _comPropertyInfo;

    public ComPropertyInfoTreeNode(ComPropertyInfo comFunctionInfo)
        : base()
    {
        _comPropertyInfo = comFunctionInfo;
        Text = _comPropertyInfo.Name;
        FullText = string.Format("{0}.{1}", _comPropertyInfo.ComTypeInfo.FullName, _comPropertyInfo.Name);
    }

    public ComPropertyInfo ComPropertyInfo => _comPropertyInfo;
}

public class ComVariableInfoTreeNode : ComTypeTreeNode
{
    private ComVariableInfo _comVariableInfo;

    public ComVariableInfoTreeNode(ComVariableInfo comVariableInfo)
        : base()
    {
        _comVariableInfo = comVariableInfo;
        Text = _comVariableInfo.Name;
        FullText = string.Format("{0}.{1}", _comVariableInfo.ComTypeInfo.FullName, _comVariableInfo.Name);
    }

    public ComVariableInfo ComVariableInfo => _comVariableInfo;
}
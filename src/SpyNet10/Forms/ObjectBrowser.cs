using SpyNet10.InteropServices;

namespace SpyNet10.Forms;

public partial class ObjectBrowser : UserControl
{
    public ObjectBrowser() => InitializeComponent();

    private void ComBrowser_Load(object sender, EventArgs e)
    {
        buttonNullObjects.Checked = comTreeView.ShowNullObjects;
        buttonEmptyCollection.Checked = comTreeView.ShowEmptyCollections;
        buttonProperties.Checked = comTreeView.ShowProperties;
    }

    private void UpdateToolStrip(ComTreeNode node)
    {
        try
        {
            var baseToolStripItems = new List<ToolStripItem>();
            var newToolStripItems = new List<ToolStripItem>();

            var index = toolStrip.Items.IndexOf(separatorNavigation);

            for (var i = 0; i <= index; i++)
            {
                baseToolStripItems.Add(toolStrip.Items[i]);
            }

            while (node != null)
            {
                var button = new ToolStripButton(node.Caption)
                {
                    ToolTipText = string.Format("{0} ({1})", node.Caption, node.TypeFullName),
                    DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
                };

                var imageIndex = node.ImageIndex == -1 ? 0 : node.ImageIndex;

                button.Image = comTreeView.ImageList.Images[imageIndex];
                button.Tag = node;
                button.Click += toolStripNavigationButton_Click;
                newToolStripItems.Insert(0, button);

                node = node.Parent as ComTreeNode;
            }

            baseToolStripItems.AddRange(newToolStripItems);
            toolStrip.Items.Clear();
            toolStrip.Items.AddRange([.. baseToolStripItems]);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void toolStripNavigationButton_Click(object sender, EventArgs e)
    {
        try
        {
            if (sender is ToolStripButton button)
            {
                if (button.Tag is ComTreeNode node)
                {
                    node.EnsureVisible();
                }
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void comTreeView_AfterSelect(object sender, TreeViewEventArgs e)
    {
        comPropertyGrid.SelectedObject = null;


        if (e.Node is ComTreeNode comTreeNode)
        {
            UpdateToolStrip(comTreeNode);
            UpdateRichTextBox(comTreeNode);

            try
            {
                if (comTreeNode is ComPtrTreeNode)
                {
                    comPropertyGrid.SelectedObject = ((ComPtrTreeNode)comTreeNode).ComPtr;
                }
            }
            catch
            {
                GlobalExceptionHandler.HandleException();
            }
        }
    }

    private void comPropertyGrid_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
    {
        try
        {
            if (comTreeView.Focused == true) return;

            var gridItem = e.NewSelection;
            if (gridItem != null)
            {
                if (gridItem.PropertyDescriptor is ComPtrPropertyDescriptor descriptor)
                {
                    typeInfoRichTextBox.DescribeComPropertyInfo(descriptor.ComPropertyInfo);
                }
            }
        }
        catch
        {
        }
    }

    private void UpdateRichTextBox(ComTreeNode node)
    {
        try
        {
            typeInfoRichTextBox.Clear();

            if (node == null) return;

            ComTypeInfo comTypeInfo = null;
            ComPropertyInfo comPropertyInfo = null;
            ComFunctionInfo comFunctionInfo = null;

            if (node is ComPtrItemTreeNode)
            {
                comFunctionInfo = ((ComPtrItemTreeNode)node).ComFunctionInfo;
            }
            else if (node is ComMethodTreeNode)
            {
                comFunctionInfo = ((ComMethodTreeNode)node).ComFunctionInfo;
            }
            else if (node is ComPropertyTreeNode)
            {
                comPropertyInfo = ((ComPropertyTreeNode)node).ComPropertyInfo;
            }
            else if (node is ComPtrTreeNode)
            {
                var comObjectPropertyTreeNode = (ComPtrTreeNode)node;

                if (comObjectPropertyTreeNode.ComPropertyInfo != null)
                {
                    comPropertyInfo = comObjectPropertyTreeNode.ComPropertyInfo;
                }
                else
                {
                    comTypeInfo = comObjectPropertyTreeNode.ComPtr.TryGetComTypeInfo();
                }
            }

            if (comTypeInfo != null)
            {
                typeInfoRichTextBox.DescribeComTypeInfo(comTypeInfo);
            }
            else if (comPropertyInfo != null)
            {
                typeInfoRichTextBox.DescribeComPropertyInfo(comPropertyInfo);
            }
            else if (comFunctionInfo != null)
            {
                typeInfoRichTextBox.DescribeComFunctionInfo(comFunctionInfo);
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void buttonNullObjects_Click(object sender, EventArgs e)
    {
        buttonNullObjects.Checked = !buttonNullObjects.Checked;
        comTreeView.ShowNullObjects = buttonNullObjects.Checked;
    }

    private void buttonEmptyCollection_Click(object sender, EventArgs e)
    {
        buttonEmptyCollection.Checked = !buttonEmptyCollection.Checked;
        comTreeView.ShowEmptyCollections = buttonEmptyCollection.Checked;
    }

    private void buttonProperties_Click(object sender, EventArgs e)
    {
        buttonProperties.Checked = !buttonProperties.Checked;
        comTreeView.ShowProperties = buttonProperties.Checked;
    }

    private void buttonMethods_Click(object sender, EventArgs e)
    {
        buttonMethods.Checked = !buttonMethods.Checked;
        comTreeView.ShowMethods = buttonMethods.Checked;
    }

    private void buttonRefresh_Click(object sender, EventArgs e)
    {
        Cursor.Current = Cursors.WaitCursor;

        Connect();

        Cursor.Current = Cursors.Default;
    }

    public void Connect()
    {
        Disconnect();

        ComPtr pApplication = IntPtr.Zero;

        try
        {
            if (MarshalEx.Succeeded(MarshalEx.GetActiveObject("SolidEdge.Application", out pApplication)))
            {
                comTreeView.AddRootNode(pApplication, "Application");
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
        finally
        {
        }
    }

    public void Disconnect()
    {
        try
        {
            comPropertyGrid.SelectedObject = null;

            UpdateToolStrip(null);

            comTreeView.CleanupAndRemoveNodes(comTreeView.Nodes);
            typeInfoRichTextBox.Clear();
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void typeInfoRichTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
    {
        try
        {
            var linkInfo = e.LinkText.Split(['#']);

            switch (linkInfo.Length)
            {
                case 1:
                    ComTypeManager.Instance.LookupAndSelect(linkInfo[0]);
                    break;

                case 2:
                    ComTypeManager.Instance.LookupAndSelect(linkInfo[1]);
                    break;
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void comTreeView_Enter(object sender, EventArgs e) => comTreeView_AfterSelect(comTreeView, new TreeViewEventArgs(comTreeView.SelectedNode, TreeViewAction.Unknown));

    private void comPropertyGrid_Enter(object sender, EventArgs e) => comPropertyGrid_SelectedGridItemChanged(comPropertyGrid, new SelectedGridItemChangedEventArgs(null, comPropertyGrid.SelectedGridItem));
}
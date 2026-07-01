using SpyNet10.InteropServices;

namespace SpyNet10.Forms;

public partial class TypeBrowser : UserControl
{
    private bool _scanComplete = false;
    private readonly NavigationController<object> _navigationController;

    public TypeBrowser()
    {
        InitializeComponent();

        _navigationController = new NavigationController<object>(buttonBack, buttonForward, 20);

        ComTypeManager.Instance.ComTypeLibrarySelected += Instance_ComTypeLibrarySelected;
        ComTypeManager.Instance.ComTypeInfoSelected += Instance_ComTypeInfoSelected;

        comTypeTreeView.GotFocus += comTypeTreeView_GotFocus;
    }

    private void TypeBrowser_Load(object sender, EventArgs e)
    {
        _navigationController.GotoItem += _navigationController_GotoItem;

        if (comTypeTreeView.Nodes.Count == 0)
        {
            comTypeTreeView.RefreshNodes();
        }
    }

    private void _navigationController_GotoItem(object sender, NavigationControllerEventArgs<object> e)
    {
        try
        {
            var treeNode = e.Item as TreeNode;
            var listViewItem = e.Item as ListViewItem;

            if (treeNode != null)
            {
                treeNode.TreeView.SelectedNode = treeNode;
                treeNode.EnsureVisible();
                treeNode.TreeView.Focus();
            }
            else if (listViewItem != null)
            {
                listViewItem.Selected = true;
                listViewItem.EnsureVisible();
                listViewItem.ListView.Focus();
            }
        }
        catch
        {
        }
    }

    private void Instance_ComTypeInfoSelected(object sender, ComTypeInfo comTypeInfo)
    {
        _scanComplete = false;

        try
        {
            if (comTypeTreeView.IsFiltered)
            {
                comTypeTreeView.Filter = string.Empty;
            }

            if (comTypeTreeView.Nodes.Count == 0)
            {
                comTypeTreeView.RefreshNodes();
            }

            ScanForNodeAndSelect(comTypeInfo, comTypeTreeView.Nodes);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void Instance_ComTypeLibrarySelected(object sender, ComTypeLibrary comTypeLibrary)
    {
        _scanComplete = false;

        try
        {
            if (comTypeTreeView.IsFiltered)
            {
                comTypeTreeView.Filter = string.Empty;
            }

            if (comTypeTreeView.Nodes.Count == 0)
            {
                comTypeTreeView.RefreshNodes();
            }

            ScanForNodeAndSelect(comTypeLibrary, comTypeTreeView.Nodes);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    public void ScanForNodeAndSelect(ComTypeLibrary comTypeLibrary, TreeNodeCollection nodes)
    {
        if (comTypeLibrary == null) return;
        if (nodes == null) return;
        if (_scanComplete == true) return;

        foreach (TreeNode node in nodes)
        {
            var comTypeLibraryTreeNode = node as ComTypeLibraryTreeNode;

            if (comTypeLibraryTreeNode != null)
            {
                if (comTypeLibraryTreeNode.ComTypeLibrary.Name.Equals(comTypeLibrary.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _scanComplete = true;
                    node.TreeView.SelectedNode = node;
                    node.EnsureVisible();
                    return;
                }
            }
        }
    }

    public void ScanForNodeAndSelect(ComTypeInfo comTypeInfo, TreeNodeCollection nodes)
    {
        if (comTypeInfo == null) return;
        if (nodes == null) return;
        if (_scanComplete == true) return;

        foreach (TreeNode node in nodes)
        {
            var comTypeLibraryTreeNode = node as ComTypeLibraryTreeNode;
            var comTypeInfoTreeNode = node as ComTypeInfoTreeNode;

            if (comTypeLibraryTreeNode != null)
            {
                if (comTypeLibraryTreeNode.ComTypeLibrary.Name.Equals(comTypeInfo.ComTypeLibrary.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ScanForNodeAndSelect(comTypeInfo, node.Nodes);
                    return;
                }
            }
            else if (comTypeInfoTreeNode != null)
            {
                if (comTypeInfoTreeNode.ComTypeInfo.Name.Equals(comTypeInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _scanComplete = true;
                    node.TreeView.SelectedNode = node;
                    node.EnsureVisible();
                    return;
                }
            }
        }
    }

    private void comTypeTreeView_GotFocus(object sender, EventArgs e) => comTypeTreeView_AfterSelect(comTypeTreeView, new TreeViewEventArgs(comTypeTreeView.SelectedNode));

    private void buttonRefresh_Click(object sender, EventArgs e) => comTypeTreeView.RefreshNodes();

    private void comTypeTreeView_AfterSelect(object sender, TreeViewEventArgs e)
    {
        _navigationController.Remove(comTypeListView.Items.OfType<ListViewItem>().ToArray());
        _navigationController.CurrentItem = e.Node;

        var comTypeLibraryTreeNode = e.Node as ComTypeLibraryTreeNode;
        var comTypeInfoTreeNode = e.Node as ComTypeInfoTreeNode;
        var comFunctionInfoTreeNode = e.Node as ComFunctionInfoTreeNode;
        var comPropertyInfoTreeNode = e.Node as ComPropertyInfoTreeNode;
        var comVariableInfoTreeNode = e.Node as ComVariableInfoTreeNode;

        comTypeListView.SelectedComTypeInfo = null;

        if (comTypeLibraryTreeNode != null)
        {
            typeInfoRichTextBox.DescribeComTypeLibrary(comTypeLibraryTreeNode.ComTypeLibrary);
        }
        else if (comTypeInfoTreeNode != null)
        {
            var comTypeInfo = comTypeInfoTreeNode.ComTypeInfo;
            comTypeListView.SelectedComTypeInfo = comTypeInfo;
            typeInfoRichTextBox.DescribeComTypeInfo(comTypeInfo);
        }
        else if (comFunctionInfoTreeNode != null)
        {
            typeInfoRichTextBox.DescribeComFunctionInfo(comFunctionInfoTreeNode.ComFunctionInfo);
        }
        else if (comPropertyInfoTreeNode != null)
        {
            typeInfoRichTextBox.DescribeComPropertyInfo(comPropertyInfoTreeNode.ComPropertyInfo);
        }
        else if (comVariableInfoTreeNode != null)
        {
            typeInfoRichTextBox.DescribeComVariableInfo(comVariableInfoTreeNode.ComVariableInfo);
        }
    }

    private void comTypeListView_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (comTypeListView.SelectedItems.Count > 0)
        {
            var item = comTypeListView.SelectedItems[0];

            if (item.Tag is ComFunctionInfo)
            {
                typeInfoRichTextBox.DescribeComFunctionInfo((ComFunctionInfo)item.Tag);
            }
            else if (item.Tag is ComPropertyInfo)
            {
                typeInfoRichTextBox.DescribeComPropertyInfo((ComPropertyInfo)item.Tag);
            }
            else if (item.Tag is ComVariableInfo)
            {
                typeInfoRichTextBox.DescribeComVariableInfo((ComVariableInfo)item.Tag);
            }

            _navigationController.CurrentItem = item;
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

    private void textBoxSearch_TextAccepted(object sender, EventArgs e) => buttonSearch_Click(sender, e);

    private void buttonSearch_Click(object sender, EventArgs e) => comTypeTreeView.Filter = textBoxSearch.Text;

    private void buttonClearSearch_Click(object sender, EventArgs e) => comTypeTreeView.Filter = string.Empty;

    private void comTypeTreeView_FilterChanged(object sender, EventArgs e)
    {
        if (comTypeTreeView.IsFiltered == false)
        {
            textBoxSearch.Text = string.Empty;
        }

        _navigationController.Clear();
        comTypeListView.SelectedComTypeInfo = null;
        typeInfoRichTextBox.Clear();
    }
}
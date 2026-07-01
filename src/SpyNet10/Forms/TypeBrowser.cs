using SpyNet10.InteropServices;

namespace SpyNet10.Forms;

public partial class TypeBrowser : UserControl
{
    private bool _scanComplete;
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
            if (e.Item is TreeNode treeNode)
            {
                treeNode.TreeView.SelectedNode = treeNode;
                treeNode.EnsureVisible();
                treeNode.TreeView.Focus();
            }
            else if (e.Item is ListViewItem listViewItem)
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
            if (node is ComTypeLibraryTreeNode comTypeLibraryTreeNode)
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
            if (node is ComTypeLibraryTreeNode comTypeLibraryTreeNode)
            {
                if (comTypeLibraryTreeNode.ComTypeLibrary.Name.Equals(comTypeInfo.ComTypeLibrary.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ScanForNodeAndSelect(comTypeInfo, node.Nodes);
                    return;
                }
            }
            else if (node is ComTypeInfoTreeNode comTypeInfoTreeNode)
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

        comTypeListView.SelectedComTypeInfo = null;

        if (e.Node is ComTypeLibraryTreeNode comTypeLibraryTreeNode)
        {
            typeInfoRichTextBox.DescribeComTypeLibrary(comTypeLibraryTreeNode.ComTypeLibrary);
        }
        else if (e.Node is ComTypeInfoTreeNode comTypeInfoTreeNode)
        {
            var comTypeInfo = comTypeInfoTreeNode.ComTypeInfo;
            comTypeListView.SelectedComTypeInfo = comTypeInfo;
            typeInfoRichTextBox.DescribeComTypeInfo(comTypeInfo);
        }
        else if (e.Node is ComFunctionInfoTreeNode comFunctionInfoTreeNode)
        {
            typeInfoRichTextBox.DescribeComFunctionInfo(comFunctionInfoTreeNode.ComFunctionInfo);
        }
        else if (e.Node is ComPropertyInfoTreeNode comPropertyInfoTreeNode)
        {
            typeInfoRichTextBox.DescribeComPropertyInfo(comPropertyInfoTreeNode.ComPropertyInfo);
        }
        else if (e.Node is ComVariableInfoTreeNode comVariableInfoTreeNode)
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
        if (!comTypeTreeView.IsFiltered)
        {
            textBoxSearch.Text = string.Empty;
        }

        _navigationController.Clear();
        comTypeListView.SelectedComTypeInfo = null;
        typeInfoRichTextBox.Clear();
    }
}
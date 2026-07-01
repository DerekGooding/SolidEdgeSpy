using SpyNet10.Extensions;
using SpyNet10.InteropServices;
using System.ComponentModel;
using System.Data;

namespace SpyNet10.Forms;

public partial class CommandBrowser : UserControl
{
    private readonly List<ListViewItem> _commandItems = [];
    private string? _filter;

    public CommandBrowser() => InitializeComponent();

    private void TextBoxSearch_TextAccepted(object sender, EventArgs e) => ButtonSearch_Click(sender, e);

    private void ButtonSearch_Click(object sender, EventArgs e)
    {
        _filter = textBoxSearch.Text;
        LoadItems();
    }

    private void ButtonClearSearch_Click(object sender, EventArgs e)
    {
        _filter = null;
        LoadItems();
    }

    private void TextBoxCommandID_TextChanged(object sender, EventArgs e) => buttonStart.Enabled = int.TryParse(textBoxCommandID.Text, out _);

    private void ButtonStart_Click(object sender, EventArgs e)
    {
        var commandId = 0;

        try
        {
            if (int.TryParse(textBoxCommandID.Text, out commandId))
            {
                var application = ActiveEnvironment.Application;
                application.StartCommand((SolidEdgeFramework.SolidEdgeCommandConstants)commandId);
            }
        }
        catch
        {
        }
    }

    private void ListViewEx_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
    {
        textBoxCommandID.Text = string.Empty;

        try
        {
            var item = e.Item;

            if (item != null)
            {
                var commandId = (int)item.Tag;
                textBoxCommandID.Text = commandId.ToString();
            }
        }
        catch
        {
        }
    }

    private void LoadItems()
    {
        listViewEx.Items.Clear();
        _commandItems.Clear();

        if (ActiveEnvironment == null) return;

        try
        {
            //Solid Edge Constants Type Library
            var typeLibGuid = new Guid("{C467A6F5-27ED-11D2-BE30-080036B4D502}");

            var constantsTypeLib = ComTypeManager.Instance.ComTypeLibraries.FirstOrDefault(x => x.Guid.Equals(typeLibGuid));

            if (constantsTypeLib != null)
            {
                var commandType = ActiveEnvironment.GetCommandType();

                if (commandType != null)
                {
                    var enumInfo = constantsTypeLib.Enums.FirstOrDefault(x => x.Name.Equals(commandType.Name));

                    foreach (var variable in enumInfo.Variables)
                    {
                        var enumName = variable.Name;
                        var enumValue = variable.ConstantValue;
                        var listViewItem = new ListViewItem
                        {
                            Text = enumName
                        };
                        listViewItem.SubItems.Add(string.Format("{0}", (int)enumValue));
                        listViewItem.SubItems.Add(commandType.FullName);
                        listViewItem.Tag = (int)enumValue;
                        _commandItems.Add(listViewItem);
                    }

                    // Sort commands by name.
                    _commandItems.Sort((a, b) => a.Text.CompareTo(b.Text));
                }
            }
        }
        catch
        {
        }

        if (string.IsNullOrWhiteSpace(_filter))
        {
            listViewEx.Items.AddRange([.. _commandItems]);
        }
        else
        {
            if (!int.TryParse(_filter, out var idFilter))
            {
                var filteredItems = _commandItems
                    .Where(x => x.Text.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                listViewEx.Items.AddRange(filteredItems);
            }
            else
            {
                var filteredItems = _commandItems
                    .Where(x => x.Tag?.ToString()?.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();

                listViewEx.Items.AddRange(filteredItems);
            }
        }

        listViewEx.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SolidEdgeFramework.Environment? ActiveEnvironment
    {
        get;
        set
        {
            field = value;

            try
            {
                textBoxSearch.Text = string.Empty;
                textBoxCommandID.Text = string.Empty;
                LoadItems();
            }
            catch
            {
            }
        }
    }
}
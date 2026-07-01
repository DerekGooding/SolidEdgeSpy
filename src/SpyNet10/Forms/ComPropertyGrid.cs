using SpyNet10.InteropServices;
using System.ComponentModel;

namespace SpyNet10.Forms;

public partial class ComPropertyGrid : UserControl
{
    public event SelectedGridItemChangedEventHandler SelectedGridItemChanged;

    private GridItem _selectedGridItem;

    public ComPropertyGrid() => InitializeComponent();

    private void propertyGrid_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
    {
        _selectedGridItem = e.NewSelection;

        if (SelectedGridItemChanged != null)
        {
            var propertyGrid = sender as PropertyGrid;
            SelectedGridItemChanged(sender, e);
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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ComPtr SelectedObject
    {
        get => (ComPtr)propertyGrid.SelectedObject; set => propertyGrid.SelectedObject = value;
    }

    public GridItem SelectedGridItem => _selectedGridItem;
}
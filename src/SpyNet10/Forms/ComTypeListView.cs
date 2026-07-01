using SpyNet10.InteropServices;
using System.ComponentModel;

namespace SpyNet10.Forms;

public class ComTypeListView : ListViewEx
{
    public const int MethodImageIndex = 0;
    public const int PropertyImageIndex = 1;
    public const int EventImageIndex = 2;
    public const int ConstantImageIndex = 3;

    public ComTypeListView()
        : base() => SetupImageList();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ComTypeInfo SelectedComTypeInfo
    {
        get;

        set
        {
            field = value;
            UpdateItems();
        }
    }

    private void UpdateItems()
    {
        var list = new List<ListViewItem>();

        if (SelectedComTypeInfo != null)
        {
            foreach (var comFunctionInfo in SelectedComTypeInfo.GetMethods(true))
            {
                var item = new ListViewItem(comFunctionInfo.Name);
                item.SubItems.Add(comFunctionInfo.Description);
                item.ImageIndex = MethodImageIndex;
                item.Tag = comFunctionInfo;

                if (comFunctionInfo.IsRestricted)
                {
                    item.ForeColor = Color.DarkGray;
                }

                list.Add(item);
            }

            foreach (var comPropertyInfo in SelectedComTypeInfo.GetProperties(true))
            {
                var item = new ListViewItem(comPropertyInfo.Name);
                item.SubItems.Add(comPropertyInfo.Description);
                item.ImageIndex = PropertyImageIndex;
                item.Tag = comPropertyInfo;

                if (comPropertyInfo.GetFunction != null)
                {
                    if (comPropertyInfo.GetFunction.IsRestricted)
                    {
                        item.ForeColor = Color.DarkGray;
                    }
                }

                list.Add(item);
            }

            foreach (var comVariableInfo in SelectedComTypeInfo.Variables)
            {
                var item = new ListViewItem(comVariableInfo.Name);
                item.SubItems.Add(comVariableInfo.Description);
                item.ImageIndex = ConstantImageIndex;
                item.Tag = comVariableInfo;

                list.Add(item);
            }

            if (SelectedComTypeInfo is ComCoClassInfo)
            {
                var comCoClassInfo = (ComCoClassInfo)SelectedComTypeInfo;
                foreach (var comFunctionInfo in comCoClassInfo.Events)
                {
                    var item = new ListViewItem(comFunctionInfo.Name);
                    item.SubItems.Add(comFunctionInfo.Description);
                    item.ImageIndex = EventImageIndex;
                    item.Tag = comFunctionInfo;
                    list.Add(item);
                }
            }
            else
            {
            }
        }

        BeginUpdate();
        Items.Clear();
        Items.AddRange([.. list]);
        AutoResizeColumns();
        EndUpdate();
    }

    private void SetupImageList()
    {
        SmallImageList = new ImageList
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(16, 16)
        };
        SmallImageList.Images.Add(Resources.Method_16x16);
        SmallImageList.Images.Add(Resources.Property_16x16);
        SmallImageList.Images.Add(Resources.Event_16x16);
        SmallImageList.Images.Add(Resources.Constant_16x16);
    }
}
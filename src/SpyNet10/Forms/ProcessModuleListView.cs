using SpyNet10.InteropServices;
using System.Diagnostics;

namespace SpyNet10.Forms;

public class ProcessModuleListView : ListViewEx
{
    public ProcessModuleListView()
        : base()
    {
    }

    protected override void OnCreateControl()
    {
        base.OnCreateControl();

        Columns.Clear();
        Columns.Add("Name");
        Columns.Add("Start address");
        Columns.Add("End address");
        Columns.Add("Entry point address");
        Columns.Add("Product Version");
        Columns.Add("File Version");
        Columns.Add("Description");
        Columns.Add("FileName");
        HideSelection = false;
        FullRowSelect = true;
        View = System.Windows.Forms.View.Details;

        SmallImageList = new ImageList();
        SmallImageList.ColorDepth = ColorDepth.Depth32Bit;
        SmallImageList.ImageSize = new Size(16, 16);
    }

    public void SetItems(ProcessModuleCollection processModules)
    {
        if (this.Created == false) return;

        var list = new List<ListViewItem>();
        var processModuleList = processModules.Cast<ProcessModule>().ToList();

        processModuleList.Sort(delegate (ProcessModule a, ProcessModule b)
        {
            return a.BaseAddress.ToInt64().CompareTo(b.BaseAddress.ToInt64());
        });

        try
        {
            foreach (var processModule in processModuleList)
            {
                var item = new ListViewItem();
                item.Text = processModule.ModuleName;

                var startAddress = string.Format("0x{0}", processModule.BaseAddress.ToString("X16"));
                var endAddress = string.Format("0x{0}", (processModule.BaseAddress.ToInt64() + processModule.ModuleMemorySize).ToString("X16"));
                var entryPointAddress = string.Format("0x{0}", processModule.EntryPointAddress.ToString("X16"));

                item.SubItems.Add(startAddress);
                item.SubItems.Add(endAddress);
                item.SubItems.Add(entryPointAddress);
                item.SubItems.Add(processModule.FileVersionInfo.ProductVersion);
                item.SubItems.Add(processModule.FileVersionInfo.FileVersion);
                item.SubItems.Add(processModule.FileVersionInfo.FileDescription);
                item.SubItems.Add(processModule.FileVersionInfo.FileName);

                try
                {
                    var fileName = processModule.FileVersionInfo.FileName;
                    var extension = Path.GetExtension(fileName).ToLower();
                    item.ImageKey = extension;

                    if (SmallImageList.Images.ContainsKey(extension) == false)
                    {
                        var icon = IconTools.GetIconForFile(fileName, ShellIconSize.SmallIcon);
                        if (icon != null)
                        {
                            SmallImageList.Images.Add(icon);
                            SmallImageList.Images.SetKeyName(SmallImageList.Images.Count - 1, extension);
                        }
                    }
                }
                catch
                {
                }
                item.Tag = processModule;

                list.Add(item);
            }
        }
        catch
        {
        }

        BeginUpdate();
        Items.Clear();
        Items.AddRange(list.ToArray());
        AutoResizeColumns();
        EndUpdate();
    }
}
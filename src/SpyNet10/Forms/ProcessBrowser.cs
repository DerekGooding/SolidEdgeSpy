using System.ComponentModel;
using System.Diagnostics;

namespace SpyNet10.Forms;

public partial class ProcessBrowser : UserControl
{
    public ProcessBrowser() => InitializeComponent();

    private void ProcessBrowser_Load(object sender, EventArgs e) => RefreshProcessInformation();

    private void buttonRefresh_Click(object sender, EventArgs e) => RefreshProcessInformation();

    public void RefreshProcessInformation()
    {
        Cursor.Current = Cursors.WaitCursor;

        if (ProcessId == 0) return;

        try
        {
            processModuleListView.Items.Clear();
            var process = Process.GetProcessById(ProcessId);
            processModuleListView.SetItems(process.Modules);
        }
        catch
        {
        }

        Cursor.Current = Cursors.Default;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ProcessId
    {
        get;
        set
        {
            field = value;

            try
            {
                RefreshProcessInformation();
            }
            catch
            {
            }
        }
    }

    private void toolStripMenuItem1_Click(object sender, EventArgs e)
    {
        try
        {
            if (processModuleListView.SelectedItems.Count == 1)
            {
                var listViewItem = processModuleListView.SelectedItems[0];
                if (listViewItem.Tag is ProcessModule processModule)
                {
                    var info = new NativeMethods.SHELLEXECUTEINFO();
                    info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
                    info.lpVerb = "properties";
                    info.lpFile = processModule.FileName;
                    info.nShow = NativeMethods.SW_SHOW;
                    info.fMask = NativeMethods.SEE_MASK_INVOKEIDLIST;
                    NativeMethods.ShellExecuteEx(ref info);
                }
            }
        }
        catch
        {
        }
    }

    private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
    {
        if (processModuleListView.SelectedItems.Count != 1)
        {
            e.Cancel = true;
        }
    }
}
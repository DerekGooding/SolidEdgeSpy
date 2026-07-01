//using SpyNet10.Extensions;
namespace SpyNet10.Forms;

public partial class EventMonitor : UserControl
{
    public const int EventImageIndex = 0;

    public EventMonitor() => InitializeComponent();

    private void EventBrowser_Load(object sender, EventArgs e) => SetupImageList();

    private void buttonErase_Click(object sender, EventArgs e) => listView.Items.Clear();

    public void LogEvent(EventMonitorItem item)
    {
        try
        {
            LogEvents([item]);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    public void LogEvents(EventMonitorItem[] items)
    {
        try
        {
            if (items.Length > 0)
            {
                listView.BeginUpdate();

                foreach (var item in items)
                {
                    item.ImageIndex = EventImageIndex;
                }

                listView.Items.AddRange(items);
                listView.AutoResizeColumns();
                listView.FocusedItem = null;
                listView.SelectedItems.Clear();

                items[items.Length - 1].EnsureVisible();
                items[items.Length - 1].Selected = true;

                listView.EndUpdate();
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void SetupImageList()
    {
        listView.SmallImageList = new ImageList
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(16, 16)
        };
        listView.SmallImageList.Images.Add(Resources.Event_16x16);
    }

    private void buttonSelectEvents_ButtonClick(object sender, EventArgs e)
    {
        var dialog = new SelectEventsDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
        }
    }
}

public class EventMonitorItem : ListViewItem
{
    public EventMonitorItem(string eventString, string environmentName, string environmentCaption, string environmentCATID)
        : base([eventString, environmentName, environmentCaption, environmentCATID])
    {
    }
}
using System.ComponentModel;

namespace SpyNet10.Forms;

public class TabControlEx : TabControl
{
    public TabControlEx()
        : base()
    {
    }

    protected override void WndProc(ref Message m)
    {
        var bHandled = false;

        if (!DesignMode)
        {
            // Hide tabs by trapping the TCM_ADJUSTRECT message
            if (m.Msg == 0x1328)
            {
                if (!HeaderVisible)
                {
                    m.Result = (IntPtr)1;
                    bHandled = true;
                }
            }
        }

        if (!bHandled)
        {
            base.WndProc(ref m);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HeaderVisible { get; set; } = true;
}
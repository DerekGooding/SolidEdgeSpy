using System.ComponentModel;

namespace SpyNet10.Forms;

//http://msdn.microsoft.com/en-us/library/vstudio/ms404304.aspx
public class ToolStripSpringTextBox : ToolStripTextBox
{
    public event EventHandler TextAccepted;

    public ToolStripSpringTextBox() => this.Text = InactiveText;

    public override string Text
    {
        get
        {
            if (base.Text.Equals(InactiveText))
            {
                return string.Empty;
            }

            return base.Text;
        }

        set => base.Text = value;
    }

    protected override void OnGotFocus(EventArgs e)
    {
        if (base.Text.Equals(InactiveText))
        {
            base.Text = string.Empty;
        }

        base.OnGotFocus(e);
    }

    protected override bool ProcessCmdKey(ref Message m, Keys keyData)
    {
        if (keyData == Keys.Enter)
        {
            if (TextAccepted != null)
            {
                TextAccepted(this, new EventArgs());
            }

            return true;
        }

        return base.ProcessCmdKey(ref m, keyData);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(this.Text))
        {
            this.Text = InactiveText;
        }

        base.OnLostFocus(e);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);

        if (!this.Focused)
        {
            if (string.IsNullOrEmpty(this.Text))
            {
                this.Text = InactiveText;
            }
        }
    }

    public override Size GetPreferredSize(Size constrainingSize)
    {
        if (DesignMode) return DefaultSize;

        // Use the default size if the text box is on the overflow menu
        // or is on a vertical ToolStrip.
        if (IsOnOverflow || Owner.Orientation == Orientation.Vertical)
        {
            return DefaultSize;
        }

        // Declare a variable to store the total available width as
        // it is calculated, starting with the display width of the
        // owning ToolStrip.
        var width = Owner.DisplayRectangle.Width;

        // Subtract the width of the overflow button if it is displayed.
        if (Owner.OverflowButton.Visible)
        {
            width = width - Owner.OverflowButton.Width -
                Owner.OverflowButton.Margin.Horizontal;
        }

        // Declare a variable to maintain a count of ToolStripSpringTextBox
        // items currently displayed in the owning ToolStrip.
        var springBoxCount = 0;

        foreach (ToolStripItem item in Owner.Items)
        {
            // Ignore items on the overflow menu.
            if (item.IsOnOverflow) continue;

            if (item is ToolStripSpringTextBox)
            {
                // For ToolStripSpringTextBox items, increment the count and
                // subtract the margin width from the total available width.
                springBoxCount++;
                width -= item.Margin.Horizontal;
            }
            else
            {
                // For all other items, subtract the full width from the total
                // available width.
                width = width - item.Width - item.Margin.Horizontal;
            }
        }

        // If there are multiple ToolStripSpringTextBox items in the owning
        // ToolStrip, divide the total available width between them.
        if (springBoxCount > 1) width /= springBoxCount;

        // If the available width is less than the default width, use the
        // default width, forcing one or more items onto the overflow menu.
        if (width < DefaultSize.Width) width = DefaultSize.Width;

        // Retrieve the preferred size from the base class, but change the
        // width to the calculated width.
        var size = base.GetPreferredSize(constrainingSize);
        size.Width = width;
        return size;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string InactiveText
    {
        get;
        set
        {
            field = value;
            Text = field;
        }
    }
}
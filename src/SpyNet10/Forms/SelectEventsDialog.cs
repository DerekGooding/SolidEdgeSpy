using System.ComponentModel;

namespace SpyNet10.Forms;

public partial class SelectEventsDialog : Form
{
    public SelectEventsDialog() => InitializeComponent();

    private void SelectEventsDialog_Load(object sender, EventArgs e)
    {
    }

    private void buttonOK_Click(object sender, EventArgs e)
    {
        DialogResult = System.Windows.Forms.DialogResult.OK;
        Close();
    }

    private void buttonCancel_Click(object sender, EventArgs e)
    {
        DialogResult = System.Windows.Forms.DialogResult.Cancel;
        Close();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Type[] EventTypes { get; set; } = [];
}
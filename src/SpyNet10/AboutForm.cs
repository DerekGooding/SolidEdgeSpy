using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SpyNet10;

public partial class AboutForm : Form
{
    public AboutForm() => InitializeComponent();

    private void AboutForm_Load(object sender, EventArgs e)
    {
        var link = new LinkLabel.Link
        {
            LinkData = Resources.CodePlexUrl
        };
        linkCodeplex.Links.Add(link);

        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName();

        var asemblyCompanyAttribute
            = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)
            .OfType<AssemblyCompanyAttribute>()
            .FirstOrDefault();
        var assemblyDescriptionAttribute
            = assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
            .OfType<AssemblyDescriptionAttribute>()
            .FirstOrDefault();

        var items = new List<ListViewItem>();

        if (assemblyDescriptionAttribute != null)
        {
            items.Add(new ListViewItem(["Description", assemblyDescriptionAttribute.Description]));
        }

        if (asemblyCompanyAttribute != null)
        {
            items.Add(new ListViewItem(["Author", asemblyCompanyAttribute.Company]));
        }

        items.Add(new ListViewItem(["Version", assemblyName.Version.ToString()]));
        items.Add(new ListViewItem(["Website", Resources.CodePlexUrl]));
        items.Add(new ListViewItem([".NET Runtime Version", assembly.ImageRuntimeVersion]));
        items.Add(new ListViewItem(["Solid Edge Version", GetSolidEdgeVersion()]));

        listView.Items.AddRange([.. items]);
        listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
    }

    private void ButtonClose_Click(object sender, EventArgs e) => Close();

    private void LinkCodeplex_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(e.Link.LinkData as string);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private string GetSolidEdgeVersion()
    {
        object installData = null;

        try
        {
            var type = Type.GetTypeFromProgID("SolidEdge.InstallData");

            if (type != null)
            {
                installData = Activator.CreateInstance(type);

                var version = installData.GetType().InvokeMember("GetVersion", BindingFlags.InvokeMethod, null, installData, null);

                if (version != null)
                {
                    return version.ToString();
                }
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
        finally
        {
            if (installData != null)
            {
                Marshal.FinalReleaseComObject(installData);
            }
        }

        return string.Empty;
    }
}
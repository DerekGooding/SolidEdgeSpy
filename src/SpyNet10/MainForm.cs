using SolidEdgeCommunity;
using SpyNet10.Extensions;
using SpyNet10.Forms;
using SpyNet10.InteropServices;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace SpyNet10;

public partial class MainForm : Form, SolidEdgeFramework.ISEApplicationEvents
{
    private SolidEdgeFramework.Application _application;
    private Dictionary<IConnectionPoint, int> _connectionPoints = new();
    private ConcurrentQueue<SpyNet10.Forms.EventMonitorItem> _eventQueue = new();
    private static AutoResetEvent _uiAutoResetEvent = new(false);
    private ConnectionPointController _connectionPointController;

    private const int TabPageObjectBrowserIndex = 0;
    private const int TabPageTypeBrowserIndex = 1;
    private const int TabPageCommandBrowserIndex = 2;
    private const int TabPageEventMonitorIndex = 3;
    private const int TabPageGlobalParametersIndex = 4;
    private const int TabPageProcessBrowserIndex = 5;

    public MainForm()
    {
        this.Font = SystemFonts.MessageBoxFont;
        InitializeComponent();
        _connectionPointController = new ConnectionPointController(this);
    }

    private void Application_Load(object sender, EventArgs e)
    {
        try
        {
            // Register with OLE to handle concurrency issues on the current thread.
            OleMessageFilter.Register();

            PreloadTypeLibraries();

            ComTypeManager.Instance.ComTypeLibrarySelected += Instance_ComTypeLibrarySelected;
            ComTypeManager.Instance.ComTypeInfoSelected += Instance_ComTypeInfoSelected;
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }

        SetupToolStripManager();
    }

    private void Application_FormClosing(object sender, FormClosingEventArgs e)
    {
        try
        {
            OleMessageFilter.Unregister();
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void startupTimer_Tick(object sender, EventArgs e)
    {
        startupTimer.Enabled = false;

        if (ConnectToSolidEdge() == false)
        {
            startupTimer.Enabled = true;
        }
    }

    private void eventMonitorTimer_Tick(object sender, EventArgs e)
    {
        var items = new List<EventMonitorItem>();
        EventMonitorItem item = null;

        while (_eventQueue.TryDequeue(out item))
        {
            items.Add(item);
        }

        eventMonitor.LogEvents(items.ToArray());

        if (_application != null)
        {
            if (commandBrowser.ActiveEnvironment == null)
            {
                commandBrowser.ActiveEnvironment = _application.GetActiveEnvironment();
            }
        }
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        DisconnectFromSolidEdge(false);
        Close();
    }

    private void projectWebsiteToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(Resources.CodePlexUrl);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void projectForumsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(Resources.CodePlexDiscussionsUrl);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void documentationToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(Resources.CodePlexDocumentationurl);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void solidEdgeCommunityToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(Resources.GitHubSolidEdgeCommunityUrl);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void githubSamplesForSolidEdgeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(Resources.GitHubSamples);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void nugetInteropSolidEdgeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(Resources.NuGetInteropSolidEdge);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void nugetSolidEdgeCommunityToolStripMenuItem1_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(Resources.NuGetSolidEdgeCommunity);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void nugetSolidEdgeCommunityReaderToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(Resources.NuGetSolidEdgeCommunityReader);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            using (var form = new AboutForm())
            {
                form.ShowDialog(this);
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private void objectBrowserToolStripMenuItem_Click(object sender, EventArgs e) => tabControl.SelectedIndex = TabPageObjectBrowserIndex;

    private void typeBrowserToolStripMenuItem_Click(object sender, EventArgs e) => tabControl.SelectedIndex = TabPageTypeBrowserIndex;

    private void commandBrowserToolStripMenuItem_Click(object sender, EventArgs e) => tabControl.SelectedIndex = TabPageCommandBrowserIndex;

    private void eventMonitorToolStripMenuItem_Click(object sender, EventArgs e) => tabControl.SelectedIndex = TabPageEventMonitorIndex;

    private void globalParametersToolStripMenuItem_Click(object sender, EventArgs e) => tabControl.SelectedIndex = TabPageGlobalParametersIndex;

    private void Instance_ComTypeInfoSelected(object sender, ComTypeInfo comTypeInfo) => tabControl.SelectedIndex = TabPageTypeBrowserIndex;

    private void Instance_ComTypeLibrarySelected(object sender, ComTypeLibrary comTypeLibrary) => tabControl.SelectedIndex = TabPageTypeBrowserIndex;

    private void processBrowserToolStripMenuItem_Click(object sender, EventArgs e) => tabControl.SelectedIndex = TabPageProcessBrowserIndex;

    private void SetupToolStripManager()
    {
        try
        {
            ToolStripManager.RenderMode = ToolStripManagerRenderMode.Professional;

            var renderer = ToolStripManager.Renderer as ToolStripProfessionalRenderer;
            if (renderer != null)
            {
                renderer.RoundedEdges = false;
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    private bool ConnectToSolidEdge()
    {
        ComPtr pApplication = IntPtr.Zero;

        try
        {
            if (MarshalEx.Succeeded(MarshalEx.GetActiveObject("SolidEdge.Application", out pApplication)))
            {
                _application = pApplication.TryGetUniqueRCW<SolidEdgeFramework.Application>();
                _connectionPointController.AdviseSink<SolidEdgeFramework.ISEApplicationEvents>(_application);

                commandBrowser.ActiveEnvironment = _application.GetActiveEnvironment();
                globalParameterBrowser.RefreshGlobalParameters();

                objectBrowser.Connect();

                // Older versions of Solid Edge don't have the ProcessID property.
                try
                {
                    processBrowser.ProcessId = _application.ProcessID;
                }
                catch
                {
                }

                return true;
            }
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
        finally
        {
            pApplication.Dispose();
        }

        return false;
    }

    private void DisconnectFromSolidEdge(bool resetStartupTimer)
    {
        _connectionPointController.UnadviseAllSinks();

        HandleAutoResetEvent();

        globalParameterBrowser.SelectedObject = null;

        try
        {
            Marshal.FinalReleaseComObject(_application);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }

        startupTimer.Enabled = resetStartupTimer;

        _application = null;
    }

    private void HandleAutoResetEvent()
    {
        objectBrowser.Disconnect();
        globalParameterBrowser.RefreshGlobalParameters();
        _uiAutoResetEvent.Set();
    }

    private void PreloadTypeLibraries()
    {
        try
        {
            var version = new Version(1, 0);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.RevisionManager, version);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.SEInstallDataLib, version);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.SolidEdgeAssembly, version);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.SolidEdgeConstants, version);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.SolidEdgeDraft, version);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.SolidEdgeFileProperties, version);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.SolidEdgeFramework, version);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.SolidEdgeFrameworkSupport, version);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.SolidEdgeGeometry, version);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.SolidEdgePart, version);
            ComTypeManager.Instance.LoadRegTypeLib(TypeLibGuid.StructureEditor, version);
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }
    }

    public SolidEdgeFramework.Application Application => _application;

    #region SolidEdgeFramework.ISEApplicationEvents

    public void AfterActiveDocumentChange(object theDocument)
    {
        var eventString = Resources.AfterActiveDocumentChangeFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theDocument.SafeInvokeGetProperty("Name", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));

        this.BeginInvokeIfRequired(frm => frm.HandleAutoResetEvent());

        _uiAutoResetEvent.WaitOne(2000);
        _uiAutoResetEvent.Reset();
    }

    public void AfterCommandRun(int theCommandID)
    {
        var eventString = Resources.AfterCommandRunFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, CommandHelper.ResolveCommandId(_application, theCommandID));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));
    }

    public void AfterDocumentOpen(object theDocument)
    {
        var eventString = Resources.AfterDocumentOpenFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theDocument.SafeInvokeGetProperty("Name", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));

        this.BeginInvokeIfRequired(frm => frm.HandleAutoResetEvent());

        _uiAutoResetEvent.WaitOne(2000);
        _uiAutoResetEvent.Reset();
    }

    public void AfterDocumentPrint(object theDocument, int hDC, ref double ModelToDC, ref int Rect)
    {
        var eventString = Resources.AfterDocumentPrintFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theDocument.SafeInvokeGetProperty("Name", "IUnknown"), hDC, ModelToDC, Rect);
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));
    }

    public void AfterDocumentSave(object theDocument)
    {
        var eventString = Resources.AfterDocumentSaveFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theDocument.SafeInvokeGetProperty("Name", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));
    }

    public void AfterEnvironmentActivate(object theEnvironment)
    {
        var eventString = Resources.AfterEnvironmentActivateFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;
        SolidEdgeFramework.Environment environment = null;

        try
        {
            environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            commandBrowser.BeginInvokeIfRequired(ctl => ctl.ActiveEnvironment = environment);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theEnvironment.SafeInvokeGetProperty("Name", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));

        //this.BeginInvokeIfRequired(frm =>
        //{
        //    frm.RefreshGlobalPropertiesPropertyGrid();
        //});
    }

    public void AfterNewDocumentOpen(object theDocument)
    {
        var eventString = Resources.AfterNewDocumentOpenFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theDocument.SafeInvokeGetProperty("Name", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));

        this.BeginInvokeIfRequired(frm => frm.HandleAutoResetEvent());

        _uiAutoResetEvent.WaitOne(2000);
        _uiAutoResetEvent.Reset();
    }

    public void AfterNewWindow(object theWindow)
    {
        var eventString = Resources.AfterNewWindowFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theWindow.SafeInvokeGetProperty("Caption", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));
    }

    public void AfterWindowActivate(object theWindow)
    {
        var eventString = Resources.AfterWindowActivateFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theWindow.SafeInvokeGetProperty("Caption", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));
    }

    public void BeforeCommandRun(int theCommandID)
    {
        var eventString = Resources.BeforeCommandRunFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, CommandHelper.ResolveCommandId(_application, theCommandID));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));
    }

    public void BeforeDocumentClose(object theDocument)
    {
        var eventString = Resources.BeforeDocumentCloseFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theDocument.SafeInvokeGetProperty("Name", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));

        try
        {
            var document = theDocument as SolidEdgeFramework.SolidEdgeDocument;

            if ((document != null) && (document.IsTemporary() == false))
            {
                this.BeginInvokeIfRequired(frm => frm.HandleAutoResetEvent());

                _uiAutoResetEvent.WaitOne(2000);
                _uiAutoResetEvent.Reset();
            }
        }
        catch
        {
        }
    }

    public void BeforeDocumentPrint(object theDocument, int hDC, ref double ModelToDC, ref int Rect)
    {
        var eventString = Resources.BeforeDocumentPrintFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theDocument.SafeInvokeGetProperty("Name", "IUnknown"), hDC, ModelToDC, Rect);
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));
    }

    public void BeforeDocumentSave(object theDocument)
    {
        var eventString = Resources.BeforeDocumentSaveFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theDocument.SafeInvokeGetProperty("Name", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));
    }

    public void BeforeEnvironmentDeactivate(object theEnvironment)
    {
        var eventString = Resources.BeforeEnvironmentDeactivateFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theEnvironment.SafeInvokeGetProperty("Name", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));
    }

    public void BeforeQuit()
    {
        var eventString = Resources.BeforeQuitFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));

        this.BeginInvokeIfRequired(frm => frm.DisconnectFromSolidEdge(true));

        _uiAutoResetEvent.WaitOne(2000);
        _uiAutoResetEvent.Reset();
    }

    public void BeforeWindowDeactivate(object theWindow)
    {
        var eventString = Resources.BeforeWindowDeactivateFormat;
        var environmentName = string.Empty;
        var environmentCaption = string.Empty;
        var environmentCATID = string.Empty;

        try
        {
            var environment = _application.GetActiveEnvironment();
            environment.GetInfo(out environmentName, out environmentCaption, out environmentCATID);
        }
        catch
        {
        }

        try
        {
            eventString = string.Format(eventString, theWindow.SafeInvokeGetProperty("Caption", "IUnknown"));
        }
        catch
        {
        }

        _eventQueue.Enqueue(new Forms.EventMonitorItem(eventString, environmentName, environmentCaption, environmentCATID));
    }

    #endregion SolidEdgeFramework.ISEApplicationEvents
}
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Microsoft.Win32;
using Microsoft.VisualStudio;

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;


namespace ClassOutline
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading =true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(ClassOutlineToolWindow))]
    [Guid(GuidList.guidVSPackage1PkgString)]
    [ProvideService(typeof(IClassOutlineSettingsProvider), IsAsyncQueryable = true)]
    [ProvideOptionPage(typeof(OptionPageGrid), "Firefly Community", "Class Outline", 0, 0, true )]
    public sealed class VSPackage1Package : AsyncPackage, IClassOutlineSettingsProvider
    {

        public bool FireflyImagesEnabled
        {
            get
            {
                var p =(OptionPageGrid) GetDialogPage(typeof (OptionPageGrid));
                return p.FireflyImagesEnabled;
            }
            set
            {
                var p = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
                p.FireflyImagesEnabled = value;

            }

        }
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VSPackage1Package()
        {
            XmlConfigurator.Configure(new FileInfo("logging.config"));
            var logger = LogManager.GetLogger(this.GetType());
            logger.Debug("Startup");

            AsyncServiceCreatorCallback cc = CreateServiceAsync;
            this.AddService(typeof(IClassOutlineSettingsProvider), cc, true );
            registerExceptionHandler();
        }


        private void registerExceptionHandler()
        {
            // Register Unhandled Exception Handler
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception ex = (Exception)args.ExceptionObject;

            var logger = LogManager.GetLogger(this.GetType());
            string ErrorMessage = String.Format(
            "Error: {0}\r\n" +
            "Runtime Terminating: {1}\r\n----- ----- ----- ----- ----- -----\r\n\r\n" +
            "{2}\r\n\r\n####################################\r\n",
                ex.Message,
                args.IsTerminating,
                ex.StackTrace.Trim());

            logger.Fatal("Fatal Error:"+ErrorMessage);

        }

        private async Task<object> CreateServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            if (typeof(IClassOutlineSettingsProvider) == serviceType)
            {
                return this;
            }

            return null;
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(ClassOutlineToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members


        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            await base.InitializeAsync(cancellationToken, progress);

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidVSPackage1CmdSet, (int)PkgCmdIDList.cmdidMyTool);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand( menuToolWin );
            }
        }
        #endregion

    }

    [Guid("62629AEE-9993-46FA-807D-36E28BEB0AAF")]
    [ComVisible(true )]
    public interface IClassOutlineSettingsProvider
    {
        bool FireflyImagesEnabled { get; set; }
    }

    public class OptionPageGrid : DialogPage
    {
        private bool _fireflyImagesEnabled;

        [Category("General")]
        [DisplayName("Highlight UIController")]
        [Description("Use special icons for UI Controller classes")]
        [DefaultValue(true)]
        public bool FireflyImagesEnabled
        {
            get { return _fireflyImagesEnabled; }
            set { _fireflyImagesEnabled = value; }
        }
    }
}

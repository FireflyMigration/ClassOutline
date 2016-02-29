using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClassOutline.Annotations;
using ClassOutline.ControlLibrary;
using ClassOutline.Logging;
using ClassOutline.Services;
using ClassOutline.TreeNodes;
using EnvDTE;
using EnvDTE80;
using log4net;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Image = System.Windows.Controls.Image;
using MenuItem = System.Windows.Controls.MenuItem;
using Orientation = System.Windows.Controls.Orientation;
using Task = System.Threading.Tasks.Task;
using UserControl = System.Windows.Controls.UserControl;
using Window = EnvDTE.Window;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using CodeNamespace = EnvDTE.CodeNamespace;

namespace ClassOutline
{
    /// <summary>
    ///     Interaction logic for ClassOutlineControl.xaml
    /// </summary>
    public partial class ClassOutlineControl : UserControl, INotifyPropertyChanged
    {
        private ILog _log = LogManager.GetLogger(typeof (ClassOutlineControl));

        private const int MAX_PATH = 260;
        private const int IMAGE_NONE = -1;
       
        private const int S_OK = 0;
     

        private readonly Lazy<CodeElementHelper> _codeElementHelper =
            new Lazy<CodeElementHelper>(() => new CodeElementHelper());

        private DTE _dte = null ;
        private  DTE2 _dte2 = null ;
       
    
  
        private Timer _codeElementSyncTimer;
        private Lazy<DocumentEvents> _documentEvents;
     
        private Lazy<Events> _eventRoot;
        private ImageCache _imageCache;
        private CodeElement _previousCodeElement;
        private List<ICodeRegion> _regions;
        private string _shownWindow = string.Empty;
        private Lazy<SolutionEvents> _solutionEvents;
        private bool _syncing;
        // a dictionary of names/signatures of all elements we encouter in the code file and their location in the file
        private List<EncounteredCodeElement> _treeElements;
        private Lazy<WindowEvents> _windowEvents;
       // private OutlineItem _selected;
        private OutlineItem _data;
      
       
        private Lazy<IClassOutlineSettingsProvider> _settingService;
        private DteInitializer _dteInitializer;
        private Lazy<SelectionEvents> _selectionEvents;
        private ProjectItem _displayedProjectItem;

        internal class DteInitializer : IVsShellPropertyEvents
        {
            private IVsShell shellService;
            private uint cookie;
            private Action callback;

            internal DteInitializer(IVsShell shellService, Action callback)
            {
                int hr;

                this.shellService = shellService;
                this.callback = callback;

                // if not in zombie-mode, invoke callback
                if (!zombieMode(shellService))
                {
                    callback();
                    return;
                }
                // Set an event handler to detect when the IDE is fully initialized
                hr = this.shellService.AdviseShellPropertyChanges(this, out this.cookie);
                
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
            }

            private bool zombieMode(IVsShell vsShell)
            {
                object initialised = false;

                var result = vsShell.GetProperty((int)__VSSPROPID4.VSSPROPID_ShellInitialized, out initialised );
                if (result == 0)
                {
                    return (bool) initialised == false;
                }
                return true;
            }

            int IVsShellPropertyEvents.OnShellPropertyChange(int propid, object var)
            {
                int hr;
                bool isZombie;

                if (propid == (int)__VSSPROPID.VSSPROPID_Zombie)
                {
                    isZombie = (bool)var;

                    if (!isZombie)
                    {
                        // Release the event handler to detect when the IDE is fully initialized
                        hr = this.shellService.UnadviseShellPropertyChanges(this.cookie);

                        Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);

                        this.cookie = 0;

                        this.callback();
                    }
                }
                return VSConstants.S_OK;
            }
        }

        private ImageCache ImageCache
        {
            get
            {
                if (_imageCache == null) _imageCache = new ImageCache(getImagesFolder());
                return _imageCache;
            }
        }
        private void InitializeDTE()
        {
            IVsShell shellService;
  shellService = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell;

            _dteInitializer = new DteInitializer(shellService, () =>
            {

                _dte = ServiceProvider.GlobalProvider.GetService(typeof (DTE)) as DTE;

                _dteInitializer = null;
                _dte2 = ServiceProvider.GlobalProvider.GetService(typeof (DTE)) as DTE2;

                _settingService = new Lazy<IClassOutlineSettingsProvider>(() =>
                    ServiceProvider.GlobalProvider.GetService(typeof (IClassOutlineSettingsProvider)) as
                        IClassOutlineSettingsProvider);


                CodeSyncTimerInteral = 1000; // every 2 secs

                addDTEEventHandlers();
                InitializeComponent();


                Application.ResourceAssembly = Assembly.GetExecutingAssembly(); // define our resource assembly

                refreshButton.Click += refreshButton_Click; // hook up buttons

                refreshToolWindows();
            });

        }


        public ClassOutlineControl()
        {
            InitializeDTE();
        }

        /// <summary>
        ///     Reference to environment app
        /// </summary>
        public DTE2 DTE => _dte2;

        /// <summary>
        ///     Reference to current document that is being "tracked"
        ///     i.e., this is the last document we outlined
        /// </summary>
        public Document CurrentDoc { get; private set; }

        public int CodeSyncTimerInteral { get; set; }

     

        private string getImagesFolder()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            return Path.Combine(folder, "ClassOutlineImages");
        }

        private void addDTEEventHandlers()
        {
            _eventRoot = new Lazy<Events>(() => _dte2.Events);

            _solutionEvents = new Lazy<SolutionEvents>(() => _eventRoot.Value.SolutionEvents);
            _documentEvents = new Lazy<DocumentEvents>(() => _eventRoot.Value.DocumentEvents);
            _windowEvents = new Lazy<WindowEvents>(() => _eventRoot.Value.WindowEvents);
            _selectionEvents = new Lazy<SelectionEvents>(() => _eventRoot.Value.SelectionEvents);
            _selectionEvents.Value.OnChange += new _dispSelectionEvents_OnChangeEventHandler(onSelectionChanged );
            _documentEvents.Value.DocumentSaved += onDocumentEventHandler;

            _documentEvents.Value.DocumentOpened += onDocumentEventHandler;
          
            _solutionEvents.Value.AfterClosing += onSolutionClosedEventHandler;
            _eventRoot.Value.DTEEvents.OnBeginShutdown += detachDTEEventHandlers;

            _windowEvents.Value.WindowActivated += onWindowEventHandler;

            // setup the visual studio logger
            VisualStudioOutputLogger.DTE = _dte;

        }

        private void onSelectionChanged()
        {

            OutlineCode();
        }

        private void onWindowEventHandler(Window gotfocus, Window lostfocus)
        {
            if (gotfocus.Kind != "Document")
            {
                Debug.WriteLine($"Not refreshing: WindowKind = {gotfocus.Kind}");
                return;
            }

            if (!string.IsNullOrEmpty(gotfocus.Caption) && !string.IsNullOrEmpty(_shownWindow))
            {
                if (gotfocus.Caption == _shownWindow)
                    return;
            }
            _shownWindow = gotfocus.Caption;


            refreshToolWindows();
        }

        private void onSolutionClosedEventHandler()
        {
            refreshToolWindows();
        }

        private void onDocumentEventHandler(Document document)
        {
            refreshToolWindows();
        }

        private void detachDTEEventHandlers()
        {
          
            _solutionEvents.Value.AfterClosing -= onSolutionClosedEventHandler;
            _documentEvents.Value.DocumentSaved -= onDocumentEventHandler;
            _documentEvents.Value.DocumentOpened -= onDocumentEventHandler;
            _windowEvents.Value.WindowActivated -= onWindowEventHandler;
        }

        private void refreshToolWindows()
        {
            OutlineCode();
        }

        // when refresh button is clicked outline code from scratch
        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
          
                OutlineCode();
            
        }

        /// <summary>
        ///     Clear all elements in the tree
        /// </summary>
        public void ClearElements()
        {
            
            CurrentDoc = null;
        }

        public  void OutlineCode()
        {
            if(noActiveWindow())
            {
                clearOutline();
            
            }

            var projectItem = getSelectedProjectItem();
            if(projectItem!=null) OutlineCode(projectItem);
        }

        private ProjectItem getSelectedProjectItem()
        {
            return DTE?.ActiveWindow?.ProjectItem;

            UIHierarchy uih = _dte2.ToolWindows.SolutionExplorer;
            Array selectedItems = (Array)uih.SelectedItems;

            foreach (UIHierarchyItem item in selectedItems)
            {
                var pi = item.Object as ProjectItem;
                if (pi != null) return pi;
            }
            return null;
        }

        private bool noActiveWindow()
        {
            try
            {
                return DTE == null || DTE.ActiveWindow == null || DTE.ActiveDocument == null ||
                       DTE.ActiveDocument.ProjectItem == null;
            }
            catch (ArgumentException e)
            {
                return true;
            }
            return false;
        }

        private static bool TryGetHierarchyProperty<T>(IVsHierarchy hierarchy, uint itemid, int propid, out T value)
        {
            object obj;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemid, propid, out obj)) && obj is T)
            {
                value = (T) obj;
                return true;
            }

            value = default(T);
            return false;
        }

        private static bool TryGetHierarchyProperty<T>(IVsHierarchy hierarchy, uint itemid, int propid,
            Func<object, T> converter, out T value)
        {
            object obj;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemid, propid, out obj)))
            {
                value = converter(obj);
                return true;
            }

            value = default(T);
            return false;
        }


        /// <summary>
        ///     Performs outline of activated code file
        /// </summary>
        public async Task OutlineCode(ProjectItem activeProjectItem)
        {
            CodeElements elements;
            _displayedProjectItem = activeProjectItem;
            clearOutline();
           
            // get code elements in file
            try
            {
                elements = activeProjectItem.FileCodeModel.CodeElements;
            }
            catch(Exception e)
            
            {
                _log.Debug("Error finding elements in OutlineCode", e);
                return;
            }
            try
            {
                // get the regions
                var d = activeProjectItem.Document;
                var c = new RegionParser();

                var regionTask = c.GetRegions(d);
                var vp = new ViewParser();


                Data = new OutlineItem();

                // "expand" each element
                for (var i = 1; i <= elements.Count; i++)
                {
                    createClassList(elements.Item(i), Data);


                }

                // set the "tracked" document to the current document
                CurrentDoc = d;

                var regions = await regionTask;


                Debug.WriteLine("Regions completed");

                initSynctimer();

                _regions = new List<ICodeRegion>();
                if(regions!=null) _regions.AddRange(regions);


                //var result = vp.FindViews(activeProjectItem, views);
                Data.AddRegions(_regions);

                selectActiveCodeInTree();
                Data.IsExpanded = true;
                if (Data.Children.Any()) Data.Children.First().IsExpanded = true;

            }
            catch (Exception e)
            {
                _log.Error("Failed OutlineCode2", e);
            }
        }

       
        public OutlineItem Data
        {
            get { return _data; }
            set
            {
                if (_data == value) return;
                try
                {
                    _data = value;
                    if (tvOutline != null)
                    {
                        tvOutline.DataContext = _data;
                        if (_data != null && _data.Children.Any())
                        {
                            tvOutline.Select(_data.Children.First());
                        }
                    }
                    OnPropertyChanged();
                }
                catch (Exception e)
                {
                    _log.Error("Failed OutlineItem.Data(set)", e );
                }
            }
        }

        public bool FireflyImagesEnabled
        {
            get
            {
                var b = _settingService.Value.FireflyImagesEnabled;
                return b;
            }
            set
            {
                _settingService.Value.FireflyImagesEnabled = value;

                OnPropertyChanged();
                
            }
        }


        private void initSynctimer()
        {
            _codeElementSyncTimer = new Timer
            {
                Interval = CodeSyncTimerInteral,
                Enabled = true
            };
            _codeElementSyncTimer.Tick += CodeElementSyncTimerOnTick;
        }

        private void CodeElementSyncTimerOnTick(object sender, EventArgs eventArgs)
        {
            selectActiveCodeInTree();
        }

        private void selectActiveCodeInTree()
        {
            if (_syncing) return;
            _syncing = true;

            try
            {
                var currentCodeElement = _codeElementHelper.Value.GetCodeElementAtCursor(_dte);
                
                if (currentCodeElement != null)
                {
                    if (currentCodeElement.ProjectItem != _displayedProjectItem)
                    {
                       var t= OutlineCode(currentCodeElement.ProjectItem);
                        t.ContinueWith((task) =>
                        {
                            _syncing = false;
                            selectActiveCodeInTree();
                        });
                        return;
                    }
                    if (_previousCodeElement == null || !Equals(currentCodeElement,_previousCodeElement))
                    {
                        _previousCodeElement = currentCodeElement;

                            selectItemInTree(currentCodeElement );
                        
                    }
                }
            }
            catch (Exception e)
            {
               _log.Error("error selectActiveCodeInTree", e );
            }
            _syncing = false;
        }

        private bool Equals(CodeElement a, CodeElement b)
        {
            if (a.FullName == b.FullName) return true;
            return false;
        }
        private void selectItemInTree(CodeElement 
            itemToHighlight)
        {
            if (Data == null)
            {
                return;
            }
            if (itemToHighlight == null)
            {
                return;
            }
            if (itemToHighlight.FullName == null)
            {
                return;
            }
            if (tvOutline == null) return;

            var itm = Data.Find(x => x.FullName == itemToHighlight.FullName);
            
            if (itm != null)
            {
                
                tvOutline.Select(itm);
                
            }
        }

        private void clearOutline()
        {
            Data = null;

            _treeElements = new List<EncounteredCodeElement>();
           
            _previousCodeElement = null;

            _regions?.Clear();
            if (_codeElementSyncTimer != null)
            {
                _codeElementSyncTimer.Enabled = false;
                _codeElementSyncTimer = null;
            }
        }

        private void createClassList(CodeElement element, OutlineItem  parent)
        {
    

            // if it's a namespace, expand each of its members
            if (element.Kind == vsCMElement.vsCMElementNamespace)
            {
                var codeNamespace = element as CodeNamespace;
                if (codeNamespace != null)
                {
                    var members = codeNamespace.Members;

                    for (var i = 1; i <= members.Count; i++)
                        createClassList(members.Item(i), parent);
                }
            }

            // if it's a class...
            else if (element.Kind == vsCMElement.vsCMElementClass)
            {
                var cls = (CodeClass)element;
                var tn = new ClassTreeNode(cls);

                var child = new OutlineItem();
                parent.AddChild( child);
                child.Name = cls.Name;
                child.ToolTipText = createClassSummary(cls.DocComment);
                child.FullName = element.FullName;
                child.BaseTypeName = tn.GetBaseTypeName();
                child.ImageUri = new Uri("/Resources/Classes.png", UriKind.Relative);
                child.GotoCodeLocationEventHandler += gotoCodeLocation;
                child.OpenProjectItemEventHandler  += openProjectItem;
                child.ProjectItem = cls.ProjectItem;
                child.UpdateViewsEventHandler  += UpdateViewItems;
                if (FireflyImagesEnabled && child.BaseTypeName.Contains("UIController"))
                {

                    child.ImageUri = new Uri("/Resources/UIController.png", UriKind.Relative);
                }
                child.StartLineOfCode = element.StartPoint.Line ;
                child.EndLineOfCode = element.EndPoint.Line;

                var members = ((CodeClass)element).Members;

              
                // expand each of the class's members
                for (var i = 1; i <= members.Count; i++)
                    createClassList(members.Item(i), child);
            }
            // else, we are at a regular code element (field, property, method, etc.)
            else
            {
                // add methods
                if (element.Kind == vsCMElement.vsCMElementFunction)
                {
                    var f = (CodeFunction2) element;
                    var priority = getMethodPriority(f);

                    if (priority >0)
                    {
                        var signature = getSignature(f);
                        parent.AddMethod(new OutlineItem.Method() {SortOrder = -priority,Signature = signature, LineNumber = f.StartPoint.Line, Name = getFunctionName(f), FullName = f.FullName,  Category = "Override", Comment = f.DocComment ?? f.Comment });
                    }

                  
                }

                //expandNonClassElements(element, parent, items, hierarchyItem);
            }
        }

        private string getFunctionName(CodeFunction f)
        {
            var functionName = f.Name;
            if (f.FunctionKind == vsCMFunction.vsCMFunctionConstructor) functionName = "Constructor";
            return functionName;
        }
        private string getSignature(CodeFunction2 f)
        {
            var paramlist = new List<string>();
            foreach (CodeElement p in f.Parameters)
            {
                var cp = p as CodeParameter;
                if (cp != null)
                {
                    paramlist.Add(string.Format("{0}", cp.Name ));
                }
            }
         

            return getFunctionName(f) + "(" + string.Join(",", paramlist) + ")";

        }

        private void UpdateViewItems(object src, UpdateViewsEventArgs args)
        {
            // get the regions
            var item = src as OutlineItem;
            if (item == null) return;

            var activeProjectItem = item.ProjectItem;
            var d = activeProjectItem.Document;
            if (d == null)
            {
                Debug.WriteLine("No Document found for the selected item");
                return;
            }
            var c = new RegionParser();
            var vp = new ViewParser();
           
            var viewTask = vp.GetViews(d, item.StartLineOfCode, item.EndLineOfCode);
            viewTask.Wait();

            var views = viewTask.Result;
            if (views == null) return;

            vp.FindView(activeProjectItem, views);

            item.AddViews(views.Select(
                x =>
                    new OutlineItem.ViewReference()
                    {
                        CodeElement = x.CodeElement,
              
                        ViewTypeName = x.TypeName
                    }));

        }

        private void gotoView(OutlineItem item)
        {
            // get the regions
           
            var activeProjectItem =  DTE.ActiveDocument.ProjectItem;
            var d = activeProjectItem.Document;
          
            var vp = new ViewParser();

            var viewTask = vp.GetViews(d, item.StartLineOfCode, item.EndLineOfCode );
            viewTask.Wait();

            var views = viewTask.Result;
            
           vp.FindView(activeProjectItem,views);
        }

      

        private IEnumerable<CodeAssignStatement> getAllAssignments(CodeElements elements)
        {
            foreach (CodeElement e in elements)
            {
                if (e.Kind == vsCMElement.vsCMElementAssignmentStmt)
                {
                    yield return (CodeAssignStatement) e;
                }
            }

            
        }

        private void openProjectItem(object src, OpenProjectItemEventArgs args)
        {
            var o = src as OutlineItem;
            ProjectItem pi = o.ProjectItem  as ProjectItem;
            if (pi == null) return;

            var w= pi.Open();
            pi.ExpandView();
            w.Visible = true;

            var s = w?.Document?.Selection as TextSelection;
            s?.MoveTo(args.LineNumber ,0);
        }


     
        private static int getMethodPriority(CodeFunction2 f)
        {
            if (f.Name == "Run") return 100;
            if (f.FunctionKind == vsCMFunction.vsCMFunctionConstructor) return 99;
            if (f.Name.Contains("Initialize")) return 90;
            if (f.OverrideKind == vsCMOverrideKind.vsCMOverrideKindOverride) return 80;
            // dont include
            return 0;

        }

        private void gotoCodeLocation(object src, GotoCodeLocationEventArgs args )
        {
            var o = src as OutlineItem;
           displayCode( o.ProjectItem, args.LineNumber );
        }


        public static bool FindAny(CodeClass element, Func<CodeClass, bool> p)
        {
            if (p(element))
            {
                if (element.Bases.Cast<CodeClass>().Select(codeElement => FindAny(codeElement, p)).Any(tmp => tmp))
                {
                    return true;
                }
                return true;
            }
            return false;
        }
        
        private string createClassSummary(  string docComment)
        {
         
                // Note that in c#, the docComments are un-encoded xml documents
                // <doc>
                // <summary>some invalid html & stuff here</summary>
                // </doc>
                // so, lets find the summary start/end tags and just take out the text
                var summaryText = getTaggedText(docComment, "summary");
                if (summaryText == null) return null;

              

                var txt = summaryText;
                txt = txt.Trim('\n', '\r').Trim();
            

            return txt;
        }

        private string getTaggedText(string docComment, string tag)
        {
            var startTag = $"<{tag}>";
            var emptyTag = $"<{tag}/>";
            var endTag = $"</{tag}>";
            docComment = docComment.Replace(emptyTag, startTag + endTag);
            var tokens = docComment.Split(new[] {startTag}, StringSplitOptions.RemoveEmptyEntries);
            return (from t in tokens let endpos = t.IndexOf(endTag, StringComparison.Ordinal) where endpos >= 0 select t.Substring(0, endpos)).FirstOrDefault();
        }

      

      
        
   

        // double click event handler
        private void item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // activate the currently tracked document
            if (DTE.ActiveDocument == null) return;

            DTE.ActiveDocument.Activate();


            // get the name TextBlock
            //    var nameBlock = GetTreeViewItemNameBlock((TreeViewItem) sender);
            var t = (TreeViewItem) e.Source;
            if (t.DataContext  == BindingOperations.DisconnectedSource )
            {
                return;
            }

        var element = (OutlineItem)( t.DataContext );

            if (element == null)
                return;

          //  if (_selected == element) return;

            
            // if we found it, move the cursor to it in the document
            if (element.StartLineOfCode >0)
            {
                var linenumber = element.StartLineOfCode;

                displayCode(element.ProjectItem, linenumber);
            }
        }

        private void displayCode(ProjectItem projectItem, int linenumber)
        {
// get the selection object and the found element
            
           
             var w = projectItem.Open(EnvDTE.Constants.vsViewKindCode);
            w.Activate();
            var selection = (TextSelection) DTE.ActiveDocument.Selection;
            
            if (selection == null)
            {
                Debug.WriteLine("Failed to obtain selection for ActiveDocument");
                return;
            }
            // move the cursor to the location of the element
            selection.ActivePoint.TryToShow();

            selection.GotoLine(linenumber);
        }

        private void syncButton_Click(object sender, RoutedEventArgs e)
        {
            // locate and highlight the currently selected item
            selectActiveCodeInTree();
        }

   
    

        // struct to represent an encountered code element; we keep a list of these to look up their positions in the file
        // later when they're double clicked in the tree
        private struct EncounteredCodeElement
        {
           public CodeElement Element;
            public string FullName; // fully qualified name
            public TextPoint Location; // location in code file
            public string Name; // unqualified name
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void tvOutline_ItemDoubleClick(object sender, MouseButtonEventArgs args)
        {
            item_MouseDoubleClick(sender, args );
        }


        private void FireflyImagesEnabled_Checked(object sender, RoutedEventArgs e)
        {
            refreshButton_Click(sender, e);
        }
    }
   
}
 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Input;
using EnvDTE;
using log4net;

namespace ClassOutline.ControlLibrary
{
    public class GotoCodeLocationEventArgs : EventArgs
    {
        public int LineNumber { get; private set; }

        public GotoCodeLocationEventArgs(int lineNumber)
        {
            LineNumber = lineNumber;
        }
    }

    public class OpenProjectItemEventArgs : GotoCodeLocationEventArgs
    {
        public OpenProjectItemEventArgs(int lineNumber) : base(lineNumber)
        {
        }
    }
    public class UpdateViewsEventArgs : EventArgs { }

    public class OutlineItem : INotifyPropertyChanged
    {

        public EventHandler<GotoCodeLocationEventArgs> GotoCodeLocationEventHandler;
        public EventHandler<OpenProjectItemEventArgs> OpenProjectItemEventHandler;
        public EventHandler<UpdateViewsEventArgs> UpdateViewsEventHandler;
    
        private string _baseTypeName;
        private Uri _imageUri;
        private string _name;
        private string _toolTipText;
        private List<Method> _methodList;
        private List<Region> _regionList;
        private IList<ContextMenuItem> _regionMenus;
        private IList<ContextMenuItem> _methodMenus;
        private bool _isSelected;
        private bool _isExpanded;
        private IEnumerable<ViewReference> _views;
        private IList<ContextMenuItem> _usageMenus;

        public OutlineItem Find(Func<OutlineItem, bool> predicate)
        {
            if (predicate(this))
            {
                return this;
            }
            foreach (var outlineItem in Children)
            {
                var ret = outlineItem.Find(predicate);
                if (ret != null) return ret;
            }
            return null;
        }

        public OutlineItem()
        {
            IsExpanded = true;
       
            Children = new ObservableCollection<OutlineItem>();
            Children.CollectionChanged += ChildrenOnCollectionChanged;
        }

        private void ChildrenOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.OldItems != null)
            {
                foreach (var n in args.OldItems.Cast<OutlineItem>()) n.Parent = null;
            }

            if (args.NewItems != null)
            {
                foreach (var n in args.NewItems.Cast<OutlineItem>())
                {
                    n.Parent = this;
                }
            }
        }

        public string ToolTipText
        {
            get { return _toolTipText; }
            set
            {
                if (value == _toolTipText) return;
                _toolTipText = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<ContextMenuItem> MenuItems
        {
            get
            {
                var ret = createMenuItems();


                return ret;
            }
        }

        private List<ContextMenuItem> createMenuItems()
        {
            var ret = new List<ContextMenuItem>();
            // build menu items
            if (_usageMenus == null)
            {
                _usageMenus = createViewMenuItems();
                //  var m = new ContextMenuItem() {Caption = "Goto View", Command = new GotoViewCommand(GotoView, this )};
                // ret.Add(m);
            }
            if (_usageMenus != null) ret.AddRange(_usageMenus);
            
            if (_regionMenus == null)
            {
                _regionMenus = createRegionMenuItems();
            }
            if (_methodMenus == null) _methodMenus = createMethodMenuItems();


            if (_methodMenus != null)
            {
                if (ret.Any()) ret.Add(new ContextMenuItem() { Caption = "-" });
                ret.AddRange(_methodMenus);
            }
            if (_regionMenus != null)
            {
                if (ret.Any()) ret.Add(new ContextMenuItem() { Caption = "-" });
                ret.AddRange(_regionMenus);
            }
            return ret;
        }

        private void fireUpdateViews()
        {
            var u = this.UpdateViewsEventHandler;
            if(u!=null)u(this, new UpdateViewsEventArgs());

        }

        private void fireGotoCodeLocation(int linenumber)
        {
            var e = this.GotoCodeLocationEventHandler;
            if (e != null)
            {
                e(this, new GotoCodeLocationEventArgs(linenumber));
            }
        }
        private void fireOpenProjectItem(object tgt, int linenumber)
        {
            var ce = tgt as CodeElement;
            if (ce == null)
            {
                Debug.WriteLine("Cannot locate target object");
                return;
            }
            var w = ce.ProjectItem.Open();
            if (w == null)
            {
                Debug.WriteLine("Failed to open window");
                return;
            }
            w.Activate();

            //var e = this.OpenProjectItemEventHandler;
            //if (e != null)
            //{
            //    e(this, new OpenProjectItemEventArgs(linenumber ));
            //}
        }
        private IList<ContextMenuItem> createViewMenuItems()
        {
            if (_views == null ) fireUpdateViews();

            if (_views != null && _views.Any())
            {

                var ret = new List<ContextMenuItem>();

                foreach (var usage in _views)
                {
                    var m = new ContextMenuItem();

                    m.Caption = usage.ViewTypeName;
                    m.Command = new GotoViewCommand((ce,linenumber)=> fireOpenProjectItem(ce,linenumber), usage.CodeElement, usage.ViewTypeName );
                    m.ToolTipText = usage.ViewTypeName;

                    ret.Add(m);
                }

                return ret;
            }
            return null;
        }

        
        private IList<ContextMenuItem> createMethodMenuItems()
        {
            if (_methodList != null && _methodList.Any())
            {

                var ret = new List<ContextMenuItem>();

                foreach (var methodGroup in _methodList.OrderBy(x => x.SortOrder ).OrderBy(x => x.LineNumber ).GroupBy(x => x.Name ))
                {
                    
                    if (methodGroup.Count() > 1)
                    {
                        foreach (var method in methodGroup)
                        {
                            var m = new ContextMenuItem();
                            m.Caption = method.Signature ;
                            m.Command = new GotoCodeLocationCommand(method.LineNumber,(linenumber)=>fireGotoCodeLocation(linenumber ));
                            m.ToolTipText = method.Comment;

                            ret.Add(m);

                        }
                    }
                    else
                    {
                        var method = methodGroup.First();
                        var m = new ContextMenuItem();
                        m.Caption = method.Name;
                        m.Command = new GotoCodeLocationCommand(method.LineNumber, (linenumber) => fireGotoCodeLocation(linenumber));
                        m.ToolTipText = method.Comment;

                        ret.Add(m);


                    }

                }

                return ret;
            }
            return null;
        }
        private IList<ContextMenuItem> createRegionMenuItems()
        {
            if (_regionList != null && _regionList.Any())
            {

                var ret = new List<ContextMenuItem>();
                // show unique reginos by name
                var uniqueRegions =
                    _regionList.GroupBy(x => x.Name).Select(y => new { Name = y.Key, LineNumber = y.First().LineNumber });

                foreach (var r in uniqueRegions)
                {
                    var m = new ContextMenuItem();
                    m.Caption = r.Name;
                    m.Command = new GotoCodeLocationCommand(r.LineNumber, (linenumber) => fireGotoCodeLocation(linenumber));
                    /*
                    if (r.Children != null)
                    {


                        addChildMenus(r.Children, m);
                    }
                    */

                    ret.Add(m);
                }

                return ret;
            }
            return null;
        }

        private void addChildMenus(List<Region> children, ContextMenuItem parentContextMenuItem)
        {
            foreach (var r in children)
            {
                var m = new ContextMenuItem();
                m.Caption = r.Name;

                addChildMenus(r.Children, m);
                parentContextMenuItem.MenuItems.Add(m);

            }
        }

        public Uri ImageUri
        {
            get { return _imageUri; }
            set
            {
                if (Equals(value, _imageUri)) return;
                _imageUri = value;
                OnPropertyChanged();
            }
        }

        public string BaseTypeName
        {
            get { return _baseTypeName; }
            set
            {
                if (value == _baseTypeName) return;
                _baseTypeName = value;
                OnPropertyChanged();
                OnPropertyChanged("ImageName");
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Path
        {
            get { return $"{Parent?.Path}\\{Name}"; }
        }

        public OutlineItem Parent { get; set; }
        public ObservableCollection<OutlineItem> Children { get; set; }
        public object Tag { get; set; }
        public int StartLineOfCode { get; set; }
        public int EndLineOfCode { get; set; }
        public string FullName { get; set; }



        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; }
        }

        public ProjectItem ProjectItem { get; set; }

        public class ViewReference

        {
            // public Object ProjectItem { get; set; }
            // int Line { get; set; }

            //  public string ProjectItemName { get; set; }
            //public int LineNumber { get; set; }
            public string ViewTypeName { get; set; }
            public Func<object> CodeElement { get; set; }

            //   public Func<CodeElement> CodeElement { get; set; }
        }
        public class Method
        {
            public string Name { get; set; }
            public int LineNumber { get; set; }
            public string Category { get; set; }
            public string FullName { get; set; }
            public string Comment { get; set; }
            public int SortOrder { get; set; }
            public string  Signature { get; set; }
        }

        public class Region
        {
            public string Name { get; set; }
            public int LineNumber { get; set; }

            public List<Region> Children { get; set; }

            public void AddChild(Region child)
            {
                if (Children == null) Children = new List<Region>();
                Children.Add(child);
            }
        }

        public void AddRegion(Region r)
        {
            if (_regionList == null) _regionList = new List<Region>();
            _regionList.Add(r);
        }

        public void AddMethod(Method m)
        {
            if (_methodList == null) _methodList = new List<Method>();
            _methodList.Add(m);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void AddChild(OutlineItem child)
        {
            child.Parent = this;

            Children.Add(child);
        }


        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool AddRegion(ICodeRegion region)
        {
            var r = new Region();
            r.Name = region.Name;
            r.LineNumber = region.LineStart;
            _regionList.Add(r);
            //this.Name = "regions" + _regionList.Count();

            foreach (var nestedRegion in region.NestedRegions)
            {
                AddRegion(nestedRegion, r);
            }
            return true;
        }

        private void AddRegion(ICodeRegion region, Region parentRegion)
        {
            var r = new Region();
            r.Name = region.Name;
            r.LineNumber = region.LineStart;
            Debug.WriteLine("Added region {0} to {1}", r.Name, this.Name);

            parentRegion.AddChild(r);

            foreach (var nestedRegion in region.NestedRegions)
            {
                AddRegion(nestedRegion, r);
            }

        }

        public void AddRegions(List<ICodeRegion> regions)
        {
            _regionList = new List<Region>();

            foreach (var c in Children)
            {

                c.AddRegions(regions);
            }

            var theseRegions = regions.Where(x => x.LineStart > this.StartLineOfCode && x.LineEnd < this.EndLineOfCode).ToArray();
            // add the remaining regions
            foreach (var r in theseRegions)
            {

                if (AddRegion(r))
                {
                    regions.Remove(r);
                }
            }
        }

        internal class GotoProjectItemCommand : ICommand
        {
            private readonly Action<int> _onExecute;
            private ILog _log = LogManager.GetLogger(typeof (GotoProjectItemCommand));

            private int _lineNumber;

            public GotoProjectItemCommand(Action<int> onExecute,int linenumber)
            {
               
                _onExecute = onExecute;
               
                _lineNumber = linenumber;

            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                try
                {
                    _onExecute(_lineNumber);
                }
                catch (Exception e)
                {
                    _log.Error("GotoProjectItem failed", e );
                }

            }

            public event EventHandler CanExecuteChanged;
        }
        internal class GotoCodeLocationCommand : ICommand
        {
            private int _lineNumber;
            private readonly Action<int> _onExecute;
            private ILog _log = LogManager.GetLogger(typeof (GotoCodeLocationCommand));

            public GotoCodeLocationCommand(int lineNumber, Action<int> onExecute)
            {
                _lineNumber = lineNumber;
       
                _onExecute = onExecute;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                try
                {
                    _onExecute(this._lineNumber);
                }
                catch (Exception e)
                {
                    _log.Error("GotoCodeLocation failed", e );
                }
            }

            public event EventHandler CanExecuteChanged;
        }

        public void AddViews(IEnumerable<ViewReference> views)
        {
            _views = views;

        }
        public class GotoViewCommand : ICommand
        {
            public Action<object, int> OnExecute { get; set; }
            private ILog _log = LogManager.GetLogger(typeof(GotoViewCommand));



            private readonly Func<object> _getCodeElementFunc;
            private readonly string _viewTypeName;

            public GotoViewCommand(Action<object, int> onExecute, Func<object> getCodeElementFunc, string viewTypeName)
            {
                OnExecute = onExecute;
          
                _getCodeElementFunc = getCodeElementFunc;
                _viewTypeName = viewTypeName;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {

                try
                {
                 
                var ce = (CodeElement)_getCodeElementFunc();
                if (ce == null)
                {
                    MessageBox.Show("Cannot find type:" + _viewTypeName);
                    return;
                }
                
                OnExecute(ce, ce.StartPoint.Line);
                }
                catch (Exception e)
                {
                    _log.Error("GotoView failed", e);
                }
            }

            public event EventHandler CanExecuteChanged;
        }

    }



    public interface ICodeRegion
    {
        string Name { get; }
        int LineStart { get; }
        int LineEnd { get; }

        IEnumerable<ICodeRegion> NestedRegions { get; }

    }
}
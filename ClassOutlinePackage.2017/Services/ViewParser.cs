using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EnvDTE;
using log4net;

namespace ClassOutline.Services
{
    public class ViewParser
    {
        private ILog _log = LogManager.GetLogger(typeof (ViewParser));

        
        public class ViewAssignment
        {
            public string TypeName { get; set; }
            public int LineNumber { get; set; }
            public Func<CodeElement> CodeElement { get; set; }
            public int LineOffset { get; set; }
        }
        private string _documentText;

        private CodeElement FindType(IEnumerable<ProjectItem> projectItems, string typename)
        {
            
            foreach(var pi in projectItems)
            {
               
                var found = FindType(pi.ProjectItems, typename);
                if (found != null) return found;
            }
            return null;
        }

        private CodeElement FindType(ProjectItems projectItems, string typename)
        {
            var tokens = typename.Split('.');
            var path =new Queue<string>(tokens.ToList());
         

            while ( path.Count>0)
            {
                var itemName = path.Dequeue();
                var  found = false;
                Debug.WriteLine("Searching for " + itemName );
                if (projectItems == null) break;

                foreach (ProjectItem projectItem in projectItems)
                {
                    Debug.WriteLine("Checking " + projectItem.Name );
                    if (projectItem.Name.Equals(itemName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Debug.WriteLine("Found the project Item!!!");
                        found = true;

                        if (projectItem.ProjectItems != null && projectItem.ProjectItems.Count > 0)
                        {
                            Debug.WriteLine("Searching children");
                            // search the children of this projectitem
                            var foundHere = FindType(projectItem.ProjectItems, string.Join(".", path.ToArray()));

                            if (foundHere != null)
                            {
                                Debug.WriteLine("Found in children of " + projectItem.Name );

                                return foundHere;
                            }
                            Debug.WriteLine("Continuing looking");
                            
                            break;
                        }
                    }
                    else
                    {
                       var theType = FindType(projectItem, typename);

                        if (theType != null)
                        {
                            Debug.WriteLine("Found it!!!!" + theType.FullName );
                            return theType;
                        }
                    }
                    
                }
                if (!found)
                {
                    Debug.WriteLine("Didnt find this token" + itemName );
                    break;
                }
            }
            return null;
        }

        private CodeElement  FindType(ProjectItem parent, string typename)
        {
            if (parent.FileCodeModel != null)
            {
                return FindType(parent.FileCodeModel.CodeElements, typename);

            }
            return null;
        }

        private CodeElement FindType(CodeElements codeElements, string typename)
        {
            foreach (CodeElement ce in codeElements)
            {
               
                if (ce.Kind == vsCMElement.vsCMElementNamespace)
                {
                    var ret = FindType(ce.Children, typename);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
                if (ce.Kind != vsCMElement.vsCMElementClass) continue;
                Debug.WriteLine(ce.FullName);
                if (ce.Name == typename || ce.FullName == typename || ce.FullName.EndsWith(typename ))
                {
                    return ce;
                }
                if (ce.Children != null)
                {
                    var ret = FindType(ce.Children, typename);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }
            return null;
        }

        private IEnumerable<ProjectItem> GetAllParents(ProjectItem currentItem)
        {
            var ret = new List<ProjectItem>();
            ret.Add(currentItem);

            var parentItem = currentItem.Collection.Parent as ProjectItem;

            if (parentItem!=null)
            {
                ret.AddRange(GetAllParents(parentItem));
            }

            return ret;
        }

        public void FindView(ProjectItem activeProjectItem, IEnumerable<ViewAssignment> views)
        {
            if (views == null) return;
            if (activeProjectItem == null) return;

            foreach (var v in views)
            {
                if (v != null)
                {
                    Debug.WriteLine(v.TypeName);

                    var placesToSearch = GetAllParents(activeProjectItem);

                    
                    v.CodeElement = () => FindType(placesToSearch, v.TypeName);
                }
            }
        }

 

        // Possible implementation of GetCodeElements:
        private IEnumerable<CodeElement> GetCodeElements(CodeElement root)
        {
            List<CodeElement> result = new List<CodeElement>();
            if (root == null)
                return result;

            // If the current CodeElement is an Interface or a class, add it to the results
            if (root.Kind == vsCMElement.vsCMElementClass || root.Kind == vsCMElement.vsCMElementInterface)
            {
                result.Add(root);
            }

            // Check children recursively
            if (root.Children != null && root.Children.Count > 0)
            {
                foreach (var item in root.Children.OfType<CodeElement>())
                {
                    var codeElements = this.GetCodeElements(item);
                    if(codeElements!=null) result.AddRange(codeElements);
                }
            }
            return result;
        }

        public IEnumerable<ViewAssignment> GetViewAssignments(string src, int startLine, int startLineOfCodeOffset)
        {
            var ret = new List<ViewAssignment>();

            var r = new Regex(@"View\s?=\s?\(\)\s?=>\s?new\s?");
            foreach (Match  m in r.Matches(src))
            {
                var start = m.Index;
                var l = m.Length;
                var nextline = src.IndexOf('\r', start);
                start += l;
                var found = src.Substring(start , nextline - start);
                var lineNumber = src.Substring(0, start - 1).Count(c => c == '\r');

                var lineEnd = found.IndexOfAny(new[] {'(',';'});

                var typeshortname = found.Substring(0, lineEnd);

                // find the class name
                var typename = getTypeName(typeshortname);

                ret.Add(new ViewAssignment() { TypeName = typename, LineNumber = lineNumber + startLine , LineOffset = start -start });

                
            }
            return ret;

        }

        private string getTypeName(string typeshortname)
        {

            return typeshortname;
        }

        public string DocumentText { get { return _documentText; } }

        public async Task<IEnumerable<ViewAssignment>> GetViews(Document document, int startline, int startLineOfCodeOffset, int endline)
        {
            _documentText = getDocumentText(document, startline,startLineOfCodeOffset, endline );
            
            if (_documentText == null) return null;
            var assignments= GetViewAssignments(_documentText, startline, startLineOfCodeOffset );

            var filteredAssignments = removeAssignmentsNotInClass (assignments, document, startline, startLineOfCodeOffset);

            return filteredAssignments;
        }

        private IEnumerable<ViewAssignment> removeAssignmentsNotInClass(IEnumerable<ViewAssignment> assignments, Document document, int startline, int startLineOfCodeOffset)
        {
            var parentCE = getCodeElementFromLine(document, startline, startLineOfCodeOffset);

            var ret = new List<ViewAssignment>();
            foreach (var va in assignments)
            {
                CodeElement viewCE = null;
                if (va.CodeElement != null) viewCE = va.CodeElement();
                if (viewCE == null)
                {
                    viewCE = getCodeElementFromLine(document, va.LineNumber, va.LineOffset);
                }
                if (viewCE != null)
                {
                    if (viewCE.FullName == parentCE.FullName)
                    {
                        ret.Add(va);
                    }
                }
            }
            return ret;
        }

        private CodeElement getCodeElementFromLine(Document document, int startline, int startLineOfCodeOffset)
        {
            var textDocument = (TextDocument) document.Object("TextDocument");
            EditPoint editPoint = null;
            if (startline >= 0)
            {
                try
                {
                    editPoint = textDocument.CreateEditPoint();
                    editPoint.MoveToLineAndOffset(startline,Math.Max(1, startLineOfCodeOffset));

                    return editPoint.CodeElement[vsCMElement.vsCMElementClass];
                }
                catch (COMException e)
                {
                    _log.Error("Failed to getCodeElementFromLine", e );
                }

            }
            return null;
        }

        private string getDocumentText(Document document, int startline, int startLineOfCodeOffset, int endline)
        {
            if (document == null) return null;
            try
            {
                var textDocument = (TextDocument) document.Object("TextDocument");
                EditPoint editPoint = null;
                if (startline >= 0)
                {
                    editPoint = textDocument.CreateEditPoint();
                    return editPoint.GetLines(startline, endline);
                }

                editPoint = textDocument.StartPoint.CreateEditPoint();
                
                return editPoint.GetText(textDocument.EndPoint);
            }
            catch (Exception e )
            {
                return null;
            }
        }

        public IEnumerable<ViewAssignment> FindViews(ProjectItem activeProjectItem, IEnumerable<ViewAssignment> views)
        {
            FindView(activeProjectItem, views);
            return null;

        }
    }
}
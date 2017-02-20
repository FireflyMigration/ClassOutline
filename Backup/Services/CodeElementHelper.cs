using System;
using System.Diagnostics;
using EnvDTE;
using log4net;

namespace ClassOutline.Services
{
    public class CodeElementHelper
    {
        private ILog _log;

        public CodeElementHelper()
        {
            _log = LogManager.GetLogger(GetType());
        }
       public  CodeElement GetCodeElementAtCursor(DTE dte)
        {
           try
           {
               var cursor = getCursorTextPoint(dte);
               if (cursor != null)
               {

                   var c = cursor.CodeElement[vsCMElement.vsCMElementClass];
                   if (c != null)
                   {
                       Debug.WriteLine("Found cursor in " + c.FullName);
                     
                       return c;
                   }

                   return getCodeElementAtTextPoint(vsCMElement.vsCMElementClass,
                       dte.ActiveDocument.ProjectItem.FileCodeModel.CodeElements, cursor);

               }
           }
           catch (Exception e)
           {
               _log.Error("GetCodeElementAtCursor failed", e );
           }
           return null;

        }

        private CodeElement getCodeElementAtTextPoint(vsCMElement requestedElementKind, CodeElements codeElements, TextPoint cursor)
        {
            if (codeElements != null)
            {
                var c = cursor.CodeElement[vsCMElement.vsCMElementClass];
                if (c != null)
                {
                    Debug.WriteLine("Cursor is in " + c.FullName );
                }
                foreach (CodeElement e in codeElements)
                {
                    Debug.WriteLine("Searching..." + getEleentName(e) );
                    if (e.StartPoint.GreaterThan(cursor) || e.EndPoint.LessThan(cursor))
                    {
                        // ignore
                    }
                    else
                    {

                        var children = getMembers(e);
                        if (children != null)
                        {
                            var memberElement = getCodeElementAtTextPoint(requestedElementKind, children, cursor);
                            if (memberElement != null)
                            {
                                return memberElement;
                            }
                        }

                        if (e.Kind.Equals(requestedElementKind))
                        {
                            return e;
                        }
                    }
                }
            }
            Debug.WriteLine("Element not found");
            return null;

        }

        private void dumpCursorInfo(TextPoint cursor)
        {
            // Discover every code element containing the insertion point.
            string elems = "";
            vsCMElement scopes = 0;

            foreach (vsCMElement scope in Enum.GetValues(scopes.GetType()))
            {
                CodeElement elem = cursor.CodeElement[scope];

                if (elem != null)
                {
                    elems += elem.Name +
                             " (" + scope.ToString() + ")\n";
                }
            }
            Debug.WriteLine(elems);
        }

        private string getEleentName(CodeElement codeElement)
        {
         
                switch (codeElement.Kind)
                {
                    case vsCMElement.vsCMElementClass:
                        case vsCMElement.vsCMElementFunction:
                        return codeElement.FullName;
                      
                    default:
                        return codeElement.Kind.ToString();
                       

                }
            
        }

        private CodeElements getMembers(CodeElement parentCodeElement)
        {
            if (parentCodeElement is CodeNamespace)
            {
                return ((CodeNamespace) parentCodeElement).Members;
            }
            if (parentCodeElement is CodeType)
            {
                return ((CodeType) parentCodeElement).Members;
            }
            if (parentCodeElement is CodeFunction)
            {
                return ((CodeFunction) parentCodeElement).Parameters;
            }
            return null;

        }

        private TextPoint getCursorTextPoint(DTE dte )
        {
            try
            {
                var doc = (TextDocument)dte.ActiveDocument.Object("TextDocument");
                if (doc != null)
                {
                    return doc.Selection.ActivePoint;
                }

            }
            catch{}

            return null;
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassOutline.ControlLibrary;
using ClassOutline.Entities;
using EnvDTE;

namespace ClassOutline.Services
{
    public class RegionParser
    {
        private string _documentText;

     
        public IEnumerable<ICodeRegion> GetRegions(string src)
        {
            var ret = new List<ICodeRegion>();

            var lines = src.Split(new char[] {'\n'});

            CodeRegion currentCodeRegion = null;
            var regionStack = new Stack<CodeRegion>();

            int lineNumber = 0;
            foreach (var line in lines)
            {
                if (line.Contains("#region"))
                {
                    // start a new region
                    var start = line.IndexOf("#region") + "#region".Length;

                    var name = line.Substring(start).Trim();

                    currentCodeRegion = new CodeRegion();
                    currentCodeRegion.Name = name;
                    currentCodeRegion.LineStart = lineNumber;

                    // add as child of the last item in the stack
                    if (regionStack.Count>0 && regionStack.Peek()!=null)
                    {
                        regionStack.Peek().Add(currentCodeRegion);
                       
                    }
                    else
                    {
                     ret.Add(currentCodeRegion);  
                    }
                    regionStack.Push(currentCodeRegion);
                }
                if (line.Contains("#endregion"))
                {
                    if (regionStack.Peek() != null)
                    {
                        var c = regionStack.Pop();
                        
                        c.LineEnd = lineNumber;
                    }
                }
                lineNumber++;
            }
            return ret;
        }

        public string DocumentText { get { return _documentText; } }
     
        public async Task< IEnumerable<ICodeRegion>> GetRegions(Document document)
        {
            _documentText = getDocumentText(document);
            if (_documentText == null) return null;
            return GetRegions(_documentText);

        }

        private string getDocumentText(Document document)
        {
            if (document == null) return null;

            var textDocument = (TextDocument)document.Object("TextDocument");
            EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
            var content = editPoint.GetText(textDocument.EndPoint);
            return content;
        }
    }
}
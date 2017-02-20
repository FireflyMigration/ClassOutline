using EnvDTE80;

namespace ClassOutline.TreeNodes
{
    public class ImportStatementTreeNode : GenericTreeNode<CodeImport>
    {
        public ImportStatementTreeNode(CodeImport element) : base(element)
        {
           
            Name = element.Name;
            FullName = element.FullName;
            FullType = element.Kind.ToString();

            Kind = "Others";
            
        }
    }
}
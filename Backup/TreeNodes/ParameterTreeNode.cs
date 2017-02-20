using EnvDTE;

namespace ClassOutline.TreeNodes
{
    public class ParameterTreeNode : GenericTreeNode<CodeParameter>
    {
        public ParameterTreeNode(CodeParameter src) : base(src)
        {
            FullType = src.Type.AsString ;
            Name = src.Name;
            FullName = src.FullName;

        }

        public override string ToString()
        {
            return UnqualifyType(FullType) + " " + FullName;
        }
    }
}
using System;
using EnvDTE;

namespace ClassOutline.TreeNodes
{
    public class VariableTreeNode : GenericTreeNode<CodeVariable>
    {
        public VariableTreeNode(CodeVariable src)
            : base(src)
        {

            Name = src.Name;
            FullName = src.FullName;
            FullType = src.Type.AsString;

            // if it's shared (static), specify that in the "kind" string/grouping
            if (src.IsShared)
                Kind += "Static ";

            // we separate constants from fields, so check that here
            if (src.IsConstant)
                Kind += "Constants";
            else
                Kind += "Fields";

            // save the access modifier for later
            try
            {
                Access = src.Access;
            }
            catch (Exception) { }
        }
    }
}
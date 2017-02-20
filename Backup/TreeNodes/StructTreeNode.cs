using System;
using EnvDTE;

namespace ClassOutline.TreeNodes
{
    public class StructTreeNode : GenericTreeNode<CodeStruct>
    {
        public StructTreeNode(CodeStruct st) : base(st)
        {

            Name = st.Name;
            FullName = st.FullName;

            Kind = "Structs";

            try
            {
                Access = st.Access;
            }
            catch (Exception ) { }

        }
    }
}
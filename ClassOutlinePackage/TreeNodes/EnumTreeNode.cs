using System;
using EnvDTE;

namespace ClassOutline.TreeNodes
{
    public class EnumTreeNode : GenericTreeNode<CodeEnum>
    {
        public EnumTreeNode(CodeEnum en) : base(en)
        {

            Name = en.Name;
            FullName = en.FullName;

            Kind = "Enumerations";

            try
            {
                Access = en.Access;
            }
            catch (Exception ) { }

        }
    }
}
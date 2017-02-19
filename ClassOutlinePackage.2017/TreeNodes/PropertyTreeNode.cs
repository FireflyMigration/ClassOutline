using System;
using EnvDTE;

namespace ClassOutline.TreeNodes
{
    public class PropertyTreeNode : GenericTreeNode<CodeProperty>
    {
        public PropertyTreeNode(CodeProperty prop) : base(prop)
        {

            Name = prop.Name;
            FullName = prop.FullName;
            FullType = prop.Type.AsString;

            // we have to check the getter and setter to see if it's static
            if ((prop.Getter != null && prop.Getter.IsShared) || (prop.Setter != null && prop.Setter.IsShared))
                Kind += "Static ";

            Kind += "Properties";

            try
            {
                Access = prop.Access;
            }
            catch (Exception ) { }

        }
    }
}
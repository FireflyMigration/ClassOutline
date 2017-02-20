using System;
using EnvDTE80;

namespace ClassOutline.TreeNodes
{
    public class EventTreeNode : GenericTreeNode<CodeEvent>
    {
        public EventTreeNode(CodeEvent ev) : base(ev)
        {

            Name = ev.Name;
            FullName = ev.FullName;
            FullType = ev.Type.AsString;

            Kind = "Events";

            try
            {
                Access = ev.Access;
            }
            catch (Exception ) { }
        }
    }
}
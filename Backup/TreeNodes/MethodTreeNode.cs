using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace ClassOutline.TreeNodes
{
    public class MethodTreeNode : GenericTreeNode<CodeFunction>
    {
        private List<ParameterTreeNode> _parameters =new List<ParameterTreeNode>(); 
        public MethodTreeNode(CodeFunction func, string parentName) : base(func)
        {

            Name = func.FullName.Split('.').Last();     // must use fullname to include <T> if present
            FullName = func.FullName;
            FullType = func.Type.AsString;

            // in addition to the name of the method, we want to see its parameters in its TreeViewItem, so do that here
            Name += "(";

            // for each param, add it with a comma to mimic the method signature
            for (int i = 1; i <= func.Parameters.Count; i++)
            {
                _parameters.Add(new ParameterTreeNode((CodeParameter)func.Parameters.Item(i)));
              
            }

            if (_parameters.Any())
            {
                Name += "(" + String.Join(",",_parameters.Select(x => x.ToString())) + ")";
            }

            if (func.Type.AsString == "void" &&
                func.Parameters.Count == 2 &&
                ((CodeParameter)func.Parameters.Item(1)).Type.AsFullName == "System.Object" &&
                ((CodeParameter)func.Parameters.Item(2)).Type.CodeType.IsDerivedFrom["System.EventArgs"])
            {
                Kind = "Event Handlers";
            }
           

            // if the parent item (class item) has the same name as the method, this is a constructor
            if (func.FunctionKind == vsCMFunction.vsCMFunctionConstructor)
            {
                Kind = "Constructors";
            }
            else
            {
                // otherwise this is a regular method
                // add its access modifier to the "kind" string/grouping and whether it's static or not
                Kind += getMappedAccessString(func.Access) + " ";

                if (func.IsShared)
                    Kind += "Static ";

                Kind += "Methods";
            }

            
            try
            {
                Access = func.Access;
            }
            catch (Exception ) { }
        }
    }
}
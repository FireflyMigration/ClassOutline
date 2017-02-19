using System.Collections.Generic;
using EnvDTE;

namespace ClassOutline.TreeNodes
{
    public class GenericTreeNode<T> : TreeNodeBase
      
    {
        Dictionary<vsCMAccess, string> accessMapping = new Dictionary<vsCMAccess, string>
        { 
            { vsCMAccess.vsCMAccessPublic, "Public" },
            { vsCMAccess.vsCMAccessAssemblyOrFamily, "Internal" },
            { vsCMAccess.vsCMAccessProtected, "Protected" },
            { vsCMAccess.vsCMAccessPrivate, "Private" },
            { vsCMAccess.vsCMAccessProject, "Internal" },
            { vsCMAccess.vsCMAccessDefault, "Default" },
            { vsCMAccess.vsCMAccessProjectOrProtected, "Internal/Protected" },
            { vsCMAccess.vsCMAccessWithEvents, "With Events" }
        };

        public override string Group { get { return  Kind; }
            protected set
            {
                
            }
        }

        protected string getMappedAccessString(vsCMAccess access)
        {
            if (accessMapping.ContainsKey(access)) return accessMapping[access];
            return string.Empty;

        }
        protected GenericTreeNode(T src)
        {
           
        }
    }
}
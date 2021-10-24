using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace ClassOutline.TreeNodes
{
    public class TreeNodeBase
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string FullType { get; set; }
        public string Kind { get; set; }

        public virtual string Group { get; protected  set;  }
        public vsCMAccess Access { get; protected set; }

       

        /// <summary>
        /// Unqualifies a type including all of its types within angle brackets
        /// </summary>
        /// <param name="fullType">the fully qualified type</param>
        /// <returns>unqualified type complete with types within angle brackets if included</returns>
        protected string UnqualifyType(string fullType)
        {
            string type = string.Empty;

            List<string> typeParts = fullType.Split(',', '<', '>').Where(s => s != "").ToList();

            for (int i = 0; i < typeParts.Count; i++)
            {
                typeParts[i] = typeParts[i].Split('.').Last();

                if (i == 1)
                    type += "<";
                else if (i > 1)
                    type += ", ";

                type += typeParts[i];

                if (typeParts.Count > 1 && i == typeParts.Count - 1)
                    type += ">";
            }

            return type;
        }
    
    }
}
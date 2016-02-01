using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Web.UI.Design;
using EnvDTE;

namespace ClassOutline.TreeNodes
{
    public class ClassTreeNode: GenericTreeNode<CodeClass>
    {
        public IList<string> BaseClassList { get; private set; }
        public  ClassTreeNode(CodeClass src) : base(src)
        {

            // grab its qualified and unqualified name
            FullName = src.FullName;
            Name = src.FullName.Split('.').Last();
            Kind = "Classes";
            Access = src.Access;

            BaseClassList = new List<string>();
            foreach (CodeElement item in src.Bases)
            {
                BaseClassList.Add(item.FullName );

            }
            FullType = src.FullName;
        }

        public Type GetBaseType()
        {

            var baseClazz = BaseClassList.FirstOrDefault();

            if (baseClazz == null) return null ;

            var typeName = baseClazz;
            try
            {
                var t = Type.GetType(typeName, true, true);
                return t;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to create type : " + typeName + ". " + e.Message);
            }
            return null;
        }

        public string GetBaseTypeName()
        {
            return BaseClassList.FirstOrDefault();
        }
    }
}
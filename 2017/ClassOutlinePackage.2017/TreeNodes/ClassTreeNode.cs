using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Web.UI.Design;
using EnvDTE;
using log4net;

namespace ClassOutline.TreeNodes
{
    public class ClassTreeNode: GenericTreeNode<CodeClass>
    {
        private ILog _log = LogManager.GetLogger(typeof (ClassTreeNode));

        public IList<string> BaseClassList { get; private set; }
        public  ClassTreeNode(CodeClass src) : base(src)
        {
            try
            {
                // grab its qualified and unqualified name
                FullName = src.FullName;
                Name = src.FullName.Split('.').Last();
                Kind = "Classes";
                Access = src.Access;
                _log.Debug("Created TreeNode:" + Name);

                BaseClassList = addBaseClasses(src);
              
                FullType = src.FullName;
            }
            catch (Exception e)
            {
                _log.Error("Failed to create ClassTreeNode", e );
                throw;
            }
        }

        private IList<string> addBaseClasses(CodeClass src)
        {
            var ret = new List<string>();

            foreach (CodeElement item in src.Bases)
            {
                ret.Add(item.FullName);
                var baseClass = item as CodeClass;
                if (baseClass != null)
                {
                    ret.AddRange(addBaseClasses(baseClass));
                }
            }

            return ret;
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
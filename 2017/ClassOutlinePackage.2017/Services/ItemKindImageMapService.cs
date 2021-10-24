using System.Collections.Generic;

namespace ClassOutline.Services
{
    /**
     * Provides the resource name for the given kind of projecittem
     */

    public class ItemKindImageMapService : IItemKindImageMapService
    {
        public string DefaultKey { get; private set; }

        // another dictionary mapping "kinds" (fields, properties, etc.) to a resource image
        private  readonly Dictionary<string, string> _kindImageMapping = new Dictionary<string, string>
        {
            {"Classes", "Classes"},
            {"Static Constants", "Constants"},
            {"Constants", "Constants"},
            {"Static Fields", "Fields"},
            {"Fields", "Fields"},
            {"Static Properties", "Properties"},
            {"Properties", "Properties"},
            {"Indexers", string.Empty},
            {"Events", "Events"},
            {"Constructors", "Constructors"},
            {"Destructors", string.Empty},
            {"Delegates", string.Empty},
            {"Event Handlers", "Methods"},
            {"Public Static Methods", "Methods"},
            {"Public Methods", "Methods"},
            {"Internal Static Methods", "Methods"},
            {"Internal Methods", "Methods"},
            {"Protected Static Methods", "Methods"},
            {"Protected Methods", "Methods"},
            {"Private Static Methods", "Methods"},
            {"Private Methods", "Methods"},
            {"Interface Members", string.Empty},
            {"Inner Types", "Classes"},
            {"Structs", string.Empty},
            {"Enumerations", string.Empty}
        };

        public ItemKindImageMapService()
        {
            DefaultKey = string.Empty;
        }

        public ItemKindImageMapService(Dictionary<string, string> sourceData):this()
        {
         _kindImageMapping = new Dictionary<string, string>(sourceData);   
        }

        public string getImageKey(string kind)
        {
            return _kindImageMapping[kind];

        } 
    }
}
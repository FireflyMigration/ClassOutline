using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Input;

namespace ClassOutline.ControlLibrary
{
    public class ContextMenuItem
    {
        public List<ContextMenuItem> MenuItems { get; set; }

        public ContextMenuItem()
        {
            MenuItems = new List<ContextMenuItem>();
        }
        public string Caption { get; set; }
        public ICommand Command { get; set; }
        public string ToolTipText { get; set; }
    }
}
using System;
using System.Collections.Generic;
using ClassOutline.ControlLibrary;

namespace ClassOutline.Entities
{
    public  class CodeRegion: ICodeRegion
    {
        public string Name { get; set; }
        public int LineStart { get; set; }
        public int LineEnd { get; set; }

        public bool ContainsLineNumber(int ln)
        {
            return (LineStart <= ln && LineEnd >= ln);
        }
        private IList<CodeRegion> _nestedRegions = new List<CodeRegion>(); 
        public IEnumerable<ICodeRegion> NestedRegions { get { return _nestedRegions; } }

            
        internal void addAll(IEnumerable<CodeRegion> regions)
        {
            foreach (var r in regions)
            {
                _nestedRegions.Add(r);
            }
        }

        public void Add(CodeRegion codeRegion)
        {
            _nestedRegions.Add(codeRegion);
        }

        public void RecurseThis(Func<CodeRegion, bool> fn)
        {
            foreach (var nestedRegion in _nestedRegions)
            {
                if (!fn(nestedRegion)) return;

                nestedRegion.RecurseThis(fn);
            }
        }
    }
}
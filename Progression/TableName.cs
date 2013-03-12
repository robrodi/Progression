using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Progression
{
    public class TableNameAttribute : Attribute
    {
        public string Name { get; private set; }
        public TableNameAttribute(string name) { Name = name; }
    }
}

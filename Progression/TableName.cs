using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Progression
{
    /// <summary>
    /// Attribute to store the table name for a entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableNameAttribute : Attribute
    {
        /// <summary>
        /// the name of the table to store the entity
        /// </summary>
        public string Name { get; private set; }

        public TableNameAttribute(string name) { Name = name; }
    }
}

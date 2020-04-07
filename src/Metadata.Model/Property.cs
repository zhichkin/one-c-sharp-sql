using System.Collections.Generic;

namespace OneCSharp.Metadata.Model
{
    public enum PropertyPurpose
    {
        /// <summary>The property is being used by system.</summary>
        System,
        /// <summary>The property is being used as a property.</summary>
        Property,
        /// <summary>The property is being used as a dimension.</summary>
        Dimension,
        /// <summary>The property is being used as a measure.</summary>
        Measure,
        /// <summary>This property is used to reference parent (adjacency list).</summary>
        Hierarchy
    }
    public sealed class Property
    {
        public string Name { get; set; }
        public PropertyPurpose Purpose { get; set; }
        public int Ordinal { get; set; }
        public bool IsPrimaryKey { get; set; }
        public List<int> PropertyTypes { get; set; } = new List<int>();
        public List<Field> Fields { get; set; } = new List<Field>();
        public override string ToString()
        {
            return Name;
        }
    }
}
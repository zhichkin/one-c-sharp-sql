using System;
using System.Collections.Generic;

namespace OneCSharp.Metadata.Model
{
    public sealed class BaseObject
    {
        public string Name { get; set; }
        public List<MetaObject> MetaObjects { get; set; } = new List<MetaObject>();
        public override string ToString()
        {
            return Name;
        }
    }
    public sealed class MetaObject
    {
        public Guid UUID { get; set; }
        public int TypeCode { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Table { get; set; }
        public string Schema { get; set; }
        public List<Property> Properties { get; set; } = new List<Property>();
        public List<MetaObject> MetaObjects { get; set; } = new List<MetaObject>();
        public override string ToString()
        {
            return Name;
        }
    }
}
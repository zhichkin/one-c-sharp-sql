using System.Collections.Generic;

namespace OneCSharp.Metadata.Model
{
    public sealed class InfoBase
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public List<BaseObject> BaseObjects { get; set; } = new List<BaseObject>();
        public override string ToString()
        {
            return Name;
        }
    }
}
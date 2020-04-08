using System.Collections.Generic;

namespace OneCSharp.Metadata.Model
{
    public sealed class InfoBase
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Version { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public List<BaseObject> BaseObjects { get; set; } = new List<BaseObject>();
        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? Database : Name;
        }
    }
}
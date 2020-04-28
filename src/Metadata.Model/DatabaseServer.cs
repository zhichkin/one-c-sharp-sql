using System.Collections.Generic;

namespace OneCSharp.Metadata.Model
{
    public sealed class DatabaseServer
    {
        public string Name { get; set; }
        public string Address { get; set; } = string.Empty;
        public List<InfoBase> Databases { get; set; } = new List<InfoBase>();
    }
}
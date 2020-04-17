using System.Collections.Generic;

namespace OneCSharp.Metadata.Model
{
    public sealed class DatabaseServer
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public List<InfoBase> Databases { get; } = new List<InfoBase>();
    }
}
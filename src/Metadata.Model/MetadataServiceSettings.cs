using System.Collections.Generic;

namespace OneCSharp.Metadata.Model
{
    public sealed class MetadataServiceSettings
    {
        public string Catalog { get; set; }
        public List<DatabaseServer> Servers { get; } = new List<DatabaseServer>();
    }
}
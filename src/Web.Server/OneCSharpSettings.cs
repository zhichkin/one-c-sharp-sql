using System.Collections.Generic;

namespace OneCSharp.Web.Server
{
    public sealed class OneCSharpSettings
    {
        public string UseServer { get; set; }
        public List<string> UseDatabases { get; } = new List<string>();
    }
}
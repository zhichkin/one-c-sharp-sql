using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    public sealed class SchemaMapper
    {
        public Dictionary<string, string> Mappings { get; } = new Dictionary<string, string>();
        public string MapColumnIdentifier(string identifier)
        {
            return "_Ref";
        }
        public Dictionary<string, TableReference> TableAliases { get; } = new Dictionary<string, TableReference>();
    }
}
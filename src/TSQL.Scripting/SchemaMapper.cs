using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    public sealed class SchemaMapper
    {
        public Dictionary<string, string> Mappings { get; } = new Dictionary<string, string>();
        public Dictionary<string, TableReference> TableAliases { get; } = new Dictionary<string, TableReference>();
        public void ClearCash()
        {
            TableAliases.Clear();
        }

    }
}
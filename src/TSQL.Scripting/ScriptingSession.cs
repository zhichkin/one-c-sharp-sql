using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    internal interface IScriptingSession
    {
        InfoBase InfoBase { get; set; }
        SelectStatement Statement { get; set; }
        Dictionary<string, NamedTableReference> TableAliases { get; }
        Dictionary<string, string> TableAliasAndOriginalName { get; }
    }
    internal sealed class ScriptingSession : IScriptingSession
    {
        public InfoBase InfoBase { get; set; }
        public SelectStatement Statement { get; set; }
        public Dictionary<string, NamedTableReference> TableAliases { get; } = new Dictionary<string, NamedTableReference>();
        public Dictionary<string, string> TableAliasAndOriginalName { get; } = new Dictionary<string, string>();
    }
}
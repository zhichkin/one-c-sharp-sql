using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OneCSharp.TSQL.Scripting
{
    public interface IScriptingService
    {
        string PrepareScript(string script, out IList<ParseError> errors);
    }
    public sealed class ScriptingService : IScriptingService
    {
        private TSql150Parser Parser { get; }
        private Sql150ScriptGenerator Generator { get; }
        private IMetadataService MetadataService { get; set; }
        public ScriptingService()
        {
            Parser = new TSql150Parser(false, SqlEngineType.Standalone);
            Generator = new Sql150ScriptGenerator(new SqlScriptGeneratorOptions()
            {
                AlignClauseBodies = true
            });
            InitializeService();
        }
        private void InitializeService()
        {
            MetadataService = new MetadataService();
            InfoBase infoBase = GetDefaultInfoBase();
            MetadataService.InitializeMetadata(infoBase);
        }
        public string PrepareScript(string script, out IList<ParseError> errors)
        {
            TSqlFragment fragment = Parser.Parse(new StringReader(script), out errors);
            if (errors.Count > 0)
            {
                return script;
            }

            ScriptingSession session = new ScriptingSession()
            {
                InfoBase = GetDefaultInfoBase()
            };
            var visitor = new SelectStatementVisitor(MetadataService, session);
            if (visitor != null)
            {
                fragment.Accept(visitor);
            }
            Generator.GenerateScript(fragment, out string sql);
            return sql;
        }
        private InfoBase GetDefaultInfoBase()
        {
            return new InfoBase()
            {
                Name = "reverse_engineering",
                Alias = "1C# Integrator Demo App",
                Version = "0.1.0.0",
                Server = "zhichkin",
                Database = "reverse_engineering"
            };
        }
    }
}
//StatementList statements = Parser.ParseStatementList(new StringReader(query), out errors);
//if (errors.Count > 0)
//{
//    return query;
//}
//foreach (var statement in statements.Statements)
//{
//    // TODO
//}
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace OneCSharp.Scripting.Services
{
    public interface IScriptingService
    {
        string PrepareScript(string script, out IList<ParseError> errors);
        string ExecuteScript(string script, out IList<ParseError> errors);
    }
    public sealed class ScriptingService : IScriptingService
    {
        private TSql150Parser Parser { get; }
        private Sql150ScriptGenerator Generator { get; }
        private IQueryExecutor ScriptExecutor { get; }
        private IMetadataService MetadataService { get; }
        public ScriptingService(IMetadataService metadata, IQueryExecutor executor)
        {
            Parser = new TSql150Parser(false, SqlEngineType.Standalone);
            Generator = new Sql150ScriptGenerator(new SqlScriptGeneratorOptions()
            {
                AlignClauseBodies = true
            });
            ScriptExecutor = executor;
            MetadataService = metadata;
        }
        public string PrepareScript(string script, out IList<ParseError> errors)
        {
            if (MetadataService.CurrentDatabase == null) throw new InvalidOperationException("Current database is not defined!");

            TSqlFragment fragment = Parser.Parse(new StringReader(script), out errors);
            if (errors.Count > 0)
            {
                return script;
            }

            ScriptNode result = new ScriptNode() { InfoBase = MetadataService.CurrentDatabase };
            SyntaxTreeVisitor visitor = new SyntaxTreeVisitor(MetadataService);
            visitor.Visit(fragment, result);

            Generator.GenerateScript(fragment, out string sql);
            return sql;
        }
        public string ExecuteScript(string script, out IList<ParseError> errors)
        {
            errors = new ParseError[] { }; // TODO
            return ScriptExecutor.ExecuteJson(script);
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
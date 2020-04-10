using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace OneCSharp.TSQL.Scripting
{
    public interface IScriptingService
    {
        void Initialize(string server, IList<string> databases);
        void UseServer(string server);
        void UseDatabase(string database);
        string PrepareScript(string script, out IList<ParseError> errors);
    }
    public sealed class ScriptingService : IScriptingService
    {
        private TSql150Parser Parser { get; }
        private Sql150ScriptGenerator Generator { get; }
        private IMetadataService MetadataService { get; }
        public ScriptingService()
        {
            Parser = new TSql150Parser(false, SqlEngineType.Standalone);
            Generator = new Sql150ScriptGenerator(new SqlScriptGeneratorOptions()
            {
                AlignClauseBodies = true
            });
            MetadataService = new MetadataService();
        }
        public void Initialize(string server, IList<string> databases)
        {
            if (string.IsNullOrWhiteSpace(server)) throw new ArgumentNullException(nameof(server));
            if (databases == null) throw new ArgumentNullException(nameof(databases));
            if (databases.Count == 0) throw new InvalidOperationException(nameof(databases));

            MetadataService.UseServer(server);
            foreach (var db in databases)
            {
                MetadataService.UseDatabase(db);
            }
            MetadataService.UseDatabase(databases[0]); // set current database !
        }
        public void UseServer(string server)
        {
            MetadataService.UseServer(server);
        }
        public void UseDatabase(string database)
        {
            MetadataService.UseDatabase(database);
        }
        public string PrepareScript(string script, out IList<ParseError> errors)
        {
            if (MetadataService.CurrentDatabase == null) throw new InvalidOperationException("Current database is not set!");

            TSqlFragment fragment = Parser.Parse(new StringReader(script), out errors);
            if (errors.Count > 0)
            {
                return script;
            }
            BatchContext context = new BatchContext(MetadataService)
            {
                InfoBase = MetadataService.CurrentDatabase
            };
            var visitor = new SelectStatementVisitor(MetadataService, context);
            if (visitor != null)
            {
                fragment.Accept(visitor);
            }
            Generator.GenerateScript(fragment, out string sql);
            return sql;
        }
        public string ExecuteScript(string script, out IList<ParseError> errors)
        {
            // TODO:
            // 1. prepare script
            // 2. execute script
            // 3. serialize result to JSON
            // 4. return JSON
            errors = new ParseError[] { };
            return null;
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
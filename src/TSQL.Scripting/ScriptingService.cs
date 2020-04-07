using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OneCSharp.TSQL.Scripting
{
    public sealed class ScriptingService
    {
        private TSql150Parser Parser { get; }
        private Sql150ScriptGenerator Generator { get; }
        private SchemaMapper Mapper { get; }
        private Dictionary<Type, TSqlConcreteFragmentVisitor> Visitors { get; }
        public ScriptingService(SchemaMapper mapper)
        {
            Mapper = mapper;
            Parser = new TSql150Parser(false, SqlEngineType.Standalone);
            Generator = new Sql150ScriptGenerator(new SqlScriptGeneratorOptions()
            {
                AlignClauseBodies = true
            });
            Visitors = new Dictionary<Type, TSqlConcreteFragmentVisitor>();
            InitializeVisitors();
        }
        private void InitializeVisitors()
        {
            Visitors.Add(typeof(SelectStatementVisitor), new SelectStatementVisitor(Mapper));
            Visitors.Add(typeof(ColumnReferenceVisitor), new ColumnReferenceVisitor(Mapper));
        }
        private T GetVisitor<T>() where T : TSqlConcreteFragmentVisitor
        {
            if (Visitors.TryGetValue(typeof(T), out TSqlConcreteFragmentVisitor visitor))
            {
                return (T)visitor;
            }
            else
            {
                return null;
            }
        }
        public string MapIdentifiers(string query, out IList<ParseError> errors)
        {
            TSqlFragment fragment = Parser.Parse(new StringReader(query), out errors);
            if (errors.Count > 0)
            {
                return query;
            }

            //Parser.ParseStatementList

            var visitor = GetVisitor<SelectStatementVisitor>();
            if (visitor != null)
            {
                fragment.Accept(visitor);
            }

            var columnVisitor = GetVisitor<ColumnReferenceVisitor>();
            if (columnVisitor != null)
            {
                fragment.Accept(columnVisitor);
            }

            Generator.GenerateScript(fragment, out string new_sql);
            return new_sql;
        }
    }
}
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    internal class SelectStatementVisitor : TSqlConcreteFragmentVisitor
    {
        private IMetadataService MetadataService { get; }
        private IScriptingSession ScriptingSession { get; }
        internal SelectStatementVisitor(IMetadataService metadata, IScriptingSession session)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
            ScriptingSession = session ?? throw new ArgumentNullException(nameof(session));
        }
        public override void Visit(SelectStatement node)
        {
            if (node == null) return;
            var specification = node.QueryExpression as QuerySpecification;
            IList<TableReference> tables = specification?.FromClause?.TableReferences;
            if (tables == null) return;

            ScriptingSession.Statement = node;
            ScriptingSession.TableAliases.Clear();
            ScriptingSession.TableAliasAndOriginalName.Clear();

            var tableVisitor = new TableVisitor(MetadataService, ScriptingSession);
            foreach (var table in tables)
            {
                table.Accept(tableVisitor);
            }

            var columnVisitor = new ColumnVisitor(MetadataService, ScriptingSession);
            if (columnVisitor != null)
            {
                node.Accept(columnVisitor);
            }
        }
    }
}
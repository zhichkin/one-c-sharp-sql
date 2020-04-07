using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    internal class SelectStatementVisitor : TSqlConcreteFragmentVisitor
    {
        private SchemaMapper Mapper { get; }
        public SelectStatementVisitor(SchemaMapper mapper)
        {
            Mapper = mapper;
        }
        public override void Visit(SelectStatement node)
        {
            if (node == null) return;
            var specification = node.QueryExpression as QuerySpecification;
            IList<TableReference> tables = specification?.FromClause?.TableReferences;
            if (tables == null) return;

            Mapper.ClearCash();

            var tableVisitor = new TableVisitor(Mapper);
            foreach (var table in tables)
            {
                table.Accept(tableVisitor);
            }

            var columnVisitor = new ColumnVisitor(Mapper);
            if (columnVisitor != null)
            {
                node.Accept(columnVisitor);
            }
        }
    }
}
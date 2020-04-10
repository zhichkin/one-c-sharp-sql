using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;

namespace OneCSharp.TSQL.Scripting
{
    internal class SelectStatementVisitor : TSqlConcreteFragmentVisitor
    {
        private IMetadataService MetadataService { get; }
        private IBatchContext BatchContext { get; }
        internal SelectStatementVisitor(IMetadataService metadata, IBatchContext context)
        {
            BatchContext = context ?? throw new ArgumentNullException(nameof(context));
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public override void Visit(SelectStatement select)
        {
            if (select == null) return;

            // TODO: move it to QueryExpressionVisitor !? see TableVisitor code
            if (!(select.QueryExpression is QuerySpecification query)) return;

            // this is root context of the SELECT statement
            SelectContext context = new SelectContext(BatchContext)
            {
                Statement = select
            };
            VisitTables(query, context);
            VisitColumns(query, context); // including WHERE clause
        }
        private void VisitTables(QuerySpecification query, ISelectContext context)
        {
            if (query == null) return;
            if (context == null) return;
            if (query.FromClause == null) return;
            if (query.FromClause.TableReferences == null) return;
            if (query.FromClause.TableReferences.Count == 0) return;

            var tableVisitor = new TableVisitor(MetadataService, context);
            foreach (var table in query.FromClause.TableReferences)
            {
                tableVisitor.VisitTableReference(table);
            }
        }
        private void VisitColumns(QuerySpecification query, ISelectContext context)
        {
            if (query == null) return;
            if (context == null) return;
            if (query.SelectElements == null) return;
            if (query.SelectElements.Count == 0) return;

            var columnVisitor = new ColumnVisitor(MetadataService, context);
            foreach (var element in query.SelectElements)
            {
                if (columnVisitor != null)
                {
                    element.Accept(columnVisitor);
                }
            }

            if (query.WhereClause == null) return;
            if (query.WhereClause.SearchCondition == null) return;
            query.WhereClause.SearchCondition.Accept(columnVisitor);
        }
    }
}
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    internal sealed class TableVisitor : TSqlConcreteFragmentVisitor // The table reference to a CTE or schema object.
    {
        private IMetadataService MetadataService { get; }
        private ISelectContext SelectContext { get; }
        internal TableVisitor(IMetadataService metadataService, ISelectContext context)
        {
            SelectContext = context ?? throw new ArgumentNullException(nameof(context));
            MetadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        }
        public void VisitTableReference(TableReference table)
        {
            if (table is QualifiedJoin join)
            {
                this.Visit(join);
            }
            else if (table is QueryDerivedTable derived)
            {
                this.Visit(derived);
            }
            else if (table is NamedTableReference schemaTable)
            {
                this.Visit(schemaTable);
            }
        }
        public override void Visit(QualifiedJoin tableReference)
        {
            VisitTableReference(tableReference.FirstTableReference);
            VisitTableReference(tableReference.SecondTableReference);

            if (tableReference.SearchCondition == null) return;
            var columnVisitor = new ColumnVisitor(MetadataService, SelectContext);
            tableReference.SearchCondition.Accept(columnVisitor);
        }
        public override void Visit(QueryDerivedTable tableReference)
        {
            if (tableReference == null) return;

            string alias = tableReference.Alias?.Value;
            if (!string.IsNullOrEmpty(alias))
            {
                SelectContext.Tables.Add(alias, new TableInfo()
                {
                    Alias = alias,
                    Table = tableReference
                });
            }

            // TODO: move it to QueryExpressionVisitor !? see SelectStatementVisitor code
            var specification = tableReference.QueryExpression as QuerySpecification;
            IList<TableReference> tables = specification?.FromClause?.TableReferences;
            if (tables == null) return;

            // this is child context of the SELECT statement
            SelectContext context = new SelectContext(SelectContext.Batch)
            {
                Statement = SelectContext.Statement
            };
            var tableVisitor = new TableVisitor(MetadataService, context);
            foreach (var table in tables)
            {
                tableVisitor.VisitTableReference(table);
            }
            VisitColumns(specification, context); // including WHERE clause
        }
        public override void Visit(NamedTableReference tableReference)
        {
            if (tableReference == null) return;

            SchemaObjectName name = tableReference.SchemaObject;
            string serverIdentifier = name.ServerIdentifier?.Value;
            string databaseIdentifier = name.DatabaseIdentifier?.Value;
            string schemaIdentifier = name.SchemaIdentifier?.Value;
            string tableIdentifier = name.BaseIdentifier?.Value;
            if (string.IsNullOrEmpty(tableIdentifier)) return;

            if (serverIdentifier != null)
            {
                if (tableIdentifier.Contains('+')) // [server].[database].Документ.[ПоступлениеТоваровУслуг+Товары]
                {
                    tableIdentifier = $"[{schemaIdentifier}+{tableIdentifier}]";
                    schemaIdentifier = string.Empty; // dbo
                }
                else
                {
                    string schemaName = MetadataService.MapSchemaIdentifier(databaseIdentifier);
                    if (schemaName == string.Empty) // [database].Документ.ПоступлениеТоваровУслуг.Товары
                    {
                        tableIdentifier = $"[{databaseIdentifier}+{schemaIdentifier}+{tableIdentifier}]";
                        schemaIdentifier = string.Empty; // dbo
                        databaseIdentifier = serverIdentifier;
                        serverIdentifier = null;
                    }
                    else // [server].[database].Документ.ПоступлениеТоваровУслуг
                    {
                        tableIdentifier = $"[{schemaIdentifier}+{tableIdentifier}]";
                        schemaIdentifier = string.Empty; // dbo
                    }
                }
            }
            else if (databaseIdentifier != null)
            {
                string schemaName = MetadataService.MapSchemaIdentifier(databaseIdentifier);
                if (schemaName == string.Empty) // Документ.ПоступлениеТоваровУслуг.Товары
                {
                    tableIdentifier = $"[{databaseIdentifier}+{schemaIdentifier}+{tableIdentifier}]";
                    schemaIdentifier = null;
                    databaseIdentifier = null;
                }
                else // [database].Документ.ПоступлениеТоваровУслуг
                {
                    tableIdentifier = $"[{schemaIdentifier}+{tableIdentifier}]";
                    schemaIdentifier = string.Empty; // dbo
                }
            }
            else if (schemaIdentifier != null) // Документ.ПоступлениеТоваровУслуг
            {
                tableIdentifier = $"[{schemaIdentifier}+{tableIdentifier}]";
                schemaIdentifier = null;
            }
            else // ПоступлениеТоваровУслуг
            {
                return;
            }

            string databaseName = null;
            if (databaseIdentifier != null)
            {
                databaseName = databaseIdentifier.TrimStart('[').TrimEnd(']');
            }

            name = new SchemaObjectName();
            if (serverIdentifier != null)
            {
                name.Identifiers.Add(new Identifier() { Value = serverIdentifier });
            }
            if (databaseIdentifier != null)
            {
                name.Identifiers.Add(new Identifier() { Value = databaseIdentifier });
            }
            if (schemaIdentifier != null)
            {
                name.Identifiers.Add(new Identifier() { Value = schemaIdentifier });
            }
            if (tableIdentifier != null)
            {
                name.Identifiers.Add(new Identifier() { Value = tableIdentifier });
                name.BaseIdentifier.Value = MetadataService.MapTableIdentifier(databaseName, tableIdentifier);
            }
            tableReference.SchemaObject = name;

            string alias = tableReference.Alias?.Value;
            if (string.IsNullOrEmpty(alias)) // no alias table - just table name
            {
                SelectContext.Tables.Add(tableIdentifier, new TableInfo()
                {
                    Alias = null,
                    Table = tableReference,
                    Database = databaseName,
                    Identifier = tableIdentifier // dictionary key : $"[{schemaIdentifier}+{tableIdentifier}]"
                });
            }
            else
            {
                SelectContext.Tables.Add(alias, new TableInfo()
                {
                    Alias = alias, // dictionary key
                    Table = tableReference,
                    Database = databaseName,
                    Identifier = tableIdentifier
                });
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
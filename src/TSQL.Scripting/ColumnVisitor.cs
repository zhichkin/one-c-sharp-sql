using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OneCSharp.TSQL.Scripting
{
    internal class ColumnVisitor : TSqlConcreteFragmentVisitor
    {
        private IMetadataService MetadataService { get; }
        private ISelectContext SelectContext { get; }
        public ColumnVisitor(IMetadataService metadataService, ISelectContext context)
        {
            SelectContext = context ?? throw new ArgumentNullException(nameof(context));
            MetadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        }
        public override void Visit(ColumnReferenceExpression node)
        {
            if (node.ColumnType != ColumnType.Regular) return;

            Identifier identifier = null;
            if (node.MultiPartIdentifier.Identifiers.Count == 1) // no table alias - just column name
            {
                if (SelectContext.Tables == null || SelectContext.Tables.Count == 0) return;
                identifier = node.MultiPartIdentifier.Identifiers[0];
                foreach (TableInfo table in SelectContext.Tables.Values
                    .Where(t => string.IsNullOrEmpty(t.Alias)))
                {
                    Property @property = MetadataService.GetProperty(table.Database, table.Identifier, identifier.Value);
                    if (@property == null) continue;
                    if (@property.Fields.Count == 1)
                    {
                        identifier.Value = @property.Fields[0].Name;
                        break;
                    }
                    SelectContext.Actions.Add(new TransformAction()
                    {
                        Column = node,
                        Property = @property
                    });
                    break;
                }
            }
            else if (node.MultiPartIdentifier.Identifiers.Count == 2)
            {
                Identifier alias = node.MultiPartIdentifier.Identifiers[0];
                identifier = node.MultiPartIdentifier.Identifiers[1];
                
                if (SelectContext.Tables.TryGetValue(alias.Value, out TableInfo table))
                {
                    if (string.IsNullOrEmpty(table.Identifier))
                    {
                        // TODO: query derived table ... get column from there to understand what to do ... tunneling ...
                        return;
                    }
                    Property @property = MetadataService.GetProperty(table.Database, table.Identifier, identifier.Value);
                    if (@property == null) return;
                    if (@property.Fields.Count == 1)
                    {
                        identifier.Value = @property.Fields[0].Name;
                        return;
                    }
                    SelectContext.Actions.Add(new TransformAction()
                    {
                        Column = node,
                        Property = @property
                    });
                    return;
                }
            }
            else
            {
                // TODO: resolve property, ex. Т.Ссылка.Код
                // 1. Add LEFT JOIN operator
                // 2. Replace MultiPartIdentifier[1] with reference to the last property in the expression
                // 3. Remove all Identifiers from MultiPartIdentifier where index > 1
            }
        }
    }
}
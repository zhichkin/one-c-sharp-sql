using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    internal sealed class SelectElementVisitor : ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal SelectElementVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return null; } }

        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            SelectElement element = node as SelectElement;
            if (element == null) return result;

            StatementNode statement = result as StatementNode;
            if (statement == null) return result;

            statement.VisitContext = element; // set current visiting context

            if (!(element is SelectScalarExpression expression)) return result;

            string columnName;
            if (expression.ColumnName == null)
            {
                columnName = string.Empty;
            }
            else
            {
                columnName = expression.ColumnName.Value;
            }

            statement.Columns.Add(new ColumnNode()
            {
                Parent = result,
                Fragment = node,
                ParentFragment = parent,
                TargetProperty = sourceProperty,
                Name = columnName
            });

            return result;
        }
    }
}
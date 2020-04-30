using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.Scripting.Services
{
    internal sealed class BooleanBinaryExpressionVisitor : ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal BooleanBinaryExpressionVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return null; } }
        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            BooleanBinaryExpression expression = node as BooleanBinaryExpression;
            if (expression == null) return result;

            StatementNode statement = result as StatementNode;
            if (statement == null) return result;

            if (statement.VisitContext is WhereClause // WHERE
                || statement.VisitContext is QualifiedJoin) // ON
            {
                VisitBooleanBinaryExpression(expression, parent, sourceProperty);
            }

            return result;
        }
        private void VisitBooleanBinaryExpression(BooleanBinaryExpression expression, TSqlFragment parent, string sourceProperty)
        {
            //if (property.Fields.Count == 1) return;

            //if(expression.FirstExpression is BooleanComparisonExpression)

            //ColumnReferenceExpression operand;
            //if (sourceProperty == "FirstExpression")
            //{
            //    operand = parent.SecondExpression as ColumnReferenceExpression;
            //}
            //else if (sourceProperty == "SecondExpression")
            //{
            //    operand = parent.FirstExpression as ColumnReferenceExpression;
            //}
            //else { return; }

            //if (operand == null)
            //{
            //    return;
            //}
        }
    }
}
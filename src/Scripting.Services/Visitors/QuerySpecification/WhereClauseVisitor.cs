using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.Scripting.Services
{
    internal sealed class WhereClauseVisitor :ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal WhereClauseVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return null; } }
        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            WhereClause where = node as WhereClause;
            if (where == null) return result;

            StatementNode statement = result as StatementNode;
            if (statement == null) return result;

            statement.VisitContext = where; // set current visiting context

            return result;
        }
    }
}
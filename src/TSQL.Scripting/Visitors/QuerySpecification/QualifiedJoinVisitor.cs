using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    internal sealed class QualifiedJoinVisitor : ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal QualifiedJoinVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return new List<string>() { "FirstTableReference", "SecondTableReference" }; } }
        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            QualifiedJoin join = node as QualifiedJoin;
            if (join == null) return result;

            StatementNode statement = result as StatementNode;
            if (statement == null) return result;

            statement.VisitContext = join; // set current visiting context
            // TODO: how to set VisitingContext to null when QualifiedJoin visiting scope is missed ???
            // TODO: EnterContext !?
            // TODO: ExitContext  !?

            return result;
        }
    }
}
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    internal sealed class InsertSpecificationVisitor : ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal InsertSpecificationVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return new List<string>() { "Target", "Columns", "InsertSource" }; } }
        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            InsertSpecification insert = node as InsertSpecification;
            if (insert == null) return result;

            StatementNode statement = new StatementNode()
            {
                Parent = result,
                Fragment = node,
                ParentFragment = parent,
                TargetProperty = sourceProperty
            };
            if (result is ScriptNode script)
            {
                if (parent is InsertStatement)
                {
                    script.Statements.Add(statement);
                    return statement;
                }
            }
            return result;
        }
    }
}
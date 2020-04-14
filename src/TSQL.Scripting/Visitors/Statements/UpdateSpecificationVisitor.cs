using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    internal sealed class UpdateSpecificationVisitor : ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal UpdateSpecificationVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return new List<string>() { "Target", "FromClause", "SetClauses"  }; } }
        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            UpdateSpecification update = node as UpdateSpecification;
            if (update == null) return result;

            StatementNode statement = new StatementNode()
            {
                Parent = result,
                Fragment = node,
                ParentFragment = parent,
                TargetProperty = sourceProperty
            };
            if (result is ScriptNode script)
            {
                if (parent is UpdateStatement)
                {
                    script.Statements.Add(statement);
                    return statement;
                }
            }
            return result;
        }
    }
}
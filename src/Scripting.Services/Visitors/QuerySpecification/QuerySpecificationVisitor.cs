using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.Scripting.Services
{
    internal sealed class QuerySpecificationVisitor : ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal QuerySpecificationVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return new List<string>() { "FromClause" }; } }
        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            QuerySpecification specification = node as QuerySpecification;
            if (specification == null) return result;

            StatementNode statement = new StatementNode()
            {
                Parent = result,
                Fragment = node,
                ParentFragment = parent,
                TargetProperty = sourceProperty
            };
            if (result is ScriptNode script)
            {
                if (parent is SelectStatement)
                {
                    script.Statements.Add(statement);
                }
            }
            else if (result is StatementNode query)
            {
                if (parent is TableReferenceWithAlias table)
                {
                    string alias = GetAlias(table);
                    query.Tables.Add(alias, statement);
                }
            }
            else
            {
                return result; // TODO: error ?
            }
            return statement;
        }
        private string GetAlias(TableReferenceWithAlias table)
        {
            if (table.Alias == null) // TODO: error ?
            {
                return string.Empty;
            }
            else
            {
                return table.Alias.Value;
            }
        }
    }
}
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
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

            SelectNode query = new SelectNode()
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
                    script.Statements.Add(query);
                }
            }
            else if (result is SelectNode select)
            {
                if (parent is TableReferenceWithAlias table)
                {
                    string alias = GetAlias(table);
                    select.Tables.Add(alias, query);
                }
            }
            else
            {
                return result; // TODO: error ?
            }
            return query;
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
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.Scripting.Services
{
    internal sealed class NamedTableReferenceVisitor : ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal NamedTableReferenceVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return null; } }
        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            NamedTableReference tableReference = node as NamedTableReference;
            if (tableReference == null) return result;
            
            StatementNode statement = result as StatementNode;
            if (statement == null) return result;

            SchemaObjectName name = tableReference.SchemaObject;
            string serverIdentifier = name.ServerIdentifier?.Value;
            string databaseIdentifier = name.DatabaseIdentifier?.Value;
            string schemaIdentifier = name.SchemaIdentifier?.Value;
            string tableIdentifier = name.BaseIdentifier?.Value;
            if (string.IsNullOrEmpty(tableIdentifier)) return result;

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
                return result;
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

            MetaObject @object = MetadataService.GetMetaObject(databaseName, tableIdentifier);
            TableNode table = new TableNode()
            {
                Parent = result,
                Fragment = node,
                ParentFragment = parent,
                TargetProperty = sourceProperty,
                MetaObject = @object
            };

            string alias = tableReference.Alias?.Value;
            if (string.IsNullOrEmpty(alias))
            {
                // no alias table - just table identifier
                table.Alias = null;
                statement.Tables.Add(tableIdentifier, table);
            }
            else
            {
                table.Alias = alias;
                statement.Tables.Add(alias, table);
            }
            return result;
        }
    }
}
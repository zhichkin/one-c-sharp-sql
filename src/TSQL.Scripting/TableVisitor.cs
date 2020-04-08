using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Services;
using System;

namespace OneCSharp.TSQL.Scripting
{
    /// <summary>
    /// The table reference to a CTE or schema object.
    /// </summary>
    internal sealed class TableVisitor : TSqlConcreteFragmentVisitor
    {
        private IMetadataService MetadataService { get; }
        private IScriptingSession ScriptingSession { get; }
        internal TableVisitor(IMetadataService metadataService, IScriptingSession session)
        {
            MetadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            ScriptingSession = session ?? throw new ArgumentNullException(nameof(session));
        }
        public override void Visit(NamedTableReference tableReference)
        {
            if (tableReference == null) return;

            string alias = tableReference.Alias?.Value;
            if (!string.IsNullOrEmpty(alias))
            {
                ScriptingSession.TableAliases.Add(alias, tableReference);
            }

            SchemaObjectName name = tableReference.SchemaObject;
            string serverIdentifier = name.ServerIdentifier?.Value;
            string databaseIdentifier = name.DatabaseIdentifier?.Value;
            string schemaIdentifier = name.SchemaIdentifier?.Value;
            string tableIdentifier = name.BaseIdentifier?.Value;

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
                return;
            }
            
            if (name.ServerIdentifier != null)
            {
                name.ServerIdentifier.Value = serverIdentifier;
            }
            if (name.DatabaseIdentifier != null)
            {
                name.DatabaseIdentifier.Value = databaseIdentifier;
            }
            if (name.SchemaIdentifier != null)
            {
                name.SchemaIdentifier.Value = schemaIdentifier;
            }
            if (name.BaseIdentifier != null)
            {
                name.BaseIdentifier.Value = MetadataService.MapTableIdentifier(ScriptingSession.InfoBase, tableIdentifier);
                if (!string.IsNullOrEmpty(alias))
                {
                    ScriptingSession.TableAliasAndOriginalName.Add(alias, tableIdentifier);
                }
            }
        }
    }
}
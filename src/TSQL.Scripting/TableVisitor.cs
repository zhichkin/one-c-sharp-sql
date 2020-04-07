using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace OneCSharp.TSQL.Scripting
{
    /// <summary>
    /// The table reference to a CTE or schema object.
    /// </summary>
    public sealed class TableVisitor : TSqlConcreteFragmentVisitor
    {
        private SchemaMapper Mapper { get; }
        public TableVisitor(SchemaMapper mapper)
        {
            Mapper = mapper;
        }
        public override void Visit(NamedTableReference tableReference)
        {
            if (tableReference == null) return;

            string alias = tableReference.Alias?.Value;
            if (!string.IsNullOrEmpty(alias))
            {
                Mapper.TableAliases.Add(alias, tableReference);
            }
            SchemaObjectName name = tableReference.SchemaObject;
            string serverIdentifier = name.ServerIdentifier?.Value;
            string databaseIdentifier = name.DatabaseIdentifier?.Value;
            string schemaIdentifier = name.SchemaIdentifier?.Value;
            string tableIdentifier = name.BaseIdentifier?.Value;
            if (serverIdentifier != null)
            {
                //name.ServerIdentifier.Value = Mapper.Mappings[serverIdentifier];
            }
            if (databaseIdentifier != null)
            {
                name.DatabaseIdentifier.Value = Mapper.Mappings[databaseIdentifier];
            }
            if (schemaIdentifier != null)
            {
                name.SchemaIdentifier.Value = Mapper.Mappings[schemaIdentifier];
            }
            if (tableIdentifier != null)
            {
                name.BaseIdentifier.Value = Mapper.Mappings[tableIdentifier];
            }
        }
    }
}
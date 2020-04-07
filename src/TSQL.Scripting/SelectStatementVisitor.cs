using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Linq;

namespace OneCSharp.TSQL.Scripting
{
    internal class SelectStatementVisitor : TSqlConcreteFragmentVisitor
    {
        private SchemaMapper Mapper { get; }
        public SelectStatementVisitor(SchemaMapper mapper)
        {
            Mapper = mapper;
        }
        public override void Visit(SelectStatement node)
        {
            var specification = (node.QueryExpression) as QuerySpecification;
            var tableReference = specification.FromClause.TableReferences.FirstOrDefault() as NamedTableReference;

            string alias = tableReference.Alias?.Value;
            if (!string.IsNullOrEmpty(alias))
            {
                Mapper.TableAliases.Add(alias, tableReference);
            }

            string serverIdentifier = tableReference?.SchemaObject.ServerIdentifier?.Value;
            string databaseIdentifier = tableReference?.SchemaObject.DatabaseIdentifier?.Value;
            string schemaIdentifier = tableReference?.SchemaObject.SchemaIdentifier?.Value;
            string tableIdentifier = tableReference?.SchemaObject.BaseIdentifier?.Value;
            if (databaseIdentifier != null)
            {
                tableReference.SchemaObject.DatabaseIdentifier.Value = Mapper.Mappings[databaseIdentifier];
            }
            if (schemaIdentifier != null)
            {
                tableReference.SchemaObject.SchemaIdentifier.Value = Mapper.Mappings[schemaIdentifier];
            }
            if (tableIdentifier != null)
            {
                tableReference.SchemaObject.BaseIdentifier.Value = Mapper.Mappings[tableIdentifier];
            }
        }
    }
}
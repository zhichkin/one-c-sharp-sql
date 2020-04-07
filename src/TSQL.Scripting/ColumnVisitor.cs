using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace OneCSharp.TSQL.Scripting
{
    internal class ColumnVisitor : TSqlConcreteFragmentVisitor
    {
        private SchemaMapper Mapper { get; }
        public ColumnVisitor(SchemaMapper mapper)
        {
            Mapper = mapper;
        }
        public override void Visit(ColumnReferenceExpression node)
        {
            Identifier identifier = null;
            if (node.MultiPartIdentifier.Identifiers.Count == 1)
            {
                identifier = node.MultiPartIdentifier.Identifiers[0];
                // TODO: find table reference by unique name of this column name
                // NOTE: other tables in FROM clause do not have columns with this column's name
            }
            else if (node.MultiPartIdentifier.Identifiers.Count == 2)
            {
                Identifier alias = node.MultiPartIdentifier.Identifiers[0];
                if (Mapper.TableAliases.TryGetValue(alias.Value, out TableReference table))
                {
                    // TODO: process table by alias
                }
                identifier = node.MultiPartIdentifier.Identifiers[1];
            }
            else
            {
                // TODO: resolve property, ex. Т.Ссылка.Код
                // 1. Add LEFT JOIN operator
                // 2. Replace MultiPartIdentifier[1] with reference to the last property in the expression
                // 3. Remove all Identifiers from MultiPartIdentifier where index > 1
            }
            if (identifier != null)
            {
                string value;
                if (Mapper.Mappings.TryGetValue(identifier.Value, out value))
                {
                    identifier.Value = Mapper.Mappings[identifier.Value];
                }
            }
        }
    }
}
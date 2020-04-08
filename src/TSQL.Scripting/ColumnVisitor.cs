using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using System;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    internal class ColumnVisitor : TSqlConcreteFragmentVisitor
    {
        private IMetadataService MetadataService { get; }
        private IScriptingSession ScriptingSession { get; }
        public ColumnVisitor(IMetadataService metadataService, IScriptingSession session)
        {
            MetadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            ScriptingSession = session ?? throw new ArgumentNullException(nameof(session));
        }
        public override void Visit(ColumnReferenceExpression node)
        {
            if (node.ColumnType != ColumnType.Regular) return;

            Identifier identifier = null;
            if (node.MultiPartIdentifier.Identifiers.Count == 1)
            {
                //identifier = node.MultiPartIdentifier.Identifiers[0];
                // TODO: find table reference by unique name of this column name
                // NOTE: other tables in FROM clause do not have columns with this column's name
            }
            else if (node.MultiPartIdentifier.Identifiers.Count == 2)
            {
                Identifier alias = node.MultiPartIdentifier.Identifiers[0];
                identifier = node.MultiPartIdentifier.Identifiers[1];
                //if (ScriptingSession.TableAliases.TryGetValue(alias.Value, out NamedTableReference table))
                if (ScriptingSession.TableAliasAndOriginalName.TryGetValue(alias.Value, out string tableIdentifier))
                {
                    IList<Field> fields = MetadataService.MapColumnIdentifier(
                        ScriptingSession.InfoBase,
                        tableIdentifier,
                        identifier.Value);
                    if (fields == null) return;
                    if (fields.Count == 1)
                    {
                        identifier.Value = fields[0].Name;
                    }
                    else
                    {
                        // TODO: add new columns into current select statement !!!
                        //identifier.Value = fields[0].Name;
                        var specification = ScriptingSession.Statement.QueryExpression as QuerySpecification;
                        SelectElement @this = null;
                        foreach (SelectElement element in specification.SelectElements)
                        {
                            if (element is SelectScalarExpression expression)
                            {
                                if (expression.Expression == node)
                                {
                                    @this = element;
                                    break;
                                }
                            }
                        }
                        if (@this != null)
                        {
                            int index = specification.SelectElements.IndexOf(@this);
                            specification.SelectElements.RemoveAt(index);
                            int counter = 0;
                            foreach (Field field in fields)
                            {
                                MultiPartIdentifier mpi = new MultiPartIdentifier();
                                mpi.Identifiers.Add(new Identifier() { Value = alias.Value });
                                mpi.Identifiers.Add(new Identifier() { Value = field.Name });
                                SelectScalarExpression exp = new SelectScalarExpression()
                                {
                                    ColumnName = new IdentifierOrValueExpression()
                                    {
                                        Identifier = new Identifier()
                                        {
                                            Value = identifier.Value + (++counter).ToString()
                                        }
                                    },
                                    Expression = new ColumnReferenceExpression()
                                    {
                                        ColumnType = ColumnType.Regular,
                                        MultiPartIdentifier = mpi
                                    }
                                };
                                // TODO: changes to the AST should be remembered and applied after SELECT has been traversed
                                specification.SelectElements.Insert(index, exp);
                            }
                        }
                    }
                }
            }
            else
            {
                // TODO: resolve property, ex. Т.Ссылка.Код
                // 1. Add LEFT JOIN operator
                // 2. Replace MultiPartIdentifier[1] with reference to the last property in the expression
                // 3. Remove all Identifiers from MultiPartIdentifier where index > 1
            }
        }
    }
}
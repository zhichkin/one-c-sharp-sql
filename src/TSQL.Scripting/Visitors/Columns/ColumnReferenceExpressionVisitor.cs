using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;

namespace OneCSharp.TSQL.Scripting
{
    public sealed class ColumnReferenceExpressionVisitor : ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal ColumnReferenceExpressionVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return null; } }
        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            ColumnReferenceExpression columnReference = node as ColumnReferenceExpression;
            if (columnReference == null) return result;

            SelectNode select = result as SelectNode;
            if (select == null) return result;
            if (columnReference.ColumnType != ColumnType.Regular) return result;
            if (select.Tables == null || select.Tables.Count == 0) return result;

            TableNode table = null;
            Property property = null;
            Identifier identifier = null;
            string propertyFieldName = null;
            if (columnReference.MultiPartIdentifier.Identifiers.Count == 1)
            {
                // no table alias - just column name
                identifier = columnReference.MultiPartIdentifier.Identifiers[0];
                foreach (ISyntaxNode tableNode in select.Tables.Values)
                {
                    if (tableNode is TableNode)
                    {
                        table = (TableNode)tableNode;
                        if (table.Alias == null)
                        {
                            property = table.MetaObject.Properties.Where(p => p.Name == identifier.Value).FirstOrDefault();
                            if (property != null)
                            {
                                break;
                            }
                        }
                        //TODO: check if property name is unique for all tables having no alias
                    }
                    else if (tableNode is SelectNode)
                    {
                        // TODO: query derived table ... get column from there to understand what to do ... tunneling ...
                        return result;
                    }
                }
            }
            else if (columnReference.MultiPartIdentifier.Identifiers.Count == 2)
            {
                Identifier alias = columnReference.MultiPartIdentifier.Identifiers[0];
                identifier = columnReference.MultiPartIdentifier.Identifiers[1];
                if (select.Tables.TryGetValue(alias.Value, out ISyntaxNode tableNode))
                {
                    if (tableNode is TableNode)
                    {
                        table = (TableNode)tableNode;
                        property = table.MetaObject.Properties.Where(p => p.Name == identifier.Value).FirstOrDefault();
                    }
                    else if (tableNode is SelectNode)
                    {
                        // TODO: query derived table ... get column from there to understand what to do ... tunneling ...
                        return result;
                    }
                }    
            }
            else // columnReference.MultiPartIdentifier.Identifiers.Count == 3
            {
                Identifier alias = columnReference.MultiPartIdentifier.Identifiers[0];
                identifier = columnReference.MultiPartIdentifier.Identifiers[1];
                propertyFieldName = columnReference.MultiPartIdentifier.Identifiers[2].Value;
                if (select.Tables.TryGetValue(alias.Value, out ISyntaxNode tableNode))
                {
                    if (tableNode is TableNode)
                    {
                        table = (TableNode)tableNode;
                        property = table.MetaObject.Properties.Where(p => p.Name == identifier.Value).FirstOrDefault();
                    }
                    else if (tableNode is SelectNode)
                    {
                        // TODO: query derived table ... get column from there to understand what to do ... tunneling ...
                        return result;
                    }
                }
            }

            if (property == null) return result;
            if (property.Fields.Count == 0) return result;

            if (property.IsReferenceType)
            {
                if (propertyFieldName == null)
                {
                    VisitReferenceTypeColumn(columnReference, parent, sourceProperty, identifier, property);
                }
                else
                {
                    VisitReferenceTypeColumn(identifier, property, columnReference.MultiPartIdentifier.Identifiers, propertyFieldName);
                }
            }
            else
            {
                VisitValueTypeColumn(identifier, property);
            }

            return result;
        }
        private void VisitValueTypeColumn(Identifier identifier, Property property)
        {
            if (property.Fields.Count == 1)
            {
                identifier.Value = property.Fields[0].Name;
            }
            else
            {
                // TODO: error !? compound type properties is not supported for multivalued columns !
            }
        }
        private void VisitReferenceTypeColumn(Identifier identifier, Property property, IList<Identifier> identifiers, string fieldName)
        {
            Field field = null;
            if (fieldName == "uuid")
            {
                field = property.Fields.Where(f => f.Purpose == FieldPurpose.Object).FirstOrDefault();
            }
            else if (fieldName == "type")
            {
                field = property.Fields.Where(f => f.Purpose == FieldPurpose.TypeCode).FirstOrDefault();
            }
            else if (fieldName == "TYPE")
            {
                field = property.Fields.Where(f => f.Purpose == FieldPurpose.Discriminator).FirstOrDefault();
            }
            if (field == null) { return; } // TODO: throw new MissingMemberException !?

            identifier.Value = field.Name;
            identifiers.RemoveAt(2); // uuid | type | TYPE
        }
        private void VisitReferenceTypeColumn(ColumnReferenceExpression node, TSqlFragment parent, string sourceProperty, Identifier identifier, Property property)
        {
            if (property.Fields.Count == 1) // Т.Ссылка
            {
                string hexTypeCode = $"0x{property.PropertyTypes[0].ToString("X").PadLeft(8, '0')}";
                identifier.Value = property.Fields[0].Name;
                ParenthesisExpression expression = new ParenthesisExpression()
                {
                    Expression = new ScriptDom.BinaryExpression()
                    {
                        BinaryExpressionType = BinaryExpressionType.Add,
                        FirstExpression = new BinaryLiteral() { Value = hexTypeCode },
                        SecondExpression = node
                    }
                };
                PropertyInfo pi = parent.GetType().GetProperty(sourceProperty);
                bool isList = (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(IList<>));
                if (isList)
                {
                    IList list = (IList)pi.GetValue(parent);
                    int index = list.IndexOf(node);
                    list[index] = expression;
                }
                else
                {
                    pi.SetValue(parent, expression);
                }
            }
            else // Т.Владелец
            {
                // TODO: if parent is SearchCondition => AND TYPE + TRef + RRef

                Field typeCode = property.Fields.Where(f => f.Purpose == FieldPurpose.TypeCode).FirstOrDefault();
                Field reference = property.Fields.Where(f => f.Purpose == FieldPurpose.Object).FirstOrDefault();
                identifier.Value = reference.Name;

                MultiPartIdentifier mpi = new MultiPartIdentifier();
                foreach (var id in node.MultiPartIdentifier.Identifiers)
                {
                    mpi.Identifiers.Add(new Identifier() { Value = id.Value });
                }
                mpi.Identifiers[mpi.Count - 1].Value = typeCode.Name;
                ParenthesisExpression expression = new ParenthesisExpression()
                {
                    Expression = new ScriptDom.BinaryExpression()
                    {
                        BinaryExpressionType = BinaryExpressionType.Add,
                        FirstExpression = new ColumnReferenceExpression()
                        {
                            ColumnType = ColumnType.Regular,
                            MultiPartIdentifier = mpi
                        },
                        SecondExpression = node
                    }
                };

                PropertyInfo pi = parent.GetType().GetProperty(sourceProperty);
                bool isList = (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(IList<>));
                if (isList)
                {
                    IList list = (IList)pi.GetValue(parent);
                    int index = list.IndexOf(node);
                    list[index] = expression;
                }
                else
                {
                    pi.SetValue(parent, expression);
                }
            }
        }
    }
}
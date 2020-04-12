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
    public sealed class FunctionCallVisitor : ISyntaxTreeVisitor
    {
        private IMetadataService MetadataService { get; }
        internal FunctionCallVisitor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public IList<string> PriorityProperties { get { return null; } }
        public ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            FunctionCall functionCall = node as FunctionCall;
            if (functionCall == null) return result;
            if (functionCall.CallTarget == null) return result; // SUM(...)
            if (functionCall.Parameters != null && functionCall.Parameters.Count > 0) return result;
            if (functionCall.FunctionName.Value != "uuid" && functionCall.FunctionName.Value != "type") return result;
            MultiPartIdentifierCallTarget callTarget = functionCall.CallTarget as MultiPartIdentifierCallTarget;
            if (callTarget == null) return result;

            SelectNode select = result as SelectNode;
            if (select == null) return result;
            if (select.Tables == null || select.Tables.Count == 0) return result;

            TableNode table = null;
            Property property = null;
            Identifier identifier = null;
            if (callTarget.MultiPartIdentifier.Identifiers.Count == 1)
            {
                // no table alias - just column name
                identifier = callTarget.MultiPartIdentifier.Identifiers[0];
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
            else if (callTarget.MultiPartIdentifier.Identifiers.Count == 2)
            {
                Identifier alias = callTarget.MultiPartIdentifier.Identifiers[0];
                identifier = callTarget.MultiPartIdentifier.Identifiers[1];
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
            if (!property.IsReferenceType) return result;
            
            VisitReferenceTypeColumn(functionCall, parent, sourceProperty, callTarget.MultiPartIdentifier, property);

            select.Columns.Add(new FunctionNode()
            {
                Parent = result,
                Fragment = node,
                ParentFragment = parent,
                TargetProperty = sourceProperty,
                MetaProperty = property
            });

            return result;
        }
        private void VisitReferenceTypeColumn(FunctionCall functionCall, TSqlFragment parent, string sourceProperty, MultiPartIdentifier identifier, Property property)
        {
            string fieldName = null;
            Field field = null;
            if (functionCall.FunctionName.Value == "uuid")
            {
                if (property.Fields.Count == 1)
                {
                    fieldName = property.Fields[0].Name;
                }
                else
                {
                    field = property.Fields.Where(f => f.Purpose == FieldPurpose.Object).FirstOrDefault();
                    fieldName = field.Name;
                }
            }
            else if (functionCall.FunctionName.Value == "type")
            {
                if (property.Fields.Count == 1)
                {
                    fieldName = $"0x{property.PropertyTypes[0].ToString("X").PadLeft(8, '0')}";
                }
                else
                {
                    field = property.Fields.Where(f => f.Purpose == FieldPurpose.TypeCode).FirstOrDefault();
                    fieldName = field.Name;
                }
            }
            if (fieldName == null) return;

            if (field != null)
            {
                identifier.Identifiers[identifier.Count - 1].Value = fieldName;
            }

            BinaryLiteral binaryLiteral = new BinaryLiteral() { Value = fieldName };
            ColumnReferenceExpression columnReference = new ColumnReferenceExpression()
            {
                ColumnType = ColumnType.Regular,
                MultiPartIdentifier = identifier
            };
            object propertyValue = (field == null) ? (object)binaryLiteral : (object)columnReference;

            PropertyInfo pi = parent.GetType().GetProperty(sourceProperty);
            bool isList = (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(IList<>));
            if (isList)
            {
                IList list = (IList)pi.GetValue(parent);
                int index = list.IndexOf(functionCall);
                list[index] = propertyValue;
            }
            else
            {
                pi.SetValue(parent, propertyValue);
            }
        }
    }
}
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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
            if (functionCall.CallTarget != null) return result;
            if (functionCall.FunctionName.Value != "TYPEOF") return result;
            if (functionCall.Parameters == null || functionCall.Parameters.Count != 1) return result;
            if (!(functionCall.Parameters[0] is ColumnReferenceExpression columnReference)) return result;
            if (columnReference.ColumnType != ColumnType.Regular) return result;

            MetaObject table = GetMetaObject(columnReference.MultiPartIdentifier.Identifiers);
            if (table == null) return result;

            Transform(parent, sourceProperty, functionCall, table.TypeCode);

            return result;
        }
        private MetaObject GetMetaObject(IList<Identifier> identifiers)
        {
            IList<string> tableIdentifiers = new List<string>();
            int count = identifiers.Count;
            for (int i = 0; i < (4 - count); i++)
            {
                tableIdentifiers.Add(null);
            }
            for (int i = 0; i < count; i++)
            {
                tableIdentifiers.Add(identifiers[i].Value);
            }
            return MetadataService.GetMetaObject(tableIdentifiers);
        }
        private void Transform(TSqlFragment parent, string sourceProperty, FunctionCall functionCall, int typeCode)
        {
            string HexTypeCode = $"0x{typeCode.ToString("X").PadLeft(8, '0')}";
            BinaryLiteral binaryLiteral = new BinaryLiteral() { Value = HexTypeCode };
            
            PropertyInfo pi = parent.GetType().GetProperty(sourceProperty);
            bool isList = (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(IList<>));
            if (isList)
            {
                IList list = (IList)pi.GetValue(parent);
                int index = list.IndexOf(functionCall);
                list[index] = binaryLiteral;
            }
            else
            {
                pi.SetValue(parent, binaryLiteral);
            }
        }
    }
}
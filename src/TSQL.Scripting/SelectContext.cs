using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using System;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    internal interface ISelectContext
    {
        IBatchContext Batch { get; }
        SelectStatement Statement { get; set; }
        Dictionary<string, TableInfo> Tables { get; }
        List<TransformAction> Actions { get; }
    }
    internal sealed class SelectContext : ISelectContext
    {
        public SelectContext(IBatchContext batch)
        {
            Batch = batch ?? throw new ArgumentNullException(nameof(batch));
        }
        public IBatchContext Batch { get; }
        public SelectStatement Statement { get; set; }
        public List<TransformAction> Actions { get; } = new List<TransformAction>();
        public Dictionary<string, TableInfo> Tables { get; } = new Dictionary<string, TableInfo>();
    }
    internal sealed class TableInfo
    {
        public string Alias { get; set; }
        public string Database { get; set; }
        public string Identifier { get; set; }
        public TableReference Table { get; set; }
    }
    internal sealed class TransformAction
    {
        public Property Property { get; set; }
        public ColumnReferenceExpression Column { get; set; }
    }
}
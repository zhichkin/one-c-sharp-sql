using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using System;
using System.Collections.Generic;

namespace OneCSharp.TSQL.Scripting
{
    public interface ISyntaxNode
    {
        ISyntaxNode Parent { get; set; }
        TSqlFragment Fragment { get; set; } // fragment associated with this syntax node
        TSqlFragment ParentFragment { get; set; }
        string TargetProperty { get; set; } // property of the parent fragment which value is reference to this node's fragment
    }
    public abstract class SyntaxNode : ISyntaxNode
    {
        public ISyntaxNode Parent { get; set; }
        public TSqlFragment Fragment { get; set; }
        public TSqlFragment ParentFragment { get; set; }
        public string TargetProperty { get; set; }
        public T Ancestor<T>() where T : ISyntaxNode
        {
            Type ancestorType = typeof(T);
            ISyntaxNode ancestor = this.Parent;
            while (ancestor != null)
            {
                if (ancestor.GetType() != ancestorType)
                {
                    ancestor = ancestor.Parent;
                }
                else
                {
                    break;
                }
            }
            return (T)ancestor;
        }
    }
    internal sealed class ScriptNode : SyntaxNode
    {
        public InfoBase InfoBase { get; set; } // initial catalog = default database name
        public List<ISyntaxNode> Statements { get; } = new List<ISyntaxNode>();
    }
    internal sealed class SelectNode : SyntaxNode // SELECT => QuerySpecification
    {
        public Dictionary<string, ISyntaxNode> Tables { get; } = new Dictionary<string, ISyntaxNode>(); // TableNode | SelectNode
        public List<ISyntaxNode> Columns { get; } = new List<ISyntaxNode>(); // ColumnNode | FunctionNode | CastNode
    }
    internal sealed class TableNode : SyntaxNode // Документ.ПоступлениеТоваровУслуг => NamedTableReference
    {
        public string Alias { get; set; }
        public MetaObject MetaObject { get; set; }
    }
    internal sealed class ColumnNode : SyntaxNode // Т.Ссылка => ColumnReferenceExpression
    {
        public Property MetaProperty { get; set; }
    }
    internal sealed class FunctionNode : SyntaxNode // Т.Ссылка.type() => FunctionCall
    {
        public Property MetaProperty { get; set; }
    }
    internal sealed class CastNode : SyntaxNode // CAST(Т.Ссылка AS [УТ.Документ.ПоступлениеТоваровУслуг]) => CastCall + UserDataTypeReference
    {
        public Property MetaProperty { get; set; } // source cast value
        public MetaObject MetaObject { get; set; } // target cast type
    }
}
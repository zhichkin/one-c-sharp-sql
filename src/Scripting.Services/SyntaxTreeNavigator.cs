using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using System.Collections.Generic;

namespace OneCSharp.Scripting.Services
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
    }
    internal sealed class ScriptNode : SyntaxNode
    {
        public InfoBase InfoBase { get; set; } // initial catalog = default database name
        public List<ISyntaxNode> Statements { get; } = new List<ISyntaxNode>();
    }
    internal sealed class StatementNode : SyntaxNode // SELECT => QuerySpecification | InsertSpecification | UpdateSpecification | DeleteSpecification
    {
        public Dictionary<string, ISyntaxNode> Tables { get; } = new Dictionary<string, ISyntaxNode>(); // TableNode | StatementNode
        public List<ISyntaxNode> Columns { get; } = new List<ISyntaxNode>(); // ColumnNode | FunctionNode | CastNode
        public TSqlFragment VisitContext { get; set; } // current query clause context provided during AST traversing
        // TODO: special property visitor plus to type visitors !?
        // TODO: EnterContext + ExitContext !?
    }
    internal sealed class TableNode : SyntaxNode // Документ.ПоступлениеТоваровУслуг => NamedTableReference
    {
        public string Alias { get; set; }
        public MetaObject MetaObject { get; set; }
    }
    internal sealed class ColumnNode : SyntaxNode // Т.Ссылка AS [Ссылка] => SelectScalarExpression
    {
        public string Name { get; set; } // alias of the SELECT element, ex. in SELECT statement
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
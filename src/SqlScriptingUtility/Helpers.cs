using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Text;

namespace SqlScriptingUtility
{
    public static class TSqlDomHelpers
    {
        public static string ToSourceSqlString(this TSqlFragment fragment)
        {
            StringBuilder sqlText = new StringBuilder();
            for (int i = fragment.FirstTokenIndex; i <= fragment.LastTokenIndex; i++)
            {
                sqlText.Append(fragment.ScriptTokenStream[i].Text);
            }
            return sqlText.ToString();
        }

        public static string ToSqlString(this TSqlFragment fragment)
        {
            SqlScriptGenerator generator = new Sql120ScriptGenerator();
            string sql;
            generator.GenerateScript(fragment, out sql);
            return sql;
        }
    }
    public static class StringHelpers
    {
        public static string Indent(this string Source, int NumberOfSpaces)
        {
            string indent = new string(' ', NumberOfSpaces);
            return indent + Source.Replace("\n", "\n" + indent);
        }
        public static string Multiply(this string Source, int Multiplier)
        {
            StringBuilder stringBuilder = new StringBuilder(Multiplier * Source.Length);
            for (int i = 0; i < Multiplier; i++)
            {
                stringBuilder.Append(Source);
            }
            return stringBuilder.ToString();
        }
    }
}

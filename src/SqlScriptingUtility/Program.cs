using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.TSQL.Scripting;
using System;
using System.Collections.Generic;

namespace SqlScriptingUtility
{
    class Program
    {
        public static void Main(string[] args)
        {
            string query = "SELECT myfunc(Н.Ссылка.Код) FROM trade.Справочник.Номенклатура.Товары AS Н"; // [Номенклатура+Товары]

            SchemaMapper mapper = new SchemaMapper();
            mapper.Mappings.Add("trade", "trade");
            mapper.Mappings.Add("Справочник", "dbo");
            mapper.Mappings.Add("Номенклатура", "_Reference10");
            mapper.Mappings.Add("Товары", "_Reference10_VT20");
            mapper.Mappings.Add("Ссылка", "_IDRRef");
            ScriptingService service = new ScriptingService(mapper);

            string sql = service.MapIdentifiers(query, out IList<ParseError> errors);
            foreach (ParseError error in errors)
            {
                Console.WriteLine($"{error.Line}: {error.Message}");
            }
            if (errors.Count > 0)
            {
                Console.ReadKey(false);
                return;
            }
            Console.WriteLine(sql);
            Console.ReadKey(false);
        }
    }
}
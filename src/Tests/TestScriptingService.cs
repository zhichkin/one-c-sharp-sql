using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using OneCSharp.Scripting.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace Tests
{
    [TestClass]
    public class TestScriptingService
    {
        [TestMethod]
        public void TestQuery()
        {
            MetadataServiceSettings settings = new MetadataServiceSettings();
            settings.Catalog = @"C:\Users\User\Desktop\GitHub\publish\one-c-sharp-sql\bin\metadata";
            IMetadataService metadata = new MetadataService();
            metadata.Configure(settings);
            metadata.UseServer("sqlexpress");
            metadata.UseDatabase("trade_11_2_3_159_demo");
            metadata.UseDatabase("accounting_3_0_72_72_demo");
            IQueryExecutor executor = new QueryExecutor(metadata);
            IScriptingService scripting = new ScriptingService(metadata, executor);
            IList<ParseError> errors;
            string sql = scripting.PrepareScript(GetTestQueryText(), out errors);
            foreach (ParseError error in errors)
            {
                Console.WriteLine(error.Message);
            }
            if (errors.Count == 0)
            {
                Console.WriteLine(sql);
            }
        }
        private string GetTestQueryText()
        {
            return File.ReadAllText(@"C:\Users\User\Desktop\GitHub\one-c-sharp-sql\src\Tests\test_query.txt");
        }
    }
}
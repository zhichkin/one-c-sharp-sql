﻿using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using OneCSharp.TSQL.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SqlScriptingUtility
{
    class Program
    {
        public static void Main(string[] args)
        {
            LoadMetadata(@"C:\Users\User\Desktop\GitHub\metadata.xml");
            //foreach (string file in Directory.GetFiles(TestCatalogPath()))
            //{
            //    RunTest(file);
            //}
            Console.ReadKey(false);
        }
        private static string TestCatalogPath()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string appCatalogPath = Path.GetDirectoryName(asm.Location);
            string testCatalogPath = Path.Combine(appCatalogPath, "tests");
            if (!Directory.Exists(testCatalogPath))
            {
                _ = Directory.CreateDirectory(testCatalogPath);
            }
            return testCatalogPath;
        }
        private static void RunTest(string filePath)
        {
            string query = File.ReadAllText(filePath);
            Console.WriteLine(query);
            Console.WriteLine();

            SchemaMapper mapper = new SchemaMapper();
            mapper.Mappings.Add("trade", "trade");
            mapper.Mappings.Add("Справочник", "dbo");
            mapper.Mappings.Add("Номенклатура", "_Reference10");
            mapper.Mappings.Add("Товары", "_Reference10_VT20");
            mapper.Mappings.Add("ВидТовара", "_Reference20_VT10");
            mapper.Mappings.Add("Ссылка", "_IDRRef");
            mapper.Mappings.Add("Наименование", "_Description");
            ScriptingService service = new ScriptingService(mapper);

            string sql = service.MapIdentifiers(query, out IList<ParseError> errors);
            foreach (ParseError error in errors)
            {
                Console.WriteLine($"{error.Line}: {error.Message}");
            }
            if (errors.Count > 0)
            {
                Console.WriteLine("***");
                return;
            }
            Console.WriteLine(sql);
            Console.WriteLine("***");
        }

        private static void LoadMetadata(string file)
        {
            InfoBase ib = new InfoBase();
            XMLMetadataLoader loader = new XMLMetadataLoader();
            loader.Load(file, ib);
            
            SqlConnectionStringBuilder helper = new SqlConnectionStringBuilder()
            {
                DataSource = "zhichkin",
                InitialCatalog = ib.Database,
                IntegratedSecurity = string.IsNullOrWhiteSpace(ib.UserName)
            };
            if (!helper.IntegratedSecurity)
            {
                helper.UserID = ib.UserName;
                helper.Password = ib.Password;
                helper.PersistSecurityInfo = false;
            }
            SQLMetadataLoader metadataLoader = new SQLMetadataLoader();
            metadataLoader.Load(helper.ToString(), ib);
        }
    }
}
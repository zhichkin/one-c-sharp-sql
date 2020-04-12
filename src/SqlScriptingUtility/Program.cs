using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using OneCSharp.TSQL.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SqlScriptingUtility
{
    class Program
    {
        public static void Main(string[] args)
        {
            //LoadMetadata(@"C:\Users\User\Desktop\GitHub\metadata.xml");
            foreach (string file in Directory.GetFiles(TestCatalogPath()))
            {
                RunTest(file);
            }
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
            
            ScriptingService service = new ScriptingService();
            try
            {
                //service.UseServer("zhichkin");
                //service.UseDatabase("reverse_engineering");
                Console.WriteLine();
                Console.WriteLine("Initializing database [reverse_engineering] at server [zhichkin] ...");
                Stopwatch watch = new Stopwatch();
                watch.Start();
                service.Initialize("zhichkin", new string[] { "reverse_engineering" });
                watch.Stop();
                Console.WriteLine($"Initializing {watch.Elapsed} elapsed.");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("***");
            }

            string sql = service.PrepareScript(query, out IList<ParseError> errors);

            try
            {
                
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("***");
            }
        }

        //private static void LoadMetadata(string file)
        //{
        //    InfoBase ib = new InfoBase();
        //    XMLMetadataLoader loader = new XMLMetadataLoader();
        //    loader.Load(file, ib);
            
        //    SqlConnectionStringBuilder helper = new SqlConnectionStringBuilder()
        //    {
        //        DataSource = "zhichkin",
        //        InitialCatalog = ib.Database,
        //        IntegratedSecurity = string.IsNullOrWhiteSpace(ib.UserName)
        //    };
        //    if (!helper.IntegratedSecurity)
        //    {
        //        helper.UserID = ib.UserName;
        //        helper.Password = ib.Password;
        //        helper.PersistSecurityInfo = false;
        //    }
        //    SQLMetadataLoader metadataLoader = new SQLMetadataLoader();
        //    metadataLoader.Load(helper.ToString(), ib);
        //}
    }
}
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace OneCSharp.TSQL.Scripting
{
    class ScriptDomVizualizer
    {
        //static StringBuilder result = new StringBuilder();
        //static void Main(string[] args)
        //{
        //    TextReader rdr = new StreamReader(@"c:\ScriptDom\sampleproc.sql");

        //    IList<ParseError> errors = null;
        //    TSql150Parser parser = new TSql150Parser(true);
        //    TSqlFragment tree = parser.Parse(rdr, out errors);

        //    foreach (ParseError err in errors)
        //    {
        //        Console.WriteLine(err.Message);
        //    }

        //    ScriptDomWalk(tree, "root");

        //    TextWriter wr = new StreamWriter(@"c:\temp\scrdom.xml");
        //    wr.Write(result);
        //    wr.Flush();
        //    wr.Dispose();
        //}
        //private static void ScriptDomWalk(object fragment, string memberName)
        //{
        //    if (fragment.GetType().BaseType.Name != "Enum")
        //    {
        //        result.AppendLine("<" + fragment.GetType().Name + " memberName = '" + memberName + "'>");
        //    }
        //    else
        //    {
        //        result.AppendLine("<" + fragment.GetType().Name + "." + fragment.ToString() + "/>");
        //        return;
        //    }

        //    Type t = fragment.GetType();

        //    PropertyInfo[] pibase;
        //    if (null == t.BaseType)
        //    {
        //        pibase = null;
        //    }
        //    else
        //    {
        //        pibase = t.BaseType.GetProperties();
        //    }

        //    foreach (PropertyInfo pi in t.GetProperties())
        //    {
        //        if (pi.GetIndexParameters().Length != 0)
        //        {
        //            continue;
        //        }

        //        if (pi.PropertyType.BaseType != null)
        //        {
        //            if (pi.PropertyType.BaseType.Name == "ValueType")
        //            {
        //                result.Append("<" + pi.Name + ">" + pi.GetValue(fragment, null).ToString() + "</" + pi.Name + ">");
        //                continue;
        //            }
        //        }

        //        if (pi.PropertyType.Name.Contains(@"IList`1"))
        //        {
        //            if ("ScriptTokenStream" != pi.Name)
        //            {
        //                var listMembers = pi.GetValue(fragment, null) as IEnumerable<object>;

        //                foreach (object listItem in listMembers)
        //                {
        //                    ScriptDomWalk(listItem, pi.Name);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            object childObj = pi.GetValue(fragment, null);

        //            if (childObj != null)
        //            {
        //                if (childObj.GetType() == typeof(string))
        //                {
        //                    result.Append(pi.GetValue(fragment, null));
        //                }
        //                else
        //                {
        //                    ScriptDomWalk(childObj, pi.Name);
        //                }
        //            }
        //        }
        //    }

        //    result.AppendLine("</" + fragment.GetType().Name + ">");
        //}
    }
}
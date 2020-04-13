using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OneCSharp.TSQL.Scripting
{
    internal interface ISyntaxTreeVisitor
    {
        IList<string> PriorityProperties { get; }
        ISyntaxNode Visit(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result);
    }
    internal static class SyntaxTreeVisitor
    {
        internal static Dictionary<Type, ISyntaxTreeVisitor> Visitors = new Dictionary<Type, ISyntaxTreeVisitor>();
        internal static void Visit(TSqlFragment node, ISyntaxNode result)
        {
            VisitRecursively(node, null, null, result);
            // VisitIteratively using queue ...
        }
        private static void VisitRecursively(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            if (node == null) return;
            VisitChildren(node, VisitNode(node, parent, sourceProperty, result));
        }
        private static ISyntaxNode VisitNode(TSqlFragment node, TSqlFragment parent, string sourceProperty, ISyntaxNode result)
        {
            ISyntaxTreeVisitor visitor;
            if (Visitors.TryGetValue(node.GetType(), out visitor))
            {
                return visitor.Visit(node, parent, sourceProperty, result);
            }
            return result;
        }
        private static void VisitChildren(TSqlFragment parent, ISyntaxNode result)
        {
            Type type = parent.GetType();

            IList<PropertyInfo> properties = GetProperties(type); // considering order priority defined by type visitor

            foreach (PropertyInfo property in properties)
            {
                if (property.GetIndexParameters().Length > 0) // property is an indexer
                {
                    // indexer property name is "Item" with parameters
                    continue;
                }

                Type propertyType = property.PropertyType;
                bool isList = (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IList<>));

                if (isList)
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }
                if (!propertyType.IsSubclassOf(typeof(TSqlFragment)))
                {
                    continue;
                }

                object child = property.GetValue(parent);
                if (child == null)
                {
                    continue;
                }

                if (isList)
                {
                    IList list = (IList)child;
                    for (int i = 0; i < list.Count; i++)
                    {
                        object item = list[i];
                        VisitRecursively((TSqlFragment)item, parent, property.Name, result);
                    }
                }
                else
                {
                    VisitRecursively((TSqlFragment)child, parent, property.Name, result);
                }
            }
        }
        private static IList<PropertyInfo> GetProperties(Type type)
        {
            List<PropertyInfo> properties = null;

            IList<string> priority = null;
            ISyntaxTreeVisitor visitor;
            if (Visitors.TryGetValue(type, out visitor))
            {
                priority = visitor.PriorityProperties;
            }
            
            if (priority == null || priority.Count == 0)
            {
                properties = type.GetProperties().ToList();
            }
            else
            {
                PropertyInfo p;
                properties = new List<PropertyInfo>();
                foreach (string propertyName in priority)
                {
                    p = type.GetProperty(propertyName);
                    if (p != null)
                    {
                        properties.Add(p);
                    }
                }
                foreach (PropertyInfo pi in type.GetProperties())
                {
                    if (priority.Where(p => p == pi.Name).FirstOrDefault() == null)
                    {
                        properties.Add(pi);
                    }
                }
            }

            return properties;
        }

        internal static T Descendant<T>(TSqlFragment node) where T : TSqlFragment
        {
            // TODO: see VisitChildren method of this class
            return null;
        }

        //public T Ancestor<T>() where T : ISyntaxNode
        //{
        //    Type ancestorType = typeof(T);
        //    ISyntaxNode ancestor = this.Parent;
        //    while (ancestor != null)
        //    {
        //        if (ancestor.GetType() != ancestorType)
        //        {
        //            ancestor = ancestor.Parent;
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }
        //    return (T)ancestor;
        //}
    }
}
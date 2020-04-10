namespace OneCSharp.TSQL.Scripting
{
    internal sealed class ColumnTransformAction
    {
        public void Transform()
        {
            // TODO: add new columns into current select statement !!!
            //identifier.Value = fields[0].Name;
            //var specification = SelectContext.Statement.QueryExpression as QuerySpecification;
            //SelectElement @this = null;
            //foreach (SelectElement element in specification.SelectElements)
            //{
            //    if (element is SelectScalarExpression expression)
            //    {
            //        if (expression.Expression == node)
            //        {
            //            @this = element;
            //            break;
            //        }
            //    }
            //}
            //if (@this != null)
            //{
            //    int index = specification.SelectElements.IndexOf(@this);
            //    specification.SelectElements.RemoveAt(index);
            //    int counter = 0;
            //    foreach (Field field in fields)
            //    {
            //        MultiPartIdentifier mpi = new MultiPartIdentifier();
            //        mpi.Identifiers.Add(new Identifier() { Value = alias.Value });
            //        mpi.Identifiers.Add(new Identifier() { Value = field.Name });
            //        SelectScalarExpression exp = new SelectScalarExpression()
            //        {
            //            ColumnName = new IdentifierOrValueExpression()
            //            {
            //                Identifier = new Identifier()
            //                {
            //                    Value = identifier.Value + (++counter).ToString()
            //                }
            //            },
            //            Expression = new ColumnReferenceExpression()
            //            {
            //                ColumnType = ColumnType.Regular,
            //                MultiPartIdentifier = mpi
            //            }
            //        };
            //        // TODO: changes to the AST should be remembered and applied after SELECT has been traversed
            //        specification.SelectElements.Insert(index, exp);
            //    }
            //}
        }
    }
}
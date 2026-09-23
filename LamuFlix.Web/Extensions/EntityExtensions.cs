using LamuFlix.Data.Constants;
using LamuFlix.Web.Models.Helper;
using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LamuFlix.Web.Extensions
{
    public static class EntityExtensions
    {
        // ReSharper disable NullableWarningSuppressionIsUsed
        // Reflection result on a known member
        public static IQueryable<T> DynamicQuery<T>(this IQueryable<T> query, object? filter)
        {
            if (filter == null)
            {
                return query;
            }

            var filterHasValue = filter.HasQuery();

            if (filterHasValue)
            {
                var filterProperties = filter.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in filterProperties)
                {
                    if (filter.HasPropertyValue(property.Name))
                    {
                        var item = Expression.Parameter(typeof(T), "item");

                        object attr = property.GetCustomAttributes(true).ElementAt(0);
                        DataMapping map = (DataMapping)attr;

                        var prop = Expression.PropertyOrField(item, map.GetDestination());
                        var propType = prop.Type;
                        var value = Expression.Constant(property.GetValue(filter));
                        var queryConstExpr = Expression.Constant(query);

                        BinaryExpression equal;
                        Expression<Func<IQueryable<T>>> lambda;
                        LambdaExpression lambda1;

                        if (propType.GetInterface(nameof(IEnumerable)) != null && propType != typeof(String))
                        {
                            var innerType = propType.GetGenericArguments().FirstOrDefault()!;
                            var innerItem = Expression.Parameter(innerType, "innerItem");
                            var innerProp = Expression.PropertyOrField(innerItem, map.GetKey()!);
                            var innerPropType = innerProp.Type;
                            if (property.PropertyType.GetInterface(nameof(IEnumerable)) != null && property.PropertyType != typeof(String))
                            {
                                var containsIntMethod = typeof(Enumerable).GetMethods().Where(x => x.Name == "Contains").Single(x => x.GetParameters().Length == 2).MakeGenericMethod(typeof(int));
                                var containsExpCall = Expression.Call(containsIntMethod, value, innerProp);
                                lambda1 = Expression.Lambda(containsExpCall, innerItem);
                            }
                            else
                            {
                                equal = Expression.Equal(innerProp, Expression.Convert(value, innerPropType));
                                lambda1 = Expression.Lambda(equal, innerItem);
                            }

                            var anyMethod = typeof(Enumerable).GetTypeInfo().GetMethods().First(m => m.Name == "Any" && m.GetParameters().Count() == 2).MakeGenericMethod(innerType);
                            var anyCallExp = Expression.Call(anyMethod, prop, lambda1);
                            var lambda2 = Expression.Lambda<Func<T, bool>>(anyCallExp, item);

                            var whereExp = Expression.Call(typeof(Queryable), "Where", [typeof(T)], queryConstExpr, lambda2);
                            lambda = Expression.Lambda<Func<IQueryable<T>>>(whereExp);
                            var resultFunc = lambda.Compile();
                            query = resultFunc();
                        }
                        else
                        {
                            if (property.PropertyType == typeof(String))
                            {
                                var containsStringMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                                var toLowerMethod = typeof(string).GetTypeInfo().GetMethods().First(m => m.Name == "ToLower" && m.GetParameters().Count() == 0);
                                var toLowerPropCall = Expression.Call(prop, toLowerMethod);
                                var toLowerValueCall2 = Expression.Call(value, toLowerMethod);
                                var containsExpCall = Expression.Call(toLowerPropCall, containsStringMethod, toLowerValueCall2);
                                lambda1 = Expression.Lambda(containsExpCall, item);
                            }
                            else
                            {
                                equal = Expression.Equal(prop, Expression.Convert(value, propType));
                                lambda1 = Expression.Lambda(equal, item);
                            }
                            var whereExp = Expression.Call(typeof(Queryable), "Where", [typeof(T)], queryConstExpr, lambda1);
                            lambda = Expression.Lambda<Func<IQueryable<T>>>(whereExp);
                            var resultFunc = lambda.Compile();
                            query = resultFunc();
                        }
                    }
                }

            }

            return query;
        }
        // ReSharper restore NullableWarningSuppressionIsUsed

        // ReSharper disable NullableWarningSuppressionIsUsed
        // Reflection result on a known member
        public static IQueryable<T> DynamicSort<T>(this IQueryable<T> query, object? filter, string sortBy, string sortOrder)
        {
            string methodName;
            if (String.IsNullOrEmpty(sortBy))
            {
                sortBy = GeneralConstants.Id;
                sortOrder = GeneralConstants.Descending;
            }

            if (sortOrder == GeneralConstants.Descending)
            {
                methodName = GeneralConstants.OrderByDescending;
            }
            else
            {
                methodName = GeneralConstants.OrderBy;
            }

            MethodInfo OrderByMethod = typeof(Queryable).GetMethods().Single(method => method.Name == methodName && method.GetParameters().Length == 2);
            ParameterExpression paramterExpression = Expression.Parameter(typeof(T));
            Expression orderByProperty = Expression.Property(paramterExpression, sortBy);
            LambdaExpression lambda = Expression.Lambda(orderByProperty, paramterExpression);
            MethodInfo genericMethod = OrderByMethod.MakeGenericMethod(typeof(T), orderByProperty.Type);
            object? ret = genericMethod.Invoke(null, new object[] { query, lambda });

            return (IQueryable<T>)ret!;
        }
        // ReSharper restore NullableWarningSuppressionIsUsed
    }
}

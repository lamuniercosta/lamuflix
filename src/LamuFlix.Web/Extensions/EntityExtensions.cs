using LamuFlix.Data.Constants;
using LamuFlix.Web.Models.Helper;
using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LamuFlix.Web.Extensions;

public static class EntityExtensions
{
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
                    query = ApplyPropertyFilter(query, filter, property);
                }
            }

        }

        return query;
    }

    private static IQueryable<T> ApplyPropertyFilter<T>(IQueryable<T> query, object filter, PropertyInfo property)
    {
        var item = Expression.Parameter(typeof(T), "item");

        var attr = property.GetCustomAttributes(true).ElementAt(0);
        var map = (DataMapping)attr;

        var prop = Expression.PropertyOrField(item, map.GetDestination());
        var propType = prop.Type;
        var value = Expression.Constant(property.GetValue(filter));
        var queryConstExpr = Expression.Constant(query);

        if (propType.GetInterface(nameof(IEnumerable)) != null && propType != typeof(string))
        {
            query = ApplyCollectionFilter<T>(property, item, map, prop, propType, value, queryConstExpr);
        }
        else
        {
            query = ApplyScalarFilter<T>(property, item, prop, propType, value, queryConstExpr);
        }

        return query;
    }

    // ReSharper disable NullableWarningSuppressionIsUsed
    // Reflection result on a known member
    private static IQueryable<T> ApplyCollectionFilter<T>(PropertyInfo property, ParameterExpression item, DataMapping map, MemberExpression prop, Type propType, ConstantExpression value, ConstantExpression queryConstExpr)
    {
        var innerType = propType.GetGenericArguments().FirstOrDefault()!;
        var innerItem = Expression.Parameter(innerType, "innerItem");
        var innerProp = Expression.PropertyOrField(innerItem, map.GetKey()!);
        var innerPropType = innerProp.Type;
        LambdaExpression lambda1;
        if (property.PropertyType.GetInterface(nameof(IEnumerable)) != null && property.PropertyType != typeof(string))
        {
            var containsIntMethod = typeof(Enumerable).GetMethods().Where(x => x.Name == "Contains").Single(x => x.GetParameters().Length == 2).MakeGenericMethod(typeof(int));
            var containsExpCall = Expression.Call(containsIntMethod, value, innerProp);
            lambda1 = Expression.Lambda(containsExpCall, innerItem);
        }
        else
        {
            var equal = Expression.Equal(innerProp, Expression.Convert(value, innerPropType));
            lambda1 = Expression.Lambda(equal, innerItem);
        }

        var anyMethod = typeof(Enumerable).GetTypeInfo().GetMethods().First(m => m.Name == "Any" && m.GetParameters().Count() == 2).MakeGenericMethod(innerType);
        var anyCallExp = Expression.Call(anyMethod, prop, lambda1);
        var lambda2 = Expression.Lambda<Func<T, bool>>(anyCallExp, item);

        var whereExp = Expression.Call(typeof(Queryable), "Where", [typeof(T)], queryConstExpr, lambda2);
        var lambda = Expression.Lambda<Func<IQueryable<T>>>(whereExp);
        var resultFunc = lambda.Compile();
        return resultFunc();
    }
    // ReSharper restore NullableWarningSuppressionIsUsed

    // ReSharper disable NullableWarningSuppressionIsUsed
    // Reflection result on a known member
    private static IQueryable<T> ApplyScalarFilter<T>(PropertyInfo property, ParameterExpression item, MemberExpression prop, Type propType, ConstantExpression value, ConstantExpression queryConstExpr)
    {
        LambdaExpression lambda1;
        if (property.PropertyType == typeof(string))
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
            var equal = Expression.Equal(prop, Expression.Convert(value, propType));
            lambda1 = Expression.Lambda(equal, item);
        }
        var whereExp = Expression.Call(typeof(Queryable), "Where", [typeof(T)], queryConstExpr, lambda1);
        var lambda = Expression.Lambda<Func<IQueryable<T>>>(whereExp);
        var resultFunc = lambda.Compile();
        return resultFunc();
    }
    // ReSharper restore NullableWarningSuppressionIsUsed

    // ReSharper disable NullableWarningSuppressionIsUsed
    // Reflection result on a known member
    public static IQueryable<T> DynamicSort<T>(this IQueryable<T> query, object? filter, string sortBy, string sortOrder)
    {
        string methodName;
        if (string.IsNullOrEmpty(sortBy))
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

        var orderByMethod = typeof(Queryable).GetMethods().Single(method => method.Name == methodName && method.GetParameters().Length == 2);
        var paramterExpression = Expression.Parameter(typeof(T));
        Expression orderByProperty = Expression.Property(paramterExpression, sortBy);
        var lambda = Expression.Lambda(orderByProperty, paramterExpression);
        var genericMethod = orderByMethod.MakeGenericMethod(typeof(T), orderByProperty.Type);
        var ret = genericMethod.Invoke(null, new object[] { query, lambda });

        return (IQueryable<T>)ret!;
    }
    // ReSharper restore NullableWarningSuppressionIsUsed
}
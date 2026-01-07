using DataTableJs.ServerSide.Models.Requests;
using DataTableJs.ServerSide.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataTableJs.ServerSide
{
    public static class Parser
    {
        /// <summary>
        /// Applies all datatable.js filters, sorting, and pagination to the provided IQueryable source.
        /// </summary>
        /// <remarks>The returned UnpaginatedQuery includes all filters and sorting but excludes
        /// pagination, which can be useful for obtaining total record counts. PaginatedQuery includes all filters,
        /// sorting, and pagination as specified in the request.</remarks>
        /// <typeparam name="T">The type of the elements in the queryable source.</typeparam>
        /// <param name="src">The queryable data source to which filters, sorting, and pagination will be applied. Cannot be null.</param>
        /// <param name="request">The DataTable server request containing filtering, sorting, and pagination parameters. Cannot be null.</param>
        /// <returns>A tuple containing the queryable result after applying all filters and sorting (UnpaginatedQuery), and the
        /// queryable result after applying pagination (PaginatedQuery).</returns>
        /// <exception cref="ArgumentNullException">Thrown if either src or request is null.</exception>
        public static (IQueryable<T> UnpaginatedQuery, IQueryable<T> PaginatedQuery) ApplyFilters<T>(this IQueryable<T> src, DataTableServerRequest request)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Apply global search filter
            var beforePagination = ApplyGlobalSearch(src, request);

            // Apply column-specific search filters
            beforePagination = ApplyColumnSearch(beforePagination, request);

            // Apply sorting
            beforePagination = ApplyOrdering(beforePagination, request);

            // Apply pagination
            var afterPagination = ApplyPagination(beforePagination, request);

            return (beforePagination, afterPagination);
        }

        /// <summary>
        /// Applies global search filtering across all searchable columns.
        /// </summary>
        public static IQueryable<T> ApplyGlobalSearch<T>(IQueryable<T> src, DataTableServerRequest request)
        {
            if (request.Search == null || string.IsNullOrWhiteSpace(request.Search.Value))
                return src;

            if (request.Columns == null || !request.Columns.Any())
                return src;

            var searchValue = request.Search.Value.Trim();
            var searchableColumns = request.Columns.Where(c => c.Searchable && !string.IsNullOrWhiteSpace(c.Data)).ToList();

            if (!searchableColumns.Any())
                return src;

            // Build OR expression for all searchable columns
            var parameter = Expression.Parameter(typeof(T), "x");
            Expression orExpression = null;

            foreach (var column in searchableColumns)
            {
                var propertyExpression = GetPropertyExpression(parameter, column.Data);
                if (propertyExpression == null)
                    continue;

                var searchExpression = BuildSearchExpression(propertyExpression, searchValue);
                if (searchExpression != null)
                {
                    orExpression = orExpression == null
                        ? searchExpression
                        : Expression.OrElse(orExpression, searchExpression);
                }
            }

            if (orExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(orExpression, parameter);
                src = src.Where(lambda);
            }

            return src;
        }

        /// <summary>
        /// Applies column-specific search filters.
        /// </summary>
        public static IQueryable<T> ApplyColumnSearch<T>(IQueryable<T> src, DataTableServerRequest request)
        {
            if (request.Columns == null || !request.Columns.Any())
                return src;

            var parameter = Expression.Parameter(typeof(T), "x");

            foreach (var column in request.Columns)
            {
                if (column.Search == null || string.IsNullOrWhiteSpace(column.Search.Value))
                    continue;

                if (string.IsNullOrWhiteSpace(column.Data))
                    continue;

                var searchValue = column.Search.Value.Trim();
                var propertyExpression = GetPropertyExpression(parameter, column.Data);

                if (propertyExpression == null)
                    continue;

                var searchExpression = BuildSearchExpression(propertyExpression, searchValue);
                if (searchExpression != null)
                {
                    var lambda = Expression.Lambda<Func<T, bool>>(searchExpression, parameter);
                    src = src.Where(lambda);
                }
            }

            return src;
        }

        /// <summary>
        /// Applies sorting based on the order specifications.
        /// </summary>
        public static IQueryable<T> ApplyOrdering<T>(IQueryable<T> src, DataTableServerRequest request)
        {
            if (request.Order == null || !request.Order.Any())
                return src;

            if (request.Columns == null || !request.Columns.Any())
                return src;

            IOrderedQueryable<T> orderedQuery = null;
            bool isFirstOrder = true;

            foreach (var order in request.Order)
            {
                if (order.Column < 0 || order.Column >= request.Columns.Count)
                    continue;

                var column = request.Columns[order.Column];

                if (!column.Orderable || string.IsNullOrWhiteSpace(column.Data))
                    continue;

                var isAscending = string.Equals(order.Dir, "asc", StringComparison.OrdinalIgnoreCase);

                if (isFirstOrder)
                {
                    orderedQuery = isAscending
                        ? src.OrderByProperty(column.Data)
                        : src.OrderByPropertyDescending(column.Data);
                    isFirstOrder = false;
                }
                else
                {
                    orderedQuery = isAscending
                        ? orderedQuery.ThenByProperty(column.Data)
                        : orderedQuery.ThenByPropertyDescending(column.Data);
                }
            }

            return orderedQuery ?? src;
        }

        /// <summary>
        /// Applies pagination (skip and take).
        /// </summary>
        public static IQueryable<T> ApplyPagination<T>(IQueryable<T> src, DataTableServerRequest request)
        {
            if (request.Start > 0)
                src = src.Skip(request.Start);

            if (request.Length > 0)
                src = src.Take(request.Length);

            return src;
        }

        #region PRIVATE

        /// <summary>
        /// Gets a property expression for a given property path (supports nested properties).
        /// </summary>
        private static Expression GetPropertyExpression(Expression parameter, string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
                return null;

            try
            {
                var properties = propertyPath.Split('.');
                Expression expression = parameter;

                foreach (var prop in properties)
                {
                    var propertyInfo = expression.Type.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (propertyInfo == null)
                        return null;

                    expression = Expression.Property(expression, propertyInfo);
                }

                return expression;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds a search expression appropriate for the property type.
        /// </summary>
        private static Expression BuildSearchExpression(Expression propertyExpression, string searchValue)
        {
            try
            {
                var propertyType = propertyExpression.Type;
                var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                
                if (underlyingType == typeof(string))
                {
                    return BuildStringContainsExpression(propertyExpression, searchValue);
                }
                else if (underlyingType.IsEnum)
                {
                    return BuildEnumEqualityExpression(propertyExpression, propertyType, underlyingType, searchValue);
                }
                else if (IsNumericType(underlyingType))
                {
                    return BuildNumericEqualityExpression(propertyExpression, propertyType, underlyingType, searchValue);
                }
                else
                {
                    return BuildToStringContainsExpression(propertyExpression, propertyType, underlyingType, searchValue);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds a Contains expression for string properties.
        /// </summary>
        private static Expression BuildStringContainsExpression(Expression propertyExpression, string searchValue)
        {
            var notNullExpression = Expression.NotEqual(propertyExpression, Expression.Constant(null, typeof(string)));
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var searchExpression = Expression.Constant(searchValue);
            var containsCall = Expression.Call(propertyExpression, containsMethod, searchExpression);
            
            return Expression.AndAlso(notNullExpression, containsCall);
        }

        /// <summary>
        /// Builds an equality expression for enum properties.
        /// </summary>
        private static Expression BuildEnumEqualityExpression(Expression propertyExpression, Type propertyType, Type underlyingType, string searchValue)
        {
            try
            {
                var enumValue = Enum.Parse(underlyingType, searchValue, true);
                var constantValue = Expression.Constant(enumValue, underlyingType);
                
                if (propertyType != underlyingType)
                {
                    // Nullable enum
                    var hasValueProperty = propertyType.GetProperty("HasValue");
                    var valueProperty = propertyType.GetProperty("Value");
                    
                    var hasValue = Expression.Property(propertyExpression, hasValueProperty);
                    var value = Expression.Property(propertyExpression, valueProperty);
                    var equality = Expression.Equal(value, constantValue);
                    
                    return Expression.AndAlso(hasValue, equality);
                }
                else
                {
                    // Non-nullable enum
                    return Expression.Equal(propertyExpression, constantValue);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds an equality expression for numeric properties.
        /// </summary>
        private static Expression BuildNumericEqualityExpression(Expression propertyExpression, Type propertyType, Type underlyingType, string searchValue)
        {
            try
            {
                var parsedValue = Convert.ChangeType(searchValue, underlyingType);
                var constantValue = Expression.Constant(parsedValue, underlyingType);
                
                if (propertyType != underlyingType)
                {
                    // Nullable numeric
                    var hasValueProperty = propertyType.GetProperty("HasValue");
                    var valueProperty = propertyType.GetProperty("Value");
                    
                    var hasValue = Expression.Property(propertyExpression, hasValueProperty);
                    var value = Expression.Property(propertyExpression, valueProperty);
                    var equality = Expression.Equal(value, constantValue);
                    
                    return Expression.AndAlso(hasValue, equality);
                }
                else
                {
                    // Non-nullable numeric
                    return Expression.Equal(propertyExpression, constantValue);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds a Contains expression for other types by converting to string first.
        /// </summary>
        private static Expression BuildToStringContainsExpression(Expression propertyExpression, Type propertyType, Type underlyingType, string searchValue)
        {
            Expression stringExpression;
            Expression notNullExpression;
            
            if (propertyType != underlyingType)
            {
                // Nullable type
                var hasValueProperty = propertyType.GetProperty("HasValue");
                var valueProperty = propertyType.GetProperty("Value");
                
                notNullExpression = Expression.Property(propertyExpression, hasValueProperty);
                
                var valueExpression = Expression.Property(propertyExpression, valueProperty);
                var toStringMethod = underlyingType.GetMethod("ToString", Type.EmptyTypes);
                stringExpression = Expression.Call(valueExpression, toStringMethod);
            }
            else
            {
                // Non-nullable type
                var toStringMethod = underlyingType.GetMethod("ToString", Type.EmptyTypes);
                stringExpression = Expression.Call(propertyExpression, toStringMethod);
                notNullExpression = Expression.Constant(true);
            }
            
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var searchExpression = Expression.Constant(searchValue);
            var containsCall = Expression.Call(stringExpression, containsMethod, searchExpression);
            
            return Expression.AndAlso(notNullExpression, containsCall);
        }

        /// <summary>
        /// Checks if a type is a numeric type.
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) ||
                   type == typeof(long) ||
                   type == typeof(short) ||
                   type == typeof(byte) ||
                   type == typeof(uint) ||
                   type == typeof(ulong) ||
                   type == typeof(ushort) ||
                   type == typeof(sbyte) ||
                   type == typeof(decimal) ||
                   type == typeof(double) ||
                   type == typeof(float);
        }

        /// <summary>
        /// Orders a queryable by a property name.
        /// </summary>
        private static IOrderedQueryable<T> OrderByProperty<T>(this IQueryable<T> source, string propertyName)
        {
            return ApplyOrder(source, propertyName, "OrderBy");
        }

        /// <summary>
        /// Orders a queryable by a property name in descending order.
        /// </summary>
        private static IOrderedQueryable<T> OrderByPropertyDescending<T>(this IQueryable<T> source, string propertyName)
        {
            return ApplyOrder(source, propertyName, "OrderByDescending");
        }

        /// <summary>
        /// Applies a secondary sort by a property name.
        /// </summary>
        private static IOrderedQueryable<T> ThenByProperty<T>(this IOrderedQueryable<T> source, string propertyName)
        {
            return ApplyOrder(source, propertyName, "ThenBy");
        }

        /// <summary>
        /// Applies a secondary sort by a property name in descending order.
        /// </summary>
        private static IOrderedQueryable<T> ThenByPropertyDescending<T>(this IOrderedQueryable<T> source, string propertyName)
        {
            return ApplyOrder(source, propertyName, "ThenByDescending");
        }

        /// <summary>
        /// Applies an ordering method to a queryable using reflection.
        /// </summary>
        private static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string propertyName, string methodName)
        {
            var properties = propertyName.Split('.');
            var type = typeof(T);
            var parameter = Expression.Parameter(type, "x");
            Expression expression = parameter;

            foreach (var prop in properties)
            {
                var propertyInfo = type.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new ArgumentException($"Property '{prop}' not found on type '{type.Name}'");

                expression = Expression.Property(expression, propertyInfo);
                type = propertyInfo.PropertyType;
            }

            var lambda = Expression.Lambda(expression, parameter);
            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(T), type },
                source.Expression,
                Expression.Quote(lambda)
            );

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(resultExpression);
        }
        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MiCloudServiceSiteAPI.Helpers.DatatableHelper
{
    public class DatatableCustomSearchService : IDatatableCustomSearchService
    {
        private static MethodInfo containsMethod = typeof(string).GetMethod("Contains");

        private static MethodInfo startsWithMethod =
        typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) });

        private static MethodInfo endsWithMethod =
        typeof(string).GetMethod("EndsWith", new Type[] { typeof(string) });

        public IEnumerable<T> CustomSearch<T>(IEnumerable<T> source, DataTableAjaxPostModel model,
            out int filteredResultsCount,
            out int totalResultsCount)
        {
            var columnsSearch = (from c in model.columns
                                 where c.search.value != null
                                 select new ColumnFilter
                                 {
                                     Column = c.data,
                                     Value = c.search.value
                                 }).ToList();

            var searchBy = (model.search != null) ? model.search.value : null;
            var take = model.length;
            var skip = model.start;

            string sortBy = "";
            bool sortDir = true;

            if (model.order != null)
            {
                // in this example we just default sort on the 1st column
                sortBy = model.columns[model.order[0].column].data;
                sortDir = model.order[0].dir.ToLower() == "asc";
            }

            if (!string.IsNullOrEmpty(searchBy))
            {
                //filter via search box
                var columns = (from c in model.columns select c.data).ToArray();
                source = FilterData(source, columns, searchBy);
            }

            if (columnsSearch.Count > 0)
            {
                //filter per column search box
                source = FilterData(source, columnsSearch);
            }

            // search the dbase taking into consideration table sorting and paging
            var result = PaginateData(source, searchBy, take, skip, sortBy, sortDir, out filteredResultsCount, out totalResultsCount);
            if (result == null)
            {
                // empty collection...
                return new List<T>();
            }

            return result;
        }

        public IEnumerable<T> PaginateData<T>(IEnumerable<T> source, string searchBy, int take, int skip, string sortBy, bool sortDir, out int filteredResultsCount, out int totalResultsCount)
        {
            var result = sortDir ? source
                           .OrderBy(b => sortBy)
                           .Skip(skip)
                           .Take(take) :
                           source
                           .OrderByDescending(b => sortBy)
                           .Skip(skip)
                           .Take(take);

            // now just get the count of items (without the skip and take) - eg how many could be returned with filtering
            filteredResultsCount = result.Count();
            totalResultsCount = source.Count();

            return result;
        }

        public IEnumerable<T> FilterData<T>(IEnumerable<T> source, string[] fields, string value)
        {
            var param = Expression.Parameter(typeof(T));
            var props = typeof(T);
            Expression exp = null;
            int counter = 1;
            foreach (string column in fields)
            {
                double retNum;
                bool isNum = Double.TryParse(column, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
                if (string.IsNullOrEmpty(column) || isNum)
                {
                    continue;
                }

                if (fields.Length == 1)
                {
                    exp = CreateExpression(source, props, param, column, value);
                }
                else
                {
                    if (counter == 1)
                    {
                        exp = CreateExpression(source, props, param, column, value);
                    }
                    else
                    {
                        //merge
                        var newExpression = CreateExpression(source, props, param, column, value);
                        if (newExpression != null)
                        {
                            exp = Expression.AndAlso(exp, newExpression);
                        }
                    }
                }

                counter++;
            }

            if (exp != null)
            {
                var condition =
                Expression.Lambda<Func<T, bool>>(
                    exp,
                    param
                ).Compile();

                return source.Where(condition);
            }

            return Enumerable.Empty<T>();
        }

        public IEnumerable<T> FilterData<T>(IEnumerable<T> source, List<ColumnFilter> columnFilters)
        {
            var param = Expression.Parameter(typeof(T));
            var props = typeof(T);
            Expression exp = null;
            int counter = 1;
            foreach (var filter in columnFilters)
            {
                double retNum;
                string column = filter.Column;
                string value = filter.Value;
                bool isNum = Double.TryParse(column, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
                if (string.IsNullOrEmpty(column) || isNum)
                {
                    continue;
                }

                if (columnFilters.Count == 1)
                {
                    exp = CreateExpression(source, props, param, column, value);
                }
                else
                {
                    if (counter == 1)
                    {
                        exp = CreateExpression(source, props, param, column, value);
                    }
                    else
                    {
                        //merge
                        var newExpression = CreateExpression(source, props, param, column, value);
                        if (newExpression != null)
                        {
                            exp = Expression.AndAlso(exp, newExpression);
                        }
                    }
                }

                counter++;
            }

            if (exp != null)
            {
                var condition =
                Expression.Lambda<Func<T, bool>>(
                    exp,
                    param
                ).Compile();

                return source.Where(condition);
            }

            return Enumerable.Empty<T>();
        }

        private Expression CreateExpression<T>(IEnumerable<T> src, Type t, ParameterExpression param, string column, string value)
        {
            MethodCallExpression expr = null;
            var prop = t.GetProperty(column);
            var propValue = GetValues(src, column).FirstOrDefault();

            if (propValue != null)
            {
                switch (prop.PropertyType.Name)
                {
                    case "String":
                        expr = Expression.Call(
                                Expression.Property(param, column),
                                containsMethod,
                                Expression.Constant(value));
                        break;

                    default:
                        expr = null;
                        break;
                }
            }
            return expr;
        }

        private static IEnumerable<object> GetValues<T>(IEnumerable<T> items, string propertyName)
        {
            Type type = typeof(T);
            var prop = type.GetProperty(propertyName);
            foreach (var item in items)
                yield return prop.GetValue(item, null);
        }
    }
}

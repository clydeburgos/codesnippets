using System.Collections.Generic;

namespace MiCloudServiceSiteAPI.Helpers.DatatableHelper
{
    public interface IDatatableCustomSearchService
    {
        IEnumerable<T> CustomSearch<T>(IEnumerable<T> source, DataTableAjaxPostModel model,
            out int filteredResultsCount,
            out int totalResultsCount);

        IEnumerable<T> PaginateData<T>(IEnumerable<T> source, string searchBy, int take, int skip, string sortBy, bool sortDir, out int filteredResultsCount, out int totalResultsCount);

        IEnumerable<T> FilterData<T>(IEnumerable<T> source, string[] fields, string value);

        IEnumerable<T> FilterData<T>(IEnumerable<T> source, List<ColumnFilter> columnFilters);
    }
}

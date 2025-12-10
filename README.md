# DataTableJs.ServerSide

A minimal .NET Standard 2.0 library for server-side processing with DataTables.js 1.10+.

Contains models for expected requests and responses,
and a LINQ extension method to apply filtering, sorting, and pagination to any IQueryable using reflection (EF friendly).

### Usage

```csharp
public IActionResult GetData([FromBody] DataTableServerRequest request)
{
    try
    {
        var query = _context.YourEntities.AsQueryable();
        
        var totalRecords = query.Count();
        var (unpaginatedQuery, paginatedQuery) = query.ApplyFilters(request);
        var filteredRecords = unpaginatedQuery.Count();
        var data = paginatedQuery.ToList();
        
        return Json(DataTableServerResponse.Success(
            draw: request.Draw,
            recordsTotal: totalRecords,
            recordsFiltered: filteredRecords,
            data: data
        ));
    }
    catch (Exception ex)
    {
        return Json(DataTableServerResponse.Fail(request.Draw, ex.Message));
    }
}
```

## Features

- **Global search** - Search across all searchable columns
- **Column-specific search** - Individual search filters per column
- **Multi-column sorting** - Sort by multiple columns with ascending/descending support
- **Pagination** - Skip and take support
- **Nested property support** - Access nested properties using dot notation (e.g., `"User.Name"`)
- **Null-safe operations** - Handles null values gracefully
- **Case-insensitive** - Property matching is case-insensitive
- **EF-friendly** - Uses IQueryable and generates efficient SQL queries

## API

### Parser Methods

- `ApplyFilters<T>(IQueryable<T>, DataTableServerRequest)` - Applies all filters, sorting, and pagination. Returns a tuple with unpaginated and paginated queries.
- `ApplyGlobalSearch<T>(IQueryable<T>, DataTableServerRequest)` - Applies global search only.
- `ApplyColumnSearch<T>(IQueryable<T>, DataTableServerRequest)` - Applies column-specific search only.
- `ApplyOrdering<T>(IQueryable<T>, DataTableServerRequest)` - Applies sorting only.
- `ApplyPagination<T>(IQueryable<T>, DataTableServerRequest)` - Applies pagination only.

### Response Helpers

- `DataTableServerResponse.Success<T>(int draw, int recordsTotal, int recordsFiltered, IEnumerable<T> data, string continuationToken = null)` - Creates a success response.
- `DataTableServerResponse.Fail(int draw, string errorMessage)` - Creates an error response.

## Dependencies

- Newtonsoft.Json (13.0.1)

## License

MIT

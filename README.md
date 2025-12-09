# DataTableJs.ServerSide

A minimal .NET Standard 2.0 library for server-side processing with DataTables.js 1.10+.

Contains models for expected requests and responses,
and a LINQ extension method to apply filtering, sorting, and pagination to any IQueryable using reflection (EF friendly).

## Usage

```csharp
public IActionResult GetData([FromBody] DataTableServerRequest request)
{
    var query = _context.YourEntities.AsQueryable();
    
    var totalRecords = query.Count();
    var filteredData = query.ParseQuery(request);
    var filteredRecords = filteredData.Count();
    var data = filteredData.ToList();
    
    return Json(new DataTableServerResponse<YourEntity>
    {
        Draw = request.Draw,
        RecordsTotal = totalRecords,
        RecordsFiltered = filteredRecords,
        Data = data
    });
}
```

## Supports

- Global search
- Column-specific search filters
- Multi-column sorting
- Pagination
- Nested property support

## Dependencies
- Newtonsoft.Json

## License

MIT

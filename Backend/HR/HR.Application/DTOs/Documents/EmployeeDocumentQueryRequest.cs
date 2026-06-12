namespace HR.Application.DTOs.Documents;

public class EmployeeDocumentQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

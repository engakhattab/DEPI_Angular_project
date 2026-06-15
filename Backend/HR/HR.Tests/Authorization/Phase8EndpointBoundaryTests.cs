using HR.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.Tests.Authorization;

public class Phase8EndpointBoundaryTests
{
    [Fact]
    public void EmployeesController_HasExpectedRouteAndAuthorizeAttribute()
    {
        var attributes = typeof(EmployeesController).GetCustomAttributes(false);
        Assert.Contains(attributes, a => a is AuthorizeAttribute);
        Assert.Contains(attributes, a => a is ApiControllerAttribute);
        Assert.Contains(attributes, a => a is RouteAttribute ra && ra.Template == "api/[controller]");
    }

    [Fact]
    public void VacationRequestsController_HasExpectedRouteAndAuthorizeAttribute()
    {
        var attributes = typeof(VacationRequestsController).GetCustomAttributes(false);
        Assert.Contains(attributes, a => a is AuthorizeAttribute);
        Assert.Contains(attributes, a => a is ApiControllerAttribute);
        Assert.Contains(attributes, a => a is RouteAttribute ra && ra.Template == "api/[controller]");
    }

    [Fact]
    public void TripsController_HasExpectedRouteAndAuthorizeAttribute()
    {
        var attributes = typeof(TripsController).GetCustomAttributes(false);
        Assert.Contains(attributes, a => a is AuthorizeAttribute);
        Assert.Contains(attributes, a => a is ApiControllerAttribute);
        Assert.Contains(attributes, a => a is RouteAttribute ra && ra.Template == "api/[controller]");
    }
}

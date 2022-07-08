using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using XSched.API.Entities;

namespace XSched.API.EntityDataModels;

public static class XSchedEntityDataModel
{
    public static IEdmModel GetEntityDataModel()
    {
        var builder = new ODataConventionModelBuilder()
        {
            Namespace = "XSched",
            ContainerName = "XSchedContainer"
        };
        builder.EntitySet<UserProfile>("Profiles");
        builder.EntitySet<CalendarEvent>("CalendarEvents");

        return builder.GetEdmModel();
    }
}
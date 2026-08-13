using Enrollments.Infrastructure.Acl;
using Microsoft.Extensions.Configuration;
namespace Enrollments.Integration.Tests;

public sealed class CourseAuthoringOptionsTests
{
    [Fact]
    public void Options_SeEnlazanDesdeLaSeccionServicesCourseAuthoring()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["Services:CourseAuthoring:BaseUrl"] = "http://course-authoring.test",
            ["Services:CourseAuthoring:TimeoutSeconds"] = "7",
            ["Services:CourseAuthoring:RetryAfterSeconds"] = "11",
        });

        Assert.Equal("http://course-authoring.test", options.BaseUrl);
        Assert.Equal(7, options.TimeoutSeconds);
        Assert.Equal(11, options.RetryAfterSeconds);
    }

    [Fact]
    public void Options_SinDeclararlos_UsanTresYCincoSegundos()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["Services:CourseAuthoring:BaseUrl"] = "http://course-authoring.test",
        });

        Assert.Equal(3, options.TimeoutSeconds);
        Assert.Equal(5, options.RetryAfterSeconds);
    }

    private static CourseAuthoringOptions Bind(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var options = new CourseAuthoringOptions();

        configuration.GetSection(CourseAuthoringOptions.SectionName).Bind(options);

        return options;
    }
}

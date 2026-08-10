using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace CourseAuthoring.Infrastructure.Persistence;
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CourseAuthoringDbContext>
{
    private const string LocalDevelopmentConnection =
        "Host=localhost;Port=5432;Database=course_authoring;" +
        "Username=course_authoring_user;Password=course_authoring_dev";

    public CourseAuthoringDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("COURSE_AUTHORING_CONNECTION")
            ?? LocalDevelopmentConnection;

        var options = new DbContextOptionsBuilder<CourseAuthoringDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CourseAuthoringDbContext(options);
    }
}

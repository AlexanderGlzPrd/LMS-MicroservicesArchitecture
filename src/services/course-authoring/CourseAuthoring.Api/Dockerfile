FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/building-blocks/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj src/building-blocks/BuildingBlocks.Observability/
COPY src/services/course-authoring/CourseAuthoring.Api/CourseAuthoring.Api.csproj                       src/services/course-authoring/CourseAuthoring.Api/
COPY src/services/course-authoring/CourseAuthoring.Application/CourseAuthoring.Application.csproj       src/services/course-authoring/CourseAuthoring.Application/
COPY src/services/course-authoring/CourseAuthoring.Domain/CourseAuthoring.Domain.csproj                 src/services/course-authoring/CourseAuthoring.Domain/
COPY src/services/course-authoring/CourseAuthoring.Infrastructure/CourseAuthoring.Infrastructure.csproj src/services/course-authoring/CourseAuthoring.Infrastructure/

RUN dotnet restore src/services/course-authoring/CourseAuthoring.Api/CourseAuthoring.Api.csproj

FROM restore AS publish
COPY src/services/course-authoring/ src/services/course-authoring/
COPY src/building-blocks/BuildingBlocks.Observability/ src/building-blocks/BuildingBlocks.Observability/
RUN dotnet publish src/services/course-authoring/CourseAuthoring.Api/CourseAuthoring.Api.csproj \
    -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "CourseAuthoring.Api.dll"]

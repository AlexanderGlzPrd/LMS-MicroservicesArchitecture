FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/building-blocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj         src/building-blocks/BuildingBlocks.Messaging/
COPY src/services/learning/Learning.Api/Learning.Api.csproj                               src/services/learning/Learning.Api/
COPY src/services/learning/Learning.Application/Learning.Application.csproj               src/services/learning/Learning.Application/
COPY src/services/learning/Learning.Contracts/Learning.Contracts.csproj                   src/services/learning/Learning.Contracts/
COPY src/services/learning/Learning.Domain/Learning.Domain.csproj                         src/services/learning/Learning.Domain/
COPY src/services/learning/Learning.Infrastructure/Learning.Infrastructure.csproj         src/services/learning/Learning.Infrastructure/
COPY src/services/enrollment/Enrollments.Contracts/Enrollments.Contracts.csproj           src/services/enrollment/Enrollments.Contracts/

RUN dotnet restore src/services/learning/Learning.Api/Learning.Api.csproj

FROM restore AS publish
COPY src/building-blocks/BuildingBlocks.Messaging/       src/building-blocks/BuildingBlocks.Messaging/
COPY src/services/learning/                              src/services/learning/
COPY src/services/enrollment/Enrollments.Contracts/      src/services/enrollment/Enrollments.Contracts/
RUN dotnet publish src/services/learning/Learning.Api/Learning.Api.csproj \
    -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Learning.Api.dll"]

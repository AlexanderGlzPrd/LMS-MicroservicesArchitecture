FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/building-blocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj                 src/building-blocks/BuildingBlocks.Messaging/
COPY src/services/enrollment/Enrollments.Api/Enrollments.Api.csproj                               src/services/enrollment/Enrollments.Api/
COPY src/services/enrollment/Enrollments.Application/Enrollments.Application.csproj               src/services/enrollment/Enrollments.Application/
COPY src/services/enrollment/Enrollments.Contracts/Enrollments.Contracts.csproj                   src/services/enrollment/Enrollments.Contracts/
COPY src/services/enrollment/Enrollments.Domain/Enrollments.Domain.csproj                         src/services/enrollment/Enrollments.Domain/
COPY src/services/enrollment/Enrollments.Infrastructure/Enrollments.Infrastructure.csproj         src/services/enrollment/Enrollments.Infrastructure/
COPY src/services/paid-enrollment/PaidEnrollment.Contracts/PaidEnrollment.Contracts.csproj        src/services/paid-enrollment/PaidEnrollment.Contracts/

RUN dotnet restore src/services/enrollment/Enrollments.Api/Enrollments.Api.csproj

FROM restore AS publish
COPY src/building-blocks/BuildingBlocks.Messaging/           src/building-blocks/BuildingBlocks.Messaging/
COPY src/services/enrollment/                                src/services/enrollment/
COPY src/services/paid-enrollment/PaidEnrollment.Contracts/  src/services/paid-enrollment/PaidEnrollment.Contracts/
RUN dotnet publish src/services/enrollment/Enrollments.Api/Enrollments.Api.csproj \
    -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Enrollments.Api.dll"]

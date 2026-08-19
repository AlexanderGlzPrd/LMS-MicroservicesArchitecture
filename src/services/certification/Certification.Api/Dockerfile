
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/building-blocks/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj src/building-blocks/BuildingBlocks.Observability/
COPY src/building-blocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj                     src/building-blocks/BuildingBlocks.Messaging/
COPY src/services/certification/Certification.Api/Certification.Api.csproj                            src/services/certification/Certification.Api/
COPY src/services/certification/Certification.Application/Certification.Application.csproj            src/services/certification/Certification.Application/
COPY src/services/certification/Certification.Domain/Certification.Domain.csproj                      src/services/certification/Certification.Domain/
COPY src/services/certification/Certification.Infrastructure/Certification.Infrastructure.csproj      src/services/certification/Certification.Infrastructure/
COPY src/services/learning/Learning.Contracts/Learning.Contracts.csproj                               src/services/learning/Learning.Contracts/

RUN dotnet restore src/services/certification/Certification.Api/Certification.Api.csproj

FROM restore AS publish
COPY src/building-blocks/BuildingBlocks.Messaging/   src/building-blocks/BuildingBlocks.Messaging/
COPY src/services/certification/                     src/services/certification/
COPY src/services/learning/Learning.Contracts/       src/services/learning/Learning.Contracts/
COPY src/building-blocks/BuildingBlocks.Observability/ src/building-blocks/BuildingBlocks.Observability/
RUN dotnet publish src/services/certification/Certification.Api/Certification.Api.csproj \
    -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Certification.Api.dll"]

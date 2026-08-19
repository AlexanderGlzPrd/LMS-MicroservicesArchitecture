FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/bff/BffComposition.Api/BffComposition.Api.csproj src/bff/BffComposition.Api/

RUN dotnet restore src/bff/BffComposition.Api/BffComposition.Api.csproj

FROM restore AS publish
COPY src/bff/ src/bff/
RUN dotnet publish src/bff/BffComposition.Api/BffComposition.Api.csproj \
    -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "BffComposition.Api.dll"]

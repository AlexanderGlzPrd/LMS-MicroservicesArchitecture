FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/building-blocks/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj src/building-blocks/BuildingBlocks.Observability/
COPY src/building-blocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj                        src/building-blocks/BuildingBlocks.Messaging/
COPY src/services/payment-provider-sim/PaymentProviderSim.Worker/PaymentProviderSim.Worker.csproj        src/services/payment-provider-sim/PaymentProviderSim.Worker/
COPY src/services/payment-provider-sim/PaymentProviderSim.Contracts/PaymentProviderSim.Contracts.csproj  src/services/payment-provider-sim/PaymentProviderSim.Contracts/
COPY src/services/paid-enrollment/PaidEnrollment.Contracts/PaidEnrollment.Contracts.csproj               src/services/paid-enrollment/PaidEnrollment.Contracts/

RUN dotnet restore src/services/payment-provider-sim/PaymentProviderSim.Worker/PaymentProviderSim.Worker.csproj

FROM restore AS publish
COPY src/building-blocks/BuildingBlocks.Messaging/           src/building-blocks/BuildingBlocks.Messaging/
COPY src/services/payment-provider-sim/                      src/services/payment-provider-sim/
COPY src/services/paid-enrollment/PaidEnrollment.Contracts/  src/services/paid-enrollment/PaidEnrollment.Contracts/
COPY src/building-blocks/BuildingBlocks.Observability/ src/building-blocks/BuildingBlocks.Observability/
RUN dotnet publish src/services/payment-provider-sim/PaymentProviderSim.Worker/PaymentProviderSim.Worker.csproj \
    -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "PaymentProviderSim.Worker.dll"]

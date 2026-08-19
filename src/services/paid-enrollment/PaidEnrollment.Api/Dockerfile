FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/building-blocks/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj src/building-blocks/BuildingBlocks.Observability/
COPY src/building-blocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj                        src/building-blocks/BuildingBlocks.Messaging/
COPY src/services/paid-enrollment/PaidEnrollment.Api/PaidEnrollment.Api.csproj                           src/services/paid-enrollment/PaidEnrollment.Api/
COPY src/services/paid-enrollment/PaidEnrollment.Application/PaidEnrollment.Application.csproj           src/services/paid-enrollment/PaidEnrollment.Application/
COPY src/services/paid-enrollment/PaidEnrollment.Contracts/PaidEnrollment.Contracts.csproj               src/services/paid-enrollment/PaidEnrollment.Contracts/
COPY src/services/paid-enrollment/PaidEnrollment.Domain/PaidEnrollment.Domain.csproj                     src/services/paid-enrollment/PaidEnrollment.Domain/
COPY src/services/paid-enrollment/PaidEnrollment.Infrastructure/PaidEnrollment.Infrastructure.csproj     src/services/paid-enrollment/PaidEnrollment.Infrastructure/
COPY src/services/payment-provider-sim/PaymentProviderSim.Contracts/PaymentProviderSim.Contracts.csproj  src/services/payment-provider-sim/PaymentProviderSim.Contracts/
COPY src/services/enrollment/Enrollments.Contracts/Enrollments.Contracts.csproj                          src/services/enrollment/Enrollments.Contracts/

RUN dotnet restore src/services/paid-enrollment/PaidEnrollment.Api/PaidEnrollment.Api.csproj

FROM restore AS publish
COPY src/building-blocks/BuildingBlocks.Messaging/                    src/building-blocks/BuildingBlocks.Messaging/
COPY src/services/paid-enrollment/                                    src/services/paid-enrollment/
COPY src/services/payment-provider-sim/PaymentProviderSim.Contracts/  src/services/payment-provider-sim/PaymentProviderSim.Contracts/
COPY src/services/enrollment/Enrollments.Contracts/                   src/services/enrollment/Enrollments.Contracts/
COPY src/building-blocks/BuildingBlocks.Observability/ src/building-blocks/BuildingBlocks.Observability/
RUN dotnet publish src/services/paid-enrollment/PaidEnrollment.Api/PaidEnrollment.Api.csproj \
    -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "PaidEnrollment.Api.dll"]

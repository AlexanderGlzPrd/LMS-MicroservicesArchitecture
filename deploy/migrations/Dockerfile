FROM mcr.microsoft.com/dotnet/sdk:10.0 AS bundles
WORKDIR /src

ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 10.0.10

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ src/

RUN dotnet restore src/services/course-authoring/CourseAuthoring.Infrastructure \
 && dotnet restore src/services/enrollment/Enrollments.Infrastructure \
 && dotnet restore src/services/learning/Learning.Infrastructure \
 && dotnet restore src/services/certification/Certification.Infrastructure \
 && dotnet restore src/services/paid-enrollment/PaidEnrollment.Infrastructure \
 && dotnet restore src/services/payment-provider-sim/PaymentProviderSim.Worker

RUN dotnet ef migrations bundle --self-contained --force \
      --project src/services/course-authoring/CourseAuthoring.Infrastructure -o /bundles/course-authoring \
 && dotnet ef migrations bundle --self-contained --force \
      --project src/services/enrollment/Enrollments.Infrastructure -o /bundles/enrollment \
 && dotnet ef migrations bundle --self-contained --force \
      --project src/services/learning/Learning.Infrastructure -o /bundles/learning \
 && dotnet ef migrations bundle --self-contained --force \
      --project src/services/certification/Certification.Infrastructure -o /bundles/certification \
 && dotnet ef migrations bundle --self-contained --force \
      --project src/services/paid-enrollment/PaidEnrollment.Infrastructure -o /bundles/paid-enrollment \
 && dotnet ef migrations bundle --self-contained --force \
      --project src/services/payment-provider-sim/PaymentProviderSim.Worker -o /bundles/payment-provider-sim

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /bundles

RUN apt-get update \
 && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
 && rm -rf /var/lib/apt/lists/*

COPY --from=bundles /bundles/ /bundles/
COPY deploy/migrations/apply.sh /usr/local/bin/apply.sh
RUN chmod +x /bundles/* /usr/local/bin/apply.sh

USER app
ENTRYPOINT ["/usr/local/bin/apply.sh"]

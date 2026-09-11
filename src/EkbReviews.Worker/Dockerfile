FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props EkbReviews.sln ./
COPY src/EkbReviews.Domain/EkbReviews.Domain.csproj src/EkbReviews.Domain/
COPY src/EkbReviews.Application/EkbReviews.Application.csproj src/EkbReviews.Application/
COPY src/EkbReviews.Infrastructure/EkbReviews.Infrastructure.csproj src/EkbReviews.Infrastructure/
COPY src/EkbReviews.Worker/EkbReviews.Worker.csproj src/EkbReviews.Worker/
RUN dotnet restore src/EkbReviews.Worker/EkbReviews.Worker.csproj

COPY src/EkbReviews.Domain/ src/EkbReviews.Domain/
COPY src/EkbReviews.Application/ src/EkbReviews.Application/
COPY src/EkbReviews.Infrastructure/ src/EkbReviews.Infrastructure/
COPY src/EkbReviews.Worker/ src/EkbReviews.Worker/
RUN dotnet publish src/EkbReviews.Worker/EkbReviews.Worker.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EkbReviews.Worker.dll"]

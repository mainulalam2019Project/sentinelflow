FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY SentinelFlow.sln ./
COPY src/SentinelFlow.Domain/SentinelFlow.Domain.csproj src/SentinelFlow.Domain/
COPY src/SentinelFlow.Application/SentinelFlow.Application.csproj src/SentinelFlow.Application/
COPY src/SentinelFlow.Infrastructure/SentinelFlow.Infrastructure.csproj src/SentinelFlow.Infrastructure/
COPY src/SentinelFlow.Api/SentinelFlow.Api.csproj src/SentinelFlow.Api/
RUN dotnet restore src/SentinelFlow.Api/SentinelFlow.Api.csproj
COPY . .
RUN dotnet publish src/SentinelFlow.Api/SentinelFlow.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SentinelFlow.Api.dll"]

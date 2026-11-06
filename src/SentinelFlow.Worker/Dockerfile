FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY SentinelFlow.sln ./
COPY src/SentinelFlow.Domain/SentinelFlow.Domain.csproj src/SentinelFlow.Domain/
COPY src/SentinelFlow.Application/SentinelFlow.Application.csproj src/SentinelFlow.Application/
COPY src/SentinelFlow.Infrastructure/SentinelFlow.Infrastructure.csproj src/SentinelFlow.Infrastructure/
COPY src/SentinelFlow.Worker/SentinelFlow.Worker.csproj src/SentinelFlow.Worker/
RUN dotnet restore src/SentinelFlow.Worker/SentinelFlow.Worker.csproj
COPY . .
RUN dotnet publish src/SentinelFlow.Worker/SentinelFlow.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SentinelFlow.Worker.dll"]


# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# Use the SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/RateLimiter.Api/RateLimiter.Api.csproj", "src/RateLimiter.Api/"]
RUN dotnet restore "src/RateLimiter.Api/RateLimiter.Api.csproj"
COPY . .
WORKDIR "/src/src/RateLimiter.Api"
RUN dotnet build "RateLimiter.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RateLimiter.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RateLimiter.Api.dll"]

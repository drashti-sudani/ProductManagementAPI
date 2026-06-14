# Multi-stage build for .NET 8 API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["src/API/API.csproj", "src/API/"]
RUN dotnet restore "src/API/API.csproj"

# Copy everything else and publish
COPY . .
WORKDIR /src/src/API
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]

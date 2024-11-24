# Use the ASP.NET Core runtime image for .NET 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Build stage: Use the .NET SDK for .NET 8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["fabric-deployment-hub.csproj", "./"]
RUN dotnet restore "./fabric-deployment-hub.csproj"

# Copy the entire source code and build the application
COPY . .
RUN dotnet publish "./fabric-deployment-hub.csproj" -c Release -o /app/publish

# Final stage: Use the runtime image and copy the built application
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "fabric-deployment-hub.dll"]
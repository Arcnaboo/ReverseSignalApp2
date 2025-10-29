# ================================
# Stage 1: Build
# ================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /build

# Copy the project file and restore dependencies
COPY ReverseSignalApp/ReverseSignalApp.csproj ReverseSignalApp/
RUN dotnet restore ReverseSignalApp/ReverseSignalApp.csproj

# Copy the remaining source code
COPY ReverseSignalApp/ ReverseSignalApp/

# Build the application
WORKDIR /build/ReverseSignalApp
RUN dotnet build -c Release -o /app/build


# ================================
# Stage 2: Publish
# ================================
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish


# ================================
# Stage 3: Runtime
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published output from previous stage
COPY --from=publish /app/publish .

# Expose the Render/Railway default port
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "ReverseSignalApp.dll"]

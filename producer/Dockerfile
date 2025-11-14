# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY SqsDemo.csproj .
RUN dotnet restore "SqsDemo.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "SqsDemo.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "SqsDemo.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy the published application from the publish stage
COPY --from=publish /app/publish .

# Change ownership to the non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose the port the app runs on
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "SqsDemo.dll"]

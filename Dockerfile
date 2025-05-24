# Stage 1: Build the application
# Use the .NET SDK image for building your application.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and project files first.
# This optimizes Docker layer caching if only code within projects changes.
COPY CampusBites.sln .
COPY CampusBites.Application/*.csproj ./CampusBites.Application/
COPY CampusBites.Infrastructure/*.csproj ./CampusBites.Infrastructure/
COPY CampusBites.Web/*.csproj ./CampusBites.Web/
# If you have other project types (e.g., Class Libraries in other folders), add them similarly.

# Restore NuGet packages for the entire solution.
RUN dotnet restore CampusBites.sln

# Copy the rest of the application code.
COPY . .

# Change working directory to your main web project.
# Replace 'CampusBites.Web' with the actual name of your web project directory if different.
WORKDIR /src/CampusBites.Web

# Publish the application for release.
# The output will be placed in the '/app/out' directory.
RUN dotnet publish -c Release -o /app/out --no-restore

# Stage 2: Create the runtime image
# Use the ASP.NET runtime image for the final deployed application.
# This image is smaller and only contains what's needed to run the app.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Expose the port your application listens on.
# Cloud Run typically expects applications to listen on port 8080.
# Your Program.cs already handles reading the PORT environment variable.
EXPOSE 8080

# Copy the published output from the build stage into the runtime image.
COPY --from=build /app/out .

# Set the entry point for the application.
# This specifies the command to execute when the container starts.
# Replace 'CampusBites.Web.dll' with the actual name of your web project's DLL.
ENTRYPOINT ["dotnet", "CampusBites.Web.dll"]
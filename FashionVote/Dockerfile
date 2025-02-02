# Use the official .NET SDK as the base image for building and running
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Set the working directory in the container
WORKDIR /app

# Copy everything from the current directory into the container
COPY . .

# Build the application
RUN dotnet build -c Release

# Expose the port that your application listens on
EXPOSE 8099

# Run the application
CMD ["dotnet", "run", "--urls", "http://0.0.0.0:8099"]

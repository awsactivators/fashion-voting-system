# Use Alpine for a smaller image
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /src
COPY FashionVote/*.csproj ./FashionVote/

# Install dependencies for Alpine (if needed)
RUN apk add --no-cache icu-libs bash

# Clear NuGet cache before restore
RUN dotnet nuget locals all --clear

# Restore dependencies
RUN dotnet restore --disable-parallel --verbosity minimal FashionVote/FashionVote.csproj

# Copy remaining files
COPY FashionVote/. ./FashionVote/
WORKDIR /src/FashionVote

# Build and publish
RUN dotnet build -c Release 
RUN dotnet publish -c Release -o /app --no-build

# Use Alpine runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .

# Copy the database to a writable location inside the container
RUN cp /app/app.db /tmp/app.db

# Set the connection string for the database in the writable location
ENV ConnectionStrings__DefaultConnection "DataSource=/tmp/app.db;Cache=Shared"


# Expose port
EXPOSE 8099

CMD ["dotnet", "FashionVote.dll"]

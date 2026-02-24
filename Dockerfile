FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the API project first (for layer caching)
COPY src/CommerceHub.Api/CommerceHub.Api.csproj src/CommerceHub.Api/
RUN dotnet restore src/CommerceHub.Api/CommerceHub.Api.csproj

# Copy the rest of the source
COPY src/ src/

# Publish
RUN dotnet publish src/CommerceHub.Api/CommerceHub.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "CommerceHub.Api.dll"]
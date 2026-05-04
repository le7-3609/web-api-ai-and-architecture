FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY WebApiShop.sln .
COPY WebApiShop/WebApiShop.csproj WebApiShop/
COPY Services/Services.csproj Services/
COPY Repositories/Repositories.csproj Repositories/
COPY Entities/Entities.csproj Entities/
COPY DTO/DTO.csproj DTO/

RUN dotnet restore WebApiShop/WebApiShop.csproj

COPY . .
RUN dotnet publish WebApiShop/WebApiShop.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApiShop.dll"]

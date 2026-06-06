FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY global.json ./

COPY SC.Api/*.csproj SC.Api/
COPY SC.Application/*.csproj SC.Application/
COPY SC.Domain/*.csproj SC.Domain/
COPY SC.Persistence/*.csproj SC.Persistence/
COPY SC.Infrastructure/*.csproj SC.Infrastructure/
COPY SC.Contract/*.csproj SC.Contract/

RUN dotnet restore SC.Api/SC.Api.csproj

COPY . .

RUN dotnet publish \
    SC.Api/SC.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/Logs && chmod 777 /app/Logs

EXPOSE 8080

ENTRYPOINT ["dotnet", "SC.Api.dll"]
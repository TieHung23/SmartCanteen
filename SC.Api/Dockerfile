FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY global.json ./
COPY SC.Api/SC.Api.csproj SC.Api/
RUN dotnet restore SC.Api/SC.Api.csproj

COPY . .
RUN dotnet publish SC.Api/SC.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "SC.Api.dll"]


# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ZeroX2C.Blog.slnx ./
COPY src/ZeroX2C.Blog.API/ZeroX2C.Blog.API.csproj src/ZeroX2C.Blog.API/
RUN dotnet restore ZeroX2C.Blog.slnx

FROM restore AS build
COPY . .
RUN dotnet publish src/ZeroX2C.Blog.API/ZeroX2C.Blog.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
USER app

ENTRYPOINT ["dotnet", "ZeroX2C.Blog.API.dll"]

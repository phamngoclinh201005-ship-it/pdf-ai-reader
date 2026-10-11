
# Stage 1: Build ASP.NET Core 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY PaperFlow.Api.csproj ./
RUN dotnet restore PaperFlow.Api.csproj

COPY . ./
RUN dotnet publish PaperFlow.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Run API
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "PaperFlow.Api.dll"]

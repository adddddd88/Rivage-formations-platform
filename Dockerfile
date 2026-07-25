# Production multi-stage image for Railway / PaaS
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Rivage.sln ./
COPY src/Rivage.Domain/Rivage.Domain.csproj src/Rivage.Domain/
COPY src/Rivage.Infrastructure/Rivage.Infrastructure.csproj src/Rivage.Infrastructure/
COPY src/Rivage.Web/Rivage.Web.csproj src/Rivage.Web/
RUN dotnet restore src/Rivage.Web/Rivage.Web.csproj
COPY src/ ./src/
RUN dotnet publish src/Rivage.Web/Rivage.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN groupadd -r rivage && useradd -r -g rivage rivage
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER rivage
ENTRYPOINT ["dotnet", "Rivage.Web.dll"]

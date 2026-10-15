# Build a partir da RAIZ do repositório:  docker build -f src/Zyft.Api/Dockerfile -t zyft-api .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props Directory.Packages.props Zyft.sln ./
COPY src/Zyft.Domain/Zyft.Domain.csproj src/Zyft.Domain/
COPY src/Zyft.Application/Zyft.Application.csproj src/Zyft.Application/
COPY src/Zyft.Infrastructure/Zyft.Infrastructure.csproj src/Zyft.Infrastructure/
COPY src/Zyft.Api/Zyft.Api.csproj src/Zyft.Api/
RUN dotnet restore src/Zyft.Api/Zyft.Api.csproj
COPY src/ src/
RUN dotnet publish src/Zyft.Api/Zyft.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

# Imagem Debian (inclui ICU, necessário para a cultura pt-BR na formatação de valores).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Frontend web sem build: servido como wwwroot em "/".
COPY web/ ./wwwroot/
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Zyft.Api.dll"]

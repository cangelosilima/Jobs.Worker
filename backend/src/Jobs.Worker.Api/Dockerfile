FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Jobs.Worker.Api/Jobs.Worker.Api.csproj", "src/Jobs.Worker.Api/"]
COPY ["src/Jobs.Worker.Application/Jobs.Worker.Application.csproj", "src/Jobs.Worker.Application/"]
COPY ["src/Jobs.Worker.Domain/Jobs.Worker.Domain.csproj", "src/Jobs.Worker.Domain/"]
COPY ["src/Jobs.Worker.Infrastructure/Jobs.Worker.Infrastructure.csproj", "src/Jobs.Worker.Infrastructure/"]
RUN dotnet restore "src/Jobs.Worker.Api/Jobs.Worker.Api.csproj"
COPY . .
WORKDIR "/src/src/Jobs.Worker.Api"
RUN dotnet build "Jobs.Worker.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Jobs.Worker.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Jobs.Worker.Api.dll"]

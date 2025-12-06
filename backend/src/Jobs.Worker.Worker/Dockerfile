FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Jobs.Worker.Worker/Jobs.Worker.Worker.csproj", "src/Jobs.Worker.Worker/"]
COPY ["src/Jobs.Worker.Application/Jobs.Worker.Application.csproj", "src/Jobs.Worker.Application/"]
COPY ["src/Jobs.Worker.Domain/Jobs.Worker.Domain.csproj", "src/Jobs.Worker.Domain/"]
COPY ["src/Jobs.Worker.Infrastructure/Jobs.Worker.Infrastructure.csproj", "src/Jobs.Worker.Infrastructure/"]
RUN dotnet restore "src/Jobs.Worker.Worker/Jobs.Worker.Worker.csproj"
COPY . .
WORKDIR "/src/src/Jobs.Worker.Worker"
RUN dotnet build "Jobs.Worker.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Jobs.Worker.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Jobs.Worker.Worker.dll"]

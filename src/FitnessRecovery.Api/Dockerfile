FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for caching restore
COPY ["src/FitnessRecovery.Api/FitnessRecovery.Api.csproj", "src/FitnessRecovery.Api/"]
COPY ["src/FitnessRecovery.Features/FitnessRecovery.Features.csproj", "src/FitnessRecovery.Features/"]
COPY ["src/FitnessRecovery.Infrastructure/FitnessRecovery.Infrastructure.csproj", "src/FitnessRecovery.Infrastructure/"]
COPY ["src/FitnessRecovery.SharedKernel/FitnessRecovery.SharedKernel.csproj", "src/FitnessRecovery.SharedKernel/"]

RUN dotnet restore "src/FitnessRecovery.Api/FitnessRecovery.Api.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/src/FitnessRecovery.Api"
RUN dotnet build "FitnessRecovery.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FitnessRecovery.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FitnessRecovery.Api.dll"]

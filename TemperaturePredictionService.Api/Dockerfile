FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["TemperaturePredictionService.Api/TemperaturePredictionService.Api.csproj", "TemperaturePredictionService.Api/"]
COPY ["TemperaturePredictionService.Application/TemperaturePredictionService.Application.csproj", "TemperaturePredictionService.Application/"]
COPY ["TemperaturePredictionService.Infrastructure/TemperaturePredictionService.Infrastructure.csproj", "TemperaturePredictionService.Infrastructure/"]
COPY ["TemperaturePredictionService.Core/TemperaturePredictionService.Core.csproj", "TemperaturePredictionService.Core/"]
COPY ["TemperaturePredictionService.Tests/TemperaturePredictionService.Tests.csproj", "TemperaturePredictionService.Tests/"]
COPY ["TemperaturePredictionService.sln", "./"]
RUN dotnet restore "TemperaturePredictionService.sln"
COPY . .
RUN dotnet publish "TemperaturePredictionService.Api/TemperaturePredictionService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/TemperaturePredictionService.Api/data ./data
RUN mkdir -p ./experiments
VOLUME ["./experiments"]
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "TemperaturePredictionService.Api.dll"]

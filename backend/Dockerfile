FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SentimentAnalyzer.csproj", "./"]
RUN dotnet restore "./SentimentAnalyzer.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "SentimentAnalyzer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SentimentAnalyzer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SentimentAnalyzer.dll"]
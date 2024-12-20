FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AzureChat/AzureChat.csproj", "AzureChat/"]
RUN dotnet restore "AzureChat/AzureChat.csproj"
COPY . .
WORKDIR "/src/AzureChat"
RUN dotnet build "AzureChat.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureChat.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzureChat.dll"]

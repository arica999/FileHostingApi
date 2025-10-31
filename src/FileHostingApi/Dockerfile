FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["FileHostingApi/FileHostingApi.csproj", "FileHostingApi/"]
RUN dotnet restore "FileHostingApi/FileHostingApi.csproj"
COPY . .
WORKDIR "/src/FileHostingApi"
RUN dotnet build "FileHostingApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileHostingApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileHostingApi.dll"]

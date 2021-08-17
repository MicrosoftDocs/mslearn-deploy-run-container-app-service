FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["SampleWeb/SampleWeb.csproj", "SampleWeb/"]
RUN dotnet restore "SampleWeb/SampleWeb.csproj"
COPY . .
WORKDIR "/src/SampleWeb"
RUN dotnet build "SampleWeb.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SampleWeb.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SampleWeb.dll"]
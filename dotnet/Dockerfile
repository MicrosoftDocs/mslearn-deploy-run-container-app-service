FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
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
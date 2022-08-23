FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["PodPing.NostrWriter/PodPing.NostrWriter.csproj", "PodPing.NostrWriter/"]
RUN dotnet restore "PodPing.NostrWriter/PodPing.NostrWriter.csproj"
COPY . .
WORKDIR "/src/PodPing.NostrWriter"
RUN dotnet build "PodPing.NostrWriter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PodPing.NostrWriter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PodPing.NostrWriter.dll"]

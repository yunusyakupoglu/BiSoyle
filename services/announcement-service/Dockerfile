FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5008

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BiSoyle.Announcement.Service.csproj", "./"]
RUN dotnet restore "BiSoyle.Announcement.Service.csproj"
COPY . .
RUN dotnet build "BiSoyle.Announcement.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiSoyle.Announcement.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5008
ENTRYPOINT ["dotnet", "BiSoyle.Announcement.Service.dll"]









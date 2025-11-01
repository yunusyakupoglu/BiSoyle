FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5004

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BiSoyle.User.Service.csproj", "./"]
RUN dotnet restore "BiSoyle.User.Service.csproj"
COPY . .
RUN dotnet build "BiSoyle.User.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiSoyle.User.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5004
ENTRYPOINT ["dotnet", "BiSoyle.User.Service.dll"]







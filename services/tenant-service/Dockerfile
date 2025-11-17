FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5005

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BiSoyle.Tenant.Service.csproj", "./"]
RUN dotnet restore "BiSoyle.Tenant.Service.csproj"
COPY . .
RUN dotnet build "BiSoyle.Tenant.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiSoyle.Tenant.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5005
ENTRYPOINT ["dotnet", "BiSoyle.Tenant.Service.dll"]





























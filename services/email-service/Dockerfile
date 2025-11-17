FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["email-service.csproj", "./"]
RUN dotnet restore "email-service.csproj"
COPY . .
RUN dotnet build "email-service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "email-service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "email-service.dll"]











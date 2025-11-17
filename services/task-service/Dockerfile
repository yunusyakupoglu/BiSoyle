FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5007

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BiSoyle.Task.Service.csproj", "./"]
RUN dotnet restore "BiSoyle.Task.Service.csproj"
COPY . .
RUN dotnet build "BiSoyle.Task.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiSoyle.Task.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5007
ENTRYPOINT ["dotnet", "BiSoyle.Task.Service.dll"]









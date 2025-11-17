FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BiSoyle.Receipt.Service.csproj", "./"]
RUN dotnet restore "BiSoyle.Receipt.Service.csproj"
COPY . .
RUN dotnet build "BiSoyle.Receipt.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiSoyle.Receipt.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5001
ENTRYPOINT ["dotnet", "BiSoyle.Receipt.Service.dll"]






























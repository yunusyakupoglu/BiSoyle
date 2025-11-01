FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BiSoyle.Transaction.Service.csproj", "./"]
RUN dotnet restore "BiSoyle.Transaction.Service.csproj"
COPY . .
RUN dotnet build "BiSoyle.Transaction.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiSoyle.Transaction.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5003
ENTRYPOINT ["dotnet", "BiSoyle.Transaction.Service.dll"]







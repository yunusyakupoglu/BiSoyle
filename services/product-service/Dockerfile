FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BiSoyle.Product.Service.csproj", "./"]
RUN dotnet restore "BiSoyle.Product.Service.csproj"
COPY . .
RUN dotnet build "BiSoyle.Product.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiSoyle.Product.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5002
ENTRYPOINT ["dotnet", "BiSoyle.Product.Service.dll"]







FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5009

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BiSoyle.Ticket.Service.csproj", "./"]
RUN dotnet restore "BiSoyle.Ticket.Service.csproj"
COPY . .
RUN dotnet build "BiSoyle.Ticket.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiSoyle.Ticket.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5009
ENTRYPOINT ["dotnet", "BiSoyle.Ticket.Service.dll"]









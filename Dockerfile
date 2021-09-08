#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal AS base
RUN apt-get update && \
    apt-get upgrade -y && \
    apt-get install -y git
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /src
COPY ["HooksHandler.csproj", "."]
RUN dotnet restore "./HooksHandler.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "HooksHandler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HooksHandler.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HooksHandler.dll"]
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/MiniShop.Api/MiniShop.Api.csproj src/MiniShop.Api/
RUN dotnet restore src/MiniShop.Api/MiniShop.Api.csproj
COPY src/MiniShop.Api/ src/MiniShop.Api/
RUN dotnet publish src/MiniShop.Api/MiniShop.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN mkdir -p /data && chown -R $APP_UID:$APP_UID /app /data
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "MiniShop.Api.dll"]

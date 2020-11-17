FROM zeekozhu/aspnetcore-build-yarn:5.0-alpine as builder

WORKDIR /builder
COPY *.fsproj ./
RUN dotnet restore -r linux-musl-x64

COPY ./ ./
RUN dotnet publish -c release -o /app -r linux-musl-x64 --self-contained false --no-restore

FROM mcr.microsoft.com/dotnet/core/aspnet:5.0
ENV ASPNETCORE_URLS=http://*:5000

WORKDIR /app

COPY --from=builder /app ./

ENTRYPOINT ["dotnet", "./CnWhiteList.App.dll"]


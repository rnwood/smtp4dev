FROM diamol/node:10.15.2 as nodejs

FROM mcr.microsoft.com/dotnet/core/sdk:3.1.102 AS build
COPY --from=nodejs /nodejs /nodejs
ENV PATH="/nodejs"
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY Rnwood.Smtp4dev/*.csproj ./Rnwood.Smtp4dev/
RUN dotnet restore Rnwood.Smtp4dev

ARG version
ENV VERSION $version

# copy everything else and build app
COPY . .
WORKDIR /app/Rnwood.Smtp4dev
RUN dotnet build -p:Version=$VERSION
RUN dotnet test -p:Version=$VERSION

FROM build AS publish
WORKDIR /app/Rnwood.Smtp4dev
RUN dotnet publish -c Release -o out -p:Version=$VERSION

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.2 AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 25
COPY --from=publish /app/Rnwood.Smtp4dev/out ./
ENTRYPOINT ["dotnet", "Rnwood.Smtp4dev.dll"]
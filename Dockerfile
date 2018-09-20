FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# replace shell with bash so we can source files
RUN rm /bin/sh && ln -s /bin/bash /bin/sh

# nvm environment variables
ENV NVM_DIR /usr/local/nvm
ENV NODE_VERSION 8.11.3

# install nvm
# https://github.com/creationix/nvm#install-script
RUN curl --silent -o- https://raw.githubusercontent.com/creationix/nvm/v0.31.2/install.sh | bash

# install node and npm
RUN source $NVM_DIR/nvm.sh \
    && nvm install $NODE_VERSION \
    && nvm alias default $NODE_VERSION \
    && nvm use default

# add node and npm to path so the commands are available
ENV NODE_PATH $NVM_DIR/v$NODE_VERSION/lib/node_modules
ENV PATH $NVM_DIR/versions/node/v$NODE_VERSION/bin:$PATH

# confirm installation
RUN node -v
RUN npm -v

# copy csproj and restore as distinct layers
COPY *.sln .
COPY Rnwood.Smtp4dev/*.csproj ./Rnwood.Smtp4dev/
RUN dotnet restore Rnwood.Smtp4dev

# copy everything else and build app
COPY . .
WORKDIR /app/Rnwood.Smtp4dev
RUN npm install
RUN dotnet build

FROM build AS publish
WORKDIR /app/Rnwood.Smtp4dev
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.1-aspnetcore-runtime AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 25
COPY --from=publish /app/Rnwood.Smtp4dev/out ./
ENTRYPOINT ["dotnet", "Rnwood.Smtp4dev.dll"]
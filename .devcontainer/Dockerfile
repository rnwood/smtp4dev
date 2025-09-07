FROM mcr.microsoft.com/devcontainers/dotnet

COPY .devcontainer/installcustom.sh /
COPY Rnwood.Smtp4dev.Tests/Rnwood.Smtp4dev.Tests.csproj /Rnwood.Smtp4dev.Tests/Rnwood.Smtp4dev.Tests.csproj
RUN /bin/sh /installcustom.sh
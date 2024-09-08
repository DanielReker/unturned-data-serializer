FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

COPY ["modules/", "/app/modules/"]

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["UnturnedDataSerializer.csproj", "."]
RUN dotnet restore "./UnturnedDataSerializer.csproj"
COPY ./src ./src
RUN dotnet build "./UnturnedDataSerializer.csproj" -c $BUILD_CONFIGURATION -o /app/build
WORKDIR /app/build
RUN cp 0Harmony.dll /app/modules/UnturnedDataSerializer/ && \
    cp UnturnedDataSerializer.dll /app/modules/UnturnedDataSerializer/


FROM cm2network/steamcmd:root AS final

RUN apt-get update && \
    apt-get install -y ca-certificates python3

COPY --from=build ["/app/modules/", "/app/modules/"]
COPY entry.py /app/entry.py
COPY [ "default_configs/", "/app/default_configs/" ]

USER steam
ENTRYPOINT [ "python3", "/app/entry.py" ]
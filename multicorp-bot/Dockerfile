# Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV BOTTOKEN=$BOTTOKEN
ENV POSTGRESCONNECTIONSTRING=$POSTGRESCONNECTIONSTRING

# Run the app on container startup
# Use your project name for the second parameter
# e.g. MyProject.dll
COPY /Skynet.json ./Skynet.json
ENTRYPOINT [ "dotnet", "multicorp-bot.dll" ]

CMD ASPNETCORE_URLS=http://*:$PORT dotnet multicorp-bot.dll
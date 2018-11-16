FROM microsoft/dotnet:2.1-sdk AS build
ARG version=0.0.1-developer
ENV TRAVIS_BUILD_NUMBER $version
ENV CI true
ENV POSTGRES_CONNECTIONSTRING "Host=localhost;Database=ironclad;Username=postgres;Password=postgres;"
ENV CONNECTIONSTRINGS__IRONCLAD "Host=localhost;Database=ironclad;Username=postgres;Password=postgres;"
ENV GOOGLE_CLIENT_ID "ClientIdGoesHere"
ENV GOOGLE_SECRET "SecretGoesHere"
WORKDIR /src
COPY . ./
RUN ./build.sh

FROM microsoft/dotnet:2.1-aspnetcore-runtime AS final
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_ENVIRONMENT Docker
COPY --from=build /build/ .
ENTRYPOINT ["dotnet", "Ironclad.dll"]
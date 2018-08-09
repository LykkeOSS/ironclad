FROM microsoft/dotnet:2.1-sdk AS build
ARG version=0.0.1-developer
WORKDIR /src

COPY ./Ironclad.Client ./Ironclad.Client 
COPY ./tests ./tests
COPY ./Ironclad.ruleset ./Ironclad.ruleset 
COPY ./NuGet.config  ./NuGet.config 
COPY ./stylecop.json ./stylecop.json

WORKDIR /src/tests/Ironclad.Tests
RUN dotnet publish -c Release -r linux-x64 -o ../../build /p:ShowLinkerSizeComparison=true /p:Version=$version

FROM microsoft/dotnet:2.1-sdk AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT Docker
COPY --from=build /src/build/ .
ENTRYPOINT ["dotnet", "vstest", "Ironclad.Tests.dll"]
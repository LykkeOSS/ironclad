#!/usr/bin/env bash

dotnet run --project ./src/build/build.csproj -- --parallel "$@"
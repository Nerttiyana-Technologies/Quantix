#!/usr/bin/env bash
#
# roslyn-floor-check.sh — enforces the generator's compiled API floor.
#
# The Quantix generator is compiled against Microsoft.CodeAnalysis.CSharp 4.8.0 so
# that it loads in every consumer SDK and IDE that can build a net8.0 app. This
# script proves that empirically: it builds a fresh net8.0 consumer of the packed
# Quantix package using the .NET 8 SDK (which hosts Roslyn 4.8). If the generator
# uses an API newer than 4.8.0 it fails to load here and the build breaks.
#
# The consumer is created in a temp directory OUTSIDE the repository so the repo's
# global.json (which pins the .NET 10 SDK) does not apply to it.

set -euo pipefail

FEED="$(pwd)/artifacts/floor-feed"
if [ ! -d "$FEED" ]; then
  echo "::error::Local package feed not found at $FEED — run 'dotnet pack' first."
  exit 1
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
cd "$WORK"

# Pin this throwaway consumer to the .NET 8 SDK feature band -> Roslyn 4.8.
cat > global.json <<'JSON'
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestPatch"
  }
}
JSON

# Restore the Quantix package from the local feed; nuget.org supplies its
# transitive Microsoft.Extensions.DependencyInjection.Abstractions dependency.
cat > nuget.config <<JSON
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="floor-feed" value="$FEED" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
JSON

# A class library is enough: the generator runs on it, and the generated
# QuantixMediator / AddQuantix must compile. Version="*-*" floats to the
# CI-built pre-release package in the local feed.
cat > Consumer.csproj <<'XML'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Quantix" Version="*-*" />
  </ItemGroup>
</Project>
XML

# One message and one handler — enough for the generator to discover work and
# emit the dispatcher and the registration extension.
cat > FloorCheck.cs <<'CS'
using Quantix;

namespace FloorCheck;

public sealed record Ping(string Text) : IQuery<string>;

public sealed class PingHandler : IQueryHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping query, CancellationToken ct)
        => new(query.Text);
}
CS

echo "Consumer SDK in use: $(dotnet --version)"
dotnet build Consumer.csproj --configuration Release
echo "OK: the Quantix generator loaded and emitted code under the .NET 8 SDK (Roslyn 4.8)."

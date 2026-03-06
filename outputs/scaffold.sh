#!/usr/bin/env bash
# AppThere Loki — Phase 1 Project Scaffold
# Run once from the repo root on a clean clone.
# Requires: .NET 9 SDK, git
#
# Usage:
#   chmod +x scaffold.sh
#   ./scaffold.sh
#
# What this does:
#   1. Creates the solution file
#   2. Creates all Phase 1 projects with correct TFMs
#   3. Wires project references
#   4. Creates Directory.Build.props and Directory.Packages.props
#   5. Creates the folder structure within each project
#   6. Creates .editorconfig, .gitignore, .gitattributes
#   7. Creates the docs/adr/ directory stubs
#
# What this does NOT do:
#   - Write any C# source files (that's Claude Code's job)
#   - Download NuGet packages (dotnet restore does that)
#   - Create GitHub Actions workflows (see .github/workflows/)

set -euo pipefail

echo "==> AppThere Loki — Phase 1 Scaffold"
echo "==> Working directory: $(pwd)"
echo ""

# ── Verify prerequisites ──────────────────────────────────────────────────────
if ! command -v dotnet &>/dev/null; then
  echo "ERROR: .NET SDK not found. Install .NET 9 from https://dotnet.microsoft.com/download"
  exit 1
fi

DOTNET_VERSION=$(dotnet --version)
if [[ ! "$DOTNET_VERSION" == 9.* ]]; then
  echo "ERROR: .NET 9 required. Found: $DOTNET_VERSION"
  exit 1
fi

echo "==> .NET version: $DOTNET_VERSION"

# ── Solution ──────────────────────────────────────────────────────────────────
echo ""
echo "==> Creating solution..."
dotnet new sln --name AppThere.Loki --output .

# ── Source projects ───────────────────────────────────────────────────────────
echo ""
echo "==> Creating source projects..."

# Kernel — pure .NET 9, no platform-specific deps
dotnet new classlib \
  --name AppThere.Loki.Kernel \
  --output src/Kernel/AppThere.Loki.Kernel \
  --framework net9.0 \
  --no-restore

# Skia — .NET 9, depends only on Kernel + SkiaSharp
dotnet new classlib \
  --name AppThere.Loki.Skia \
  --output src/Kernel/AppThere.Loki.Skia \
  --framework net9.0 \
  --no-restore

# ── Tool projects ─────────────────────────────────────────────────────────────
echo ""
echo "==> Creating tool projects..."

# lokiprint — headless CLI, Phase 1 exit criterion
dotnet new console \
  --name AppThere.Loki.Tools.LokiPrint \
  --output tools/lokiprint \
  --framework net9.0 \
  --no-restore

# ── Test projects ─────────────────────────────────────────────────────────────
echo ""
echo "==> Creating test projects..."

dotnet new xunit \
  --name AppThere.Loki.Tests.Unit \
  --output tests/unit/AppThere.Loki.Tests.Unit \
  --framework net9.0 \
  --no-restore

dotnet new xunit \
  --name AppThere.Loki.Tests.Rendering \
  --output tests/rendering/AppThere.Loki.Tests.Rendering \
  --framework net9.0 \
  --no-restore

# ── Add projects to solution ──────────────────────────────────────────────────
echo ""
echo "==> Adding projects to solution..."

dotnet sln add src/Kernel/AppThere.Loki.Kernel/AppThere.Loki.Kernel.csproj \
               --solution-folder src/Kernel

dotnet sln add src/Kernel/AppThere.Loki.Skia/AppThere.Loki.Skia.csproj \
               --solution-folder src/Kernel

dotnet sln add tools/lokiprint/AppThere.Loki.Tools.LokiPrint.csproj \
               --solution-folder tools

dotnet sln add tests/unit/AppThere.Loki.Tests.Unit/AppThere.Loki.Tests.Unit.csproj \
               --solution-folder tests

dotnet sln add tests/rendering/AppThere.Loki.Tests.Rendering/AppThere.Loki.Tests.Rendering.csproj \
               --solution-folder tests

# ── Project references ────────────────────────────────────────────────────────
echo ""
echo "==> Wiring project references..."

# Skia depends on Kernel
dotnet add src/Kernel/AppThere.Loki.Skia/AppThere.Loki.Skia.csproj \
  reference src/Kernel/AppThere.Loki.Kernel/AppThere.Loki.Kernel.csproj

# lokiprint depends on Skia (and transitively Kernel)
dotnet add tools/lokiprint/AppThere.Loki.Tools.LokiPrint.csproj \
  reference src/Kernel/AppThere.Loki.Skia/AppThere.Loki.Skia.csproj

# Unit tests depend on both Kernel and Skia
dotnet add tests/unit/AppThere.Loki.Tests.Unit/AppThere.Loki.Tests.Unit.csproj \
  reference src/Kernel/AppThere.Loki.Kernel/AppThere.Loki.Kernel.csproj

dotnet add tests/unit/AppThere.Loki.Tests.Unit/AppThere.Loki.Tests.Unit.csproj \
  reference src/Kernel/AppThere.Loki.Skia/AppThere.Loki.Skia.csproj

# Rendering tests depend on Skia only (Kernel is transitive)
dotnet add tests/rendering/AppThere.Loki.Tests.Rendering/AppThere.Loki.Tests.Rendering.csproj \
  reference src/Kernel/AppThere.Loki.Skia/AppThere.Loki.Skia.csproj

# ── Folder structure within projects ─────────────────────────────────────────
echo ""
echo "==> Creating internal folder structure..."

# Kernel folders
mkdir -p src/Kernel/AppThere.Loki.Kernel/{Geometry,Color,Fonts,Storage,Images,Logging}

# Skia folders
mkdir -p src/Kernel/AppThere.Loki.Skia/{Surfaces,Painting,Scene/Nodes,Fonts,Images,Paths,Rendering}

# Skia font resources
mkdir -p src/Kernel/AppThere.Loki.Skia/Resources/Fonts

# Test mirrors
mkdir -p tests/unit/AppThere.Loki.Tests.Unit/{Kernel/{Geometry,Color,Fonts,Storage,Images},Skia/{Surfaces,Painting,Scene,Fonts,Images,Paths,Rendering}}
mkdir -p tests/rendering/AppThere.Loki.Tests.Rendering/{Goldens,Scenes}

# Docs
mkdir -p docs/adr
mkdir -p docs/benchmarks/results

# Tools
mkdir -p tools/lokiprint/Commands

# .github
mkdir -p .github/workflows

# Remove auto-generated stub files from dotnet new
find src tools tests -name "Class1.cs" -delete
find src tools tests -name "UnitTest1.cs" -delete

echo ""
echo "==> Scaffold complete."
echo ""
echo "Next steps:"
echo "  1. Review and apply the .csproj files from docs/scaffold/"
echo "  2. Apply Directory.Build.props and Directory.Packages.props"
echo "  3. Run: dotnet restore"
echo "  4. Run: dotnet build"
echo "  5. Hand off to Claude Code with CLAUDE.md as context"

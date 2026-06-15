# Releasing FoundryDB.SDK (NuGet)

## Prerequisites

Add the following secret to the GitHub repository (`Settings > Secrets and variables > Actions`):

| Secret name | Value |
|-------------|-------|
| `NUGET_API_KEY` | A NuGet.org API key with **Push** permission for the `FoundryDB.SDK` package. Create one at https://www.nuget.org/account/apikeys. |

## Publishing a new version

1. Ensure `main` is in the state you want to release.
2. Create and push a version tag:

   ```bash
   git tag v1.2.3
   git push origin v1.2.3
   ```

3. The `Publish to NuGet` workflow triggers automatically. It packs only the library project (`FoundryDB.SDK/FoundryDB.SDK.csproj`) with the tag version injected via MSBuild `-p:Version=...` (no source-file edits), then pushes the `.nupkg` to NuGet.org.

No source-file edits are needed prior to tagging; the version in `FoundryDB.SDK.csproj` is overridden at pack time.

## Manual trigger

You can also trigger the workflow manually from the GitHub Actions UI (`workflow_dispatch`) by supplying the tag name (e.g. `v1.2.3`).

## .NET version note

This project targets **net10.0**. The CI workflow installs .NET 10.x accordingly. If you downgrade the target framework in `FoundryDB.SDK.csproj`, update the `dotnet-version` in the workflow to match.

# Publishing Guide for Jobs.Worker Client SDK

This guide explains how to publish the multi-platform Jobs.Worker Client SDK.

## 📋 Table of Contents

- [Prerequisites](#prerequisites)
- [Publishing Methods](#publishing-methods)
- [Automated Publishing (Git Tags)](#automated-publishing-git-tags)
- [Manual Publishing (Workflow Dispatch)](#manual-publishing-workflow-dispatch)
- [Local Publishing](#local-publishing)
- [Secrets Configuration](#secrets-configuration)
- [Version Management](#version-management)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Secrets

Configure these secrets in your GitHub repository (`Settings > Secrets and variables > Actions`):

| Secret Name | Description | How to Obtain |
|------------|-------------|---------------|
| `NUGET_API_KEY` | NuGet.org API key | [Create at nuget.org](https://www.nuget.org/account/apikeys) |
| `NPM_TOKEN` | npm authentication token | Run `npm token create` after `npm login` |

### Repository Settings

1. **Enable Workflows**: `Settings > Actions > General` → Allow all actions
2. **Enable Releases**: `Settings > General` → Releases enabled
3. **Branch Protection**: Protect `main` branch (recommended)

---

## Publishing Methods

### 1. Automated Publishing (Recommended)

**Trigger:** Push a Git tag matching `v*.*.*`

```bash
# Create and push a tag
git tag v1.0.0
git push origin v1.0.0
```

**What happens:**
1. ✅ Version extracted from tag (`v1.0.0` → `1.0.0`)
2. ✅ API started and clients generated via NSwag
3. ✅ .NET 8 package built and published to NuGet
4. ✅ .NET Framework 4.8 package built and published to NuGet
5. ✅ TypeScript package built and published to npm
6. ✅ GitHub release created with artifacts

**Workflow:** `.github/workflows/sdk-publish.yml`

---

### 2. Manual Publishing

**Trigger:** GitHub Actions UI → `SDK Publish - Manual Trigger` → `Run workflow`

1. Go to `Actions` tab
2. Select `SDK Publish - Manual Trigger`
3. Click `Run workflow`
4. Fill in parameters:
   - **Version**: e.g., `1.0.0` or `1.0.0-beta.1`
   - **Publish to NuGet**: ✓ (default: true)
   - **Publish to npm**: ✓ (default: true)
   - **Create GitHub release**: ✓ (default: true)
5. Click `Run workflow`

**Workflow:** `.github/workflows/sdk-publish-manual.yml`

---

### 3. Local Publishing (Advanced)

For testing or manual releases without CI/CD.

#### Prerequisites
```bash
# Install tools
dotnet tool install -g NSwag.ConsoleCore
npm install -g npm

# Login to registries
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
npm login
```

#### Generate Clients

```bash
cd Jobs.Worker.Client

# Start the API first (in separate terminal)
cd ../backend/src/Jobs.Worker.Api
dotnet run --urls="https://localhost:5001"

# Generate clients
cd ../../Jobs.Worker.Client
nswag run nswag-dotnet.json
nswag run nswag-net48.json
nswag run nswag-typescript.json
```

#### Build .NET Packages

```bash
cd Jobs.Worker.Client

# Set version
VERSION="1.0.0"

# Build .NET 8
dotnet restore Jobs.Worker.Client.csproj
dotnet build Jobs.Worker.Client.csproj --configuration Release -p:Version=$VERSION
dotnet pack Jobs.Worker.Client.csproj --configuration Release --no-build --output ./nupkg -p:PackageVersion=$VERSION

# Build .NET Framework 4.8
cd Jobs.Worker.Client.Net48
dotnet restore Jobs.Worker.Client.Net48.csproj
dotnet build Jobs.Worker.Client.Net48.csproj --configuration Release -p:Version=$VERSION
dotnet pack Jobs.Worker.Client.Net48.csproj --configuration Release --no-build --output ../nupkg -p:PackageVersion=$VERSION
cd ..
```

#### Publish to NuGet

```bash
# Publish .NET 8
dotnet nuget push ./nupkg/Jobs.Worker.Client.$VERSION.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json

# Publish .NET Framework 4.8
dotnet nuget push ./nupkg/Jobs.Worker.Client.Net48.$VERSION.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

#### Build npm Package

```bash
cd Jobs.Worker.Client

# Update version
npm version $VERSION --no-git-tag-version --allow-same-version

# Build
npm ci
npm run build

# Pack
npm pack
```

#### Publish to npm

```bash
npm publish ./jobs-worker-client-$VERSION.tgz --access public
```

---

## Secrets Configuration

### NuGet API Key

1. Go to [nuget.org/account/apikeys](https://www.nuget.org/account/apikeys)
2. Click **Create**
3. Settings:
   - **Key Name**: `Jobs.Worker.Client SDK`
   - **Glob Pattern**: `Jobs.Worker.Client*`
   - **Expire**: 365 days (or as needed)
   - **Scopes**: ✓ Push, ✓ Push new packages and package versions
4. Copy the API key
5. Add to GitHub:
   - Repository → `Settings` → `Secrets and variables` → `Actions`
   - Click `New repository secret`
   - Name: `NUGET_API_KEY`
   - Value: (paste API key)

### npm Token

1. Login to npm:
   ```bash
   npm login
   ```
2. Create token:
   ```bash
   npm token create
   ```
3. Copy the token
4. Add to GitHub:
   - Repository → `Settings` → `Secrets and variables` → `Actions`
   - Click `New repository secret`
   - Name: `NPM_TOKEN`
   - Value: (paste token)

---

## Version Management

### Semantic Versioning (SemVer)

Follow [Semantic Versioning 2.0.0](https://semver.org/):

- **MAJOR** (`1.0.0` → `2.0.0`): Breaking changes
- **MINOR** (`1.0.0` → `1.1.0`): New features (backwards compatible)
- **PATCH** (`1.0.0` → `1.0.1`): Bug fixes (backwards compatible)

### Pre-release Versions

For beta/RC releases:

```bash
# Beta release
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1

# Release candidate
git tag v1.0.0-rc.1
git push origin v1.0.0-rc.1
```

Pre-release packages will be marked as such on NuGet and npm.

### Version Checklist

Before publishing a new version:

- [ ] Update `CHANGELOG.md` with changes
- [ ] Update `README.md` if API changed
- [ ] Run all tests locally
- [ ] Test client generation
- [ ] Verify API is running before generation
- [ ] Choose appropriate version number (SemVer)
- [ ] Create and push Git tag

---

## Troubleshooting

### Client Generation Fails

**Problem:** NSwag fails to generate clients

**Solutions:**
1. Ensure API is running:
   ```bash
   curl -k https://localhost:5001/health
   ```
2. Check Swagger endpoint:
   ```bash
   curl -k https://localhost:5001/swagger/v1/swagger.json
   ```
3. Verify NSwag config files (`nswag-*.json`)
4. Check NSwag version compatibility

### NuGet Publish Fails

**Problem:** `Response status code does not indicate success: 403 (Forbidden)`

**Solutions:**
1. Verify `NUGET_API_KEY` secret is set correctly
2. Check API key hasn't expired
3. Verify API key has push permissions
4. Ensure package ID matches allowed glob pattern

### npm Publish Fails

**Problem:** `npm ERR! 403 Forbidden`

**Solutions:**
1. Verify `NPM_TOKEN` secret is set correctly
2. Check npm token hasn't expired:
   ```bash
   npm token list
   ```
3. Verify you're logged in:
   ```bash
   npm whoami
   ```
4. Check package name in `package.json` matches npm scope

### Version Already Exists

**Problem:** Package version already published

**Solutions:**
1. NuGet: Increment version number (NuGet doesn't allow overwriting)
2. npm: Use `--force` flag (not recommended) or increment version

### Workflow Not Triggering

**Problem:** Pushing tag doesn't trigger workflow

**Solutions:**
1. Verify workflow file exists: `.github/workflows/sdk-publish.yml`
2. Check tag format matches `v*.*.*` pattern
3. Ensure workflows are enabled in repository settings
4. Check Actions tab for errors

### Build Fails on GitHub Actions

**Problem:** Build succeeds locally but fails in CI

**Solutions:**
1. Check .NET SDK versions match
2. Verify all files are committed
3. Check cache issues (clear cache in workflow)
4. Review workflow logs for specific errors

---

## Workflow Files

| Workflow | Purpose | Trigger |
|----------|---------|---------|
| `sdk-publish.yml` | Full publish pipeline | Git tag `v*.*.*` |
| `sdk-publish-manual.yml` | Manual publish | Workflow dispatch |
| `sdk-validate.yml` | Build validation (no publish) | PR, push to branches |

---

## Publishing Checklist

### Pre-Release

- [ ] All tests passing
- [ ] API changes documented
- [ ] Version number decided
- [ ] Changelog updated
- [ ] README updated (if needed)
- [ ] Secrets configured (NUGET_API_KEY, NPM_TOKEN)

### Release

- [ ] Create Git tag: `git tag vX.Y.Z`
- [ ] Push tag: `git push origin vX.Y.Z`
- [ ] Monitor workflow in Actions tab
- [ ] Verify packages on NuGet.org
- [ ] Verify package on npm
- [ ] Check GitHub release created

### Post-Release

- [ ] Test installation of published packages
- [ ] Update documentation with new version
- [ ] Announce release (if applicable)
- [ ] Monitor for issues

---

## Support

For issues with publishing:

1. Check [GitHub Actions logs](../../actions)
2. Review this guide
3. Check [Troubleshooting](#troubleshooting) section
4. Open an issue with `[Publishing]` prefix

---

**Last Updated:** 2024-12-07
**SDK Version:** 1.0.0

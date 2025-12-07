# Quick Start: Publishing SDK

Fast reference for publishing the Jobs.Worker Client SDK.

## 🚀 Fastest Method: Git Tag

```bash
# 1. Ensure all changes are committed
git add .
git commit -m "Release preparation"

# 2. Create version tag
git tag v1.0.0

# 3. Push tag to trigger automated publishing
git push origin v1.0.0
```

**Done!** ✅ Workflow automatically:
- Generates clients
- Builds packages
- Publishes to NuGet + npm
- Creates GitHub release

**Monitor:** Check [Actions tab](../../actions)

---

## 📋 Pre-Release Checklist

- [ ] All tests passing
- [ ] API is documented
- [ ] Version number chosen (SemVer)
- [ ] Secrets configured (see below)

---

## 🔐 Required Secrets (One-Time Setup)

Add in `Repository Settings > Secrets and variables > Actions`:

### NUGET_API_KEY
1. Go to [nuget.org/account/apikeys](https://www.nuget.org/account/apikeys)
2. Create API key with push permissions
3. Copy and add as `NUGET_API_KEY` secret

### NPM_TOKEN
1. Run: `npm login && npm token create`
2. Choose "Automation" type
3. Copy and add as `NPM_TOKEN` secret

---

## 📦 Version Numbers (SemVer)

| Change | Version | Example |
|--------|---------|---------|
| Breaking change | MAJOR | 1.0.0 → **2**.0.0 |
| New feature | MINOR | 1.0.0 → 1.**1**.0 |
| Bug fix | PATCH | 1.0.0 → 1.0.**1** |
| Pre-release | Suffix | 1.0.0-**beta.1** |

---

## 🛠️ Alternative: Manual Trigger

1. Go to **Actions** tab
2. Select **"SDK Publish - Manual Trigger"**
3. Click **"Run workflow"**
4. Enter version (e.g., `1.0.0`)
5. Click **"Run workflow"**

---

## 📊 Workflow Status

| Workflow | When | Purpose |
|----------|------|---------|
| `sdk-publish.yml` | Tag `v*.*.*` | Auto publish |
| `sdk-publish-manual.yml` | Manual | Manual publish |
| `sdk-validate.yml` | PR/Push | Build validation |

---

## 🎯 Published Package URLs

After publishing, verify packages at:

- **NuGet (.NET 8)**: `https://www.nuget.org/packages/Jobs.Worker.Client/{VERSION}`
- **NuGet (.NET 4.8)**: `https://www.nuget.org/packages/Jobs.Worker.Client.Net48/{VERSION}`
- **npm**: `https://www.npmjs.com/package/@jobs-worker/client/v/{VERSION}`
- **GitHub Release**: `https://github.com/cangelosilima/Jobs.Worker/releases/tag/v{VERSION}`

---

## 🔍 Troubleshooting

### Workflow doesn't trigger
- Verify tag format: `v1.0.0` (starts with `v`)
- Check Actions are enabled in repo settings

### NuGet publish fails (403)
- Verify `NUGET_API_KEY` secret is set
- Check API key hasn't expired

### npm publish fails (401)
- Verify `NPM_TOKEN` secret is set
- Check token type is "Automation"

### Client generation fails
- Ensure API builds successfully
- Check `nswag-*.json` config files

---

## 📚 Full Documentation

- **Detailed Guide**: [PUBLISHING.md](./PUBLISHING.md)
- **Secrets Setup**: [.github-secrets.md](./.github-secrets.md)
- **SDK Usage**: [README.md](./README.md)

---

## 💡 Tips

- Tag format must be `v{MAJOR}.{MINOR}.{PATCH}`
- Use `-beta.1` for pre-releases
- Packages appear in ~5 minutes after publish
- Check GitHub Actions logs for errors
- Test locally before tagging

---

**Ready to publish?** Just tag and push! 🚀

```bash
git tag v1.0.0 && git push origin v1.0.0
```

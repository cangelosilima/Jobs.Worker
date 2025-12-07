# Visual Workflow Diagrams

ASCII diagrams showing the complete CI/CD pipeline flow.

## 🔄 Main Publishing Workflow (sdk-publish.yml)

### Full Pipeline Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                           TRIGGER EVENT                              │
│                   Developer pushes Git tag: v1.0.0                   │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │    JOB 1: SETUP         │
                    │  ├─ Extract version     │
                    │  ├─ Validate format     │
                    │  └─ Output: VERSION     │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────────────────────┐
                    │    JOB 2: GENERATE CLIENTS              │
                    │  ├─ Setup: .NET 8, .NET 6, Node.js     │
                    │  ├─ Install: NSwag CLI                  │
                    │  ├─ Start: Jobs.Worker.Api              │
                    │  ├─ Wait: Health check passes           │
                    │  ├─ Generate: .NET 8 client             │
                    │  ├─ Generate: .NET 4.8 client           │
                    │  ├─ Generate: TypeScript client         │
                    │  ├─ Stop: API                           │
                    │  └─ Upload: Artifacts (all clients)     │
                    └─────┬──────────────────────┬────────────┘
                          │                      │
              ┌───────────▼─────────┐   ┌───────▼────────────┐
              │ JOB 3: BUILD .NET   │   │ JOB 5: BUILD npm   │
              │  ├─ Download SDK    │   │  ├─ Download SDK   │
              │  ├─ Setup .NET 8    │   │  ├─ Setup Node.js  │
              │  ├─ Restore deps    │   │  ├─ npm ci         │
              │  ├─ Build .NET 8    │   │  ├─ npm version    │
              │  ├─ Pack .NET 8     │   │  ├─ npm build      │
              │  ├─ Build .NET 4.8  │   │  ├─ npm pack       │
              │  ├─ Pack .NET 4.8   │   │  └─ Upload tarball │
              │  └─ Upload packages │   └───────┬────────────┘
              └─────────┬───────────┘           │
                        │                       │
              ┌─────────▼──────────┐   ┌────────▼───────────┐
              │ JOB 4: PUBLISH TO  │   │ JOB 6: PUBLISH TO  │
              │       NUGET.ORG    │   │    NPM REGISTRY    │
              │  ├─ Download pkgs  │   │  ├─ Download pkg   │
              │  ├─ Setup .NET     │   │  ├─ Setup Node.js  │
              │  ├─ Publish .NET 8 │   │  └─ npm publish    │
              │  └─ Publish .NET48 │   │     (public access)│
              └─────────┬──────────┘   └────────┬───────────┘
                        │                       │
                        └───────────┬───────────┘
                                    │
                          ┌─────────▼─────────────────────┐
                          │ JOB 7: CREATE GITHUB RELEASE  │
                          │  ├─ Download all artifacts    │
                          │  ├─ Generate release notes    │
                          │  ├─ Create release tag        │
                          │  ├─ Attach .nupkg files       │
                          │  ├─ Attach .tgz file          │
                          │  └─ Mark pre-release (if '-') │
                          └─────────┬─────────────────────┘
                                    │
                          ┌─────────▼─────────┐
                          │ JOB 8: NOTIFY     │
                          │  └─ Print success │
                          │     summary       │
                          └───────────────────┘
```

---

## 🎯 Parallel vs Sequential Execution

### Without Optimization (Sequential)

```
┌────────────────────────────────────────────────────────────┐
│ Timeline: 0 ─────────────── 420 seconds ──────────────────▶│
└────────────────────────────────────────────────────────────┘

generate-clients (180s)
    │
    └──▶ build-dotnet (120s)
            │
            └──▶ publish-nuget (30s)
                    │
                    └──▶ build-npm (60s)
                            │
                            └──▶ publish-npm (20s)
                                    │
                                    └──▶ create-release (30s)

Total: ~7 minutes (420 seconds)
```

### With Optimization (Parallel)

```
┌────────────────────────────────────────────────────────────┐
│ Timeline: 0 ─────────── 310 seconds ──────────────────────▶│
└────────────────────────────────────────────────────────────┘

generate-clients (180s)
    │
    ├──▶ build-dotnet (120s) ──▶ publish-nuget (30s) ──┐
    │                                                    │
    └──▶ build-npm (60s) ──▶ publish-npm (20s) ────────┤
                                                         │
                                                         ├──▶ create-release (30s)
                                                         │
                                                         └──▶ notify (10s)

Total: ~5-6 minutes (310 seconds)
Improvement: 28% faster
```

---

## 🔀 Decision Flow

### Version Type Detection

```
                  ┌────────────────┐
                  │ Extract Version│
                  │  from Git Tag  │
                  └────────┬───────┘
                           │
                  ┌────────▼────────┐
                  │  Version String │
                  └────────┬────────┘
                           │
                  ┌────────▼────────┐
              ┌───│ Contains '-' ?  │───┐
              │   └─────────────────┘   │
              │                         │
         YES  │                         │  NO
              │                         │
    ┌─────────▼────────┐     ┌─────────▼─────────┐
    │  PRE-RELEASE     │     │  STABLE RELEASE   │
    │  v1.0.0-beta.1   │     │  v1.0.0           │
    │  v1.2.0-rc.1     │     │  v2.1.3           │
    └─────────┬────────┘     └─────────┬─────────┘
              │                         │
    ┌─────────▼────────┐     ┌─────────▼─────────┐
    │ GitHub Release:  │     │ GitHub Release:   │
    │ prerelease: true │     │ prerelease: false │
    └──────────────────┘     └───────────────────┘
```

---

## 🔄 Client Generation Flow

```
┌──────────────────────────────────────────────────────────────┐
│                 CLIENT GENERATION PROCESS                     │
└──────────────────────────────────────────────────────────────┘

1. API Startup
   │
   ├──▶ cd backend/src/Jobs.Worker.Api
   ├──▶ dotnet run --urls="https://localhost:5001" &
   └──▶ Store PID for cleanup

2. Health Check (30 attempts, 2s intervals)
   │
   ├──▶ Attempt 1: curl -k https://localhost:5001/health
   ├──▶ Attempt 2: curl -k https://localhost:5001/health
   ├──▶ ...
   └──▶ Success! API is ready

3. Generate Clients (Parallel)
   │
   ├──▶ nswag run nswag-dotnet.json
   │    └─ Output: src/dotnet/RestClient.g.cs
   │
   ├──▶ nswag run nswag-net48.json
   │    └─ Output: src/net48/RestClient.Net48.g.cs
   │
   └──▶ nswag run nswag-typescript.json
        └─ Output: src/typescript/api-client.g.ts

4. Verification
   │
   ├──▶ Check: RestClient.g.cs exists? ✅
   ├──▶ Check: RestClient.Net48.g.cs exists? ✅
   └──▶ Check: api-client.g.ts exists? ✅

5. Cleanup
   │
   └──▶ kill $API_PID

6. Upload Artifacts
   │
   ├──▶ Upload: generated-dotnet8-client
   ├──▶ Upload: generated-net48-client
   ├──▶ Upload: generated-typescript-client
   └──▶ Upload: sdk-with-generated-clients (complete)
```

---

## 📦 Package Publishing Flow

### NuGet Publishing

```
┌─────────────────────────────────────────────────────────┐
│                  NUGET PUBLISHING                        │
└─────────────────────────────────────────────────────────┘

┌──────────────────┐
│ Download Package │
│  Jobs.Worker     │
│  .Client.nupkg   │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ Verify Package   │
│  - Check size    │
│  - List files    │
└────────┬─────────┘
         │
         ▼
┌──────────────────────────────────┐
│ dotnet nuget push                │
│  --api-key ${{ secrets }}        │
│  --source nuget.org              │
│  --skip-duplicate                │
└────────┬─────────────────────────┘
         │
         ├──▶ Success ──▶ ✅ Published
         │
         └──▶ 409 Conflict ──▶ ℹ️ Already exists (skipped)
```

### npm Publishing

```
┌─────────────────────────────────────────────────────────┐
│                   NPM PUBLISHING                         │
└─────────────────────────────────────────────────────────┘

┌──────────────────┐
│ Download Package │
│  @jobs-worker    │
│  /client.tgz     │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ Setup Registry   │
│  registry-url:   │
│  npmjs.org       │
└────────┬─────────┘
         │
         ▼
┌──────────────────────────────────┐
│ npm publish                      │
│  --access public                 │
│  env: NODE_AUTH_TOKEN            │
└────────┬─────────────────────────┘
         │
         ├──▶ Success ──▶ ✅ Published
         │
         └──▶ 403 Forbidden ──▶ ❌ Check token/permissions
```

---

## 🔐 Secret Flow

```
┌──────────────────────────────────────────────────────────┐
│                    SECRET MANAGEMENT                      │
└──────────────────────────────────────────────────────────┘

GitHub Repository Settings
    │
    └──▶ Secrets and variables
            │
            └──▶ Actions
                    │
                    ├──▶ NUGET_API_KEY (masked as ***)
                    │    └──▶ Used in: publish-nuget job
                    │
                    └──▶ NPM_TOKEN (masked as ***)
                         └──▶ Used in: publish-npm job

Workflow Access:
    │
    ├──▶ Job: publish-nuget
    │    └──▶ environment: production (optional approval)
    │         └──▶ env:
    │              └──▶ ${{ secrets.NUGET_API_KEY }}
    │
    └──▶ Job: publish-npm
         └──▶ environment: production (optional approval)
              └──▶ env:
                   └──▶ NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}

Security:
    ├──▶ Secrets never logged (auto-masked as ***)
    ├──▶ Only accessible in workflow execution
    └──▶ Can be rotated without code changes
```

---

## 📊 Cache Strategy

```
┌─────────────────────────────────────────────────────────┐
│                    CACHE STRATEGY                        │
└─────────────────────────────────────────────────────────┘

NuGet Cache:
    │
    ├──▶ Path: ~/.nuget/packages
    ├──▶ Key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    ├──▶ Restore keys: ${{ runner.os }}-nuget-
    │
    └──▶ Cache Hit?
         ├──▶ YES: Restore from cache (~2s) ✅
         └──▶ NO: Download packages (~60s) ⏳

npm Cache:
    │
    ├──▶ Path: node_modules
    ├──▶ Key: ${{ runner.os }}-node-${{ hashFiles('**/package-lock.json') }}
    ├──▶ Restore keys: ${{ runner.os }}-node-
    │
    └──▶ Cache Hit?
         ├──▶ YES: Restore from cache (~1s) ✅
         └──▶ NO: Run npm ci (~40s) ⏳

Performance Impact:
    │
    ├──▶ First run (cold cache): ~7 minutes
    ├──▶ Subsequent runs (warm cache): ~5 minutes
    └──▶ Savings: ~28% faster
```

---

## 🎭 Workflow Trigger Patterns

```
┌─────────────────────────────────────────────────────────────┐
│                  WORKFLOW TRIGGERS                           │
└─────────────────────────────────────────────────────────────┘

sdk-publish.yml:
    Trigger: on.push.tags = v*.*.*
    │
    Examples:
    ├──▶ v1.0.0      ✅ Triggers
    ├──▶ v2.1.3      ✅ Triggers
    ├──▶ v1.0.0-rc.1 ✅ Triggers (pre-release)
    ├──▶ v1.0        ❌ Does not trigger (missing patch)
    └──▶ 1.0.0       ❌ Does not trigger (missing 'v' prefix)

sdk-publish-manual.yml:
    Trigger: workflow_dispatch
    │
    User inputs:
    ├──▶ version: "1.0.0"
    ├──▶ publish_nuget: true
    ├──▶ publish_npm: true
    └──▶ create_release: true

sdk-validate.yml:
    Trigger: on.pull_request + on.push.branches
    │
    Events:
    ├──▶ PR to main/develop ✅
    ├──▶ Push to main       ✅
    ├──▶ Push to develop    ✅
    ├──▶ Push to claude/**  ✅
    └──▶ Push to other      ❌
```

---

## 🔄 Artifact Lifecycle

```
┌─────────────────────────────────────────────────────────┐
│                 ARTIFACT LIFECYCLE                       │
└─────────────────────────────────────────────────────────┘

Generation → Upload → Retention → Download → Cleanup

1. GENERATED
   ├─ RestClient.g.cs
   ├─ RestClient.Net48.g.cs
   └─ api-client.g.ts

2. UPLOADED (by generate-clients job)
   ├─ generated-dotnet8-client (7 days)
   ├─ generated-net48-client (7 days)
   ├─ generated-typescript-client (7 days)
   └─ sdk-with-generated-clients (7 days)

3. BUILT
   ├─ Jobs.Worker.Client.1.0.0.nupkg
   ├─ Jobs.Worker.Client.Net48.1.0.0.nupkg
   └─ jobs-worker-client-1.0.0.tgz

4. UPLOADED (by build jobs)
   ├─ nuget-package-dotnet8 (30 days)
   ├─ nuget-package-net48 (30 days)
   └─ npm-package (30 days)

5. DOWNLOADED (by publish/release jobs)
   └─ All artifacts downloaded for publishing

6. PUBLISHED
   ├─ NuGet.org: Jobs.Worker.Client.1.0.0
   ├─ NuGet.org: Jobs.Worker.Client.Net48.1.0.0
   ├─ npm: @jobs-worker/client@1.0.0
   └─ GitHub Release: v1.0.0 (with attached files)

7. CLEANUP
   └─ Artifacts auto-deleted after retention period
```

---

## 📈 Success Path vs Error Path

```
┌─────────────────────────────────────────────────────────┐
│                   EXECUTION PATHS                        │
└─────────────────────────────────────────────────────────┘

HAPPY PATH (All Green):

setup ✅
  → generate-clients ✅
      → build-dotnet ✅ → publish-nuget ✅ ─┐
      → build-npm ✅ → publish-npm ✅ ──────┤
                                            ├→ create-release ✅
                                            │     → notify ✅
                                            │
                                            └─ Result: 🎉 SUCCESS


ERROR PATH EXAMPLES:

1. API Fails to Start:
   setup ✅
     → generate-clients ❌ (API timeout)
         → All subsequent jobs: ⏭️ SKIPPED

2. Client Generation Fails:
   setup ✅
     → generate-clients ❌ (NSwag error)
         → All subsequent jobs: ⏭️ SKIPPED

3. Build Fails:
   setup ✅
     → generate-clients ✅
         → build-dotnet ❌ (compilation error)
             → publish-nuget: ⏭️ SKIPPED
             → create-release: ⏭️ SKIPPED

4. Publish Fails:
   setup ✅
     → generate-clients ✅
         → build-dotnet ✅
             → publish-nuget ❌ (invalid API key)
                 → create-release: ⏭️ SKIPPED

5. Partial Success:
   setup ✅
     → generate-clients ✅
         → build-dotnet ✅ → publish-nuget ✅ ─┐
         → build-npm ✅ → publish-npm ❌ ──────┤
                                               │
                      create-release: ⚠️ CONDITIONAL
```

---

**Generated:** 2024-12-07
**For:** Jobs.Worker Client SDK v1.0.0

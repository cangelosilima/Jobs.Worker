# Branch Protection Configuration

This document describes the recommended branch protection rules for the repository.

## Push Validation (Bots & AI Only)

**NEW**: All pushes by bots and AI are automatically validated!

The `push-validation.yml` workflow uses **GitHub's native authentication metadata** to detect bots/AI:

**Bot/AI Detection Methods (in order):**
1. ✅ **`pusher.type == "Bot"`** - GitHub's native bot detection
   - GitHub Apps (using app tokens)
   - Automated tools with bot credentials

2. ✅ **Bot naming convention** - Username ends with `[bot]`
   - Example: `dependabot[bot]`, `renovate[bot]`

3. ✅ **Commit author email** - Bot email patterns
   - Bot-specific email addresses in commits

4. ✅ **GitHub Actions** - `github-actions` actor
   - Workflows using GITHUB_TOKEN

**Human Detection:**
- ❌ OAuth authentication (web login)
- ❌ Personal Access Token (PAT)
- ❌ SSH key authentication
- ❌ Any standard user authentication

**Validation includes:**
- ✅ Builds backend (.NET)
- ✅ Runs all backend tests
- ✅ Builds frontend (React)
- ✅ Runs all frontend tests
- ❌ **Blocks bot/AI pushes if any validation fails**

**Human developers:** Your pushes (OAuth/PAT/SSH) are NOT validated by this workflow. You'll be validated when creating PRs instead, allowing for faster iteration during development.

This ensures automated code meets quality standards while keeping development velocity high for humans.

## Main Branch Protection Rules

### Required Status Checks
Configure these status checks to be required before merging:

- ✅ **Quick Build & Test Validation** (Push Validation) - Must pass
- ✅ **Backend Build & Test** (PR Validation) - Must pass
- ✅ **Frontend Build & Test** (PR Validation) - Must pass
- ✅ **Code Quality Checks** (PR Validation) - Must pass
- ✅ **PR Validation Summary** - Must pass

### Branch Protection Settings

Go to: **Settings → Branches → Add rule** for `main` branch

#### Protect matching branches
- [x] Require a pull request before merging
  - [x] Require approvals: **1**
  - [x] Dismiss stale pull request approvals when new commits are pushed
  - [x] Require review from Code Owners

- [x] Require status checks to pass before merging
  - [x] Require branches to be up to date before merging
  - Required status checks:
    - `Quick Build & Test Validation` (from Push Validation workflow)
    - `Backend Build & Test` (from PR Validation workflow)
    - `Frontend Build & Test` (from PR Validation workflow)
    - `Code Quality Checks` (from PR Validation workflow)
    - `PR Validation Summary`

- [x] Require conversation resolution before merging

- [x] Require signed commits (optional but recommended)

- [x] Require linear history (optional)

- [x] Include administrators (apply rules to admins too)

- [x] Restrict who can push to matching branches
  - Only allow: Repository administrators

- [x] Allow force pushes: **Never**

- [x] Allow deletions: **Never**

## Develop Branch Protection (if applicable)

Same rules as main, but with:
- Require approvals: **1** (can be less strict)
- Allow force pushes: **Specify who** (only to maintainers, optional)

## Feature Branch Naming Convention

Use these prefixes for feature branches:
- `feature/` - New features
- `bugfix/` - Bug fixes
- `hotfix/` - Urgent fixes for production
- `refactor/` - Code refactoring
- `docs/` - Documentation updates
- `test/` - Test improvements
- `ci/` - CI/CD improvements

Example: `feature/add-job-health-score`

## Automated Merge Requirements

The PR validation workflow ensures:
1. All backend tests pass (Domain, Application, API, Architecture)
2. All frontend tests pass (Unit, Integration, Architecture)
3. Code quality checks pass (formatting, security)
4. No build errors in either backend or frontend
5. Code coverage meets minimum thresholds (if configured)

## Manual Steps After PR Approval

After all checks pass and PR is approved:

1. **Squash and Merge** (recommended) - Creates a clean history
2. **Merge Commit** - Preserves all commit history
3. **Rebase and Merge** - Linear history without merge commits

Choose based on your team's preference.

## Emergency Procedures

In case of critical production issues:

1. Create a `hotfix/` branch from `main`
2. Make minimal changes to fix the issue
3. Create PR with `[HOTFIX]` prefix in title
4. Fast-track review process
5. All automated checks must still pass
6. After merge to main, backport to develop if needed

## Monitoring

Monitor the following regularly:
- Failed PR checks in GitHub Actions
- Code coverage trends
- Security scan results
- Dependabot alerts

## Questions?

Contact the repository administrators or check the [GitHub Actions documentation](https://docs.github.com/en/actions).

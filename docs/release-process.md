# Release Process

## Versioning

This project uses [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)
for automatic versioning based on git history. The version is configured in
[`version.json`](../version.json) at the repo root.

### Version format

| Branch | NuGet version | Example | Pre-release? |
|--------|--------------|---------|:------------:|
| `master` | `{major}.{minor}.{height}-dev` | `3.0.42-dev` | Yes |
| `release/**` | `{major}.{minor}.{height}` | `3.0.42` | No |
| `dev/**` / PRs | `{major}.{minor}.{height}-dev.g{hash}` | `3.0.42-dev.ga1b2c3d` | Yes |

- **height** = number of commits since `version.json` was last modified.
- On `master` and `release/**` (public branches), the git hash is omitted.
- On feature branches and PRs, the git hash is appended to guarantee uniqueness.

## CI Pipeline

The CI pipeline (`.github/workflows/ci.yml`) runs on **every push** to `master`
and `release/**`, and on **every pull request**.

### What runs when

| Trigger | Build + Test | NuGet Publish | GitHub Release |
|---------|:------------:|:-------------:|:--------------:|
| Pull request | Yes | No | No |
| Push to `master` | Yes | Yes (pre-release) | No |
| Push to `release/**` | Yes | Yes (stable) | Yes |

### Required secrets

| Secret | Description |
|--------|-------------|
| `NUGET_API_KEY` | API key for nuget.org (configure in repo Settings > Secrets > Actions) |
| `GITHUB_TOKEN` | Provided automatically by GitHub Actions (used for creating releases) |

## Publishing a stable release

1. **Create the release branch** from `master`:

   ```bash
   git checkout master
   git pull
   git checkout -b release/v3.0
   git push -u origin release/v3.0
   ```

2. **The CI takes care of the rest automatically:**
   - Detects `-dev` in `version.json` and strips it (commits the change)
   - Builds, tests, and packs the library
   - Publishes the stable `.nupkg` to nuget.org
   - Creates a GitHub Release with auto-generated release notes

3. **If a hotfix is needed** on the release branch, commit directly to it.
   The CI will build and publish an incremented patch version.

## Bumping the major/minor version

After creating a release branch, bump the version on `master` for the next
development cycle:

```bash
git checkout master
# Edit version.json: change "3.0-dev" to "3.1-dev" (or "4.0-dev")
git add version.json
git commit -m "Bump version to 3.1-dev"
git push
```

Alternatively, use the `nbgv` CLI:

```bash
dotnet tool install -g nbgv
nbgv prepare-release
```

This command automatically:
- Creates a `release/v{version}` branch with the stable version
- Bumps `master` to the next minor version with `-dev` suffix

## Local version check

To see what version would be produced from your current commit:

```bash
dotnet tool install -g nbgv
nbgv get-version
```

# Release Process

The 3.0 line is being prepared for its first stable release. Documentation and CI
preparation do not publish that release: pushing a prepared `release/**` branch
does. The outstanding items in [#12](https://github.com/yllibed/TenantCloudClient/issues/12)
remain accepted, documented limitations rather than release blockers.

## Versioning

[Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)
derives versions from git history and [`version.json`](../version.json).
The CLI used by CI is pinned to the package version in
[`Directory.Packages.props`](../src/Directory.Packages.props).

| Branch | Version configuration | NuGet version | GitHub release |
|--------|-----------------------|---------------|----------------|
| `master` | `3.0-dev` | `3.0.{height}-dev` | Prerelease |
| `release/v3.0` | `3.0` | `3.0.{height}` | Stable |
| Feature branches / PRs | `3.0-dev` | Includes prerelease and commit identification | None |

The patch component is the git version height, not a manually chosen patch
number. It resets when the major/minor version changes; removing the prerelease
suffix alone does not reset it. The first stable will therefore be `3.0.x`, not
necessarily `3.0.0`. Read the computed version rather than guessing it from a
commit count. Public release refs omit the git hash from the package version.

## CI publication contract

The [workflow](../.github/workflows/ci.yml) runs on pull requests and pushes to
`master` and `release/**`.

| Trigger | Build, test, pack | Binary targets | Publish packages and GitHub release |
|---------|-------------------|----------------|-------------------------------------|
| Pull request | Yes | Portable and Windows x64 | No |
| Push to `master` | Yes | All configured targets | Prerelease |
| Push to `release/**` | Yes | All configured targets | Stable |

All jobs check out the triggering SHA. CI does not change or commit the version
file. `master` must declare a prerelease version. A release branch must use the
exact `release/vMAJOR.MINOR` form and declare a matching stable version. The
build job resolves the version once and passes it to the release job, which
targets that same SHA when creating the tag.

NuGet publication waits for builds, tests, binary packaging and the portable
archive smoke test. A successful cross-publish is not an execution test on that
target OS. Platform archives retain ReadyToRun and all required dependencies;
the portable archive is framework-dependent and requires .NET 10.

The release job uses the protected `nuget-production` environment. Its required
reviewer approves the deployment before the job can request a GitHub OIDC token.
The nuget.org trusted publishing policy must match repository owner `yllibed`,
repository `TenantCloudClient`, workflow file `ci.yml` and environment
`nuget-production`. It grants push access for new packages and package versions
matching `Yllibed.TenantCloudClient*` under the `carl.debilly` package owner.

CI uses the SHA-pinned `NuGet/login` action to exchange the OIDC token for a
short-lived API key immediately before publishing. No long-lived
`NUGET_API_KEY` secret is stored in GitHub. PRs never execute publication steps.
The workflow's scoped `GITHUB_TOKEN` creates the GitHub release.

Publication to NuGet and GitHub is not atomic. Before a retry publishes anything,
CI checks any existing tag and NuGet packages against the triggering commit using
the repository commit recorded in each package. A mismatch fails the release;
publish a new version rather than overwriting a package or moving a tag.

## 3.0 stable checklist

No release date is committed. Complete this checklist on the selected release
commit and record the results in the release PR or GitHub release notes:

- [ ] Review the [v2 migration](client-library.md#migrating-to-v3) and
  [MCP prerelease migration](mcp-server.md#pagination-and-migration-from-the-previous-mcp-contract).
- [ ] Keep the [known limitations](client-library.md#known-limitations) visible
  in the release notes. Issue #12 stays open; this release does not claim to fix
  strict `GetAll` limits, archived-contact filtering or MCP diagnostics.
- [ ] Compile the documented C# examples and check local links and packaged
  NuGet README files.
- [ ] Build the solution in Release with warnings treated as errors and run
  the tests. Record authenticated API checks separately from CI tests that skip
  without credentials; never publish account payloads or tokens as evidence.
- [ ] Verify extracted portable and Windows R2R archives outside the source
  tree: startup, a read-only CLI call, REPL paging and MCP tools/resources.
- [ ] Record platform validation with the tested commit, OS and architecture.
  The [macOS report on #14](https://github.com/yllibed/TenantCloudClient/pull/14#issuecomment-5649303990)
  is prior evidence, not a test of a future release commit. It used stored
  credentials; keep a fresh-browser sign-in check distinct from token reuse.
- [ ] Confirm all configured binary publications pass and the NuGet metadata,
  binaries, release version and tag identify the same source commit.
- [ ] Review release notes covering browser authentication, CLI/REPL/MCP,
  pagination, nullable prices, framework requirements and known limitations.
- [ ] Explicitly approve the release-branch push. Preparing documentation or
  merging a preparation PR is not approval to publish a stable version.

Keep Repl at the currently pinned stable dependency for this release. An upgrade
to a Repl prerelease is a separate change requiring its own validation.

## Prepare a stable release locally

Start from an up-to-date, clean `master`. The following commands create local
versioning commits and branches; they do not publish anything:

```bash
git switch master
git pull --ff-only
```

Install the `nbgv` CLI version matching `Directory.Packages.props` in an isolated
tool directory. For the current dependency:

```bash
dotnet tool install nbgv --version 3.9.50 --tool-path ./artifacts/tools
./artifacts/tools/nbgv prepare-release
git switch release/v3.0
./artifacts/tools/nbgv get-version -v NuGetPackageVersion
```

`prepare-release` creates the stable branch and advances local `master` to the
next minor development version according to `version.json`. Review both commits
before pushing either branch. If the release branch already exists, use that
branch and inspect its version rather than recreating it.

On the release branch, verify that `version.json` declares `3.0` without `-dev`,
and complete the checklist. The version stays committed in source; CI will not
strip the suffix for you.

## Publish after approval

**This push starts the stable publication workflow:**

```bash
git push -u origin release/v3.0
```

Review the pending `nuget-production` deployment and approve it only after
confirming the triggering commit. Watch the workflow to completion, inspect the
release assets and verify that its tag points to the approved commit. Then push
the reviewed next-development-cycle commit on `master` through the normal review
process; this starts a prerelease deployment subject to the same approval.

For hotfixes, review and test changes on the release branch before pushing.
Nerdbank.GitVersioning increments the patch component with git height. Each push
to that branch can publish another stable version.

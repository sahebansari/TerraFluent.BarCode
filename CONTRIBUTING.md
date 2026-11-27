# Contributing

Thanks for helping improve TerraFluent.BarCode.

## Building and Testing

Install the .NET 10 SDK (and the .NET 8 runtime to run every test target), then:

```shell
dotnet build -c Release
dotnet test -c Release
```

Tests run on `net10.0` and `net8.0`, and on Windows also on `net48`; the last
two exercise the library's `netstandard2.0` build. On other systems,
`dotnet build -p:BuildNetFramework=true` compiles the `net48` target without
running it.

The build treats warnings as errors and runs the .NET analyzers.

## Pull Requests

- Keep each pull request focused, and add tests for behaviour changes.
- Every public member needs XML documentation.
- New or changed public API must be recorded in
  `src/TerraFluent.BarCode/PublicAPI.Unshipped.txt`; the build tells you the
  exact line to add.
- Add an entry under `[Unreleased]` in `CHANGELOG.md`.
- For new symbologies or styles, check that the output scans, for example
  with `zbarimg` from zbar-tools.

## Releasing (maintainers)

1. Set `<Version>` in `Directory.Build.props` and move the `[Unreleased]`
   CHANGELOG entries under the new version.
2. Move the lines of `PublicAPI.Unshipped.txt` into `PublicAPI.Shipped.txt`.
3. After the first NuGet release, set `PackageValidationBaselineVersion` in the
   library project to the previous version so breaking changes fail the build.
4. Merge to `main`, then push a tag `vX.Y.Z`. The Release workflow checks the
   tag against the version, tests, packs and pushes to NuGet.org using the
   `NUGET_API_KEY` secret, and creates the GitHub release.

# NuGet Trusted Publishing Setup

This repository uses [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) to securely publish packages to NuGet.org without managing long-lived API keys.

## How It Works

1. When the release workflow runs, GitHub Actions generates a short-lived OIDC token
2. The `NuGet/login@v1` action uses this OIDC token to authenticate with NuGet.org
3. NuGet.org validates the token and issues a temporary API key (valid for ~1 hour)
4. The temporary API key is used in the `dotnet nuget push` command to publish the package

## Configuration Required on NuGet.org

The repository owner must configure a Trusted Publishing policy on NuGet.org:

1. Log in to [nuget.org](https://www.nuget.org/)
2. Go to your account settings → **Trusted Publishing**
3. Click **Add** to create a new trusted publishing policy
4. Fill in the policy details:
   - **Package ID**: `OasReader`
   - **Owner**: `christianhelle`
   - **Repository**: `oasreader`
   - **Workflow file**: `.github/workflows/release.yml`

## Workflow Configuration

The release workflow (`release.yml`) includes the required permissions and the NuGet login action:

```yaml
permissions:
  id-token: write  # Required for OIDC token generation
  contents: write  # Required for creating tags
```

The workflow uses the `NuGet/login@v1` action to obtain a temporary API key:

```yaml
- name: NuGet Login
  uses: NuGet/login@v1
  id: nuget-login

- name: Push packages to NuGet
  run: dotnet nuget push **/*.nupkg --api-key ${{ steps.nuget-login.outputs.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --no-symbols
```

## Benefits

- **Enhanced Security**: No long-lived API keys to manage or rotate
- **Reduced Risk**: Credentials cannot be leaked or stolen
- **Simplified CI/CD**: No secrets to configure in GitHub repository settings
- **Audit Trail**: All publishing activity is tied to specific workflow runs

## Additional Resources

- [Microsoft Learn: Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
- [NuGet Blog: Enhanced Security with Trusted Publishing](https://devblogs.microsoft.com/dotnet/enhanced-security-is-here-with-the-new-trust-publishing-on-nuget-org/)
- [GitHub Documentation: OIDC Authentication](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)

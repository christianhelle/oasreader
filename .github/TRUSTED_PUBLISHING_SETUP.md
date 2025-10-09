# NuGet Trusted Publishing Setup Guide

This repository has been migrated to use **NuGet Trusted Publishing** for secure, API-key-free package deployment. To complete the setup, follow these steps:

## What is Trusted Publishing?

Trusted Publishing uses OpenID Connect (OIDC) to establish trust between GitHub Actions and NuGet.org without requiring long-lived API keys. This improves security by:
- Eliminating the need to store API keys as repository secrets
- Reducing the risk of credential leaks
- Providing automatic token rotation
- Enabling fine-grained access control

## Required Configuration on NuGet.org

To enable Trusted Publishing for the `OasReader` package:

1. **Log in to NuGet.org** with the account that owns the `OasReader` package

2. **Navigate to Package Management**:
   - Go to https://www.nuget.org/packages/OasReader/manage
   - Or: Click your profile → "Manage Packages" → Select "OasReader"

3. **Configure Trusted Publishing**:
   - Look for the "Trusted Publishers" or "Publishing" section
   - Click "Add Trusted Publisher" or similar button
   - Select "GitHub Actions" as the provider

4. **Enter Repository Details**:
   ```
   Owner: christianhelle
   Repository: oasreader
   Workflow File: .github/workflows/release.yml
   Environment: (leave empty - not using environments)
   ```

5. **Save the Configuration**

## Testing the Setup

After configuring Trusted Publishing on NuGet.org:

1. Trigger the release workflow by pushing to the `release` branch
2. Monitor the workflow run in GitHub Actions
3. The "Push packages to NuGet" step should succeed without requiring `NUGET_KEY`
4. Verify the new package version appears on NuGet.org

## Cleanup

Once Trusted Publishing is working successfully:

1. **Remove the old API key secret**:
   - Go to repository Settings → Secrets and variables → Actions
   - Delete the `NUGET_KEY` secret (no longer needed)

2. **Document the change**:
   - Update any internal documentation about the release process
   - Inform team members that API keys are no longer required

## Troubleshooting

### Authentication Fails
- **Verify NuGet.org configuration**: Double-check that the repository details match exactly
- **Check permissions**: Ensure the workflow has `id-token: write` permission (already configured)
- **Review workflow logs**: Look for OIDC token generation errors

### Package Not Found Error
- Trusted Publishing only works for existing packages
- The first release may still require an API key to create the package
- After the first release, Trusted Publishing will work for updates

## References

- [Official NuGet Trusted Publishing Documentation](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package#trusted-publishing)
- [GitHub Actions OIDC Documentation](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)

## Workflow Changes Made

The following changes were implemented in `.github/workflows/release.yml`:

1. Added OIDC permissions:
   ```yaml
   permissions:
     id-token: write
     contents: write
   ```

2. Removed API key from push command:
   ```yaml
   # Before:
   run: dotnet nuget push **/*.nupkg --api-key ${{ secrets.NUGET_KEY }} --source ${{ env.NUGET_REPO_URL }} --no-symbols
   
   # After:
   run: dotnet nuget push **/*.nupkg --source ${{ env.NUGET_REPO_URL }} --no-symbols
   ```

3. Added explicit dotnet setup step for clarity (optional but recommended)

---

**Questions?** If you encounter issues, please refer to the official documentation or open an issue in this repository.

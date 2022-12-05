# Creating a Release

To create a new release a developer needs to create and push a git tag in semver format.

Two formats are currently supported by the automated workflow

- v1.2.3 - this is used to create a production release
- v1.2.3-alpha.4 - this is used to create alpha/prereleases so they can be tested and used before they are made generally available.


To create a tag and push it, do the following.

```bash
git tag v0.0.0-alpha.0
git push --tags
```

## Beware

Once pushed the workflow will automatically trigger, and nuget packages cannot be deleted was released.

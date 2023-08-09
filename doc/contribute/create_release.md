# Creating a Release

To create a new release a developer needs to do this from the GitHub portal.

GitHub has written a [guide](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository#creating-a-release) on the subject.

The new tag should be in one of the following formats:
- `v1.2.3` - this is used to create a production release
- `v1.2.3-rc.4` - this is used to create release-candidates so they can be tested and used before they are made generally available, when doing this, remember to set the `Pre-release` flag to `true` in the GitHub portal.

Once the release is `publishes` a workflow will start, and build and publish the release and all packages tied to it.

# How to contribute

First of all, thank you for wanting to contribute to Ironclad! We really appreciate all the awesome support we get from our community. We want to keep it as easy as possible to contribute changes that get things working in your environment. There are a few guidelines we need contributors to follow to keep the project flowing smoothly.

These guidelines are for code changes but we are always very grateful to receive other forms of contribution, e.g. updates to the [documentation](https://github.com/LykkeOSS/ironclad/wiki), answering [questions on StackOverflow](https://stackoverflow.com/search?q=ironclad), providing help in the [chat room](https://gitter.im/LykkeOSS/ironclad/), blog posts, etc. :wink:

## Preparation

Before starting work on a *functional* change, i.e. a new feature, a change to an existing feature or a fixing a bug, please ensure an [issue has been raised](https://github.com/LykkeOSS/ironclad/issues/). Indicate your intention to do the work by writing a comment on the issue. This will prevent duplication of effort. If the change is non-trivial, it's usually best to propose a design in the issue comments before making a significant effort on the implementation.

It is **not** necessary to raise an issue for non-functional changes, e.g. refactoring, adding tests, reformatting code, documentation, updating packages, etc.

## Tests

All new features must be covered by feature tests in the `Ironclad.Tests` project.

## Spaces not tabs

Pull requests containing tabs will not be accepted. Make sure you set your editor to replace tabs with spaces. Indents should be 4 spaces wide for C# files, and 2 spaces wide for all other file types.

## Line endings

The repository is configured to change line endings on commit to allow for cross-platform development where you are _not_ responsible for your line endings. When this works you don't have to do anything.

## Line width

Try to keep lines of code no longer than 120 characters wide. This isn't a strict rule. Occasionally a line of code can be more readable if allowed to spill over slightly. A good way to remember this rule is to use a Visual Studio extension such as [Editor Guidelines](https://marketplace.visualstudio.com/items?itemName=PaulHarrington.EditorGuidelines).

## Coding style

Try to keep your coding style in line with the existing code. It might not exactly match your preferred style but it's better to keep things consistent.

## Code analysis

Try and avoid introducing code analysis violations. The non-test projects have largely been kept free of code analysis violations and we would like to keep it that way. Any code analysis rule changes or suppressions must be clearly justified.

## Resharper artifacts

Please do not add ReSharper suppressions to code using comments. You may tweak your local ReSharper settings but do not commit these to the repo.

## Branches

There are two kinds of mainline branches, `dev` and `master`. The `dev` branch is used for development work for the next release. All new features, changes, etc. must be applied to `dev`. The `master` branch is used for stable releases. A patch to version `master` must be applied to `dev`.

## Making changes

1. [Fork](http://help.github.com/forking/) on GitHub
1. Clone your fork locally
1. Configure the `upstream` repo (`git remote add upstream git://github.com/LykkeOSS/ironclad.git`)
1. Checkout `dev` (`git checkout dev`) or, if you are working on a bug fix, checkout `master` (`git checkout master`)
1. Create a local branch (`git checkout -b my-branch`). The branch name should be descriptive, or it can just be the GitHub issue number which the work relates to, e.g. `123`.
1. Work on your change
1. Rebase if required (see 'Handling updates from `upstream`' below)
1. Test the build locally by running `build.cmd`, `build.ps1`, OR `build.sh` (depending on your platform)
1. Push the branch up to GitHub (`git push origin my-branch`)
1. Send a pull request on GitHub (see 'Sending a Pull Request' below)

You should **never** work on a clone of `dev`/`master`, and you should **never** send a pull request from `dev`/`master`. Always use a feature/patch branch. The reasons for this are detailed below.

## Handling updates from `upstream`

While you're working away in your branch it's quite possible that your `upstream` `dev`/`master` may be updated. If this happens you should:

(If you are working on patch, replace `dev` with `master` when following these steps.)

1. [Stash](http://progit.org/book/ch6-3.html) any un-committed changes you need to
1. `git checkout dev`
1. `git pull upstream dev --ff-only`
1. `git checkout my-branch`
1. `git rebase dev my-branch`
1. `git push origin dev` (optional) this keeps `dev` in your fork up to date

These steps ensure your history is "clean" i.e. you have one branch from `dev`/`master` followed by your changes in a straight line. Failing to do this ends up with several "messy" merges in your history, which we don't want. This is the reason why you should always work in a branch and you should never be working in or sending pull requests from `dev`/`master`.

If you're working on a long running feature you may want to do this quite often to reduce the risk of tricky merges later on.

## Sending a pull request

While working on your feature/patch you may well create several branches, which is fine, but before you send a pull request you should ensure that you have rebased back to a single "feature/patch branch". We care about your commits, and we care about your feature/patch branch, but we don't care about how many or which branches you created while you were working on it. :smile:

When you're ready to go you should confirm that you are up to date and rebased with `upstream` `dev`/`master` (see "Handling updates from `upstream`" above) and then:

1. `git push origin my-branch`
1. Send a descriptive [pull request](http://help.github.com/pull-requests/) on GitHub.
  - Make sure the pull request is **from** the branch on your fork **to** the `LykkeOSS/ironclad` `dev` (or `LykkeOSS/ironclad` `master` if patching).
  - If your changes relate to a JIRA, add the JIRA key to the pull request description in the format `KEY-123`.
  - If your changes relate to a GitHub issue, add the issue number to the pull request description in the format `#123`.
1. If GitHub determines that the pull request can be merged automatically, a test build will commence shortly after you raise the pull request. The build status will be reported on the pull request.
  - If the build fails, there may be a problem with your changes which you will have to fix before the pull request can be merged. Follow the link to the build server and inspect the build logs to see what caused the failure.
  - Occasionally, build failures may be due to problems on the build server rather than problems in your changes. If you determine this to be the case, please add a comment on the pull request and one of the maintainers will address the problem.

## What happens next?

The maintainers will review your pull request and provide any feedback required. If your pull request is accepted, your changes will be included in the next release.

If you contributed a new feature or a change to an existing feature then we are always very grateful to receive updates to the [documentation](https://github.com/LykkeOSS/ironclad/wiki).
# Git Workflow Rules

## 1. Start From `develop`

- Checkout `develop` first.

## 2. Create a Branch

- Use one of these prefixes:
- `feat/...` for new features
- `bug/...` for bug fixes raised by teammates
- Branch name format:
- `feat/CPCQNG-99-short-description`
- `bug/CPCQNG-99-short-description`
- `CPCQNG-99` is the Jira task code.

## 3. Code And Test

- After coding, run:
- `dotnet test`
- All tests must pass before continuing.

## 4. Sync With `develop`

- Fetch latest updates.
- If `develop` has new commits, pull and merge `develop` into your branch.

## 5. Commit

- Commit message format:
- `CPCQNG-99: detailed description`
- The commit message should be more detailed than the branch name.

## 6. Create Pull Request

- Push your branch and create a Pull Request.
- Wait for all checks/actions to pass.
- Send the PR to me for review.

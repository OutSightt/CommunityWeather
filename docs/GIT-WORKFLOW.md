# Git workflow for this submission

Use your own repository and let Git record the real commit times. Do not change commit dates or history. The graders verify the deadline from commit history, and anything pushed after the deadline counts as late.

## One-time setup (skip what you already have)
```bash
git init
git remote add origin https://github.com/<your-username>/<your-repo>.git
git add .gitignore README.md CommunityWeather.csproj
git commit -m "Add project skeleton, README and git ignore rules"
git branch -M main
git push -u origin main
```
Check that `appsettings.Local.json` does not appear in `git status`. It holds your passwords and API keys.

## One branch and pull request per feature
```bash
# 1. Registration
git checkout -b feature/user-registration
git add Models Data Services/AccountService.cs Services/PasswordHelper.cs Views/Account/Register.cshtml Program.cs
git commit -m "Add user model, password hashing and registration with duplicate-email check"
git push -u origin feature/user-registration
```
Open a pull request on GitHub, merge it, then:
```bash
git checkout main && git pull
# 2. Login and logout
git checkout -b feature/login-logout
git add Controllers/AccountController.cs Views/Account
git commit -m "Add login with lockout, secure cookie session and logout"
git push -u origin feature/login-logout
# open and merge the pull request, then
git checkout main && git pull
# 3. Role-based dashboards
git checkout -b feature/role-based-dashboards
git add Controllers Views docs
git commit -m "Add resident and administrator dashboards, role-based menus and user management"
git push -u origin feature/role-based-dashboards
# open and merge the pull request
```
Adjust the file lists so each commit holds what it describes (`git status` shows what is left). Small, honest commits are better than one big one.

Put the branch names and pull request links in section "Branches and pull requests" of the completion report.

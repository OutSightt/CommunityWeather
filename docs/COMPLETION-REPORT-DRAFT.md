# Implementation / Completion Report (draft content)

Paste each part into the matching section of the official template. Keep every template section, even if you have little to add.
Save the final file as PDF named `CommunityWeather-Singson-MarkSteven-NonBlockBSIT3-PD2.pdf` (adjust if your template says otherwise), submit it, and commit it to the `docs/` folder.

## Student and project
- Student: Mark Steven Singson
- Section: Non-Block BSIT 3
- Project: Community Weather Monitoring and Disaster Information System
- Repository: <paste GitHub link>

## Features implemented
1. **User account creation.** Residents register with full name, email and password. Fields are validated in the browser and on the server. Passwords need 8+ characters with upper case, lower case and a number. Emails are unique (database index plus an existence check, case-insensitive). Passwords are stored only as PBKDF2-SHA256 hashes with a per-user salt. New accounts get the Resident role. Administrators are created only by the seeded first admin or by another administrator.
2. **User login and logout.** Login checks the MySQL `Users` table. Invalid credentials show one generic error. Five failed attempts lock the account for 10 minutes. Disabled accounts cannot log in. The session uses a secure HttpOnly cookie that is re-checked against the database on every request. Logout ends the session. Protected pages redirect to login.
3. **Role-based dashboards.** Residents land on a resident dashboard (advisories, weather, email-alert preference, preparedness links). Community administrators land on an administrator dashboard (account and content counts, items needing attention, quick actions). Menus differ by role. Access is enforced on the backend with `[Authorize(Roles=...)]`. Administrators can promote, demote, disable and enable accounts on the Users and roles page.

Additional project features already in the repository: weather dashboard (OpenWeather), advisories, evacuation map (Google Maps), preparedness guides, email alerts (SendGrid).

## Branches and pull requests
| Branch | Purpose | Pull request link |
|---|---|---|
| feature/user-registration | Registration, hashing, duplicate prevention | <link> |
| feature/login-logout | Login, lockout, session, logout | <link> |
| feature/role-based-dashboards | Dashboards, RBAC, user management | <link> |

## Remaining tasks
- Email verification and "forgot password" flow
- Automated unit and integration tests
- Use EF Core migrations instead of `EnsureCreated`
- Replace the sample evacuation centers with verified real locations
- Complete and polish the non-account modules (advisory email delivery testing, weather and map keys on the demo machine)

## Outstanding issues and known limitations
- Changing a password does not sign out the user's other browsers.
- Lockout is per account, not per IP address.
- Weather and email features need your own API keys and show a notice without them.
- The evacuation map uses Google Maps when `GoogleMaps:ApiKey` is set. Without a key it falls back to Leaflet with OpenStreetMap. The proposal names Google Maps Platform, so confirm with the instructor that the fallback is acceptable for the demo.
- <Add any bug you find while testing.>

## Test accounts
| Role | Email | Password | How it is created |
|---|---|---|---|
| Community Administrator | value of `SeedAdmin:Email` in your `appsettings.Local.json` (default `admin@community.local`) | value of `SeedAdmin:Password` | Seeded into the database on first run, password stored hashed |
| Resident | e.g. `resident1@example.com` | <the password you registered with> | Register on the Register page before the demo |

## Test results
Copy the table from `docs/ACCOUNT-FEATURE.md` and fill in Pass or Fail for each test after you run it.

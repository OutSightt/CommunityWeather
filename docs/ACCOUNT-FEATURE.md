# User Accounts and Authentication: design and test cases

## Roles (from the SRS, section 6.3)
| Role | Constant | How it is assigned |
|---|---|---|
| Resident | `Roles.Resident` | Automatically, on public registration |
| Community Administrator | `Roles.Admin` | First one seeded from configuration; others promoted by an administrator |

Public registration can never create an administrator.

## How each requirement is met
**1. Account creation**
- Required fields and formats are validated on the client (jQuery validation) and again on the server (`ModelState`).
- Password rule: 8 to 100 characters with an uppercase letter, a lowercase letter and a number. Confirm password must match.
- Email is trimmed and lower-cased. A unique database index and an existence check prevent duplicates, including two simultaneous registrations.
- Passwords are hashed with PBKDF2-HMAC-SHA256 (600,000 iterations, random 16-byte salt per user) in `PasswordHelper`. Only the hash is stored.

**2. Login and logout**
- Credentials are checked against the `Users` table. Nothing is hardcoded or mocked.
- A wrong password and an unknown email both show "Invalid email or password." (no way to discover which emails exist), and an unknown email still costs the same hashing time.
- After 5 failed attempts the account is locked for 10 minutes. Disabled accounts cannot log in.
- Session cookie: `HttpOnly`, `SameSite=Lax`, `Secure` outside development, 8 hour sliding expiry. "Remember me" makes it persistent.
- On every request the cookie is checked against the database. A disabled account or changed role ends the session immediately.
- Logout is a POST with an anti-forgery token and deletes the cookie. Pages shown to a signed-in user are sent with `Cache-Control: no-store`, so the Back button cannot reopen them after logout.
- All POST actions require an anti-forgery token. Return URLs must be local (no open redirect).

**3. Role-based dashboards**
- After login, residents go to `/Resident` and administrators go to `/Admin`. The **Dashboard** menu link does the same.
- Navigation: everyone sees Weather, Advisories, Evacuation map and Prepare. Signed-in users also see Dashboard and their profile. Only administrators see the **Manage** menu.
- Backend enforcement: `[Authorize(Roles = Roles.Admin)]` on `AdminController`, `AdvisoriesController` manage actions, `CentersController` manage actions and `GuidesController` manage actions. `[Authorize(Roles = Roles.Resident)]` on `ResidentController`. Hiding a menu item is never the only protection.
- Wrong role gets the **Access denied** page. Not signed in gets redirected to Login and returned to the page after login.

## Manual test cases
Mark each Pass or Fail when you demonstrate it.

| # | Test | Expected result |
|---|---|---|
| 1 | Register with empty fields | Field errors shown, no account created |
| 2 | Register with password `abc` | Password rule error |
| 3 | Register with mismatched confirm password | Error on confirm password |
| 4 | Register a new resident | Account created, signed in, lands on Resident dashboard |
| 5 | Register again with the same email (also try different letter case) | "An account with this email already exists." |
| 6 | In MySQL Workbench: `SELECT Email, Role, PasswordHash FROM Users;` | Hash starts with `v1.600000.`, no plain password, new user has role Resident |
| 7 | Log in with wrong password | "Invalid email or password." |
| 8 | Log in with an email that does not exist | The same message |
| 9 | Fail login 5 times | "Too many failed attempts. Try again in 10 minute(s)." Correct password also refused until it ends |
| 10 | Log in as resident | Redirected to Resident dashboard |
| 11 | Log in as administrator | Redirected to Administrator dashboard |
| 12 | Resident opens `/Admin` and `/Advisories/Manage` | Access denied page |
| 13 | Administrator opens `/Resident` | Access denied page |
| 14 | Logged out, open `/Admin` | Redirected to Login, then to `/Admin` after logging in as admin |
| 15 | Resident menu | No Manage menu |
| 16 | Administrator menu | Manage menu with Users and roles, Advisories, Locations, Guides |
| 17 | Log out, then press the browser Back button | Login page or a reload, not the dashboard |
| 18 | Admin makes a resident an administrator while that resident is logged in | Resident's next click requires logging in again, then they land on the admin dashboard |
| 19 | Admin disables an account while it is logged in | Next click signs them out. Login shows "This account has been disabled." |
| 20 | Admin tries to change own role or disable self | Blocked with a message |
| 21 | Change password with wrong current password | Error. With correct one it succeeds, and the old password no longer works |

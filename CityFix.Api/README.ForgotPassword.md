# Forgot Password Flow

This website now supports password reset by email verification code.

## Website pages

- `wwwroot/LogIn/forgot-password.html`
  - User enters email and requests verification code.
- `wwwroot/LogIn/reset-password.html`
  - User enters email, 6-digit code, and new password.

The login pages now link to the forgot-password page:

- `wwwroot/LogIn/customer-login.html`
- `wwwroot/LogIn/worker-login.html`
- `wwwroot/LogIn/admin-login.html`

## API endpoints

- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`

## Security behavior

- Generic response on forgot-password (prevents account enumeration).
- 6-digit code, valid for 10 minutes.
- Code stored as hash (not plain text).
- Code becomes single-use after success.

## Email configuration

Configure SMTP in `appsettings.Development.json` or `appsettings.json`:

```json
"EmailSettings": {
  "Enabled": false,
  "Host": "smtp.gmail.com",
  "Port": 587,
  "UseSsl": true,
  "UserName": "",
  "Password": "",
  "FromEmail": "",
  "FromName": "CityFix"
}
```

- `Enabled: false` => email is logged to server console for development.
- `Enabled: true` => SMTP sends real email.

## Database

Apply migrations:

```powershell
dotnet ef database update
```





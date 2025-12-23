# License Manager Web Configuration

Please create a file named `.env.local` in the root of the `license-manager-web` directory and add the following content:

```env
# MongoDB Connection String
MONGODB_URI=mongodb://localhost:27017/license_manager

# JWT Secret for signing tokens
JWT_SECRET=super_secret_key_for_jwt_auth_12345

# Initial Admin Credentials
ADMIN_EMAIL=admin@inventory.local
ADMIN_PASSWORD=admin123
```

## How to Run
1. Install dependencies: `npm install`
2. Start development server: `npm run dev`
3. Access at `http://localhost:3000`

## Features
- **Authentication**: JWT-based login/register.
- **Licenses**: Request 1-month or 1-year plans.
- **Admin**: Revoke/Manage all licenses.
- **Integrate with Desktop**: Use `POST /api/validate` with `licenseKey` and `deviceFingerprint`.

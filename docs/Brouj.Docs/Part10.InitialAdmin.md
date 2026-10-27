# Initial administrator configuration

Initial administrator provisioning is disabled unless `InitialAdmin:Enabled` is explicitly set to `true`.

For this development project, all bootstrap settings belong in the API project's existing **Manage User Secrets** file, outside the repository. The API already declares a `UserSecretsId`; `WebApplication.CreateBuilder` loads those secrets in Development. Production uses environment variables or a deployment secret provider instead. See [Microsoft's user-secrets guidance](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0).

Add this section alongside the existing settings, preserving the rest of the secrets file:

```json
{
  "InitialAdmin": {
    "Enabled": false,
    "Id": "<new stable GUID>",
    "Name": "<administrator name>",
    "Email": "<administrator email>",
    "Phone": "<administrator phone>",
    "Role": "SuperAdmin",
    "Password": "<new private password>"
  }
}
```

Replace every placeholder before enabling provisioning. Name, email, phone and password follow the existing staff-account validation rules, including the approved phone format and minimum password length. `Id` must be non-empty. `Role` is the C# configuration enum name `SuperAdmin`; persistence continues using the approved database string converter. A role other than SuperAdmin is rejected for this bootstrap operation.

The configured ID must remain the same across instances sharing the same empty database. SQL Server locks the configured ID before rechecking the users table. Concurrent startup creates at most one account. Password hashing occurs before the short transaction, and only the hash is persisted.

Provisioning runs before the API accepts traffic. When disabled it performs no database access. When any staff row already exists, including a soft-deleted account, it does not add, reset or restore an account. After the first administrator is provisioned, disable `InitialAdmin:Enabled` and use the existing authenticated account-management flows.

A missing/invalid enabled configuration or database failure stops startup with a fixed, sanitized error and credential-free `ILogger<UsersSeeding>` logs. The seeder does not apply migrations or grant SQL privileges.

The former password was present in Git history and must not be reused. Moving configuration does not change a password on an existing local account; change that account's password through the approved flow if it was created by the old seeder. Repository history has not been rewritten.

User-secrets is a development convenience, not an encrypted production secret vault. Do not copy this file into the project or deployment output. The normal application reads configuration through `IOptions<InitialAdminOptions>` and never depends on the physical secrets-file path.

The local secret update performed for Part 10 is recorded in the security report without exposing its values. Tests use their own synthetic configuration and explicitly disable local bootstrap settings in the HTTP test host.

Create an admin or superadmin user. Arguments: $ARGUMENTS

Parse $ARGUMENTS as: `<email> <username> [--super]`

Examples of valid input:
- `admin@example.com john` → creates a regular Admin
- `admin@example.com john --super` → creates a SuperAdmin

**Step 1 — Validate arguments**
If email or username is missing, stop and ask the user to provide them in the format:
`/create-admin <email> <username>` or `/create-admin <email> <username> --super`

**Step 2 — Run the command**
If `--super` is present in the arguments, run:
```
dotnet run --project src/Cli -- create-admin <email> <username> --super
```

Otherwise run:
```
dotnet run --project src/Cli -- create-admin <email> <username>
```

**Step 3 — Show the result**
The CLI prints a generated password once. Show it to the user prominently and remind them:
> Save this password immediately — it is shown only once.

If the command fails (e.g. email already exists), show the error clearly.

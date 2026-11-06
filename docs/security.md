# Security

- ASP.NET Core Identity password hashing (PBKDF2)
- JWT bearer auth; issuer/audience/signing-key validated
- Role-based authorization: Admin vs User policies on mutating endpoints
- Rate limiting per user/IP
- API keys stored as SHA-256 hashes; raw key shown once
- Simulator endpoints restricted to Admin and intended for development only
- Correlation IDs on every request; audit logs for sensitive actions
- No secrets in source — all via environment variables / .env

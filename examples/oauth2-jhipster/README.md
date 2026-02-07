# OAuth2/XOAUTH2 Authentication Demo with JHipster Registry

This example demonstrates end-to-end OAuth2/XOAUTH2 authentication using JHipster Registry as the Identity Provider (IDP).

## Overview

JHipster Registry is a service registry and configuration server that includes an embedded OAuth2/OpenID Connect server (using Spring Security OAuth2). This demo shows how to configure smtp4dev to validate XOAUTH2 tokens against JHipster Registry.

## Prerequisites

- Docker and Docker Compose
- Python 3.7+ (for test script)
- curl or similar HTTP client

## Quick Start

### 1. Start the Services

```bash
docker-compose up -d
```

This starts:
- **JHipster Registry** on http://localhost:8761 (OAuth2/OIDC provider)
- **smtp4dev** on http://localhost:5000 (web UI) with SMTP on port 2525

### 2. Wait for Services to Start

```bash
# Wait for JHipster Registry to be ready (may take 30-60 seconds)
until curl -sf http://localhost:8761/management/health > /dev/null; do
    echo "Waiting for JHipster Registry..."
    sleep 5
done
echo "JHipster Registry is ready!"

# Check smtp4dev is ready
until curl -sf http://localhost:5000/api/server > /dev/null; do
    echo "Waiting for smtp4dev..."
    sleep 2
done
echo "smtp4dev is ready!"
```

### 3. Run the Test Script

```bash
python3 test_oauth2.py
```

This script will:
1. Obtain an OAuth2 access token from JHipster Registry
2. Authenticate to smtp4dev SMTP server using XOAUTH2 with the token
3. Send a test email
4. Verify the email was received

### 4. View Results

- **Web UI**: http://localhost:5000 - See received emails
- **JHipster Registry**: http://localhost:8761 - Login with admin/admin

## Configuration Details

### JHipster Registry OAuth2 Configuration

JHipster Registry comes pre-configured with OAuth2 support:

- **Client ID**: `internal`
- **Client Secret**: `internal` 
- **Token Endpoint**: `http://localhost:8761/oauth/token`
- **JWKS Endpoint**: `http://localhost:8761/.well-known/jwks.json`
- **Issuer**: `http://localhost:8761`

Default users:
- **Username**: `admin`
- **Password**: `admin`

### smtp4dev Configuration

The smtp4dev container is configured with:

```yaml
environment:
  # Require authentication
  - ServerOptions__AuthenticationRequired=true
  # Don't allow any credentials (require OAuth2 validation)
  - ServerOptions__SmtpAllowAnyCredentials=false
  # OAuth2 Authority URL for JHipster Registry
  - ServerOptions__OAuth2Authority=http://jhipster-registry:8761
  # OAuth2 Audience (optional, JHipster doesn't enforce this)
  - ServerOptions__OAuth2Audience=internal
  # Enable XOAUTH2 authentication type
  - ServerOptions__SmtpEnabledAuthTypesWhenNotSecureConnection=XOAUTH2
  # Configure allowed user (matches the 'sub' claim in JWT)
  - ServerOptions__Users__0__Username=admin
  - ServerOptions__Users__0__Password=not-used-for-oauth2
```

## How It Works

### Authentication Flow Diagram

```
┌─────────────┐                 ┌──────────────────┐                ┌─────────────┐
│   Client    │                 │    JHipster      │                │  smtp4dev   │
│  (Test      │                 │    Registry      │                │   Server    │
│   Script)   │                 │  (OAuth2 IDP)    │                │             │
└──────┬──────┘                 └────────┬─────────┘                └──────┬──────┘
       │                                 │                                  │
       │ 1. Request Access Token         │                                  │
       │   POST /oauth/token             │                                  │
       │   (username=admin, password=admin)                                 │
       │────────────────────────────────>│                                  │
       │                                 │                                  │
       │ 2. Return Access Token (JWT)    │                                  │
       │<────────────────────────────────│                                  │
       │                                 │                                  │
       │ 3. Connect to SMTP              │                                  │
       │────────────────────────────────────────────────────────────────────>│
       │                                 │                                  │
       │ 4. AUTH XOAUTH2                 │                                  │
       │────────────────────────────────────────────────────────────────────>│
       │                                 │                                  │
       │ 5. Send XOAUTH2 credentials     │                                  │
       │    (username + JWT token)       │                                  │
       │────────────────────────────────────────────────────────────────────>│
       │                                 │                                  │
       │                                 │ 6. Fetch JWKS (public keys)      │
       │                                 │<─────────────────────────────────│
       │                                 │                                  │
       │                                 │ 7. Return JWKS                   │
       │                                 │──────────────────────────────────>│
       │                                 │                                  │
       │                                 │          8. Validate Token:      │
       │                                 │             - Verify signature   │
       │                                 │             - Check expiration   │
       │                                 │             - Validate issuer    │
       │                                 │             - Extract subject    │
       │                                 │             - Match username     │
       │                                 │             - Check Users list   │
       │                                 │                                  │
       │ 9. Authentication Success       │                                  │
       │<────────────────────────────────────────────────────────────────────│
       │                                 │                                  │
       │ 10. Send Email                  │                                  │
       │────────────────────────────────────────────────────────────────────>│
       │                                 │                                  │
       │ 11. Email Accepted              │                                  │
       │<────────────────────────────────────────────────────────────────────│
       │                                 │                                  │
```

### 1. Token Acquisition

The client obtains an OAuth2 access token from JHipster Registry using the OAuth2 password grant flow:

```bash
curl -X POST http://localhost:8761/oauth/token \
  -H "Authorization: Basic $(echo -n 'internal:internal' | base64)" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=admin&password=admin"
```

Response includes an access token (JWT):
```json
{
  "access_token": "eyJhbGci...",
  "token_type": "bearer",
  "expires_in": 3600,
  "scope": "read write"
}
```

### 2. XOAUTH2 Authentication

The client connects to smtp4dev SMTP server and authenticates using XOAUTH2:

```
C: AUTH XOAUTH2
S: 334
C: <base64-encoded: user=admin\x01auth=Bearer eyJhbGci...\x01\x01>
S: 235 2.7.0 Authentication successful
```

### 3. Token Validation

smtp4dev validates the token:

1. **Fetches JWKS**: Gets public keys from `http://jhipster-registry:8761/.well-known/jwks.json`
2. **Validates Signature**: Verifies the token was signed by JHipster Registry
3. **Validates Expiration**: Checks token hasn't expired
4. **Validates Issuer**: Verifies issuer matches the authority
5. **Extracts Subject**: Gets the `sub` claim from the token (e.g., "admin")
6. **Checks Username**: Verifies the username in XOAUTH2 matches the subject claim
7. **Checks User List**: Verifies the username is in the configured Users list

If all validations pass, authentication succeeds.

## Testing Different Scenarios

### Valid Authentication (Should Succeed)

```bash
# Get token and authenticate
python3 test_oauth2.py
```

### Invalid Token (Should Fail)

```python
# In test_oauth2.py, modify the token to be invalid
access_token = "invalid-token-here"
```

Expected error: "Token validation error"

### Wrong Username (Should Fail)

```python
# In test_oauth2.py, change username to something else
username = "wronguser"
```

Expected error: "subject mismatch"

### User Not in List (Should Fail)

Remove the user from docker-compose.yml:
```yaml
# Comment out this line:
# - ServerOptions__Users__0__Username=admin
```

Restart and test.

Expected error: "username not in configured users list"

## Troubleshooting

### JHipster Registry Not Starting

Check logs:
```bash
docker-compose logs jhipster-registry
```

JHipster Registry can take 30-60 seconds to start fully.

### Token Validation Fails

Check smtp4dev logs:
```bash
docker-compose logs smtp4dev
```

Look for OAuth2 authentication messages.

### Connection Refused

Ensure services are running:
```bash
docker-compose ps
```

All services should show "Up" status.

## Cleanup

```bash
# Stop and remove containers
docker-compose down

# Remove volumes (optional)
docker-compose down -v
```

## Advanced Configuration

### Using Different Users

1. Add users in JHipster Registry (see JHipster documentation)
2. Add corresponding users to smtp4dev configuration:

```yaml
environment:
  - ServerOptions__Users__0__Username=admin
  - ServerOptions__Users__0__Password=not-used
  - ServerOptions__Users__1__Username=user
  - ServerOptions__Users__1__Password=not-used
```

### Using Client Credentials Grant

Modify the test script to use client credentials instead of password grant:

```python
token_data = {
    'grant_type': 'client_credentials',
    'scope': 'read write'
}
```

Note: The subject claim will be different (client ID instead of username).

### Production Considerations

For production use:

1. **Use HTTPS**: Configure TLS for both JHipster Registry and smtp4dev
2. **Strong Secrets**: Change default client secrets and passwords
3. **Network Isolation**: Use Docker networks to isolate services
4. **Token Expiration**: Configure appropriate token lifetimes
5. **Audience Validation**: Configure specific audience values
6. **Logging**: Monitor authentication logs for security events

## References

- [JHipster Registry Documentation](https://www.jhipster.tech/jhipster-registry/)
- [OAuth2 RFC](https://tools.ietf.org/html/rfc6749)
- [XOAUTH2 SASL Mechanism](https://developers.google.com/gmail/imap/xoauth2-protocol)
- [smtp4dev OAuth2 Configuration Documentation](../../docs/Configuration.md#oauth2xoauth2-authentication)

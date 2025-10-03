# Docker Security Guidelines for smtp4dev

## Security Warning: Prevent Accidental Public Exposure

**IMPORTANT**: When running smtp4dev in Docker, you must be careful about how you publish ports to prevent accidental public exposure of your development email server.

## The Problem

By default, Docker's port publishing mechanism (`-p` flag or `ports:` in docker-compose) makes published ports accessible not only to the Docker host but potentially to the outside world as well. This can accidentally expose your smtp4dev instance publicly, allowing unauthorized access to intercepted emails and test configurations.

## Secure Port Publishing

### Docker Command Line

❌ **INSECURE** (exposes to all interfaces):
```bash
docker run -p 5000:80 -p 25:25 -p 143:143 -p 110:110 rnwood/smtp4dev:v3
```

✅ **SECURE** (localhost only):
```bash
docker run -p 127.0.0.1:5000:80 -p 127.0.0.1:25:25 -p 127.0.0.1:143:143 -p 127.0.0.1:110:110 rnwood/smtp4dev:v3
```

### Docker Compose

❌ **INSECURE** docker-compose.yml:
```yaml
services:
  smtp4dev:
    image: rnwood/smtp4dev:v3
    ports:
      - '5000:80'    # Exposed to all interfaces
      - '25:25'      # Exposed to all interfaces  
      - '143:143'    # Exposed to all interfaces
      - '110:110'    # POP3 plain (exposed to all interfaces)
```

✅ **SECURE** docker-compose.yml:
```yaml
services:
  smtp4dev:
    image: rnwood/smtp4dev:v3
    ports:
      - '127.0.0.1:5000:80'    # Localhost only
      - '127.0.0.1:25:25'      # Localhost only
      - '127.0.0.1:143:143'    # Localhost only
      - '127.0.0.1:110:110'    # POP3 plain (localhost only)
```

## How It Works

When you specify `127.0.0.1:` (or `::1:` for IPv6) as the bind address when publishing ports:
- The service is only accessible from the Docker host machine itself
- External machines cannot connect to the published ports
- Your development email server remains private

Without the bind address specification:
- Docker binds to all available interfaces (`0.0.0.0`)
- The service becomes accessible from any machine that can reach your Docker host
- This can lead to accidental public exposure

## Example: Secure Setup

Here's a complete secure docker-compose.yml example:

```yaml
version: '3'

services:
  smtp4dev:
    image: rnwood/smtp4dev:v3
    restart: always
    ports:
      - '127.0.0.1:5000:80'    # Web interface
      - '127.0.0.1:2525:25'    # SMTP server (using non-standard port)
      - '127.0.0.1:1143:143'   # IMAP server (using non-standard port)
      - '127.0.0.1:1110:110'   # POP3 server (using non-standard port)
    volumes:
      - smtp4dev-data:/smtp4dev
    environment:
      # Container configuration (this binds within the container)
      - ServerOptions__Urls=http://*:80
      - ServerOptions__HostName=smtp4dev

volumes:
  smtp4dev-data:
```

## When You Might Need Public Access

If you're running smtp4dev in a development environment where you need to access it from other machines (e.g., testing from mobile devices, other VMs), you have several safer options:

### Option 1: Specific Network Interface
Bind to a specific internal network interface instead of all interfaces:
```bash
docker run -p 192.168.1.100:5000:80 rnwood/smtp4dev:v3
```

### Option 2: Reverse Proxy
Use a reverse proxy (nginx, traefik) with proper authentication and SSL termination.

### Option 3: Network Restrictions
Use Docker networks and firewall rules to control access:
```yaml
services:
  smtp4dev:
    image: rnwood/smtp4dev:v3
    networks:
      - internal
    ports:
      - '5000:80'  # Only accessible within the Docker network

networks:
  internal:
    driver: bridge
    internal: true  # No external access
```

## Additional Security Considerations

1. **Use Non-Standard Ports**: Consider using non-standard ports (e.g., 2525 for SMTP instead of 25)
2. **Firewall Rules**: Implement host-level firewall rules as an additional layer
3. **Container Isolation**: Run smtp4dev in isolated Docker networks when possible
4. **Regular Updates**: Keep your smtp4dev Docker images updated
5. **Monitor Access**: Log and monitor access to your development tools

## Reference

For more information about Docker port publishing security, see:
- [Docker Documentation: Port Publishing](https://docs.docker.com/network/porting/)
- [Docker Security Best Practices](https://docs.docker.com/develop/security-best-practices/)

## Summary

**Always use `127.0.0.1:` prefix when publishing Docker ports for smtp4dev unless you specifically need and understand the implications of external access.**
# Fake gateways (Konzulta SMS and Targito e-mail)

Applications that send SMS or transactional e-mail through external HTTP gateways are hard to test end to end
without real accounts. smtp4dev can imitate two such gateways locally:

- **Konzulta** - SMS through an HTTP XML API.
- **Targito** - transactional e-mail through a JSON-RPC 2.0 API.

The application under test is pointed at smtp4dev only by changing the gateway URL in its configuration.
Every received request is kept in memory (not in the database) and can be read through an HTTP API, so an automated
test can fetch an SMS verification code or a confirmation link. Passwords sent by the application are never stored or logged.

## Enabling

The feature is off by default. Enable it with one of:

```bash
smtp4dev --fakegateways
# or
FakeGateways__Enabled=true smtp4dev
# or in appsettings.json next to the binary
"FakeGateways": { "Enabled": true }
```

`--FakeGateways:Enabled=true` is **not** a valid command line argument. Do not place the `FakeGateways` section in the
user settings file (`{AppData}/smtp4dev/appsettings.json`) - smtp4dev rewrites that file when settings are saved in the web UI.

Other settings (see `appsettings.json`): `MaxStoredItems` (default 1000 per gateway), `Konzulta:RefuseNumbers`
(glob patterns of phone numbers answered with a refusal, e.g. `+42070000*`), `Targito:UpstreamUrl` (forward methods that are
not simulated to a real server) and `Targito:TemplateNames` (templateId to name).

When disabled, the fake endpoints and `/api/fakegateways/*` return 404 (except `/api/fakegateways/health`).

## Fake endpoints

These are anonymous (no web authentication needed) and are served below `ServerOptions:BasePath`:
with `BasePath` `/smtp4dev` the URLs are `/smtp4dev/konzulta/...` and `/smtp4dev/targito/JsonRpc`.

| Gateway | Request |
|---|---|
| Konzulta | `POST /konzulta/webservices/webservice.aspx` (any `POST /konzulta/...` is accepted), body `text/xml` |
| Targito | `POST /targito/JsonRpc`, body JSON-RPC 2.0 |

Konzulta always answers `200` with `Status=0` per `DataItem` (`Status=1` for refused numbers or incomplete items) and `400` for invalid XML.
Targito always answers HTTP `200`; `SendMailingToRecipient` returns `mailingId`, `ChangeEmailAddress` returns `recipientId`,
other methods return JSON-RPC error `-32601` unless `UpstreamUrl` is configured. An empty template list is never returned.

## API

The API uses the same web authentication as the rest of the smtp4dev API. Times are ISO-8601 UTC with milliseconds
(`2026-11-17T10:15:30.123Z`). It is documented in the Swagger UI at `/api`.

| Request | Purpose |
|---|---|
| `GET /api/fakegateways/health` | `{status: "ok"\|"disabled", features, version}`, always available and anonymous |
| `GET /api/fakegateways/sms?to=&since=&textContains=&codeRegex=` | list SMS, newest first |
| `GET /api/fakegateways/sms/wait?to=&since=&textContains=&codeRegex=&timeoutSeconds=` | wait for the oldest matching SMS (`since` is required, timeout 1-120 s, default 30; `404 {"error":"timeout"}` on timeout) |
| `GET /api/fakegateways/sms/{id}`, `DELETE /api/fakegateways/sms/{id}` | get / delete one |
| `DELETE /api/fakegateways/sms?to=` | delete matching SMS (all without `to`) |
| `GET /api/fakegateways/targito?email=&templateId=&method=&dataKey=&since=` | list Targito calls, newest first |
| `GET /api/fakegateways/targito/wait?...&timeoutSeconds=` | wait (method defaults to `SendMailingToRecipient`) |
| `GET/DELETE /api/fakegateways/targito/{id}`, `DELETE /api/fakegateways/targito?email=` | get / delete |

Phone numbers are normalised for comparison (spaces removed, leading `00` becomes `+`); e-mails are compared case-insensitively.
`codeRegex` is a .NET regex with a 1 second timeout; group 1 (or the whole match) is returned in the `code` field.
Invalid parameters give `400 {"error": "..."}`.

## Examples

```bash
since=$(date -u +%Y-%m-%dT%H:%M:%S.000Z)

# Konzulta: send an SMS, then read the code
curl -s -X POST http://localhost:5000/konzulta/webservices/webservice.aspx -H 'Content-Type: text/xml; charset=utf-8' --data-binary @- <<'XML'
<?xml version="1.0" encoding="utf-8" ?>
<SmsServices><DataHeader><DataType>SMS</DataType><UserName>api-user</UserName><Password>secret</Password></DataHeader>
<DataArray><DataTemplate><LongSms>1</LongSms><DataItem><MobileTerminate>+420700000001</MobileTerminate>
<Text>your code: 123456.</Text><SmsId>1</SmsId></DataItem></DataTemplate></DataArray></SmsServices>
XML
curl -s -G http://localhost:5000/api/fakegateways/sms/wait --data-urlencode 'to=+420700000001' \
  --data-urlencode "since=$since" --data-urlencode 'codeRegex=(\d{6})' --data-urlencode 'timeoutSeconds=5'

# Targito: send an e-mail, then read the confirmation link
curl -s -X POST http://localhost:5000/targito/JsonRpc -H 'Content-Type: application/json' \
  -d '{"method":"SendMailingToRecipient","auth":{"username":"api-user","password":"secret"},"id":1,"jsonrpc":"2.0","params":{"email":"a@example.com","templateId":"t1","data":{"CONFIRM_URL":"https://app.example.com/verify?code=C"}},"sender_domain":"example.cz"}'
curl -s -G http://localhost:5000/api/fakegateways/targito/wait --data-urlencode 'email=a@example.com' \
  --data-urlencode "since=$since" --data-urlencode 'dataKey=CONFIRM_URL' --data-urlencode 'timeoutSeconds=5'
```

To run smtp4dev on a developer machine with everything on non-privileged ports:
`smtp4dev --urls=http://localhost:5000 --smtpport=2525 --imapport= --pop3port= --fakegateways`.

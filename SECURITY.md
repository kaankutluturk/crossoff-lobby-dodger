# Security

Report vulnerabilities privately through GitHub's security-advisory feature when available. Do not include tokens, private evidence, or personal information in a public issue.

The intended trust boundary is narrow:

- screenshots are processed in memory and discarded;
- recognized OCR text is shown transiently and is not persisted;
- the only network request is an HTTPS GET for the configured blacklist JSON;
- the client keeps one cached blacklist and local UI settings under `%LocalAppData%\CrossOffLobbyDodger`;
- optional automation first displays a non-activating warning, emits an Escape key-down/key-up pair, waits 400 ms, verifies that Dead by Daylight remains foreground, and then emits an Enter key-down/key-up pair through the standard Windows `SendInput` API.

Any change that accesses game memory, injects code, installs a driver, captures network traffic, or uploads screen/name data is outside the project's scope.

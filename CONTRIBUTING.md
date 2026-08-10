# Contributing

## Blacklist submissions

Use the **Blacklist submission** issue form. Include the exact visible alias, the associated group label, a concise factual reason, and a durable evidence link or attachment.

Maintainers should:

1. Verify that the evidence shows the submitted alias.
2. Check for duplicate or conflicting records.
3. Avoid treating a shared/duplicate display name as stable identity.
4. Ask a second staff member to review when practical.
5. Add or update the entry in `blacklist/blacklist.json` only after approval.
6. Set `updatedAt` to the approval time in UTC.
7. Preserve corrections in Git history rather than silently rewriting context.

An accepted entry should look like:

```json
{
  "id": "group-player-20260810",
  "group": "Example group",
  "aliases": ["Exact Visible Name"],
  "reason": "Concise, evidence-backed description",
  "evidenceUrl": "https://example.invalid/evidence",
  "addedAt": "2026-08-10T15:00:00Z",
  "active": true
}
```

Do not add protected personal information, real-world identities, addresses, private messages without consent, or allegations unsupported by the linked evidence.

## Appeals and corrections

Open an issue with the existing entry ID and the correction or appeal evidence. Maintainers can deactivate an entry by setting `active` to `false`; deletion should be reserved for records that should not have been published at all.

## Code changes

Keep the client screen-only and local-first. Changes must not add process-memory access, injection, kernel drivers, packet inspection, game-file modification, account automation, or screenshot/name telemetry.

# API Deviations

Coil.NET's technical specification (`doc/11 SDK – Technische Spezifikation.md`) describes endpoints, scopes, and protocol details as they were understood at planning time. Tesla changes the Fleet API's hosts, scopes, and payloads without much notice, so every endpoint, scope, and protocol detail is re-verified against [developer.tesla.com](https://developer.tesla.com) (and, for the Vehicle Command Protocol, [`teslamotors/vehicle-command`](https://github.com/teslamotors/vehicle-command)) before it ships in code.

This document is the record of what changed: whenever verification turns up a difference between what the spec assumed and what the real API actually requires, it gets logged here — with a link back to the source that confirmed it. Treat it as the changelog between "what we planned" and "what we shipped," and check it before trusting a hostname, scope name, or field from the spec doc at face value.

## Deviations

| Date | Area | Spec said | Actual | Source |
|---|---|---|---|---|
| _(none yet)_ | | | | |

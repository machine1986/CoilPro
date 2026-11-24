# Security Policy

CoilPro handles OAuth tokens and signs commands that can control a vehicle, so security reports are taken seriously.

## Reporting a vulnerability

Please report vulnerabilities **privately** through GitHub security advisories:
**Security tab → "Report a vulnerability"** in [github.com/machine1986/CoilPro](https://github.com/machine1986/CoilPro).
Do not open a public issue for a security problem.

Include the affected version, a description of the impact, and steps to reproduce.

**Never include private keys, client secrets, access or refresh tokens, or real VINs in an issue, advisory or
log excerpt** — redact them. If you believe a private key was exposed, rotate it (generate a new key pair,
re-register the domain, re-pair the virtual key) before reporting.

## Supported versions

CoilPro is in preview; only the latest published preview receives fixes.

## Scope

In scope: this repository's code and packages (`CoilPro`, `CoilPro.Commands`, `CoilPro.AspNetCore`, `CoilPro.Cli`).
Issues in Tesla's services or vehicles should be reported to Tesla; CoilPro is unofficial and not affiliated
with Tesla, Inc.

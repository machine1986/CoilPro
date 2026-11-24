# Third-party notices

CoilPro is licensed under the MIT License (see `LICENSE`). It includes material from the following
third-party project.

## teslamotors/vehicle-command

- Source: https://github.com/teslamotors/vehicle-command
- Commit: `0a582da430c4aca6d397fb6cbed5b4d6bb380125` (2026-11-03)
- License: Apache License 2.0 — full text in `src/CoilPro.Commands/Protos/LICENSE`, shipped in the
  `CoilPro.Commands` and `CoilPro.Cli` packages as `licenses/vehicle-command/LICENSE`.
- Used in CoilPro:
  - The protobuf definitions in `src/CoilPro.Commands/Protos/*.proto`, copied unmodified.
  - The Vehicle Command Protocol implementation in `src/CoilPro.Commands` is a C# port of the protocol as
    implemented and documented in that repository (`pkg/protocol/protocol.md`, `internal/authentication`,
    `internal/dispatcher`). Ported source files say so in their header.

CoilPro is not affiliated with, endorsed by, or sponsored by Tesla, Inc.

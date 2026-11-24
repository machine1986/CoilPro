# Vendored protobuf definitions

The `.proto` files in this folder are copied **unmodified** from
[teslamotors/vehicle-command](https://github.com/teslamotors/vehicle-command), directory
`pkg/protocol/protobuf/`, commit `0a582da430c4aca6d397fb6cbed5b4d6bb380125` (2026-11-03).

They are licensed under the Apache License 2.0 — see [`LICENSE`](./LICENSE) in this folder. The upstream
repository ships no `NOTICE` file. CoilPro itself is MIT-licensed; see `THIRD-PARTY-NOTICES.md` in the
repository root.

C# code is generated at build time by `Grpc.Tools` (`GrpcServices="None"`, `Access="Internal"`), so the
generated types are internal to `CoilPro.Commands`.

To update: copy the files from a newer upstream commit, update the commit hash above, rebuild, and run the
test vectors in `tests/CoilPro.Commands.Tests`. If a file ever has to be modified, Apache-2.0 §4(b) requires a
prominent change notice in that file.

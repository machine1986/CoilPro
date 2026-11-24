# M3 — Vehicle Command Protocol: research notes

Source of truth: [`teslamotors/vehicle-command`](https://github.com/teslamotors/vehicle-command) @ `0a582da` (2026-11-03, Apache-2.0), especially `pkg/protocol/protocol.md`, `internal/authentication/`, `internal/dispatcher/`, `pkg/connector/inet/`, `pkg/vehicle/`. File:line references below point into that commit. Research date: 2026-11-24. Target vehicle for live tests: EU Model Y, firmware 2026.38.6, `vehicle_command_protocol_required: true`, virtual key paired (`fleet_status`).

All crypto steps below were recomputed in C# with `System.Security.Cryptography` only (.NET 10, macOS) against the published test vectors — byte-identical. No BouncyCastle needed.

## 1. Transport

- `POST /api/1/vehicles/{VIN}/signed_command` — **VIN**, not the numeric id (`inet.go:254`; the proxy rejects non-17-char ids).
- Request body `{"routable_message":"<standard padded base64 of RoutableMessage>"}`; response `{"response":"<base64 RoutableMessage>"}` (`inet.go:250-268`, `protocol.md:126-136`). Response size cap 100 000 bytes.
- **HTTP 200 does not mean the command succeeded** — decode the inner message.
- Status handling (`inet.go:123-158`):

| Status | Meaning | Action |
|---|---|---|
| 422 | vehicle does not support the protocol | fail (REST fallback is irrelevant for our car) |
| 503, or 408 with "vehicle is offline" | asleep | `VehicleAsleep` error; optional wake-and-poll |
| 408 other | temporary | retry same bytes |
| 421 | wrong region | switch to host named in body (`use base URL: https://…`, only `*.tesla.com`/`.cn`/`teslamotors.com`) |
| other 5xx | may have executed | **never resend** |

- The session-info handshake also goes through `signed_command`, unsigned.
- Wake is REST only (`POST …/wake_up`); the in-car wake action is BLE-only. Go never polls for `online`.
- Commands to one VIN must be serialized (VCSEC needs strictly ordered counters).

## 2. Domains and envelope

- Domain 2 = VCSEC (payload `VCSEC.UnsignedMessage`, reply `VCSEC.FromVCSECMessage`), domain 3 = Infotainment (payload `CarServer.Action`, reply `CarServer.Response`).
- Envelope (`vehicle.go:181-199`): `RoutableMessage{ to_destination{domain}, from_destination{routing_address = 16 random bytes}, protobuf_message_as_bytes, uuid (16 random), flags = 2 (FLAG_ENCRYPT_RESPONSE), signature_data }`. Routing address: new per message for VCSEC, one per connection for Infotainment.
- Handshake only the domain a command needs (each handshake is a billable call).

Starter command set:

| Command | Domain | Protobuf path |
|---|---|---|
| Lock / Unlock | VCSEC | `UnsignedMessage.RKEAction = RKE_ACTION_LOCK(1)` / `UNLOCK(0)` (serializes as `10 00`) |
| Honk / Flash lights | Infotainment | `Action.vehicleAction.vehicleControlHonkHornAction(27){}` / `vehicleControlFlashLightsAction(26){}` |
| Climate on/off | Infotainment | `vehicleAction.hvacAutoAction(10){power_on}` |
| Set temperature | Infotainment | `vehicleAction.hvacTemperatureAdjustmentAction(14){driver_temp_celsius, passenger_temp_celsius, level = TEMP_MAX}` (Go always sets `TEMP_MAX`) |
| Charge start / stop | Infotainment | `vehicleAction.chargingStartStopAction(6){start(2)}` / `{stop(5)}` |
| Set charge limit | Infotainment | `vehicleAction.chargingSetLimitAction(5){percent}` |
| Open charge port | Infotainment | `vehicleAction.chargePortDoorOpen(62){}` |
| Set charging amps | Infotainment | `vehicleAction.setChargingAmpsAction(43){charging_amps}` |

## 3. Session handshake

- Request: `RoutableMessage{ to_destination.domain, from_destination.routing_address, uuid, session_info_request{ public_key = our 65-byte point 04‖X‖Y } }` (`dispatcher.go:463-488`). No challenge field; the `uuid` is the challenge.
- Reply: `session_info` (raw `SessionInfo` bytes: counter, domain public key, 16-byte epoch, `clock_time` fixed32, status, handle, flags), `signature_data.session_info_tag.tag` (32 bytes), `request_uuid`.
- Authenticate: `tag == HMAC(HMAC(K,"session info"), TLV(0,[6]) ‖ TLV(2,VIN) ‖ TLV(6,request_uuid) ‖ 0xFF ‖ raw session_info)` — constant-time compare, use the received bytes as-is, omit TLV 6 if `request_uuid` is empty. Discard if the reply arrives > 10 s after the request.
- `SessionInfo.status = 1` (KEY_NOT_ON_WHITELIST) → key not paired.
- Clock: `timeZero = now − clock_time`; vehicle time = seconds since `timeZero`. On resync never lower the counter; reject a changed domain public key.

## 4. Keys

1. `Sx = ECDiffieHellman.DeriveRawSecretAgreement(vehiclePub)` — 32 bytes big-endian, leading zeros kept (net8+).
2. `K = SHA1(Sx)[0..16]`.
3. Sub-keys: `HMAC-SHA256(K, "session info")`, `HMAC-SHA256(K, "authenticated command")`.

ECDsa is not used for commands.

## 5. Signing

| Domain | Fleet API signature type |
|---|---|
| Infotainment | `AES_GCM_DETACHED (10)` if `SessionInfo.flags` bit 0 set, else `HMAC_PERSONALIZED (8)` |
| VCSEC | `HMAC_PERSONALIZED (8)` |

Metadata M (`metadata.go`): `tag ‖ len ‖ value`, ascending tag order, integers u32 **big-endian**, terminator `0xFF`. Request tags: 0 signature type, 1 domain, 2 VIN, 3 epoch, 4 expires_at, 5 counter, 7 flags (only if > 0), 10 command prefix (detached only). Note: `expires_at`/`clock_time` are little-endian `fixed32` on the wire but big-endian in M.

- **HMAC_PERSONALIZED:** `tag = HMAC(K_cmd, M ‖ payload)`, 32 bytes. Wire fields: epoch, counter, expires_at, tag; `signer_identity.public_key` = our key.
- **AES_GCM_DETACHED:** encrypt only the innermost command field (walk depth 2 for Infotainment: `Action.vehicleAction → command`); the plaintext prefix goes into M as TLV 10. `AesGcm(K, tagSize 16)`, random 12-byte nonce, AAD = `SHA256(M)`. Wire fields: nonce, counter, expires_at, tag, prefix (no epoch on the wire, but in M).
- Counter: pre-increment per command; VCSEC strictly in order, Infotainment 32-entry window.
- Expiry: vehicle-epoch seconds = `now + lifetime − timeZero`; default lifetime 5 s.

## 6. Responses

- Always send `flags = 2`; firmware ≥ 2024.38 then encrypts replies (fault 28 `REQUIRES_RESPONSE_ENCRYPTION` exists).
- Decrypt `AES_GCM_Response_data` with K; AAD = `SHA256(TLV(0,[9]) ‖ TLV(1,from-domain) ‖ TLV(2,VIN) ‖ TLV(5,counter) ‖ TLV(7,response flags) ‖ TLV(8,request hash) ‖ TLV(9,fault) ‖ 0xFF)`. Request hash = `[signature type] ‖ request tag`, truncated to 17 bytes for VCSEC HMAC. Nonce must be 12 bytes. Reject a repeated response counter per request.
- Match replies by routing address and (Infotainment) `request_uuid`.
- Infotainment: `CarServer.Response.actionStatus.result == ERROR` → command refused, reason in `result_reason.plain_text` (`already_set`, `not_charging`, `disconnected`, …).
- VCSEC: empty `FromVCSECMessage` = success; `nominalError` = refused; `commandStatus.WAIT` = busy (retry).

## 7. Faults

- Retry by **re-signing** (new counter + expiry), every 1 s until the caller's deadline: 1 BUSY, 2 TIMEOUT, 5 INVALID_SIGNATURE, 6 INVALID_TOKEN_OR_COUNTER, 11 INTERNAL, 15 INCORRECT_EPOCH, 17 TIME_EXPIRED, 20 TIME_TO_LIVE_TOO_LONG.
- Before retrying, verify and adopt the session info the vehicle attaches to the fault reply. **No session reset, no new handshake.**
- Terminal: 3 UNKNOWN_KEY_ID (→ key not paired, point to `https://tesla.com/_ak/<domain>`), 4 INACTIVE_KEY, 7 INSUFFICIENT_PRIVILEGES, 14 KEYCHAIN_IS_FULL, 21 REMOTE_ACCESS_DISABLED, 23, 26–28.

## 8. Protobuf code generation

- All 9 protos from `pkg/protocol/protobuf/` compile unmodified for net8.0 and net10.0 with `Google.Protobuf` 3.34.1 + `Grpc.Tools` 2.83.0:
  ```xml
  <Protobuf Include="Protos/*.proto" ProtoRoot="Protos" GrpcServices="None" Access="Internal" />
  ```
- Generated namespaces are top-level (`UniversalMessage`, `Signatures`, `CarServer`, `VCSEC`, `Keys`, `Errors`, `ManagedCharging`) — keep them `internal`. `CarServer.Action`/`CarServer.Void` clash with `System.*` under ImplicitUsings → use aliases.
- License: protos carry no header, upstream has no NOTICE. Ship the Apache-2.0 text next to the protos and in the package, add `Protos/README.md` (source + commit, unmodified) and `THIRD-PARTY-NOTICES.md`; C# files ported from Go get a "Port of … (Apache-2.0)" header.

## 9. Test vectors (from `protocol.md`, pinned by `protocol_doc_test.go:28-89`)

| Item | Value |
|---|---|
| VIN | `5YJ30123456789ABC` |
| Sx | `6b9a2844b300e6a85e4474f70b31b23de2d8f7d3e594784cdfe8318031442f8b` |
| K | `1b2fce19967b79db696f909cff89ea9a` |
| session-info key | `fceb679ee7bca756fcd441bf238bf2f338629b41d9eb9c67be1b32c9672ce300` |
| challenge (uuid) | `1588d5a30eabc6f8fc9a951b11f6fd11` |
| session-info metadata | `000106021135594a333031323334353637383941424306101588d5a30eabc6f8fc9a951b11f6fd11ff` |
| session_info_tag | `996c1fe38331be138f8039c194b14db2198846ed7d8251e6749284d7b32ea002` |
| HVAC-on plaintext | `120452020801` |
| AES-GCM nonce / ciphertext / tag | `dbf79447fa156674dae1caed` / `38038e8c0f2e` / `c228e0ff64991481db3a7bbc133696c5` |
| request metadata example | `000105010103021135594a3330313233343536373839414243 03104c463f9cc0d3d26906e982ed224adde6 0404 00000a5f 0504 00000007 0704 00000002 ff` → AAD `54785b6878a51a70acaf240199f92f482962a73a8c3170bfd2a94b1715d127b9` |

Key pairs (client c/C, vehicle v/V) are in `protocol.md`. Further vectors: `ecdh_test.go:46-87` (shared secret with a leading zero → K `0db468f9045e01c1798c54e52d7f408f`), `metadata_test.go:45-65` (`abab04d8…868e`), `verifier_test.go:521-589` (`TestGCMKnown`, full VCSEC message), `peer_test.go:104-157` (request hash, detached prefix `0a0b1209c2bb010568656c6c6f`), `window_test.go`, `error_test.go:49-120`. No fixed response-decryption vector exists upstream — build one from the protocol.md keys.

Byte-exact tests need an injectable random source (nonce, uuid, routing address) and clock.

## 10. Live observations (2026-11-24, EU Model Y, fw 2026.38.6, `coilpro cmd … --debug`)

- **Infotainment advertises detached encryption** (`SessionInfo.flags` bit 0 set) → commands are signed with `AES_GCM_DETACHED`.
- **VCSEC does not** → commands are signed with `HMAC_PERSONALIZED`.
- **Both domains echo `request_uuid`** (16 bytes) — in the session-info reply and in the command reply.
- **Both domains encrypt command replies** (`AES_GCM_Response_data`), VCSEC included; reply `flags = 0`.
- Successful replies: Infotainment carries `signedMessageStatus = OK/NONE`, VCSEC carries none.

Still open: whether **fault replies** are encrypted — no fault occurred in the live runs. The SDK accepts both (an encrypted reply is decrypted and authenticated before its fault is raised; an unencrypted fault reply is accepted as in Go). Also open: exact bodies of 408/503 from `signed_command`.

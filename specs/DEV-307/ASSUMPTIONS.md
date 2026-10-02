# DEV-307 taste assumptions

- [assumed] Use `postgres` and `rabbitmq` as internal readiness-check registration labels (Keel Q2 recommendation; PRODUCT section 4). This spelling is internal only; any exposure in a response remains subject to Q2 owner approval.

No public response field, status spelling, duration unit, content type or HTTP status mapping is assumed. Those choices remain in the Q2 structural checkbox.

- [assumed] Decision B: use `exception.GetType().FullName` for `TracingDecorator`'s non-validation `error.type` value. Basis: DEV-307 Decision (2026-09-28)/Acceptance addition leaves type spelling open; PRODUCT section 4; OpenTelemetry [error.type registry](https://github.com/open-telemetry/semantic-conventions/blob/main/model/error/registry.yaml) recommends the canonical class name, queried via Context7 on 2026-10-02. See CONCLUSIONS.md Decision B.

- [assumed] The application's meter is identified by the existing `TelemetryConstants.ActivitySourceName` (`"LamuFlix"`), so the meter provider subscribes with `AddMeter(TelemetryConstants.ActivitySourceName)` and **no second identity constant is added** (plan.md Design §3 and §9). Basis: DEV-307 Scope 2 names `ActivitySource` "LamuFlix" "and its meter" and says the name comes from `TelemetryConstants`; verified at this HEAD that `TelemetryConstants` holds exactly one identity constant and that the repository contains no `Meter` and no meter-name constant, so `ActivitySourceName` is the only existing candidate. A second `MeterName` constant would be a second identity string for the same application. Recorded by the plan pass, 2026-10-02; Keel may overrule.

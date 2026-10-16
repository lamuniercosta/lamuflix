# LamuFlix C4 architecture

Authoritative container and component views of the system as built.
They follow the accepted ADRs in `../adr/` and the ports-and-adapters
shape from ADR-0001 / ADR-0002.

- [C4 Container](c4-container.md) — the deployable containers and
  external systems, and how they talk to each other.
- [C4 Component](c4-component.md) — the components inside the single
  `LamuFlix.Api` host: endpoints, Core handlers, ports, and
  Infrastructure adapters.

The legacy C# Web and Worker hosts are retired (ADR 0021) and appear
in neither diagram.

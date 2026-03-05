# ADR-0001: Start as a Modular Monolith

## Status

Accepted

## Context

UVSE needs strict tenant isolation, plugin extensibility, and reliable synthesis pipelines without premature distributed-system complexity.

## Decision

Use a modular monolith with explicit Web, Application, Domain, and Infrastructure layers. Enforce boundaries with `internal` implementations, project references, and NetArchTest rules.

## Consequences

The system can evolve into separate deployable services later, but starts with lower operational overhead and faster vertical-slice delivery.

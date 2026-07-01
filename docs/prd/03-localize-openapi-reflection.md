# PRD: Localize the Microsoft.OpenApi reflection

## Summary
The current implementation reads and writes `BaseOpenApiReference` properties through reflection in a small helper block. That logic is already centralized, but it remains exposed as string-based, library-specific behaviour. If we want to harden the library against future changes in Microsoft.OpenApi, we can localize that knowledge into one deep module.

## Problem
The reflection-based logic is distributed across helper methods that are tightly coupled to the OpenAPI library internals. The maintenance cost is not yet high, but the implementation is fragile: it depends on property names and the mutable shape of the underlying library types. That makes the code harder to reason about and easier to break during upgrades.

## Goals
- Localize the reflection knowledge into one internal module.
- Keep the public behaviour unchanged.
- Reduce the amount of framework-specific code exposed to the merge pipeline.
- Make future upgrades to Microsoft.OpenApi easier to reason about.

## Non-goals
- Creating a new public seam or public adapter for the OpenAPI library.
- Replacing the underlying Microsoft.OpenApi types with a custom abstraction.
- Changing the merge semantics in any way.

## Proposed solution
Introduce one internal module that wraps the Microsoft.OpenApi reference manipulation. It exposes a small, typed interface for reading/writing the reference state that the merge pipeline needs. The reflection remains inside that module, where it can be tested and updated in one place.

## Benefits
- Locality: the reflection-specific knowledge is concentrated in one module.
- Leverage: changes to the Microsoft.OpenApi surface move through one place.
- Depth: the merge logic uses a simple interface instead of reaching into library internals directly.
- Tests: the internal module can be tested in isolation without coupling to the whole merge flow.

## Acceptance criteria
- The reflection-based reference manipulation is owned by one internal module.
- The merge pipeline uses that module rather than direct reflection calls.
- Existing merge behavior remains unchanged.
- The module has focused tests for the supported reference operations.

## Open questions
- Is this worth implementing as a separate PRD now, or should it wait until a Microsoft.OpenApi upgrade actually forces the issue?
- Should the module be named around the concept of `ReferenceAdapter` or `ReferenceGateway`?

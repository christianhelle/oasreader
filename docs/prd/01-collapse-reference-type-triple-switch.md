# PRD: Collapse the `ReferenceType` triple-switch

## Summary
The merge logic currently encodes the mapping from `ReferenceType` to the target component collection in multiple places. That mapping is duplicated across the resolver, the cache update logic, and the component helper code. The result is a shallow module with a wide interface and a high chance of drift when a new component type is added.

## Problem
The current implementation scatters the same knowledge across three switches:
- existence checks
- resolution
- document population

That creates a locality problem: changing the supported component set requires touching several places, and a mismatch is easy to introduce. The `Tag` case already shows the issue: it appears in one path but not the others.

## Goals
- Consolidate the `ReferenceType` to component-collection mapping into one internal module.
- Keep the behavior of the merge flow unchanged.
- Reduce the chance of future drift when component types are added or removed.
- Improve locality so the merge engine reads as one cohesive module instead of three parallel switch statements.

## Non-goals
- Changing the public API.
- Changing the OpenAPI merge semantics beyond the refactoring itself.
- Introducing a new external seam for component-type handling.

## Proposed solution
Create one internal module that owns the `ReferenceType` to component-collection mapping. The resolver, cache updater, and component helper logic call into that module through a small interface. The implementation hides the mapping behind the seam so tests can target a single place.

## Benefits
- Locality: one place owns the type-to-collection mapping.
- Leverage: one change updates all callers.
- Depth: the module absorbs the branching instead of spreading it across the merge pipeline.
- Tests: one module can be tested for the whole supported component set rather than through repeated switch logic.

## Acceptance criteria
- The `ReferenceType` to component-collection mapping exists in one module.
- Adding or removing a supported component type requires a single change in that mapping module.
- Existing merge behavior remains unchanged for the current component types.
- The module is covered by focused tests that assert the mapping contract directly.

## Open questions
- Should the module be named around the concept of `ComponentSlot` or `ReferenceMapping`?
- Do we want to preserve the existing `ComponentResolver` name as the top-level entry point?

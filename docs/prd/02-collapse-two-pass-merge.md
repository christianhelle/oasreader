# PRD: Collapse the two-pass merge into one module

## Summary
The external-reference merge flow is currently expressed as a loop in one extension method plus two visitor modules that each participate in the same algorithm. The logic is split across multiple modules even though it describes a single behaviour: resolve, discover missing references, and repeat until the document is fully merged.

## Problem
The current shape forces a maintainer to bounce between four places to understand the merge algorithm:
- the public extension method that owns the loop
- the resolver visitor that resolves known references
- the missing-reference visitor that discovers unresolved ones
- the cache that carries state between passes

This is a shallow cluster: the interface is spread over several modules, and the implementation is not concentrated where the behaviour lives.

## Goals
- Consolidate the merge algorithm into one internal module.
- Keep the public merge entry points unchanged.
- Preserve the existing fixpoint behaviour with repeated passes until no new references are discovered.
- Make the merge logic easier to reason about and test through one entry point.

## Non-goals
- Changing the merge semantics or public API.
- Reworking document parsing or the external-document-source seam as part of this PRD.
- Replacing the current walker-based traversal with a different approach.

## Proposed solution
Introduce an internal `ExternalReferenceMerger` module that owns:
- the resolve pass
- the missing-reference pass
- the fixpoint loop
- the working-set cache

The existing public extension method delegates to this module. The individual visitors become focused helpers or are removed entirely if the merger can own the traversal directly.

## Benefits
- Locality: the merge algorithm lives in one place.
- Leverage: one interface can exercise the whole merge behaviour.
- Depth: one module absorbs the loop and its state instead of spreading it across shallow helpers.
- Tests: the merge can be validated at one seam rather than through multiple low-level visitors.

## Acceptance criteria
- One internal module owns the merge algorithm.
- The public extension method delegates to that module without changing behavior.
- The merge still resolves external references over repeated passes until fixed.
- Tests assert merged output through the module's interface rather than through visitor internals.

## Open questions
- Should the module expose a single `Merge(OpenApiDocument document, IExternalDocumentSource source)` entry point or a slightly richer internal interface?
- Should the visitors remain as private helpers inside the merger, or be eliminated entirely?

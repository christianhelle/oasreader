# Domain glossary

Shared vocabulary for the OasReader codebase. Architecture terms (module, interface,
seam, adapter, depth, leverage, locality) follow the deep-module design vocabulary.

## OpenAPI domain

- **OpenAPI document** — a parsed `OpenApiDocument` from the Microsoft.OpenApi toolset.
- **External reference** — a `$ref` whose `ExternalResource` points outside the root
  document (a sibling file or a URL), e.g. `components.yaml#/components/schemas/Pet`.
- **Component** — a reusable item under `components/*` (schema, response, parameter,
  example, request body, header, security scheme, link, callback).
- **Merge** — pulling every external reference's target into the root document and
  rewriting the reference to a local one, so the result is a single self-contained document.

## Modules

- **External document source** — the seam over *where external documents come from*.
  Interface `IExternalDocumentSource.GetDocument(string externalResource)` maps a
  reference's external-resource string to a parsed `OpenApiDocument` (or `null` on any
  failure). Production adapter `ExternalDocumentSource` loads from the filesystem or HTTP
  and parses; the in-memory test adapter resolves from a dictionary. The merge algorithm
  owns the working-set cache, so the source stays a pure loader.

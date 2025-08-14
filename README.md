[![Build](https://github.com/christianhelle/oasreader/actions/workflows/build.yml/badge.svg)](https://github.com/christianhelle/oasreader/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/oasreader?color=blue)](https://www.nuget.org/packages/oasreader)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=christianhelle_oasreader&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=christianhelle_oasreader)
[![codecov](https://codecov.io/gh/christianhelle/oasreader/graph/badge.svg?token=242YT1N6T2)](https://codecov.io/gh/christianhelle/oasreader)

# Multi Document Reader for OpenAPI.NET

An OpenAPI reader that merges external references into a single document using the [Microsoft OpenAPI](https://www.nuget.org/packages/Microsoft.OpenApi.readers) toolset. 

This is based on the work done by Jan Kokenberg and contains [source code](https://dev.azure.com/janbaarssen/Open%20API%20Generator/_git/OpenApi.Merger) from the [dotnet-openapi-merger](https://www.nuget.org/packages/dotnet-openapi-merger) CLI tool

## Usage

The class `OpenApiMultiFileReader` is used to load an OpenAPI specifications document file locally or remotely using a YAML or JSON file. `OpenApiMultiFileReader` will automatically merge external references if the OAS file uses them.

**Local Files**: External references must be in the same folder as the main OAS file or in subdirectories.

**Remote Files**: When loading OAS files remotely, external references can be:

- Absolute URLs: `https://example.com/components.yaml#/components/schemas/Pet`
- Relative URLs: `components.yaml#/components/schemas/Pet` (resolved relative to the main file's URL)

```csharp
ReadResult result = await OpenApiMultiFileReader.Read("petstore.yaml");
OpenApiDocument document = result.OpenApiDocument;
```

### Example with Local External References

In the example below, we have OpenAPI specifications that are split into multiple documents. **`petstore.yaml`** contains the **`paths`** and **`petstore.components.yaml`** contain the **`components/schemas`**

**`petstore.yaml`**

```yaml
openapi: 3.0.3
paths:
  /pet:
    post:
      tags:
      - pet
      summary: Add a new pet to the store
      description: Add a new pet to the store
      operationId: addPet
      requestBody:
        description: Create a new pet in the store
        content:
          application/json:
            schema:
              $ref: 'petstore.components.yaml#/components/schemas/Pet'          
        required: true
      responses:
        "200":
          description: Successful operation
          content:
            application/json:
              schema:
                $ref: 'petstore.components.yaml#/components/schemas/Pet'
```

### Example with Remote External References

You can also use remote external references with absolute URLs:

```yaml
openapi: 3.0.3
paths:
  /pet:
    post:
      tags:
      - pet
      summary: Add a new pet to the store
      description: Add a new pet to the store
      operationId: addPet
      requestBody:
        description: Create a new pet in the store
        content:
          application/json:
            schema:
              $ref: 'https://raw.githubusercontent.com/example/repo/main/components.yaml#/components/schemas/Pet'          
        required: true
      responses:
        "200":
          description: Successful operation
          content:
            application/json:
              schema:
                $ref: 'https://raw.githubusercontent.com/example/repo/main/components.yaml#/components/schemas/Pet'
```

Or with relative URLs when the main file is also remote:

```yaml
openapi: 3.0.3
paths:
  /pet:
    post:
      requestBody:
        content:
          application/json:
            schema:
              $ref: 'components.yaml#/components/schemas/Pet'  # Resolved relative to main file's URL
```

**`petstore.components.yaml`**

```yaml
openapi: 3.0.3
components:
  schemas:
    Pet:
      required:
      - name
      - photoUrls
      type: object
      properties:
        id:
          type: integer
          format: int64
          example: 10
        name:
          type: string
          example: doggie
        category:
          $ref: '#/components/schemas/Category'
        photoUrls:
          type: array
          xml:
            wrapped: true
          items:
            type: string
            xml:
              name: photoUrl
        tags:
          type: array
          xml:
            wrapped: true
          items:
            $ref: '#/components/schemas/Tag'
        status:
          type: string
          description: pet status in the store
          enum:
          - available
          - pending
          - sold
    Category:
      type: object
      properties:
        id:
          type: integer
          format: int64
          example: 1
        name:
          type: string
          example: Dogs
      xml:
        name: category
```

#

For tips and tricks on software development, check out [my blog](https://christianhelle.com)

If you find this useful and feel a bit generous then feel free to [buy me a coffee ☕](https://www.buymeacoffee.com/christianhelle)

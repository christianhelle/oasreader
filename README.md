[![Build](https://github.com/christianhelle/oasreader/actions/workflows/build.yml/badge.svg)](https://github.com/christianhelle/oasreader/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/oasreader?color=blue)](https://www.nuget.org/packages/oasreader)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=christianhelle_oasreader&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=christianhelle_oasreader)
[![codecov](https://codecov.io/gh/christianhelle/oasreader/graph/badge.svg?token=242YT1N6T2)](https://codecov.io/gh/christianhelle/oasreader)

# OpenAPI Multi Document Reader for .NET

An OpenAPI reader that merges external references into a single document using the [Microsoft OpenAPI](https://www.nuget.org/packages/Microsoft.OpenApi.readers) toolset

## Usage

The class `OpenApiMultiFileReader` is used to load an OpenAPI specifications document file locally or remotely using a YAML or JSON file. `OpenApiMultiFileReader` will automatically merge external references if the OAS file uses them. Merging external referenecs that the file is in the same folder as the main OAS file. When loading OAS files remotely, the external references must also be remote files. Currently, you can not load a remote OAS file that has external references to local files. 

```csharp
OpenApiDocument document = await OpenApiMultiFileReader.Read("petstore.yaml");
```

In the example above, we have OpenAPI specifications that are split into multiple documents. **`petstore.yaml`** contains the **`paths`** and **`petstore.components.yaml`** contain the **`components/schemas`**

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
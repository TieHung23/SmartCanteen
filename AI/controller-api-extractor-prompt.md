# Prompt: Extract API Spec From Controllers

Task:
1. Read the entire source code in this repository before writing results.
2. Find all files whose names end with `Controller` (for example `*Controller.cs`).
3. For each controller action, extract only what is explicitly defined in code:
   - HTTP method and route
   - Request model type and fields
   - Response model type and fields
   - Status codes if annotated/obvious from code
4. Create or update `API.md` at repository root.

Strict rules:
- Do not invent endpoints, fields, example values, or status codes.
- If information is missing in source code, write `unknown` explicitly.
- JSON samples must match actual source model fields and names.
- Keep one section per endpoint.

Required output format in `API.md`:

~~~md
# API Reference

## [METHOD] /route
- Controller: Namespace.ControllerName
- Action: ActionName
- RequestType: TypeName | none
- ResponseType: TypeName | unknown

### Request JSON
```json
{ ... }
```

### Response JSON
```json
{ ... }
```

### Notes
- Status codes: ...
- Source: path/to/controller.cs
~~~



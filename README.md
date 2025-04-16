# PDF Form API

A REST API for filling and analyzing PDF forms built with ASP.NET Core and iText.

## Overview

The PDF Form API allows you to:

1. **Fill PDF forms** with data programmatically
2. **Analyze PDF forms** to discover form fields and their properties

This API is useful for automating form filling processes or building applications that need to interact with PDF forms.

## Run using Docker

```
docker run --rm -p 5162:80 ghcr.io/rijx/pdf-form-api
```

## Run locally

```bash
git clone https://github.com/rijx/pdf-form-api.git
cd pdf-form-api
dotnet run
```

By default, the API will be available at `http://localhost:5162`.

## API Endpoints

### 1. Fill PDF Form

Fill in a PDF form with provided values.

**Endpoint:** `POST /fill`

**Request Format:** `multipart/form-data`

**Form Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `pdf` | File | Yes | The PDF form file to fill |
| `values` | JSON string | Yes | A JSON object mapping field names to values |
| `font` | String | No | Font to use (default: "freesans/FreeSans-LrmZ.ttf") |
| `autoSizeFields` | JSON string | No | Array of field names that should auto-size text |

**Response:** The filled PDF file.

**Example using curl:**

```bash
curl -X POST "http://localhost:5162/fill" \
  -F "pdf=@path/to/your/form.pdf" \
  -F 'values={"name":"John Doe","email":"john@example.com","age":"30"}' \
  -F 'autoSizeFields=["comments"]' \
  -o filled_form.pdf
```

### 2. Analyze PDF Form

Get information about all form fields in a PDF.

**Endpoint:** `POST /analyze`

**Request Format:** `multipart/form-data`

**Form Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `pdf` | File | Yes | The PDF form file to analyze |

**Response:** JSON array of field information objects.

**Example using curl:**

```bash
curl -X POST "http://localhost:5162/analyze" \
  -F "pdf=@path/to/your/form.pdf"
```

**Sample Response:**

```json
[
  {
    "name": "name",
    "type": "Tx",
    "options": []
  },
  {
    "name": "email",
    "type": "Tx",
    "options": []
  },
  {
    "name": "subscribe",
    "type": "Btn",
    "options": ["Off", "Yes"]
  }
]
```

## Field Types

The API returns the following field types in the analyze endpoint:

- `Tx`: Text field
- `Btn`: Button field (checkbox, radio button)
- `Ch`: Choice field (dropdown, list box)
- `Sig`: Signature field

## Error Handling

The API returns appropriate HTTP status codes for different error scenarios:

- `400 Bad Request`: Invalid input (e.g., missing PDF file, invalid JSON)
- `500 Internal Server Error`: Server-side error

Error responses include a message explaining the error.

## Font Configuration

Custom fonts can be used by:

1. Adding font files to the `resources/fonts/` directory
2. Passing the relative path as the `font` parameter

## Limits and Constraints

- Maximum PDF file size: 100MB
- Supported field types: Text fields, checkboxes, radio buttons, and dropdown lists
- Font specification limited to alphanumeric characters, underscore, hyphen, forward slash, and file extension

## Complete Example: Filling a Form

Let's walk through an example of filling a form with various field types:

1. First, analyze the form to discover fields:

```bash
curl -X POST "http://localhost:5162/analyze" \
  -F "pdf=@application_form.pdf" > form_fields.json
```

2. Review the fields in the JSON output:

```json
[
  {"name": "full_name", "type": "Tx", "options": []},
  {"name": "email", "type": "Tx", "options": []},
  {"name": "gender", "type": "Btn", "options": ["male", "female", "other"]},
  {"name": "agree_terms", "type": "Btn", "options": ["Off", "Yes"]},
  {"name": "comments", "type": "Tx", "options": []}
]
```

3. Fill the form with values:

```bash
curl -X POST "http://localhost:5162/fill" \
  -F "pdf=@application_form.pdf" \
  -F 'values={"full_name":"Jane Smith","email":"jane@example.com","gender":"female","agree_terms":"Yes","comments":"This is a long comment that might need to be auto-sized to fit properly."}' \
  -F 'autoSizeFields=["comments"]' \
  -o filled_application.pdf
```

## License

[MIT License](LICENSE)

# Data Importers (DataImporters)

A .NET library for importing, validating, and reporting on tabular data from Excel, JSON, CSV, and XML files.

## Target Frameworks

| Project | Targets | Brief Description |
|---|---|---|
| DataImporters.Core | `netstandard2.0`, `net9.0`, `net10.0` | Shared validation engine, attributes, and report generation |
| DataImporters.Excel | `netstandard2.0`, `net9.0`, `net10.0` | Excel importer (ClosedXML-based)|
| DataImporters.Json | `netstandard2.0`, `net9.0`, `net10.0` |JSON importer (System.Text.Json-based)|
| DataImporters.Csv | `netstandard2.0`, `net9.0`, `net10.0` | CSV importer (no external dependencies)|
| DataImporters.Xml | `netstandard2.0`, `net9.0`, `net10.0` | XML importer (no external dependencies)|

> <strong>The `netstandard2.0` target enables use from .NET Framework 4.6.1+ and any .NET Standard 2.0-compatible runtime</strong>

Reference only the packages you need. JSON consumers don't pull in ClosedXML, CSV and XML consumers have zero external dependencies beyond Core, etc.

## Quick Start

### Excel Import

```csharp
using DataImporters.Excel.Attributes;
using DataImporters.Excel.Importing;
using DataImporters.Core.Attributes;
using DataImporters.Core.Reporting;

// 1. Define a DTO with column mappings and validation rules
public sealed class EmployeeDto
{
    [ExcelColumn("First Name")]
    public string FirstName { get; set; } = "";

    [ExcelColumn("Last Name")]
    public string LastName { get; set; } = "";

    [ExcelColumn("Email")]
    [InlineValidation("Row.Email.Contains(\"@\")",
        ErrorMessage = "Email must contain @", RuleName = "EmailFormat")]
    public string Email { get; set; } = "";

    [ExcelColumn(4)]  // map by 1-based column number
    [InlineValidation("Row.Age >= 18 && Row.Age <= 80",
        ErrorMessage = "Age must be 18-80", RuleName = "AgeRange")]
    public int Age { get; set; }

    [ExcelColumn("Salary")]
    [InlineValidation("Row.Salary > 0",
        ErrorMessage = "Salary must be positive", RuleName = "PositiveSalary")]
    public decimal Salary { get; set; }

    [ExcelColumn("Start Date")]
    public DateTime StartDate { get; set; }
}

// 2. Import, validate, report
var importer = new Importer<EmployeeDto>("employees.xlsx");
var rows = importer.Import();                     // reads all rows into DTOs
var errors = importer.Validate();                 // runs all validation rules

var valid = importer.GetValidRows();              // rows with zero errors
var invalid = importer.GetInvalidRows();          // rows with at least one error

var report = importer.CreateReportGenerator();
report.Generate(ReportFormat.Html, ReportDestination.File, outputFilePath: "report.html");
report.Generate(ReportFormat.Json, ReportDestination.File, outputFilePath: "report.json");
```

**Constructor parameters:**

| Parameter | Default | Description |
|---|---|---|
| `filePath` | *(required)* | Path to the `.xlsx` file |
| `worksheetName` | `""` (first sheet) | Name of the worksheet to read |
| `dataStartRow` | `2` | 1-based row number where data begins (row 1 = headers) |

### JSON Import

```csharp
using DataImporters.Json.Attributes;
using DataImporters.Json.Importing;
using DataImporters.Core.Attributes;
using DataImporters.Core.Reporting;

// 1. Define a DTO with JSON property mappings
public sealed class EmployeeDto
{
    [JsonColumn("first_name")]
    public string FirstName { get; set; } = "";

    [JsonColumn("last_name")]
    public string LastName { get; set; } = "";

    [JsonColumn("email")]
    [InlineValidation("Row.Email.Contains(\"@\")",
        ErrorMessage = "Email must contain @", RuleName = "EmailFormat")]
    public string Email { get; set; } = "";

    [JsonColumn("age")]
    public int Age { get; set; }

    [JsonColumn("salary")]
    public decimal Salary { get; set; }

    [JsonColumn("start_date")]
    public DateTime StartDate { get; set; }
}

// 2. Import from a root-level JSON array
var importer = new JsonImporter<EmployeeDto>("employees.json");
var rows = importer.Import();
var errors = importer.Validate();

// Or import from a nested array path
var importer2 = new JsonImporter<EmployeeDto>("data.json", arrayPath: "data.employees");
```

**Constructor parameters:**

| Parameter | Default | Description |
|---|---|---|
| `filePath` | *(required)* | Path to the `.json` file |
| `arrayPath` | `null` (root array) | Dot-separated path to a nested array (e.g. `"data.employees"`) |

**Expected JSON format (root array):**

```json
[
  { "first_name": "Alice", "last_name": "Johnson", "email": "alice@acme.com", "age": 32, "salary": 120000 },
  { "first_name": "Bob", "last_name": "Smith", "email": "bob@acme.com", "age": 45, "salary": 95000 }
]
```

**Expected JSON format (nested array):**

```json
{
  "data": {
    "employees": [
      { "first_name": "Alice", "email": "alice@acme.com" }
    ]
  }
}
```

### CSV Import

```csharp
using DataImporters.Csv.Attributes;
using DataImporters.Csv.Importing;
using DataImporters.Core.Attributes;
using DataImporters.Core.Reporting;

// 1. Define a DTO with CSV column mappings
public sealed class EmployeeDto
{
    [CsvColumn("first_name")]
    public string FirstName { get; set; } = "";

    [CsvColumn("last_name")]
    public string LastName { get; set; } = "";

    [CsvColumn("email")]
    [InlineValidation("Row.Email.Contains(\"@\")",
        ErrorMessage = "Email must contain @", RuleName = "EmailFormat")]
    public string Email { get; set; } = "";

    [CsvColumn(4)]  // map by 1-based column number
    public int Age { get; set; }

    [CsvColumn("salary")]
    public decimal Salary { get; set; }

    [CsvColumn("start_date")]
    public DateTime StartDate { get; set; }
}

// 2. Import from a CSV with headers (default)
var importer = new CsvImporter<EmployeeDto>("employees.csv");
var rows = importer.Import();
var errors = importer.Validate();

// Or import without headers, mapping by column number
var importer2 = new CsvImporter<EmployeeDto>("data.csv", hasHeaderRow: false);

// Or use a custom delimiter (e.g. semicolon, tab)
var importer3 = new CsvImporter<EmployeeDto>("data.tsv", delimiter: '\t');
```

**Constructor parameters:**

| Parameter | Default | Description |
|---|---|---|
| `filePath` | *(required)* | Path to the CSV file |
| `hasHeaderRow` | `true` | Whether the first row contains column headers |
| `delimiter` | `','` | Field delimiter character |

**Expected CSV format (with headers):**

```csv
first_name,last_name,email,age,salary,start_date
Alice,Johnson,alice@acme.com,32,120000,2020-03-15
Bob,Smith,bob@acme.com,45,95000,2018-07-01
```

**Expected CSV format (without headers — uses column numbers):**

```csv
Alice,Johnson,alice@acme.com,32,120000,2020-03-15
Bob,Smith,bob@acme.com,45,95000,2018-07-01
```

**CSV parsing features:**
- RFC 4180-compliant: quoted fields, embedded commas, escaped quotes (`""`)
- Custom delimiters: semicolons (`;`), tabs (`\t`), pipes (`|`), etc.
- Blank lines are automatically skipped
- No external CSV library required

## Builder Pattern

Each importer has a corresponding builder class for fluent construction. Builders are the recommended way to create importers — they provide a clear, discoverable API and separate configuration from execution.

### ExcelImporterBuilder

```csharp
using DataImporters.Excel.Importing;

// Single-worksheet file (worksheet name omitted — uses the first sheet)
var importer = ExcelImporterBuilder<EmployeeDto>.Create()
    .FromFile("employees.xlsx")
    .ForColumn(x => x.Salary, (sal, _) => sal > 0, "Salary must be positive")
    .ForColumn(x => x.Email, (email, _) => email.Contains("@"), "Invalid email")
    .Build();

var rows = importer.Import();
var errors = importer.Validate();
var validRows = importer.GetValidRows();
```

```csharp
// Multi-worksheet file — specify the target worksheet
var importer = ExcelImporterBuilder<EmployeeDto>.Create()
    .FromFile("quarterly-data.xlsx")
    .WithWorksheet("Q1 Employees")
    .WithDataStartRow(3)
    .ForColumn(x => x.Age, (age, _) => age >= 18, "Must be 18+")
    .Build();
```

**Builder methods:**

| Method | Required | Description |
|---|---|---|
| `FromFile(path)` | Yes | Path to the `.xlsx` file |
| `WithWorksheet(name)` | No | Worksheet name. If omitted, the first worksheet is used (single-sheet assumption) |
| `WithDataStartRow(row)` | No | 1-based row where data starts (default `2`) |
| `ForColumn(selector, validator, errorMessage)` | No | Adds a fluent column-level validator. Chainable |
| `Build()` | Yes | Returns a configured `Importer<T>` |

### JsonImporterBuilder

```csharp
using DataImporters.Json.Importing;

// Root-level JSON array
var importer = JsonImporterBuilder<ProductDto>.Create()
    .FromFile("products.json")
    .ForColumn(x => x.Price, (price, _) => price > 0, "Price must be positive")
    .Build();

var rows = importer.Import();
var errors = importer.Validate();
```

```csharp
// Nested array with dot-path navigation
var importer = JsonImporterBuilder<ProductDto>.Create()
    .FromFile("catalog.json")
    .WithArrayPath("data.products")
    .ForColumn(x => x.Name, (name, _) => name.Length > 0, "Name is required")
    .ForColumn(x => x.Price, (price, row) => row.Category != "Free" || price == 0,
        "Free items must have zero price")
    .Build();
```

**Builder methods:**

| Method | Required | Description |
|---|---|---|
| `FromFile(path)` | Yes | Path to the `.json` file |
| `WithArrayPath(path)` | No | Dot-separated path to the array (e.g. `"data.items"`). If omitted, the root must be an array |
| `ForColumn(selector, validator, errorMessage)` | No | Adds a fluent column-level validator. Chainable |
| `Build()` | Yes | Returns a configured `JsonImporter<T>` |

### CsvImporterBuilder

```csharp
using DataImporters.Csv.Importing;

// Standard CSV with headers
var importer = CsvImporterBuilder<EmployeeDto>.Create()
    .FromFile("employees.csv")
    .ForColumn(x => x.Salary, (sal, _) => sal > 0, "Salary must be positive")
    .Build();

var rows = importer.Import();
var errors = importer.Validate();
```

```csharp
// No headers, semicolon-delimited
var importer = CsvImporterBuilder<EmployeeDto>.Create()
    .FromFile("data.csv")
    .WithHeaderRow(false)
    .WithDelimiter(';')
    .ForColumn(x => x.Age, (age, _) => age >= 18, "Must be 18+")
    .Build();
```

**Builder methods:**

| Method | Required | Description |
|---|---|---|
| `FromFile(path)` | Yes | Path to the CSV file |
| `WithHeaderRow(bool)` | No | Whether the first row contains headers (default `true`) |
| `WithDelimiter(char)` | No | Field delimiter character (default `','`) |
| `ForColumn(selector, validator, errorMessage)` | No | Adds a fluent column-level validator. Chainable |
| `Build()` | Yes | Returns a configured `CsvImporter<T>` |

## Column Mapping

### ExcelColumnAttribute

Map a DTO property to an Excel column by header name or 1-based column number. If no attribute is present, the property name is matched against the header row (case-insensitive).

```csharp
[ExcelColumn("Full Name")]    // match by header text
public string Name { get; set; }

[ExcelColumn(3)]              // match by column number (1-based)
public int Age { get; set; }

public string Email { get; set; }  // auto-matches header "Email" or "email"
```

### CsvColumnAttribute

Map a DTO property to a CSV column by header name or 1-based column number. If no attribute is present, the property name is matched against the header row (case-insensitive). When `hasHeaderRow` is `false`, only properties with a numeric `CsvColumn` attribute are mapped.

```csharp
[CsvColumn("Full Name")]    // match by header text
public string Name { get; set; }

[CsvColumn(3)]              // match by column number (1-based)
public int Age { get; set; }

public string Email { get; set; }  // auto-matches header "Email" or "email"
```

### JsonColumnAttribute

Map a DTO property to a JSON property name. If no attribute is present, the property name is used with case-insensitive matching.

```csharp
[JsonColumn("full_name")]
public string Name { get; set; }

public string Email { get; set; }  // auto-matches "Email" or "email"
```

## Validation

All validation features are shared across Excel, JSON, and CSV importers. Call `Validate()` after `Import()`.

### InlineValidation (Roslyn expressions)

Write C# expressions directly as attribute strings. The variable `Row` refers to the current DTO instance. Expressions are compiled once and cached.

```csharp
[InlineValidation("Row.Age >= 18 && Row.Age <= 80",
    ErrorMessage = "Age must be 18-80",
    RuleName = "AgeRange")]
public int Age { get; set; }

[InlineValidation("Row.Email.Contains(\"@\")",
    ErrorMessage = "Email must contain @",
    RuleName = "EmailFormat")]
public string Email { get; set; }
```

Can also be placed at the class level for cross-field rules:

```csharp
[InlineValidation("Row.FirstName != Row.LastName",
    ErrorMessage = "First and last name cannot be identical")]
public sealed class PersonDto { ... }
```

### Validator (reusable class-based validation)

For complex logic, implement `IRowValidator<T>` and reference it with `[Validator]`:

```csharp
using DataImporters.Core.Validation;

public sealed class SalaryRangeValidator : IRowValidator<EmployeeDto>
{
    private static readonly Dictionary<string, (decimal Min, decimal Max)> Ranges = new()
    {
        ["Engineering"] = (60_000m, 250_000m),
        ["Marketing"]   = (40_000m, 180_000m),
    };

    public bool Validate(EmployeeDto row, out string? errorMessage)
    {
        if (Ranges.TryGetValue(row.Department, out var range)
            && (row.Salary < range.Min || row.Salary > range.Max))
        {
            errorMessage = $"Salary out of range for {row.Department}";
            return false;
        }
        errorMessage = null;
        return true;
    }
}

[Validator(typeof(SalaryRangeValidator), RuleName = "SalaryRange")]
public sealed class EmployeeDto { ... }
```

### Fluent ForColumn validation

Register validators at runtime using lambda expressions. The validator receives the property value and the full row for cross-field checks.

```csharp
var importer = new Importer<EmployeeDto>("file.xlsx")
    .ForColumn(x => x.Email,
        (email, _) => email.EndsWith("@acme.com", StringComparison.OrdinalIgnoreCase),
        "Email must be an @acme.com address")
    .ForColumn(x => x.Salary,
        (salary, row) => row.Department != "Intern" || salary <= 50_000m,
        "Interns cannot earn more than $50k");

importer.Import();
var errors = importer.Validate();
```

Works identically with `JsonImporter<T>` and `CsvImporter<T>`:

```csharp
var importer = new JsonImporter<EmployeeDto>("file.json")
    .ForColumn(x => x.Email,
        (email, _) => email.EndsWith("@acme.com", StringComparison.OrdinalIgnoreCase),
        "Email must be an @acme.com address");
```

```csharp
var importer = new CsvImporter<EmployeeDto>("file.csv")
    .ForColumn(x => x.Email,
        (email, _) => email.EndsWith("@acme.com", StringComparison.OrdinalIgnoreCase),
        "Email must be an @acme.com address");
```

## Reporting

After validation, generate HTML or JSON reports to a file or clipboard.

```csharp
var report = importer.CreateReportGenerator();

// HTML report to file
report.Generate(ReportFormat.Html, ReportDestination.File, outputFilePath: "report.html");

// JSON report to file
report.Generate(ReportFormat.Json, ReportDestination.File, outputFilePath: "report.json");

// Copy to clipboard
report.Generate(ReportFormat.Json, ReportDestination.Clipboard);

// Filter rows in the report
report.Generate(ReportFormat.Html, ReportDestination.File,
    filter: ReportGenerator<EmployeeDto>.RowFilter.Invalid,
    outputFilePath: "invalid-only.html");
```

**Row filters:** `All` (default), `Valid`, `Invalid`

**Report formats:** `Html`, `Json`

**Destinations:** `File`, `Clipboard`

## Supported Types

All three importers handle these property types (including their `Nullable<T>` forms):

`string`, `int`, `long`, `double`, `decimal`, `DateTime`, `bool`

## API Summary

`Importer<T>`, `JsonImporter<T>`, and `CsvImporter<T>` share the same API shape:

| Method | Description |
|---|---|
| `Import()` | Reads all rows into DTOs. Returns `ReadOnlyCollection<T>`. |
| `Validate()` | Runs all validation rules. Returns `ReadOnlyCollection<ValidationResult>`. |
| `GetValidRows()` | Returns rows with zero errors. |
| `GetInvalidRows()` | Returns rows with at least one error. |
| `ForColumn(selector, validator, errorMessage)` | Adds a fluent column-level validator. Returns `this` for chaining. |
| `CreateReportGenerator()` | Returns a `ReportGenerator<T>` for generating reports. |

Call order: `Import()` -> `Validate()` -> `GetValidRows()` / `GetInvalidRows()` / `CreateReportGenerator()`

## Building NuGet Packages

All four library projects are configured for NuGet package generation. Packages are produced automatically on every build.

```bash
# Build Release and generate packages to ./nupkgs
dotnet build DataImporters.slnx -c Release && dotnet pack DataImporters.slnx -o ./nupkgs --no-build
```

This produces:

| Package | Description |
|---|---|
| `DataImporters.Core.1.0.0.nupkg` | Shared validation engine, attributes, and report generation |
| `DataImporters.Excel.1.0.0.nupkg` | Excel importer (depends on `DataImporters.Core`) |
| `DataImporters.Json.1.0.0.nupkg` | JSON importer (depends on `DataImporters.Core`) |
| `DataImporters.Csv.1.0.0.nupkg` | CSV importer (depends on `DataImporters.Core`) |

Each package multi-targets `netstandard2.0`, `net9.0`, and `net10.0`. Per-TFM dependency groups ensure that `net9.0`/`net10.0` consumers don't pull in polyfill packages like `PolySharp` or `System.Text.Json`.

## Dependencies

| Package | Used By | Purpose |
|---|---|---|
| ClosedXML 0.105.0 | DataImporters.Excel | Excel file reading |
| Microsoft.CodeAnalysis.CSharp.Scripting 5.0.0 | DataImporters.Core | Roslyn inline expression compilation |
| TextCopy 6.2.1 | DataImporters.Core | Clipboard support for reports |
| System.Text.Json 9.0.4 | DataImporters.Core (netstandard2.0 only) | JSON serialization for reports |
| PolySharp 1.15.0 | All src projects (netstandard2.0 only) | Language feature polyfills (`init`, records) |

> **Note:** `DataImporters.Csv` has no external dependencies beyond `DataImporters.Core`. CSV parsing is handled inline with a built-in RFC 4180 parser.

# Mubarrat.Fonts

> Modern, strongly typed, AOT-compatible font library and tooling for .NET.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.md)
[![.NET](https://img.shields.io/badge/.NET-11.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![GitHub repository](https://img.shields.io/badge/GitHub-Mubarrat%2FMubarrat.Fonts-181717?logo=github)](https://github.com/Mubarrat/Mubarrat.Fonts)

**Mubarrat.Fonts** is a framework for working with font formats and their underlying binary structures in .NET. It aims to combine low-level control over font data with strongly typed APIs, systematic validation, and an architecture suitable for modern .NET applications.

The project is under active development. Support varies by format, specification feature, and implementation maturity. See the [support matrix](#support-matrix) before relying on a particular capability.

## Table of Contents

- [Goals](#goals)
- [Support Matrix](#support-matrix)
- [Architecture](#architecture)
- [Projects and Packages](#projects-and-packages)
- [Getting Started](#getting-started)
- [Design Principles](#design-principles)
- [Validation and Diagnostics](#validation-and-diagnostics)
- [Development Roadmap](#development-roadmap)
- [Contributing](#contributing)
- [License](#license)

## Goals

Mubarrat.Fonts aims to provide:

- **Broad format coverage:** a common foundation for font containers, binary structures, and format-specific data.
- **Strongly typed APIs:** typed representations of font metadata, tables, records, and other specification-defined structures.
- **Low-level access:** control over binary data and parsing behavior without forcing every consumer to use a high-level abstraction.
- **Validation and diagnostics:** systematic checks for malformed data, violated specification requirements, and implementation-specific constraints.
- **AOT and trimming compatibility:** an architecture designed to minimize unnecessary reflection and support modern .NET deployment models.
- **Allocation-conscious parsing:** efficient binary reading without imposing an unrealistic requirement that every operation be allocation-free.
- **Maintainable specification coverage:** clear boundaries between shared infrastructure and format-specific implementations.
- **Minimal dependencies:** a preference for framework-provided `System.*` and `Microsoft.*` APIs and packages.

The goal is not merely to read font files. It is to build a reusable foundation for parsing, inspecting, validating, and eventually manipulating a broad range of font formats.

## Support Matrix

Support must be evaluated at the **feature level**, not merely by whether a format or parser exists. A format can have working infrastructure while still lacking parts of its specification.

| Format or subsystem | Scope | Status |
|---|---|---|
| SFNT infrastructure | Shared font container structures, directories, tags, and related binary mechanics | In development |
| TrueType | TrueType-specific structures and outline data | In development |
| OpenType | OpenType tables and layout-related structures | In development |
| Apple Advanced Typography (AAT) | Apple-specific tables and state-machine structures | In development |
| CFF and CFF2 | Compact Font Format outlines and related structures | Partial; verify individual features |
| Color fonts | Color palettes, color glyph data, and paint structures | Partial; verify individual features |
| Variable fonts | Variation-related tables and structures | Partial; verify individual features |
| WOFF | Web Open Font Format container and decoding | Verify implementation status before use |
| WOFF2 | Web Open Font Format 2 container and decoding | Verify implementation status before use |
| PostScript Type 1 | Type 1 font structures and parsing | Verify implementation status before use |

**Status definitions**

- **Implemented:** the documented feature has an implementation and appropriate validation or tests.
- **Partial:** some functionality exists, but coverage or behavior is incomplete.
- **In development:** implementation is actively evolving; do not assume complete specification coverage.
- **Planned:** intended work that has not yet been implemented.
- **Unsupported:** explicitly outside the supported scope.

The matrix is intentionally conservative. Update each entry as implementation and tests establish its status. Do not label an entire specification as fully supported merely because several of its tables or structures have been implemented.

For format-specific details, consult the relevant project documentation and the specification coverage recorded in the source and tests.

## Architecture

The architecture separates common binary infrastructure from format-specific interpretation.

```text
Font data
   │
   ▼
Container and binary infrastructure
   │
   ├── Source and random-access reads
   ├── Tags, primitives, and binary records
   ├── Directory and offset handling
   │
   ▼
Format-specific structures
   │
   ├── TrueType
   ├── OpenType
   ├── AAT
   ├── PostScript
   └── Web font formats
   │
   ▼
Typed data and validation
   │
   ├── Parsed representations
   ├── Diagnostics
   └── Consumer applications
```

This is a conceptual architecture, not a claim that every layer or format is already complete.

### Binary infrastructure

The binary layer provides abstractions for reading data at offsets and decoding structured records. Keeping storage access separate from format interpretation allows parsers to work with different backing sources without duplicating all their reading logic.

### Format-specific implementations

Format-specific code interprets binary structures according to the corresponding specifications. Shared primitives and container mechanics should be reused where appropriate, while format-specific rules remain explicit.

### Diagnostics

Validation can be layered over parsed data to report problems with stable identifiers, meaningful severity levels, and structured information. Parsing and validation are related but distinct: successfully reading a structure does not necessarily mean the input font is valid.

## Projects and Packages

The repository is organized around reusable library components. The exact project and package boundaries should follow the current solution structure.

| Component                    | Responsibility                                       |
| ---------------------------- | ---------------------------------------------------- |
| `Mubarrat.Fonts`             | Core font parsing infrastructure and typed font data |
| `Mubarrat.Fonts.Diagnostics` | Font validation, analysis, and diagnostics           |
| `Mubarrat.Fonts.Tests`       | Automated tests and font fixtures                    |

The names above identify existing project areas; they do not imply that every planned format already has a separate package.

As the framework grows, shared SFNT infrastructure and format-specific implementations should remain clearly separated so that consumers can depend on the components they actually need.

## Getting Started

### Requirements

- A compatible .NET SDK for the target framework configured by the solution.
- Git, if you intend to build or contribute from the repository.

### Clone the repository

```bash
git clone https://github.com/Mubarrat/Mubarrat.Fonts.git
cd Mubarrat.Fonts
```

### Build

```bash
dotnet build
```

### Run tests

```bash
dotnet test
```

These commands are the general starting point. Additional SDK requirements, test prerequisites, or project-specific instructions should be documented alongside the relevant project.

### Use the library

For applications that need the existing OpenType implementation, reference the relevant project during development:

```bash
dotnet add <your-project.csproj> reference <path-to-project.csproj>
```

Replace the placeholders with the actual application project and library project paths.

A project reference uses the source project directly and does not require a published NuGet package. If packages are published, their installation instructions should be documented separately and should identify the corresponding package names.

## Design Principles

### Strongly typed data

Where practical, binary structures and parsed semantic data should have explicit types instead of requiring consumers to interpret unstructured byte sequences.

Types should reflect the distinction between:

- Raw or wire-format data.
- Intermediate parsing structures.
- Parsed semantic representations.
- Validation results and diagnostics.

### Explicit parsing contracts

Parsing APIs should make the consumer-facing entry points clear and separate them from implementation-level parsing contracts.

For the existing OpenType infrastructure, `Source.ReadRecordAt<T>(offset)` is a public parsing entry point. `IRecord<T>.Parse` belongs to the parsing infrastructure and should not be presented as an ordinary consumer-facing API.

### Allocation-conscious parsing

Performance-sensitive code should avoid unnecessary temporary objects, buffers, and conversions. However, parsing may legitimately allocate arrays, records, strings, and other objects needed by the resulting representation.

The objective is to minimize avoidable allocations while preserving correctness, clarity, and maintainability—not to claim universal zero-allocation behavior.

### AOT and trimming

The project aims to support modern .NET deployment models, including trimming and Native AOT where compatible.

Avoiding unnecessary reflection and making type relationships explicit can help achieve these goals. Actual compatibility must still be verified through builds, analyzers, and publish tests for the relevant projects and consuming applications.

### Specification-driven correctness

Implementations should be checked against the applicable specifications and meaningful test cases. A successful build is not proof of complete specification compliance.

Tests should cover normal inputs, boundary values, malformed data, and relevant error-handling behavior.

### Dependency discipline

Prefer APIs supplied by the .NET platform and dependencies within `System.*` or `Microsoft.*` where they meet the requirement.

External dependencies should have a clear justification, licensing compatibility, and a maintainability benefit that outweighs their cost.

## Validation and Diagnostics

The project aims to make font validation systematic and useful to both library developers and consumers.

Useful validation should distinguish among:

- Invalid binary structure.
- Missing or inconsistent data.
- Violations of format-specific requirements.
- Suspicious or nonconforming data that may still be readable.
- Unsupported features or incomplete implementation coverage.

Diagnostics should have stable identifiers where practical. Their messages should explain the problem, and their severity should reflect the nature of the finding.

Not every malformed font can or should be handled identically. Implementations should document whether a condition causes parsing to fail, produces a diagnostic, or is handled through another explicit policy.

## Development Roadmap

The roadmap is organized by engineering outcomes rather than promises of complete format coverage.

| Area | Objective |
|---|---|
| Shared infrastructure | Strengthen common binary primitives, source abstractions, container handling, and parsing contracts |
| OpenType and TrueType | Expand specification coverage and verify existing implementations with focused tests |
| AAT | Continue implementing and validating Apple-specific structures |
| PostScript formats | Establish and expand support for applicable PostScript-based font formats |
| WOFF and WOFF2 | Implement or complete container decoding and integrate it with the shared parsing architecture |
| Diagnostics | Improve validation coverage, diagnostic consistency, and actionable error reporting |
| Correctness | Add boundary, malformed-input, regression, and specification-derived tests |
| Performance | Measure parsing throughput, allocation behavior, and memory use before making performance claims |
| AOT and trimming | Validate supported deployment configurations through build and publish testing |
| Documentation | Keep public API examples, support statuses, and implementation claims aligned with the code |

This roadmap describes intended engineering work, not a guarantee that every item is implemented or scheduled for a particular release.

## Contributing

Contributions, bug reports, and carefully scoped improvements are welcome.

Before submitting changes:

1. Review the repository's [contribution guidelines](CONTRIBUTING.md).
2. Identify the relevant specification and expected behavior.
3. Add or update tests for the change.
4. Run the build and test suite.
5. Update documentation if public behavior or support status changes.

When implementing format features, clearly distinguish required specification behavior from optional capabilities and implementation-specific choices. Avoid claiming full support until the relevant scope has been implemented and tested.

For security-sensitive reports, follow the repository's security reporting policy in [SECURITY.md](SECURITY.md).

## License

Mubarrat.Fonts is distributed under the MIT License. See [LICENSE.md](LICENSE.md) for the full license text.

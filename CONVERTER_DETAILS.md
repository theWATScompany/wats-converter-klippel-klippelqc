# Technical Details: Klippel QC

> **Source of truth for the Zendesk "Converter library" article.**
> Edit this file to add prose explanations, assumptions, and limitations.
> Technical tables (parameters, regex patterns, WATS step mapping) are always
> auto-extracted from source code when `generate-library-article.ps1` is run.

## Overview

Converts Klippel QC audio test result files to WATS UUT reports.

<!-- TODO: Expand the overview - explain the architecture, known format variants,
     and any integration-specific context not covered by the README. -->

## File Format Assumptions and Requirements

<!-- TODO: Document what the converter assumes about the input format.
     Examples: encoding (UTF-8/ASCII), line endings, required vs optional fields,
     field order constraints, supported file version(s) -->
- Input encoding: UTF-8
- <!-- TODO: add format-specific assumptions -->

## Hardcoded Values and Defaults

<!-- TODO: list hardcoded values, magic numbers, and why they exist -->

<!-- TODO: Explain why hardcoded values exist and any customer-agreed constants -->

## Converter Parameter Details

| Parameter | Default | Effect |
|-----------|---------|--------|
| `partNumber` | `PartNumber1` | <!-- TODO: describe effect --> |
| `partRevision` | `1.0` | <!-- TODO: describe effect --> |
| `sequenceName` | `SequenceName1` | <!-- TODO: describe effect --> |
| `sequenceVersion` | `1.0.0` | <!-- TODO: describe effect --> |
| `operationTypeCode` | `10` | <!-- TODO: describe effect --> |

<!-- TODO: For each parameter, describe what it affects and list valid values -->

## Known Limitations and Edge Cases

<!-- TODO: Document any known issues, unsupported features, or edge cases
     that operators should be aware of -->
- <!-- TODO -->

## Configuration Guidance

Parameters are configured in the WATS Client Configurator under the converter's **Arguments** XML.
Example:

```xml
<Arguments>
  <!-- TODO: paste a real example Arguments XML block here -->
</Arguments>
```

## Change History

See [CHANGELOG.md](CHANGELOG.md) for version history.
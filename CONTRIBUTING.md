# Contributing to Mubarrat.Fonts

Thank you for considering a contribution to **Mubarrat.Fonts**. Contributions of code, tests, documentation, specification research, bug reports, and carefully explained design proposals are welcome.

This document describes the repository-wide contribution process: how to propose work, prepare changes, validate implementations, document behavior, and submit pull requests. It establishes a common baseline for contributors working anywhere in the repository.

Implementation details that apply only to a particular project or directory should be documented in that project's own contribution guide. This document should remain focused on practices that apply across the repository.

## Table of Contents

- [Contribution Principles](#contribution-principles)
- [Repository Documentation](#repository-documentation)
- [What You Can Contribute](#what-you-can-contribute)
- [Before You Start](#before-you-start)
- [Development Workflow](#development-workflow)
- [General Engineering Expectations](#general-engineering-expectations)
- [Testing and Validation](#testing-and-validation)
- [Documentation Expectations](#documentation-expectations)
- [Dependencies and Licensing](#dependencies-and-licensing)
- [Commit Messages](#commit-messages)
- [Pull Requests](#pull-requests)
- [Review and Follow-up](#review-and-follow-up)
- [Using AI-Assisted Tools](#using-ai-assisted-tools)
- [Community Conduct and Security Reports](#community-conduct-and-security-reports)
- [Questions](#questions)

---

## Contribution Principles

The goal of a contribution is not merely to add code. It is to make the repository more correct, useful, understandable, maintainable, or complete without needlessly compromising its existing design.

Contributions should follow these principles:

1. **Correctness before breadth.** A smaller, well-tested change is preferable to a broad change whose behavior or specification interpretation is uncertain.
2. **Evidence before assumptions.** When correctness depends on a specification, use the relevant specification, authoritative technical documentation, or a reproducible test instead of relying on memory or intuition.
3. **Consistency before novelty.** Reuse established repository patterns when they fit. Discuss a substantially different approach before introducing it.
4. **Explicit scope.** Keep each change focused on one coherent problem. Separate unrelated fixes, refactoring, and enhancements whenever practical.
5. **Reviewability.** Make it possible for another developer to understand what changed, why it changed, and how its correctness was checked.
6. **Honest status reporting.** Distinguish implemented behavior from partial support, planned work, experimental functionality, assumptions, and known limitations.
7. **Respectful collaboration.** Technical disagreement is welcome. Personal attacks, harassment, and dismissive conduct are not.

## Repository Documentation

Use the following resources to understand the project, its requirements, and its contribution procedures.

| Resource | Purpose |
| --- | --- |
| [Repository README](README.md) | Project overview, documented capabilities, structure, and usage information. Verify claims against the current implementation when necessary. |
| [Code of Conduct](CODE_OF_CONDUCT.md) | Expected behavior in project spaces and how to report conduct concerns. |
| [Security Policy](SECURITY.md) | Private vulnerability reporting, supported-version policy, and security-reporting expectations. |
| [License](LICENSE.md) | The repository's MIT license and its terms. |
| [Mubarrat.Fonts Contributing Guide](Mubarrat.Fonts/CONTRIBUTING.md) | Project-specific architecture, binary parsing conventions, font-table implementation, diagnostics, specification coverage, and testing requirements. |

This global guide is the shared baseline for contributions throughout the repository.

A project-specific guide, when added, should explain the relevant project's architecture, directory structure, implementation patterns, API conventions, specification requirements, and specialized testing procedures. It should complement this guide rather than unnecessarily duplicate it.

If a project requires an exception to a repository-wide convention, document the exception and its rationale explicitly. Do not assume that a convention is irrelevant simply because a project has a different implementation.

## What You Can Contribute

Contributions can improve functionality, correctness, documentation, developer experience, or the long-term maintainability of the repository.

| Contribution type | Examples | What makes the contribution useful |
| --- | --- | --- |
| Bug fixes | Incorrect results, invalid boundary handling, unexpected exceptions, regressions | A reproducible failure and a regression test that demonstrates the correction |
| Feature implementation | New capabilities, additional format support, missing operations | A clear scope, a suitable design, and tests for expected behavior |
| Specification support | Implementing a missing format, version, structure, or requirement | An authoritative reference and tests that distinguish correct behavior from incorrect interpretations |
| Testing | Regression tests, malformed-input cases, edge cases, improved test fixtures | Deterministic tests that exercise meaningful behavior |
| Performance | Fewer allocations, reduced redundant work, improved throughput | Measurements or a focused benchmark when performance is the motivation |
| Diagnostics and error handling | More useful errors, consistent validation, better failure reporting | Clear expected behavior and tests for both the failure and relevant neighboring cases |
| Documentation | README improvements, API documentation, architecture explanations, contribution guidance | Accurate descriptions that match the current implementation |
| Build and maintenance | Build fixes, configuration improvements, removal of unnecessary complexity | A clear explanation of the benefit and validation of affected workflows |
| Research and design | Specification research, interoperability analysis, architectural proposals | Reliable sources, reproducible examples, alternatives, and explicit trade-offs |

A contribution does not have to be large to be valuable. A focused bug report, a precise specification clarification, or a test that captures a subtle edge case can prevent future regressions.

### Contribution quality

A useful contribution should answer three questions:

- **What problem does it solve?**
- **Why is the proposed solution appropriate?**
- **What evidence shows that the solution works?**

The evidence may consist of tests, a specification reference, a minimal reproduction, measurements, or a clear explanation of a documentation correction.

## Before You Start

### Decide whether discussion is needed

Small, self-contained changes can usually be submitted directly. Changes affecting public APIs, architecture, dependencies, major behavior, or a substantial amount of functionality should generally be discussed first.

| Proposed change | Recommended approach |
| --- | --- |
| Typo or small documentation correction | Submit a focused pull request |
| Reproducible bug with a clear expected result | Open an issue or submit a fix with a regression test |
| New feature or substantial specification support | Discuss scope and design before implementation |
| Public API or compatibility change | Discuss the impact on existing consumers |
| New runtime, build, or test dependency | Explain why existing framework and repository facilities are insufficient |
| Large refactoring | Propose a plan and divide it into reviewable stages where possible |
| Cross-project change | Identify all affected projects and the validation required for each |
| Suspected security vulnerability | Follow [SECURITY.md](SECURITY.md); do not disclose exploit details in a public issue |

An issue or discussion is an opportunity to establish the problem and agree on a reasonable scope. It does not guarantee that a proposal will be accepted or implemented.

Before opening a new issue, check whether an existing issue already covers the problem. Add new evidence to the existing discussion when appropriate instead of creating duplicates.

### Understand the existing implementation

Before making a change:

- Read the relevant source files and nearby implementations.
- Inspect existing tests to understand expected behavior and test conventions.
- Check whether an equivalent abstraction or operation already exists.
- Consult authoritative specifications or technical documentation where correctness depends on them.
- Identify public API, compatibility, performance, allocation, and error-handling implications.
- Review existing issues and documented limitations when relevant.
- Confirm which projects and tests will be affected.

Do not assume that a README, comment, issue description, or generated explanation is more authoritative than the implementation and the applicable specification. If these sources disagree, investigate the discrepancy and describe it clearly.

## Development Workflow

The following workflow is recommended for changes of any size. Skip steps that genuinely do not apply to a small documentation-only correction.

### 1. Identify the problem

Describe the current behavior, the expected behavior, and why the difference matters.

For a bug, include a minimal reproduction when possible. For a feature, describe the intended behavior and its boundaries. For a documentation change, identify the ambiguity, omission, or incorrect claim being corrected.

### 2. Establish the scope

Determine which source files, projects, APIs, tests, and documents need to change.

Avoid expanding a focused task into an unrelated cleanup project. If investigation reveals an independent issue, consider reporting it separately.

### 3. Follow the applicable conventions

Read the relevant project documentation and inspect neighboring code. When no project-specific guide exists, use established implementations and tests as evidence of local conventions.

Do not invent new repository-wide rules based on a single implementation detail.

### 4. Create a focused branch

Use a descriptive branch name that communicates the purpose of the work.

Examples:

- `fix-offset-validation`
- `add-format-tests`
- `improve-parser-diagnostics`
- `docs-contribution-guide`

Follow any more specific branching convention established for the relevant project or workflow.

### 5. Implement the smallest coherent change

Prefer a focused implementation that solves the stated problem without introducing unrelated behavior changes.

Avoid unnecessary renames, broad formatting changes, speculative abstractions, duplicated logic, and new dependencies that have not been justified.

### 6. Add or update tests

Tests should establish the expected behavior and prevent the same problem from recurring.

Where applicable, include ordinary cases, boundary conditions, invalid input, and interactions with related functionality.

### 7. Validate locally

Build and test the affected projects. Review the output and investigate failures rather than assuming they are unrelated.

If a check cannot run because of an unavailable SDK, missing fixture, unsupported platform, or another environmental limitation, record the limitation accurately.

### 8. Update documentation

Update user-facing and developer-facing documentation when behavior, supported functionality, configuration, public APIs, or contribution procedures change.

Ensure examples and links remain valid.

### 9. Review the final diff

Inspect the complete change before committing. Check for accidental files, generated output, local paths, secrets, unrelated modifications, and misleading claims.

### 10. Submit a pull request

Explain the problem, the implementation, and the validation performed. Respond constructively to review feedback and rerun relevant checks after revisions.

## General Engineering Expectations

The implementation details differ between projects, but these expectations apply across the repository.

### Correctness and robustness

- Validate input at the appropriate abstraction boundary.
- Consider empty input, boundary values, truncated input, invalid lengths, unexpected versions, and inconsistent references where applicable.
- Do not silently convert invalid or unsupported input into plausible-looking but incorrect output.
- Preserve useful error context without exposing secrets or dumping unnecessarily large amounts of input.
- Do not suppress exceptions or weaken validation merely to make a test pass.
- Make intentional limitations explicit.
- Ensure that tests reflect the intended behavior rather than simply repeating the implementation's assumptions.

### Maintainability

- Prefer descriptive, direct code over cleverness that obscures control flow or invariants.
- Use names that accurately represent the concepts they model.
- Avoid catch-all utility classes, duplicated rules, and abstractions without a clear purpose.
- Explain non-obvious decisions, invariants, and workarounds in comments when useful.
- Avoid changing public behavior as an incidental result of refactoring.
- Keep formatting changes limited to the relevant code unless broader formatting is the explicit purpose of the change.
- Do not introduce infrastructure solely because it might be useful in a hypothetical future scenario.

### Public API and compatibility

Public API changes should be deliberate and justified.

| Compatibility consideration | Questions to ask |
| --- | --- |
| Source compatibility | Will existing consumer code still compile? |
| Binary compatibility | Could existing compiled consumers break? |
| Behavioral compatibility | Does an existing operation now produce different results or exceptions? |
| Performance compatibility | Could the change introduce significant allocation or latency regressions? |
| Documentation compatibility | Do existing examples and documented assumptions remain accurate? |
| Migration | If a breaking change is necessary, is there a clear migration path? |

Do not remove, rename, or change the meaning of a public member merely to simplify an internal implementation without considering downstream impact.

Avoid claiming support for a feature solely because its types, interfaces, or initial implementation exist. Describe actual support and known limitations accurately.

### Performance

Performance changes should address a demonstrated problem or protect an important hot path.

When relevant:

- Compare equivalent work before and after the change.
- Keep input data and measurement conditions comparable.
- Consider allocations, retained memory, throughput, and latency as appropriate.
- Include benchmark results when they materially support the proposed improvement.
- Avoid sacrificing correctness, safety, or maintainability for speculative gains.

A benchmark is evidence, not a substitute for correctness tests. Faster incorrect behavior is not an improvement.

## Testing and Validation

Tests are part of the implementation. They define intended behavior, make review more objective, and help prevent regressions.

### Validation by change type

| Change type | Expected validation |
| --- | --- |
| Documentation-only | Check Markdown structure, relative links, spelling, and consistency with the current implementation |
| Bug fix | Add a regression test where practical and run the affected tests |
| New feature | Test normal use, boundaries, invalid input, and relevant interactions |
| Refactoring | Run tests that demonstrate existing behavior remains intact |
| Public API change | Build affected projects and test representative consumer-facing usage |
| Performance optimization | Run correctness tests and measure performance when practical |
| Build or configuration change | Run the affected build workflow and document environmental requirements |
| Cross-project change | Build and test every affected project |
| Test infrastructure change | Verify that the tests still execute and that failures are reported correctly |

### Standard .NET checks

From the repository root, begin with:

\`\`\`bash
dotnet build
dotnet test
\`\`\`

Use the SDK and configuration required by the current project files. Run more targeted checks when they help isolate a problem, but do not substitute a narrow successful test for broader validation when the change affects shared infrastructure.

### Test quality

Where applicable, tests should:

- Exercise the behavior that changed.
- Assert meaningful results rather than merely asserting that no exception occurred.
- Include boundary and failure cases.
- Be deterministic and reproducible.
- Avoid unnecessary network access, timing dependencies, and machine-specific assumptions.
- Preserve the strength of existing assertions.
- Include enough context to make failures understandable.

Do not delete or weaken a test merely to make the suite pass. If the expected behavior has genuinely changed, update the test and explain why.

### When validation cannot be completed

Sometimes a test requires a fixture that cannot be redistributed, a platform that is unavailable, or a toolchain that is not installed.

In such cases, explain exactly what could not be run, why it could not be run, and what evidence was obtained instead. Do not describe an unexecuted test as passing.

## Documentation Expectations

Documentation should help readers understand what a feature does, how to use it, what it guarantees, and what it does not guarantee.

### Documentation change matrix

| Documentation area | Update it when |
| --- | --- |
| README | Setup, usage, project structure, capabilities, or major limitations change |
| Public API documentation | A public type or member is added, changed, or gains important constraints |
| Architecture documentation | Component boundaries, data flow, or major design decisions change |
| Examples | Public APIs, setup requirements, or expected output change |
| Test guidance | Test commands, fixtures, or validation workflows change |
| Contribution guide | General workflow or project-specific implementation conventions change |
| Security documentation | Vulnerability reporting or security expectations change |
| Release notes or changelog | The repository's release process calls for documenting a user-visible change |

### Accuracy and status

Documentation must distinguish between:

- **Implemented:** behavior exists in the current implementation.
- **Partially implemented:** only some formats, versions, cases, or operations are supported.
- **Planned:** work is intended but has not been implemented.
- **Experimental:** behavior exists but is not yet considered stable.
- **Unsupported:** behavior is intentionally absent or outside the current implementation.

Do not describe planned work as implemented, imply comprehensive coverage without evidence, or claim that a feature has been tested when it has not.

Examples should compile against the current public API unless explicitly marked as pseudocode or conceptual examples. When a public API changes, search for examples and documentation that may have become outdated.

### Markdown links

Every link added to repository documentation should be checked.

- Confirm that the target file or page exists.
- Resolve relative paths from the Markdown file that contains the link.
- Check heading anchors when linking to a section.
- Prefer canonical repository paths for internal documents.
- Do not add a link to a planned file as if the file already exists.
- If a relevant guide has not yet been created, state that it has not been added instead of linking to a nonexistent destination.

## Dependencies and Licensing

Keep dependencies small, justified, and compatible with the repository's goals.

Before proposing a new package, generator, runtime dependency, or external service, explain:

1. What capability is missing.
2. Why existing framework APIs and repository code are insufficient.
3. Whether the dependency is required at runtime, build time, or test time.
4. Its licensing, maintenance, security, and compatibility implications.
5. Whether the same outcome can be achieved without adding long-term maintenance cost.

This repository has a strict dependency preference for \`System.*\` and \`Microsoft.*\` APIs and packages. Introducing dependencies outside that boundary requires explicit justification and discussion; do not treat such a dependency as an incidental implementation detail.

### Third-party material

Before copying code, font files, test data, documentation, or other assets into the repository, verify the applicable license and redistribution terms.

- Preserve required attribution and license notices.
- Do not assume that publicly downloadable material is free to redistribute.
- Do not introduce third-party code whose license is incompatible with the repository.
- Explain the origin of external fixtures and assets where appropriate.
- Prefer minimal, redistributable test fixtures when they are sufficient.

Contributions must comply with the repository's [MIT license](LICENSE.md). That license does not grant permission to redistribute third-party material whose own terms prohibit it.

### Secrets and sensitive data

Never commit credentials, access tokens, private keys, personal data, or other secrets.

If a secret is accidentally committed, deleting it in a later commit may not remove it from Git history. Treat it as exposed and rotate or revoke it promptly. Follow the relevant security-reporting process if exposure creates a broader risk.

## Commit Messages

Write commit subjects in the **imperative mood** and describe what the commit changes.

| Prefer | Avoid |
| --- | --- |
| \`Add tests for truncated table data\` | \`Added tests for truncated table data\` |
| \`Fix offset validation\` | \`Fixes offset validation\` |
| \`Document contribution workflow\` | \`Documentation update\` |
| \`Clarify supported format coverage\` | \`Some README changes\` |

Keep the subject concise—72 characters or fewer where practical.

For changes that need additional explanation, add a body after a blank line. Explain the motivation, design choices, relevant trade-offs, and testing performed.

Avoid vague subjects such as \`Update files\`, \`Changes\`, or \`Fix stuff\`. Do not add prefixes such as \`feat:\` or \`fix:\` unless the relevant repository workflow explicitly requires them.

Reference issues when useful. Use issue-closing keywords when the commit or pull request is intended to close the referenced issue.

A commit message should explain the change, not serve as a diary of the development session.

## Pull Requests

A pull request should be focused enough to review carefully and complete enough to evaluate on its own.

If a feature can be divided into independent, useful changes, prefer separate pull requests. If several changes must be reviewed together, explain their relationship in the pull request description.

### Pre-submission checklist

Before requesting review, confirm the items that apply:

- [ ] The change addresses a clearly described problem or agreed scope.
- [ ] The implementation follows the relevant project conventions.
- [ ] Tests were added or updated where appropriate.
- [ ] Affected builds and tests were run.
- [ ] Any validation that could not be performed is explained.
- [ ] Public API changes and user-visible behavior are documented.
- [ ] README examples and relative links remain valid.
- [ ] Dependencies and copied assets comply with repository policy and licensing.
- [ ] No secrets, local environment files, generated binaries, or unrelated changes were included.
- [ ] The final diff was reviewed for accidental modifications.
- [ ] The pull request description explains what changed, why it changed, and how it was validated.

### Pull request description

| Section | What to include |
| --- | --- |
| **What** | The behavior or documentation added, corrected, or removed |
| **Why** | The problem, requirement, or evidence that motivated the change |
| **Implementation** | Important design decisions, boundaries, and trade-offs |
| **Testing** | Commands, tests, fixtures, benchmarks, and environment details |
| **Limitations** | Known gaps, unverified behavior, or checks that could not be completed |
| **Related work** | Relevant issues, discussions, or dependent changes |

Example:

\`\`\`text
What:
Corrects validation for an offset beyond the available input.

Why:
The previous behavior allowed a read attempt outside the declared data
region.

Implementation:
Validates the offset and remaining length before attempting the read.

Testing:
Adds a regression test for the invalid offset and runs the relevant
test suite.
\`\`\`

Keep the description factual. Do not claim that tests passed, performance improved, or specification coverage is complete unless supporting evidence exists.

## Review and Follow-up

Code review is a collaborative quality check. Reviewers may request clearer tests, narrower scope, better naming, specification evidence, or a different design when the current approach introduces correctness, maintenance, or compatibility concerns.

### Contributor expectations

- Respond to technical feedback with evidence or a clear explanation of trade-offs.
- Ask for clarification when a requested change is ambiguous.
- Keep follow-up commits understandable and relevant to the pull request.
- Rerun relevant validation after changing behavior.
- Resolve review discussions when appropriate and supported by the platform.
- If you disagree with feedback, explain the technical reason respectfully.

### Reviewer expectations

- Focus on correctness, maintainability, security, consistency, and the stated scope.
- Explain why a requested change matters, especially when the reason is not self-evident.
- Distinguish blocking correctness issues from optional preferences.
- Avoid requesting unrelated cleanup in an otherwise focused pull request.
- Consider tests, documentation, and compatibility alongside the implementation.

A single review comment does not automatically establish a repository-wide convention. Repeated patterns and explicit project guidance should determine conventions. Substantial new rules should be discussed and documented.

## Using AI-Assisted Tools

AI-assisted coding, research, and writing tools may be used. However, the contributor remains responsible for everything submitted, including code, tests, documentation, licensing, and technical claims.

Before submitting AI-assisted work:

- Verify API names, framework behavior, specifications, and external references.
- Review generated code for incorrect assumptions, unnecessary abstractions, hidden dependencies, and unsafe input handling.
- Ensure tests validate expected behavior rather than simply repeating the generated implementation's assumptions.
- Confirm that examples and documentation match the actual repository state.
- Check the origin and license of any incorporated third-party code or assets.
- Never provide credentials, private keys, confidential information, or other secrets to tools that are not authorized to process them.
- Disclose AI assistance if a specific contribution process or reviewer reasonably requires it.

AI-generated output is subject to the same standards as manually written work. The use of AI does not reduce the contributor's responsibility or the standard of review.

## Community Conduct and Security Reports

All contributors are expected to follow the [Code of Conduct](CODE_OF_CONDUCT.md). Keep discussions respectful, focus criticism on ideas and implementations, and do not publish another person's private information.

**Do not report suspected vulnerabilities through a public issue or pull request.** Follow the private reporting procedure in [SECURITY.md](SECURITY.md), which explains how to submit a report and what information to include.

Ordinary correctness bugs and specification-conformance problems can generally be reported through the public issue tracker unless they also create a security risk. If you are unsure whether a problem is security-sensitive, consult the security policy before disclosing technical details publicly.

## Questions

If you are unsure where a change belongs, whether a design fits the project, or how to validate a behavior, ask a focused question or open an issue before undertaking a large implementation.

Include enough context for another contributor to understand the problem, what you have already investigated, and what decision or clarification you need.

Thank you for helping improve Mubarrat.Fonts.

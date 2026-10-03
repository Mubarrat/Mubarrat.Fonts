# Security Policy

## Supported Versions

Only the latest published release of each package receives security fixes.

| Package | Supported |
|---|---|
| `Mubarrat.Fonts.OpenType` | Latest release |
| `Mubarrat.Fonts.OpenType.Diagnostics` | Latest release |

If you are using an older version, upgrade to the latest before reporting. If the issue reproduces on the latest release, report it. If it only reproduces on an older version, the fix is to upgrade.

## Reporting a Vulnerability

**Do not open a public GitHub issue for a security vulnerability.** Public disclosure before a fix is available puts every user of the library at risk.

Report privately by emailing:

**[security@mubarrat.com](mailto:security@mubarrat.com)**

You will receive an acknowledgment within **5 business days**. If you do not, check your spam folder, then resend — the message may have been filtered. If the second message also goes unanswered, open a public issue that says only "I tried to reach the security contact and received no response" without describing the vulnerability itself.

## What to Include

A useful report contains enough information for the maintainer to reproduce the issue without guessing. Include:

- **The library version.** The exact version from the package reference or the commit SHA.
- **The .NET version and runtime.** `dotnet --info` output if possible, or at minimum the target framework.
- **The input that triggers the issue.** A font file is ideal. If the font is large or cannot be shared, a minimal reproducer that extracts the relevant table bytes is equally useful.
- **The operation that triggers the issue.** `FontFace.Parse`, `face.GetTable<GsubTable>()`, `cmap.GetGlyphId(0x1F600)`, or whichever call site is involved.
- **The observed behavior.** A stack trace, a crash dump, an exception message, or a description of the wrong output. "It hangs" is useful if you include how long you waited and what memory growth looked like.
- **The expected behavior.** What should have happened instead.

If the vulnerability is a memory safety issue, a proof-of-concept font is the single most valuable thing you can provide. A crash under a debugger is worth a page of prose.

## Response Timeline

| Stage | Target |
|---|---|
| Acknowledgment | 5 business days |
| Initial assessment (severity, scope) | 10 business days |
| Fix or mitigation available | 30 business days for critical, best effort otherwise |
| Public disclosure | Coordinated with the reporter |

These are targets, not guarantees. A single-maintainer project may take longer for complex issues. If a fix is going to take substantially longer than 30 days, you will be told why and given a rough estimate.

## Scope

A vulnerability in this library is an issue that a hostile font file can exploit to affect the process or user beyond the library's contract. The library's contract is that parsing an arbitrary byte sequence either produces a valid `FontFace` and its tables, or throws a well-defined exception.

### In scope

- **Memory safety violations.** Reading or writing memory outside the intended bounds. The library uses `MemoryMarshal.AsBytes`, `MemoryMarshal.Cast`, and `Span<T>` extensively. Any bounds-check bypass in those paths, any `IndexOutOfRangeException` that is not caught and translated, any access to freed or uninitialized memory is a security bug.
- **Unbounded resource consumption.** A font file that declares an absurd count or length and causes the parser to allocate gigabytes, spin for hours, or exhaust the stack. The library has explicit safety limits (`h.NumSizes > 1024`, `numStrikes > 65535`, `numGroups > 1_000_000`, subroutine nesting depth, etc.); a font that bypasses them is a reportable issue. A font that stays within a limit but still causes exhaustion is a design question — include the input and the resource profile in the report.
- **Infinite loops or hangs.** Any input where a `Parse` call fails to terminate, whether the loop is in the parser, the CFF charstring interpreter, or the composite glyph resolver.
- **Stack exhaustion.** Deeply recursive structures — composite glyphs referencing each other, CFF subroutines calling each other, COLR paint graphs — that exceed the intended recursion depth and crash the process with a `StackOverflowException`. The library has bounds on all three; a font that circumvents them is a reportable issue.
- **Denial of service via pathological input.** A font that is valid but takes an unreasonable amount of time or memory to parse, such that a service accepting arbitrary fonts from users would be degraded. Include a measurement of the resource consumption.
- **Any issue that causes an unhandled exception to escape a public API.** The library's contract is that every public `Parse` and every accessor either succeeds or throws a documented exception type. An `AccessViolationException`, `NullReferenceException`, `StackOverflowException`, or `OutOfMemoryException` escaping to the caller is a bug at minimum and a vulnerability if it is reachable from untrusted input.

### Out of scope

- **Fonts that are simply malformed.** The library is designed to reject malformed input by throwing `InvalidDataException` or `EndOfStreamException`. A font that triggers an exception is the library working as intended, not a vulnerability. If the exception message is unhelpful, that is a documentation or diagnostics issue, not a security issue.
- **Specification-conformance bugs.** A parser that returns the wrong glyph ID for a codepoint, or misreads a version-gated field, is a bug. Report it as a regular issue. It becomes a security issue only if the wrong output leads to a downstream memory safety violation or a resource exhaustion.
- **Issues in a consuming application.** If a downstream tool mishandles the exception the library throws, that is a bug in that tool. The library's contract is well-defined; a consumer that does not honor it is not the library's responsibility.
- **Fonts that fail to parse because they use a feature the library does not support.** The library implements the full OpenType specification, but if a future extension is added to the spec and the library does not yet support it, the correct behavior is to throw, not to silently produce wrong output. That is not a vulnerability.
- **Social engineering, phishing, or account compromise.** These are out of scope for a library vulnerability program.
- **Physical attacks.** Out of scope.

### Uncertain cases

If you find something that is a bug but you are not sure whether it qualifies as a security issue, err on the side of reporting it privately first. The maintainer can assess it and, if it turns out to be a spec conformance bug rather than a vulnerability, will suggest reopening it as a public issue.

## Disclosure Policy

The project follows **coordinated disclosure**:

1. You report the issue privately.
2. The maintainer acknowledges, investigates, and produces a fix.
3. A release containing the fix is published.
4. You and the maintainer agree on a public disclosure date, typically on or shortly after the release.
5. A GitHub Security Advisory is published describing the vulnerability, the affected versions, and the fix.

You are free to disclose the vulnerability publicly at any time after the release containing the fix is available. Before that, please keep the details private. If you have a deadline — a conference talk, a paper submission — tell the maintainer in the initial report and the timeline can be adjusted.

## Credit

Reporters are credited by name or handle in the GitHub Security Advisory and in the `CHANGELOG.md` entry for the fix, unless they request to remain anonymous. Provide the attribution you would like when you first report the issue.

If you found the issue as part of a security research program or a bug bounty, mention the program so the advisory can cite it.

## Safe Harbor

The project will not pursue legal action against researchers who:

- Follow this policy when reporting a vulnerability.
- Do not access, modify, or exfiltrate data belonging to other users.
- Do not degrade the availability of any service.
- Do not exploit the vulnerability beyond what is necessary to demonstrate it.

Good-faith security research is welcome and protected.

## Hardening Guidance for Consumers

If you are building an application that parses untrusted fonts — a service that accepts uploads, a browser, a document renderer — the library's safety limits are the first line of defense, but they are not the only one. Consider:

- **Run the parser in a sandbox** when parsing fonts from untrusted sources. A process or container with a memory cap and a wall-clock timeout contains the damage if a parser bug is exploited.
- **Set a maximum font file size** before opening the source. The library accepts any length the underlying `Source` can provide; a service should cap uploads well below what the parser can handle.
- **Set a wall-clock timeout** around the parse and table-access calls. A pathological font that triggers a long parse is not a memory-safety issue, but it is a denial-of-service vector if a service blocks on it indefinitely.
- **Do not disable the safety limits** without a clear reason. Constants like the `1024`-strike cap and the 10-level subroutine nesting limit are there because every real font is far below them. If a legitimate font trips one, open an issue — do not raise the constant locally.

## Contact

- **Security reports:** [security@mubarrat.com](mailto:security@mubarrat.com)
- **Code of Conduct reports:** [conduct@mubarrat.com](mailto:conduct@mubarrat.com)
- **General questions:** [contact@mubarrat.com](mailto:contact@mubarrat.com)

**Do not use the code of conduct address for security reports, and do not use the security address for code of conduct reports.** The three addresses exist so that each category of report reaches the right review process.

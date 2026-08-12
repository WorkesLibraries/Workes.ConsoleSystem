# PROJECT CONTEXT

## Purpose

This is the central coordination file for project documentation and AI-assisted development.

When an AI assistant is asked to make changes to this project, this file should be read before deciding which documentation files need to be inspected or updated.

## Usage

Use this file as the entry point for understanding how the documentation system fits together.

This file does not imply that every documentation file must be read or updated for every change.

For all non-trivial changes:

1. Read this file.
2. Decide which documentation files are relevant to the requested change.
3. Read only the relevant documentation files.
4. Read docs/PROJECT/TRELLO_WORKFLOW.md if the change involves task state, planning, bugs, backlog, current work, blocked work, or completed work.
5. Make the requested code or documentation changes.
6. Update only documentation that was actually affected, including CHANGELOG.md for user-visible changes.
7. Keep Trello aligned according to docs/PROJECT/TRELLO_WORKFLOW.md if task-board state is relevant and the workflow status allows it.

For trivial changes, documentation may be left untouched unless the change directly affects documented behavior, architecture, API shape, decisions, task state, first-use documentation, or AI continuity.

## Documentation Overview

| File | Purpose | Update When |
|---|---|---|
| README.md | User-facing project landing page | Public usage, setup, positioning, package relationships, or documentation links change |
| CHANGELOG.md | First-class user-facing change history | User-visible behavior changes; update during the work, not only before release |
| docs/QUICK_START.md | Required first-use guide | Installation, prerequisites, or first-use flow changes |
| docs/DESIGN/DESIGN_GUIDELINES.md | Reusable C# package design guidelines | Shared design guidance changes |
| docs/DESIGN/DESIGN_REFERENCE.md | Examples and extended design reference | Shared design examples or reference patterns change |
| docs/PROJECT/PROJECT_CONTEXT.md | Central project documentation entry point | Documentation structure or AI workflow changes |
| docs/PROJECT/TRELLO_WORKFLOW.md | Trello task-board workflow | Trello setup, board mapping, task-state rules, or CLI usage changes |
| docs/PROJECT/DOCUMENTATION_RULES.md | README and changelog rules | User-facing README or changelog rules change |
| docs/PROJECT/AI_CONTEXT.md | Compact AI working context | Meaningful development sessions or project direction changes |
| docs/PROJECT/ARCHITECTURE.md | Internal system structure and design overview | Core abstractions, flow, or structure changes |
| docs/PROJECT/API_GUIDELINES.md | Public API design philosophy and consistency rules | API design principles, conventions, or style changes |
| docs/PROJECT/DECISIONS.md | Long-lived design rationale | Important architectural or design choices are made |

## Versioning Strategy

New packages should normally start at `0.1.0`.

Use `0.x` versions while the public API is still settling. Breaking changes may happen during this phase, but they must still be documented clearly in CHANGELOG.md.

Move to `1.0.0` only when the package is ready to communicate a stable public API. After `1.0.0`, follow semantic versioning strictly:

- patch versions for backwards-compatible fixes;
- minor versions for backwards-compatible additions;
- major versions for breaking public API or behavior changes.

Release-time version selection should confirm the intended version, not reinvent this policy. Existing packages should continue from their current published version rather than being reset to `0.x`.

## Documentation Strategy

Documentation scaffold files are copied from Project Formula templates. Update the template files when the default documentation should change; do not hard-code long documentation bodies in package-creation scripts.

README.md is the package landing page. It should be concise and should link to relevant documentation instead of becoming the whole manual.

README.md installation instructions should include both the NuGet CLI command and the package-reference XML form. The CLI command must include `--version`, and the XML example must include `Version`, so users install the intended package version explicitly.

docs/QUICK_START.md is the only public user-facing documentation file that must exist for every package.

Additional public documentation files may be created directly under docs/ when the package needs focused documentation for a specific topic, such as serializers, migrations, commands, configuration, extensions, or integrations.

docs/DESIGN/ contains reusable C# package design guidance copied from the project formula templates. Read docs/DESIGN/DESIGN_GUIDELINES.md for general package design defaults, and read docs/DESIGN/DESIGN_REFERENCE.md only when examples or deeper reference material are useful.

docs/PROJECT/ contains project-control documentation used for AI continuity, architectural consistency, planning workflow, and design intent. These files are not the user manual.

## Git Branch Workflow

The default branch model is intentionally small:

- `main`: normal development and production-ready work.
- `release`: published release history.

Use the standalone Project Formula GitHub setup script only after the local project exists and has an initial commit.

Normal work happens on `main`. Merge `main` into `release` only after a package version has actually been published. Tags identify exact package versions, such as `v0.1.0`.

Do not introduce or enforce this branch workflow in an existing package unless the developer explicitly wants that package converted to it.

## Source Of Truth Rules

- Task state belongs in Trello once docs/PROJECT/TRELLO_WORKFLOW.md says the project board is ready.
- If docs/PROJECT/TRELLO_WORKFLOW.md says Trello is not ready for this project, do not invent task-board state in markdown.
- Architecture belongs in docs/PROJECT/ARCHITECTURE.md.
- API design style and conventions belong in docs/PROJECT/API_GUIDELINES.md.
- Design rationale belongs in docs/PROJECT/DECISIONS.md.
- Compact AI context belongs in docs/PROJECT/AI_CONTEXT.md.
- User-facing README and changelog rules belong in docs/PROJECT/DOCUMENTATION_RULES.md.
- User-facing first-use documentation belongs in docs/QUICK_START.md.
- Reusable package design guidance belongs in docs/DESIGN/.
- This file explains how the documentation system is used, but should not duplicate the full content of the other files.

## AI Maintenance Rules

- Do not ignore the docs folder.
- Do not assume every documentation file is relevant to every change.
- Read and update documentation selectively based on file responsibility.
- Prefer the smallest sufficient documentation set.
- Do not treat README.md or CHANGELOG.md as AI scratchpads.
- Do not duplicate large sections between files.
- If a change affects architecture, update docs/PROJECT/ARCHITECTURE.md.
- If a change affects API design style or public API shape, update docs/PROJECT/API_GUIDELINES.md.
- If a change establishes or changes a long-term decision, update docs/PROJECT/DECISIONS.md.
- If a change affects future AI continuity, update docs/PROJECT/AI_CONTEXT.md.
- If a change affects task state, follow docs/PROJECT/TRELLO_WORKFLOW.md.
- If a change affects first-use instructions, update docs/QUICK_START.md.
- If a change is user-visible, update CHANGELOG.md as part of the same work instead of waiting for release preparation.
- If a change affects general package design guidance, update docs/DESIGN/DESIGN_GUIDELINES.md or docs/DESIGN/DESIGN_REFERENCE.md as appropriate.
- If Trello authentication fails in the Codex sandbox but works in a normal terminal, use sandbox escalation for Trello commands instead of re-authenticating or storing credentials.

## Recommended AI Reading Order

1. docs/PROJECT/PROJECT_CONTEXT.md
2. docs/PROJECT/AI_CONTEXT.md
3. docs/PROJECT/TRELLO_WORKFLOW.md, if task state is relevant
4. docs/PROJECT/ARCHITECTURE.md, if structure or internals are relevant
5. docs/PROJECT/API_GUIDELINES.md, if package usage or public API design is relevant
6. docs/PROJECT/DECISIONS.md, if design rationale or architectural choices are relevant
7. docs/PROJECT/DOCUMENTATION_RULES.md, if README.md or CHANGELOG.md may be changed
8. docs/DESIGN/DESIGN_GUIDELINES.md, if general package design guidance is relevant
9. docs/DESIGN/DESIGN_REFERENCE.md, if design examples or extended reference material are relevant
10. docs/QUICK_START.md, if first-use documentation may be changed


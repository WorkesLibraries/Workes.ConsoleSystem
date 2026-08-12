# AI_CONTEXT

## Purpose

Provide compact project context for AI assistants.

## Usage

Read this file before making non-trivial changes to the project.

## Maintenance

Update after meaningful development sessions or when project direction changes.

## Rules

- Keep this concise.
- Include current conventions, constraints, and high-impact context.
- Do not duplicate full architecture or API guideline documentation.
- Reference other files instead of copying large sections.
- Treat Trello as the source of truth for task state once docs/PROJECT/TRELLO_WORKFLOW.md says the project board is ready.

## Project Summary

Workes.ConsoleSystem is an engine-neutral .NET package for a game or tool console. It currently provides one `GameConsole` root object, shared chronological console history, a logging facade, command input history, entry types, and placeholder command-system types.

## Current State

The package is still in its initial skeleton phase at version `0.1.0`. Logging into shared console history is implemented. Command registration, parsing, execution, permissions, aliases, arguments, options, and autocomplete are intentionally not implemented yet.

Project setup has been resynced with the current Project Formula docs and packaging expectations. The old `docs/project` Markdown workflow has been superseded by `docs/PROJECT` plus the mapped Trello board.

## Current Conventions

- README.md is a concise package landing page.
- CHANGELOG.md is first-class package documentation and should describe final user-visible changes.
- README.md installation examples should include explicit package versions in both CLI and XML forms.
- Public docs should be complete enough that users can discover package features without reading source.
- Default Git branch workflow is `main` for normal development and `release` for published release history.
- Merge `main` into `release` only after a package version has actually been published and tagged.

## Important Constraints

- Keep the package engine-neutral and UI-neutral.
- Keep `GameConsole` as the normal root object without introducing singleton/global state.
- Preserve one shared chronological history for user-visible console entries.
- Do not imply command execution behavior exists until the command design has been approved and implemented.
- Keep task state in Trello according to `docs/PROJECT/TRELLO_WORKFLOW.md`.

## External Task Board

Trello is used for task state after docs/PROJECT/TRELLO_WORKFLOW.md has been completed for this project.

Use Trello for current work, next work, backlog, bugs, blocked work, roadmap/planning cards if used, and completed task tracking.

Do not duplicate Trello task state here.

## AI Instructions

- Read docs/PROJECT/PROJECT_CONTEXT.md first.
- Read docs/PROJECT/TRELLO_WORKFLOW.md before inspecting or modifying task state.
- Read docs/PROJECT/ARCHITECTURE.md before making structural changes.
- Read docs/PROJECT/API_GUIDELINES.md before changing public-facing API design.
- Read docs/PROJECT/DECISIONS.md before revisiting architectural choices.
- Update this file after meaningful development sessions.
- Do not duplicate large sections from other documentation files here.


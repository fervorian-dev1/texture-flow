# Texture Flow project handoff

Read `T.A.R.D. Manual.md`, particularly chapters 18-20, before substantial changes or recovery work. It records the architecture, owner decisions, known limits, test commands and recovery procedures for version 0.3.0.

- The product is a standalone Windows app. Do not move it back into the Arma workspace.
- Leave VCC and Unity installation code/settings untouched. The owner approved automatic setup/update of Texture Flow's own companion inside selected Unity projects.
- Keep the three-step beginner workflow, supplied icon, drag-and-drop, texture browser, target-specific application and recoverable material assignment.
- Do not guess among multiple matching avatar parts or remove stale-target/session/Play-mode/restore-conflict checks.
- Use disposable projects for tests. Never run the fixture harness against a personal avatar project.
- Preserve the manual's distinction between tested features and experimental/unimplemented features.
- Exclude avatar textures/projects, caches, credentials and local logs from source/release uploads.

Application, importer/bridge and live tests are documented in the manual. Documentation-only changes do not require rerunning application tests; verify rendered documents, package contents and upload receipts as appropriate.

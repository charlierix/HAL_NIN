# Loop 2: Concept Extraction

**Goal:** Read the source inventory and decide which items deserve to become OKF concepts. Produce a concept manifest. No OKF documents are written in this loop.

---

## Input

- `scan_inventory.md` from Loop 1

## Output

Write a single file to:
```
<bundle-dir>/.staging/concept_manifest.md
```

## File format

```markdown
# Concept Manifest

**Derived from:** scan_inventory.md
**Date:** <ISO 8601>
**Concept count:** <N>

## Concepts

| # | Concept ID | Type | Title | Source ref(s) | Description | Priority |
|---|-----------|------|-------|--------------|-------------|----------|
| 1 | services/auth-api | API | Auth API | src/api/auth.py, src/api/auth_routes.py | Authentication and authorization service handling JWT tokens. | high |
| 2 | tables/users | Table | Users | db/schema/users.sql, src/models/user.py | User account table with profile and auth fields. | high |
| 3 | metrics/retention | Metric | Retention Rate | src/analytics/retention.py | 30-day rolling user retention calculation. | medium |
| ... | | | | | | |

## Skipped files (and why)

| File | Reason skipped |
|------|---------------|
| .gitignore | Config metadata, not knowledge |
| node_modules/ | Dependencies, not project knowledge |
| tests/test_utils.py | Test file, not a standalone concept |
```

## Instructions

1. Read `scan_inventory.md` completely.
2. For each file (or group of related files), decide:
   - **Is this a concept?** A concept is a *meaningful unit of knowledge* — something someone would look up to understand the project.
   - **What type?** Use descriptive type names (e.g. `Service`, `API`, `Table`, `Metric`, `Playbook`, `Reference`, `Config`, `Decision`).
   - **What should the concept ID be?** This is the path within the bundle (e.g. `services/auth-api`). Use kebab-case.
   - **Which source files inform this concept?** List them.
   - **One-line description.**
   - **Priority**: `high` (core), `medium` (useful), `low` (nice-to-have).
3. **Group related files into single concepts.** Five files that implement one service = one concept, not five.
4. **Skip noise.** Dependencies, build artifacts, git metadata, and trivial config files are not concepts.
5. Write the manifest to the output file.

## What NOT to do

- Do NOT write any OKF `.md` concept documents.
- Do NOT decide the bundle directory structure (that's Loop 3).
- Do NOT read the actual source file contents — work from the inventory summaries.
- Do NOT create a concept for every file. Group and curate.

## Stop condition

**Stop when `concept_manifest.md` is written.** Do not proceed to structure planning.

# Loop 4: Generation (Per Concept)

**Goal:** Write OKF concept documents, **one at a time**. Each iteration of this loop produces exactly one `.md` file.

This loop is **repeated** — run it once per concept in the manifest.

---

## Input

- `bundle_plan.md` from Loop 3 (for path and type info)
- `concept_manifest.md` from Loop 2 (for source refs and description)
- The **specific concept** to generate this iteration
- Access to the source files referenced by this concept

## Output

One `.md` file written to the concept's bundle path:
```
<bundle-dir>/<concept-path>.md
```

For example:
```
<bundle-dir>/services/auth-api.md
```

## File format

Follow the OKF spec (section 4). Every concept file must have:

1. **YAML frontmatter** (delimited by `---`):
   - `type` (REQUIRED, non-empty)
   - `title` (recommended)
   - `description` (recommended — one sentence)
   - `resource` (if the concept is bound to a real asset)
   - `tags` (recommended — YAML list)
   - `timestamp` (recommended — ISO 8601)

2. **Markdown body** with structural content:
   - Short prose overview (1-3 paragraphs)
   - `# Schema` — for data assets (tables, datasets)
   - `# Examples` — concrete usage examples
   - `# Citations` — external sources
   - Other headings as appropriate (`# Endpoints`, `# Configuration`, `# Dependencies`, etc.)

Example:

```markdown
---
type: API
title: Auth API
description: Authentication and authorization service handling JWT tokens.
resource: https://github.com/myorg/myproject/tree/main/src/api/auth
tags: [auth, jwt, security]
timestamp: 2026-06-28T00:00:00Z
---

# Overview

The Auth API provides JWT-based authentication and RBAC authorization
for all platform services. It exposes endpoints for login, token refresh,
and permission checking.

# Endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | /auth/login | Authenticate and receive JWT |
| POST | /auth/refresh | Refresh expired token |
| GET | /auth/permissions | Check user permissions |

# Configuration

- `JWT_SECRET` — signing key (env var)
- `TOKEN_TTL` — token lifetime in seconds (default: 3600)

# Citations

[1] [Auth API source](https://github.com/myorg/myproject/tree/main/src/api/auth)
```

## Script assistance (optional)

The `generate_concepts/swift_parser.py` script can automate this loop for **Swift** source code by reading source files and extracting class/struct/enum/function declarations via regex. It produces poor or empty results for **non-code sources** (documentation, data files, spreadsheets, prose) or other programming languages. For non-Swift sources, generate concepts manually by reading and summarizing the source files as instructed below, or add a new parser to the `generate_concepts/` folder (see `scripts/index.md`).

## Instructions (per iteration)

1. From the manifest, identify the **source ref(s)** for this concept.
2. Read the referenced source file(s). Read the actual code/docs — not just the inventory summary.
3. Compose the frontmatter:
   - Set `type` from the manifest.
   - Write `title` and `description` (one sentence).
   - Add `resource` if there's a canonical URL/path.
   - Add `tags` inferred from the content.
   - Set `timestamp` to the current date.
4. Compose the body:
   - Write a short overview.
   - Add structured sections appropriate to the concept type.
   - Include any cross-links from the bundle plan, using absolute bundle-relative paths (e.g. `/tables/users.md`).
   - Add `# Citations` with links to source files.
5. Write the file to the concept's bundle path.
6. **Stop.** Do not start the next concept.

## Concept-type body patterns

| Type | Suggested body sections |
|------|------------------------|
| `Service` / `API` | `# Overview`, `# Endpoints`, `# Configuration`, `# Dependencies` |
| `Table` / `Dataset` | `# Overview`, `# Schema`, `# Common query patterns`, `# Citations` |
| `Metric` | `# Overview`, `# Formula`, `# Examples`, `# Citations` |
| `Playbook` | `# Overview`, `# Steps`, `# Prerequisites`, `# Troubleshooting` |
| `Reference` | `# Overview`, `# Values`, `# Notes`, `# Citations` |
| `Decision` | `# Context`, `# Decision`, `# Rationale`, `# Alternatives` |

## What NOT to do

- Do NOT write multiple concepts in one iteration.
- Do NOT write `index.md` or `log.md` (that's Loop 5).
- Do NOT invent data. If the source doesn't mention something, don't add it.
- Do NOT read source files for other concepts — only the ones for this concept.

## Stop condition (per iteration)

**Stop when the single concept file is written.** The orchestrator (human or agent) should then invoke the next iteration for the next concept.

## Stop condition (entire loop)

**The generation loop is complete when every concept in the manifest has a written `.md` file.** Do not proceed to index & link until all concepts exist.

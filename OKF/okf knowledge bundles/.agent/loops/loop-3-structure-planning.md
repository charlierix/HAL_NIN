# Loop 3: Structure Planning

**Goal:** Take the concept manifest and design the OKF bundle's directory structure. Produce a bundle plan. No OKF documents are written in this loop.

---

## Input

- `concept_manifest.md` from Loop 2

## Output

Write a single file to:
```
<bundle-dir>/.staging/bundle_plan.md
```

## File format

```markdown
# Bundle Plan

**Derived from:** concept_manifest.md
**Date:** <ISO 8601>

## Directory tree

<bundle-root>/
├── index.md
├── log.md
├── services/
│   ├── index.md
│   ├── auth-api.md
│   └── payment-api.md
├── tables/
│   ├── index.md
│   ├── users.md
│   └── orders.md
├── metrics/
│   ├── index.md
│   └── retention.md
└── playbooks/
    ├── index.md
    └── deployment.md

## Concept -> path mapping

| Concept ID | Bundle path | Type | Title |
|-----------|-------------|------|-------|
| services/auth-api | services/auth-api.md | API | Auth API |
| tables/users | tables/users.md | Table | Users |
| metrics/retention | metrics/retention.md | Metric | Retention Rate |
| playbooks/deployment | playbooks/deployment.md | Playbook | Deployment |

## Index specs

| Index file | Will list |
|-----------|----------|
| index.md (root) | All top-level directories with descriptions |
| services/index.md | All concepts in services/ |
| tables/index.md | All concepts in tables/ |
| metrics/index.md | All concepts in metrics/ |
| playbooks/index.md | All concepts in playbooks/ |

## Cross-link plan

| From | To | Context |
|------|-----|---------|
| services/auth-api.md | tables/users.md | "validates against the [users table](/tables/users.md)" |
| tables/orders.md | tables/users.md | "foreign key to [users](/tables/users.md)" |
```

## Instructions

1. Read `concept_manifest.md` completely.
2. Group concepts by type into top-level directories (e.g. `services/`, `tables/`, `metrics/`, `playbooks/`, `references/`).
3. Decide if any subdirectories are needed (e.g. `references/metrics/` if there are many).
4. Assign each concept a final path in the bundle.
5. Plan which `index.md` files are needed and what they'll list.
6. Plan cross-links: which concepts reference each other, and the link text.
7. Write the bundle plan to the output file.

## Directory naming conventions

- Use **plural nouns** for directories: `services/`, `tables/`, `metrics/`, `references/`.
- Use **kebab-case** for concept filenames: `auth-api.md`, not `authApi.md` or `auth_api.md`.
- Keep it **shallow** — max 2-3 levels deep.
- Common top-level directories:

  | Directory | For concepts about... |
  |-----------|----------------------|
  | `services/` | Services, microservices, daemons |
  | `apis/` or `endpoints/` | API endpoints or API groups |
  | `tables/` | Database tables, datasets |
  | `metrics/` | Business metrics, KPIs |
  | `playbooks/` | Operational procedures, runbooks |
  | `references/` | Enums, glossaries, conventions |
  | `decisions/` | Architecture decisions, ADRs |
  | `components/` | UI components, shared libraries |

## What NOT to do

- Do NOT write any OKF `.md` concept documents.
- Do NOT create directories or files on disk yet.
- Do NOT mirror the source directory structure.
- Do NOT create more than 5-7 top-level directories.

## Stop condition

**Stop when `bundle_plan.md` is written.** Do not proceed to generation.

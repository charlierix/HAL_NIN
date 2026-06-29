# Loop 5: Index & Link

**Goal:** After all concept documents exist, generate `index.md` files, verify cross-links, and write `log.md`.

---

## Input

- Complete bundle with all concept `.md` files (from Loop 4)
- `bundle_plan.md` from Loop 3 (for index specs and cross-link plan)

## Output

- `index.md` in every directory of the bundle
- `log.md` at the bundle root
- Updated cross-links in concept docs (if any were missed during generation)

## index.md format

`index.md` files have **no frontmatter** (except the bundle root, which may carry `okf_version`).

### Bundle root index.md

```markdown
---
okf_version: "0.1"
---

# <Bundle Name>

Knowledge bundle for <project/domain>.

- [Services](./services/) — Service definitions and APIs
- [Tables](./tables/) — Database tables and datasets
- [Metrics](./metrics/) — Business metrics and KPIs
- [Playbooks](./playbooks/) — Operational procedures
```

### Subdirectory index.md

```markdown
# Services

- [Auth API](./auth-api.md) — Authentication and authorization service handling JWT tokens.
- [Payment API](./payment-api.md) — Payment processing service.
```

Each entry should use the concept's `description` from its frontmatter.

## log.md format

```markdown
# Change Log

## 2026-06-28

- Initial bundle generation from source directory `/path/to/source`.
- Created N concept documents across M directories.
- Generated index.md files and cross-links.
```

## Script assistance (optional)

The `generate_indexes.py` script can automate this loop by scanning concept `.md` files, parsing frontmatter, generating root and subdirectory `index.md` files, verifying cross-links, and writing `log.md`. It operates on the generated bundle (not the source), so it works regardless of source type. See `scripts/index.md` for usage.

## Instructions

1. **List all concept files** in the bundle:
   ```bash
   find <bundle-dir> -name '*.md' -not -name 'index.md' -not -name 'log.md' | sort
   ```

2. **Generate root index.md**:
   - List all top-level directories that contain concepts.
   - For each, write a link and one-line description.
   - Add `okf_version: "0.1"` to frontmatter.

3. **Generate subdirectory index.md files**:
   - For each directory containing concepts, list all `.md` files in that directory.
   - For each, read its frontmatter to get `title` and `description`.
   - Write a link entry: `- [Title](./file.md) — description`

4. **Verify cross-links**:
   - Scan all concept files for markdown links to other concepts.
   - Check that each link target exists in the bundle.
   - Broken links are **tolerated by the spec** (section 5.3) — don't delete them, but log them as warnings.
   - If a link from the bundle plan was missed during generation, add it now.

5. **Write log.md** with a dated entry describing the initial generation.

6. **Do NOT rewrite concept documents.** Only add missing cross-links if needed. The concept docs from Loop 4 are the source of truth.

## What NOT to do

- Do NOT add frontmatter to subdirectory `index.md` files.
- Do NOT rewrite or restructure concept documents.
- Do NOT create `index.md` files in directories that have no concepts.
- Do NOT run validation — that's Loop 6.

## Stop condition

**Stop when all `index.md` files and `log.md` are written and cross-links have been verified.** Do not proceed to validation.

# Loop 6: Validate

**Goal:** Run the deterministic OKF conformance checker, fix any hard errors, and confirm the bundle is conformant.

---

## Input

- Complete OKF bundle (all concept files, index.md files, log.md)

## Output

- Conformance report (printed to stdout)
- Fixed concept files (if any errors were found)

## Instructions

1. **Run the validator:**

   ```bash
   python3 "<bundle-dir>/.agent/skills/create-maintain/validate/scripts/okf_validate.py" <bundle-dir> --json
   ```

   If `pyyaml` is missing:
   ```bash
   python3 -m pip install --quiet pyyaml && python3 "<bundle-dir>/.agent/skills/create-maintain/validate/scripts/okf_validate.py" <bundle-dir> --json
   ```

2. **Review the report.**

   The checker reports:

   | Severity | Code | Meaning |
   |----------|------|---------|
   | **ERROR** | E1 | File has no parseable YAML frontmatter |
   | **ERROR** | E2 | Frontmatter missing or empty `type` field |
   | **warn** | W1 | Recommended field absent (title, description, tags, timestamp) |
   | **warn** | W2 | Cross-link target not found (tolerated by spec) |
   | **warn** | W3 | index.md has unexpected frontmatter |
   | **warn** | W4 | log.md has frontmatter or non-ISO date headings |

3. **Fix every ERROR (E1, E2):**

   - **E1 (no frontmatter):** Add a `---`-delimited YAML block at the top of the file with at minimum a `type` field.
   - **E2 (empty type):** Set `type` to a descriptive non-empty string.

4. **Fix warnings when cheap:**

   - W1: Add missing `title`, `description`, `tags`, or `timestamp` to frontmatter.
   - W2: Leave broken links as-is (spec tolerates them) unless the target was just misnamed.
   - W3: Remove frontmatter from subdirectory `index.md` files (only root can have `okf_version`).
   - W4: Remove frontmatter from `log.md`; fix date headings to `YYYY-MM-DD` format.

5. **Re-run the validator** after fixes. Repeat until zero ERRORs.

6. **Report results:**

   ```
   Bundle is OKF v0.1 conformant

   Errors: 0
   Warnings: <N> (non-blocking)

   Concepts: <N>
   Directories: <N>
   Total .md files: <N>
   ```

## What NOT to do

- Do NOT use the model's judgment instead of the validator script. The validator is deterministic.
- Do NOT delete files to make errors go away. Fix them.
- Do NOT treat warnings as failures. The spec is permissive by design.
- Do NOT skip this loop. Validation is the final gate before the bundle is usable.

## Stop condition

**Stop when the validator reports zero ERRORs.** Warnings are acceptable.

If errors persist after 3 fix iterations, stop and report the remaining errors for manual resolution.

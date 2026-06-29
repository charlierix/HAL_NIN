#!/usr/bin/env python3
"""
okf_generate_indexes.py — Loop 5: Generate index.md files, verify cross-links, write log.md.

Usage:
    python3 okf_generate_indexes.py <bundle-dir> [--source-dir <path>] [--name <title>]

Reads:
    All concept .md files in <bundle-dir> (excluding .staging/, index.md, log.md)

Writes:
    <bundle-dir>/index.md           (root index with okf_version frontmatter)
    <bundle-dir>/<dir>/index.md     (one per subdirectory containing concepts)
    <bundle-dir>/log.md             (change log with dated entry)

This script:
    1. Scans the bundle for all concept .md files
    2. Parses YAML frontmatter from each to get title and description
    3. Generates root and subdirectory index.md files with links
    4. Verifies cross-links between concept documents
    5. Writes a log.md entry describing the generation

Requirements:
    - Concept files must have YAML frontmatter with at least 'title' and 'description'
    - pyyaml not required (simple key:value parsing is used)
"""
import os
import re
import sys
import datetime
import argparse


def parse_frontmatter(filepath):
    """Parse simple YAML frontmatter (key: value pairs only)."""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    if not content.startswith('---'):
        return {}, content
    end = content.find('---', 3)
    if end == -1:
        return {}, content
    fm_text = content[3:end].strip()
    fm = {}
    for line in fm_text.split('\n'):
        if ':' in line:
            key, val = line.split(':', 1)
            fm[key.strip()] = val.strip().strip('"').strip("'")
    return fm, content


def find_concept_files(bundle_dir):
    """Find all .md concept files, excluding index.md, log.md, and .staging/."""
    concepts = []
    for root, dirs, files in os.walk(bundle_dir):
        if '.staging' in root:
            continue
        for f in files:
            if f.endswith('.md') and f != 'index.md' and f != 'log.md':
                concepts.append(os.path.join(root, f))
    concepts.sort()
    return concepts


def group_by_directory(bundle_dir, concept_files):
    """Group concept files by their parent directory relative to bundle root."""
    groups = {}
    for cf in concept_files:
        rel = os.path.relpath(cf, bundle_dir)
        d = os.path.dirname(rel) or '.'
        groups.setdefault(d, []).append(cf)
    return groups


def generate_root_index(bundle_dir, dir_groups, dir_descriptions, bundle_name):
    """Generate the root index.md with okf_version frontmatter."""
    lines = [
        '---',
        'okf_version: "0.1"',
        '---',
        '',
        f'# {bundle_name}',
        '',
        f'Knowledge bundle generated from source code.',
        '',
    ]
    for d in sorted(dir_groups.keys()):
        if d == '.':
            continue
        count = len(dir_groups[d])
        desc = dir_descriptions.get(d, f'{count} concepts')
        lines.append(f'- [{d.title()}](./{d}/) — {desc} ({count} concepts)')
    lines.append('')
    return '\n'.join(lines)


def generate_subdir_index(dir_name, files, bundle_dir, description=''):
    """Generate a subdirectory index.md file listing all concepts."""
    lines = [f'# {dir_name.title()}', '', description, '']
    for cf in sorted(files):
        fm, _ = parse_frontmatter(cf)
        title = fm.get('title', os.path.basename(cf).replace('.md', '').replace('-', ' ').title())
        desc = fm.get('description', '')
        fname = os.path.basename(cf)
        lines.append(f'- [{title}](./{fname}) — {desc}')
    lines.append('')
    return '\n'.join(lines)


def verify_cross_links(bundle_dir, concept_files):
    """Check that all markdown links in concept files point to existing files."""
    all_md = set()
    for root, dirs, files in os.walk(bundle_dir):
        if '.staging' in root:
            continue
        for f in files:
            if f.endswith('.md'):
                rel = os.path.relpath(os.path.join(root, f), bundle_dir)
                all_md.add(rel)

    broken = []
    link_pattern = re.compile(r'\[([^\]]+)\]\(([^)]+\.md)\)')
    for cf in concept_files:
        with open(cf, 'r', encoding='utf-8') as f:
            content = f.read()
        for match in link_pattern.finditer(content):
            target = match.group(2)
            if target.startswith('/'):
                target = target[1:]
            elif target.startswith('./'):
                target = target[2:]
            if target not in all_md:
                broken.append((os.path.basename(cf), target))
    return broken


def generate_log(bundle_dir, concept_count, dir_count, source_dir=None):
    """Generate or append to log.md."""
    log_path = os.path.join(bundle_dir, 'log.md')
    date_str = datetime.datetime.now().strftime('%Y-%m-%d')

    # Check if log.md exists and has this date already
    existing = ''
    if os.path.exists(log_path):
        with open(log_path, 'r') as f:
            existing = f.read()

    lines = ['# Change Log', '']

    if existing:
        # Preserve existing entries, insert new date at top if not present
        if date_str in existing:
            lines = existing.split('\n')
            # Find the date section and append
            return '\n'.join(lines)
        else:
            lines.append(f'## {date_str}')
            lines.append('')
            # Append old content after
            old_lines = existing.split('\n')[2:]  # Skip old header
            lines.extend(old_lines)
    else:
        lines.append(f'## {date_str}')
        lines.append('')

    src_note = f' from source directory `{source_dir}`' if source_dir else ''
    lines.append(f'- Generated {concept_count} concept documents across {dir_count} directories{src_note}.')
    lines.append(f'- Created index.md files for root and all subdirectories.')
    lines.append(f'- Verified cross-links between concept documents.')
    lines.append('')

    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(description='Generate index.md files, verify cross-links, write log.md.')
    parser.add_argument('bundle_dir', help='Bundle directory path')
    parser.add_argument('--source-dir', help='Source directory path (for log entry)', default=None)
    parser.add_argument('--name', help='Bundle name for root index title', default='Knowledge Bundle')
    args = parser.parse_args()

    bundle_dir = os.path.abspath(args.bundle_dir)

    # Find all concept files
    concept_files = find_concept_files(bundle_dir)
    print(f'Found {len(concept_files)} concept files')

    # Group by directory
    dir_groups = group_by_directory(bundle_dir, concept_files)
    print(f'Directories with concepts: {sorted(dir_groups.keys())}')

    # Auto-generate descriptions from subdirectory names
    dir_descriptions = {}
    for d in dir_groups:
        if d == '.':
            continue
        # Try to read existing index.md for description
        idx_path = os.path.join(bundle_dir, d, 'index.md')
        if os.path.exists(idx_path):
            with open(idx_path, 'r') as f:
                content = f.read()
            # Extract description from second line (after # Title)
            lines = content.split('\n')
            for line in lines[1:]:
                if line.strip():
                    dir_descriptions[d] = line.strip()
                    break
        if d not in dir_descriptions:
            dir_descriptions[d] = f'{len(dir_groups[d])} concepts in {d}'

    # Generate root index.md
    root_index = generate_root_index(bundle_dir, dir_groups, dir_descriptions, args.name)
    root_path = os.path.join(bundle_dir, 'index.md')
    with open(root_path, 'w', encoding='utf-8') as f:
        f.write(root_index)
    print(f'Written root index.md')

    # Generate subdirectory index.md files
    for d, files in sorted(dir_groups.items()):
        if d == '.':
            continue
        desc = dir_descriptions.get(d, '')
        idx_content = generate_subdir_index(d, files, bundle_dir, desc)
        idx_path = os.path.join(bundle_dir, d, 'index.md')
        with open(idx_path, 'w', encoding='utf-8') as f:
            f.write(idx_content)
        print(f'Written {d}/index.md ({len(files)} entries)')

    # Verify cross-links
    broken = verify_cross_links(bundle_dir, concept_files)
    if broken:
        print(f'\nWarning: {len(broken)} broken cross-links found:')
        for src, tgt in broken[:20]:
            print(f'  {src} -> {tgt}')
    else:
        print('No broken cross-links found.')

    # Generate log.md
    dir_count = len([d for d in dir_groups if d != '.'])
    log_content = generate_log(bundle_dir, len(concept_files), dir_count, args.source_dir)
    log_path = os.path.join(bundle_dir, 'log.md')
    with open(log_path, 'w', encoding='utf-8') as f:
        f.write(log_content)
    print(f'Written log.md')

    print(f'\n=== Loop 5 Complete ===')
    print(f'Concept files: {len(concept_files)}')
    print(f'Directories: {dir_count}')
    print(f'Broken links: {len(broken)}')


if __name__ == '__main__':
    main()

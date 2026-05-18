#!/usr/bin/env python3
"""
Migrates Views/Modern/*.cshtml:
- Upgrades any <div ... dfe-c-table-wrap ...> to dfe-f-table-wrapper + live region + data-module
- Applies govuk-table markup + sortable <th scope="col"> where labels are plain text

Run from compass root: python3 tools/_migrate_modern_ui.py
"""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent / "Views" / "Modern"


def upgrade_table_wrap_divs(text: str) -> str:
    """dfe-c-table-wrap → dfe-f-table-wrapper + sort shell."""

    def repl(m: re.Match[str]) -> str:
        tag = m.group(0)
        if "dfe-f-table-wrapper" in tag:
            return tag
        new_tag = tag.replace("dfe-c-table-wrap", "dfe-f-table-wrapper")
        if 'data-module=' not in new_tag:
            new_tag = new_tag[:-1] + ' data-module="dfe-f-table">'
        return (
            new_tag
            + "\n  <div class=\"govuk-visually-hidden\" aria-live=\"polite\" data-dfe-table-sort-live></div>"
        )

    # Any opening div whose attributes mention dfe-c-table-wrap
    return re.sub(r"<div\b[^>]*\bdfe-c-table-wrap\b[^>]*>", repl, text)


def migrate_tables(text: str) -> str:
    t = text

    # Prefer explicit partial when wrapper was only default class (already migrated elsewhere)
    INLINE_WRAP = (
        '<div class="dfe-f-table-wrapper" data-module="dfe-f-table">\n'
        '  <div class="govuk-visually-hidden" aria-live="polite" data-dfe-table-sort-live></div>'
    )
    t = t.replace('<div class="dfe-c-table-wrap">', INLINE_WRAP)

    def repl_table(m: re.Match[str]) -> str:
        full = m.group(0)
        if "govuk-table" in full:
            return full
        inner = m.group(1) or ""
        if inner.strip() == "":
            return '<table class="govuk-table">'
        if 'class="' in inner:
            return re.sub(
                r'class="([^"]*)"',
                lambda x: f'class="govuk-table {x.group(1).strip()}"',
                full,
                count=1,
            )
        return full.replace("<table", '<table class="govuk-table"', 1)

    t = re.sub(r"<table(\s[^>]*)?>", repl_table, t)

    t = re.sub(r"<thead\s*>", '<thead class="govuk-table__head">', t)
    t = re.sub(r"<tbody\s*>", '<tbody class="govuk-table__body">', t)

    t = re.sub(
        r"<tr((?![^>]*\bclass=)[^>]*)>",
        lambda m: f'<tr class="govuk-table__row"{m.group(1)}>',
        t,
    )

    t = re.sub(
        r"<td(?![^>]*\bclass=)(?![^>]*govuk-table__cell)(\s[^>]*)?>",
        lambda m: f'<td class="govuk-table__cell"{m.group(1) or ""}>',
        t,
    )

    t = re.sub(
        r'<th\s+scope="row"(?![^>]*\bclass=)(\s[^>]*)>',
        lambda m: f'<th scope="row" class="govuk-table__header"{m.group(1)}>',
        t,
    )

    def sort_th(m: re.Match[str]) -> str:
        label = m.group(1)
        if any(ch in label for ch in "<{@"):
            return m.group(0)
        lbl = label.strip()
        return (
            '<th scope="col" class="govuk-table__header" aria-sort="none">\n'
            f'          <button type="button" class="dfe-f-table-sort-button">{lbl}</button>\n'
            "        </th>"
        )

    t = re.sub(r'<th\s+scope="col"\s*>([^<]*)</th>', sort_th, t)

    def bare_th(m: re.Match[str]) -> str:
        inner = m.group(1).strip()
        if not inner or inner.startswith("<") or "@" in inner:
            return m.group(0)
        return (
            '<th scope="col" class="govuk-table__header" aria-sort="none">\n'
            f'          <button type="button" class="dfe-f-table-sort-button">{inner}</button>\n'
            "        </th>"
        )

    # Column headers written as <th>Label</th> (no scope)
    t = re.sub(r"<th>([^<]*)</th>", bare_th, t)

    def scoped_class_th(m: re.Match[str]) -> str:
        cls = m.group(1)
        label = m.group(2).strip()
        if not label or "<" in label or "@" in label:
            return m.group(0)
        if "govuk-table__header" in cls:
            return m.group(0)
        full_cls = f"govuk-table__header {cls}".strip()
        return (
            f'<th scope="col" class="{full_cls}" aria-sort="none">\n'
            f'          <button type="button" class="dfe-f-table-sort-button">{label}</button>\n'
            "        </th>"
        )

    t = re.sub(
        r'<th\s+scope="col"\s+class="([^"]*)"\s*>([^<]*)</th>',
        scoped_class_th,
        t,
    )

    return t


def process_file(path: Path) -> bool:
    raw = path.read_text(encoding="utf-8")
    new = upgrade_table_wrap_divs(raw)
    if "<table" in new or "dfe-c-table-wrap" in raw:
        new = migrate_tables(new)
    if new != raw:
        path.write_text(new, encoding="utf-8")
        return True
    return False


def main() -> None:
    n = 0
    for p in sorted(ROOT.rglob("*.cshtml")):
        try:
            if process_file(p):
                n += 1
                print(p.relative_to(ROOT.parent.parent))
        except Exception as ex:
            print("FAIL", p, ex)
    print(f"Done. {n} files updated.")


if __name__ == "__main__":
    main()

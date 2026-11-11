"""Add the translation extension to the PowerToys solution filters and solution.

Usage:
    python integrate.py --repo C:\\path\\to\\PowerToys --dry-run
    python integrate.py --repo C:\\path\\to\\PowerToys
"""
import argparse
import json
import os

EXT = r"src\modules\cmdpal\ext\Microsoft.CmdPal.Ext.Translate\Microsoft.CmdPal.Ext.Translate.csproj"
TESTS = r"src\modules\cmdpal\Tests\Microsoft.CmdPal.Ext.Translate.UnitTests\TranslateCmdPal.Core.UnitTests.csproj"

SLNF = [
    (r"src\modules\cmdpal\CommandPalette.slnf", [EXT, TESTS]),
    (r"src\modules\cmdpal\CommandPalette - no UI tests.slnf", [EXT, TESTS]),
    (r"src\modules\cmdpal\Microsoft.CmdPal.Ext.PowerToys.slnf", [EXT]),
]


def add_to_slnf(path, projects, dry_run):
    with open(path, "r", encoding="utf-8-sig") as handle:
        data = json.load(handle)
    entries = data.setdefault("solution", {}).setdefault("projects", [])
    added = []
    for project in projects:
        if project not in entries:
            entries.append(project)
            added.append(project)
    if added and not dry_run:
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(data, handle, indent=2, ensure_ascii=False)
            handle.write("\n")
    return added


def add_to_slnx(path, projects, dry_run):
    with open(path, "r", encoding="utf-8-sig") as handle:
        text = handle.read()
    added = []
    for project in projects:
        if project in text:
            continue
        line = '  <Project Path="' + project + '" />'
        closing = text.rfind("</Solution>")
        if closing < 0:
            raise RuntimeError("Could not find </Solution> in " + path)
        text = text[:closing] + line + "\n" + text[closing:]
        added.append(project)
    if added and not dry_run:
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
    return added


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=os.getcwd(), help="Root of the PowerToys checkout")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    total = 0
    for relative, projects in SLNF:
        path = os.path.join(args.repo, relative)
        if not os.path.isfile(path):
            print("SKIP (missing): " + relative)
            continue
        added = add_to_slnf(path, projects, args.dry_run)
        total += len(added)
        for project in added:
            print(("WOULD ADD " if args.dry_run else "ADDED ") + relative + " <- " + project)

    slnx = os.path.join(args.repo, "PowerToys.slnx")
    if os.path.isfile(slnx):
        added = add_to_slnx(slnx, [EXT, TESTS], args.dry_run)
        total += len(added)
        for project in added:
            print(("WOULD ADD " if args.dry_run else "ADDED ") + "PowerToys.slnx <- " + project)
    else:
        print("SKIP (missing): PowerToys.slnx")

    print(("DRY RUN: " if args.dry_run else "DONE: ") + str(total) + " project entries")
    print("Manual follow-ups: Directory.Packages.props, NOTICE.md (only for new packages), installer file list, doc/.")


if __name__ == "__main__":
    main()

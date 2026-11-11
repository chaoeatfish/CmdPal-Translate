"""Static consistency checks for the TranslateCmdPal deliverable (no build tools required)."""
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJ = os.path.join(ROOT, "Microsoft.CmdPal.Ext.Translate")
RESULTS = []


def check(name, ok, detail=""):
    RESULTS.append((name, bool(ok), detail))


def read(path):
    with open(path, "r", encoding="utf-8-sig") as handle:
        return handle.read()


def walk_text(extensions):
    for base, _, files in os.walk(ROOT):
        for name in files:
            if any(name.lower().endswith(ext) for ext in extensions):
                yield os.path.join(base, name)


# 1. CLSID must be identical in three places.
extension_source = read(os.path.join(PROJ, "TranslateExtension.cs"))
manifest = read(os.path.join(PROJ, "Package.appxmanifest"))
guid = re.search(r'\[Guid\("([^"]+)"\)\]', extension_source)
class_id = re.search(r'com:Class Id="([^"]+)"', manifest)
activation_id = re.search(r'ClassId="([^"]+)"', manifest)
ids = [m.group(1) if m else None for m in (guid, class_id, activation_id)]
check("CLSID identical in [Guid], com:Class, CreateInstance", all(i for i in ids) and len(set(ids)) == 1, str(ids))

# 2. Manifest registration surface.
check("AppExtension name", 'Name="com.microsoft.commandpalette"' in manifest)
check("SupportedInterfaces = Commands only", "<Commands/>" in manifest and "Dock" not in manifest)
check("Capabilities limited to internetClient + runFullTrust",
      'internetClient' in manifest and 'runFullTrust' in manifest and 'systemAIModels' not in manifest)
check("Unique AppExtension Id", 'Id="TranslateCmdPal"' in manifest)

# 3. XML / JSON well-formedness.
xml_files = list(walk_text([".csproj", ".props", ".pubxml", ".appxmanifest", ".resx", ".config", ".manifest", ".xml"]))
bad_xml = []
for path in xml_files:
    try:
        ET.parse(path)
    except Exception as ex:
        bad_xml.append(os.path.relpath(path, ROOT) + ": " + str(ex))
check("XML files well-formed (" + str(len(xml_files)) + ")", not bad_xml, "; ".join(bad_xml))

json_files = [os.path.join(ROOT, ".vsconfig"), os.path.join(PROJ, "Properties", "launchSettings.json")]
bad_json = []
for path in json_files:
    try:
        json.loads(read(path))
    except Exception as ex:
        bad_json.append(os.path.relpath(path, ROOT) + ": " + str(ex))
check("JSON files well-formed", not bad_json, "; ".join(bad_json))

# 4. No template leftovers.
leftovers = []
for path in walk_text([".cs", ".csproj", ".sln", ".appxmanifest", ".manifest", ".json", ".props"]):
    text = read(path)
    for token in ("TemplateCmdPalExtension", "TemplateDisplayName", "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"):
        if token in text:
            leftovers.append(os.path.relpath(path, ROOT) + " contains " + token)
check("No template placeholder leftovers", not leftovers, "; ".join(leftovers[:5]))

# 5. Core stays free of WinRT / Toolkit types (so it compiles on plain .NET).
core_dir = os.path.join(PROJ, "Core")
impure = []
for name in os.listdir(core_dir):
    if name.endswith(".cs") and "Microsoft.CommandPalette" in read(os.path.join(core_dir, name)):
        impure.append(name)
check("Core is pure .NET", not impure, ", ".join(impure))

# 6. Central package management covers every PackageReference.
props = read(os.path.join(ROOT, "Directory.Packages.props"))
versions = set(re.findall(r'<PackageVersion Include="([^"]+)"', props))
missing = []
for path in walk_text([".csproj"]):
    for package in re.findall(r'<PackageReference Include="([^"]+)"', read(path)):
        if package not in versions:
            missing.append(package)
check("All PackageReferences have central versions", not missing, ", ".join(sorted(set(missing))))

# 7. resx keys all have strongly typed accessors.
resx = ET.parse(os.path.join(PROJ, "Properties", "Resources.resx")).getroot()
keys = {node.attrib["name"] for node in resx.findall("data")}
designer = read(os.path.join(PROJ, "Properties", "Resources.Designer.cs"))
accessors = set(re.findall(r"public static string (\w+) =>", designer))
check("Every resx key has a Designer accessor", keys <= accessors, ", ".join(sorted(keys - accessors)))

# 8. Required deliverables exist.
required = [
    "README.md", "Directory.Build.props", "Directory.Packages.props", "nuget.config", ".vsconfig",
    "Microsoft.CmdPal.Ext.Translate.sln",
    "Microsoft.CmdPal.Ext.Translate/Microsoft.CmdPal.Ext.Translate.csproj",
    "Microsoft.CmdPal.Ext.Translate/TranslateCommandsProvider.cs",
    "Microsoft.CmdPal.Ext.Translate/FallbackTranslateItem.cs",
    "Microsoft.CmdPal.Ext.Translate/Core/TranslationService.cs",
    "Microsoft.CmdPal.Ext.Translate/Core/TriggerParser.cs",
    "Microsoft.CmdPal.Ext.Translate/Pages/TranslateListPage.cs",
    "Microsoft.CmdPal.Ext.Translate/Pages/TranslateDetailPage.cs",
    "Microsoft.CmdPal.Ext.Translate/Helpers/SettingsManager.cs",
    "Microsoft.CmdPal.Ext.Translate/Helpers/SecretStore.cs",
    "Microsoft.CmdPal.Ext.Translate/Properties/Resources.resx",
    "tests/TranslateCmdPal.Core.UnitTests/TranslateCmdPal.Core.UnitTests.csproj",
    "repo-integration/integrate.py", "repo-integration/INTEGRATION.md", "repo-integration/VALIDATION.md",
]
absent = [p for p in required if not os.path.isfile(os.path.join(ROOT, p.replace("/", os.sep)))]
check("Required files present (" + str(len(required)) + ")", not absent, ", ".join(absent))

# 9. Publisher still needs the developer certificate before deploying.
check("Publisher matches the local dev certificate",
      'Publisher="CN=TranslateCmdPal Dev"' in manifest
      and os.path.isfile(os.path.join(ROOT, "install", "dev-signing.cer")))

for name, ok, detail in RESULTS:
    print(("PASS  " if ok else "FAIL  ") + name + ((" | " + detail) if detail and not ok else ""))

failed = sum(1 for _, ok, _ in RESULTS if not ok)
print("\n" + str(len(RESULTS) - failed) + "/" + str(len(RESULTS)) + " checks passed")
sys.exit(0 if failed == 0 else 1)

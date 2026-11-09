#!/usr/bin/env python3
"""Static guard for consumer Agent Skill references and 1.1 guidance.

This deliberately does not claim to evaluate an AI agent: the manual scenario in
skills/minimalopenapi/evals requires an external agent runner.
"""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[3]
SKILL = ROOT / "skills/minimalopenapi/SKILL.md"
GUIDE = ROOT / "docs/consumer-agents.md"
EVAL = ROOT / "skills/minimalopenapi/evals/consumer-scenario.md"

def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"Missing {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")

def main() -> None:
    skill = read(SKILL)
    guide = read(GUIDE)
    scenario = read(EVAL)
    assert re.search(r"\A---\nname: minimalopenapi\ndescription: .+\n---\n", skill), "Invalid skill frontmatter"
    for doc in [SKILL, GUIDE, EVAL, ROOT / "README.md"]:
        content = read(doc)
        for match in re.finditer(r"\]\(([^)]+\.md)(?:#[^)]*)?\)", content):
            target = (doc.parent / match.group(1)).resolve()
            assert target.is_relative_to(ROOT) and target.is_file(), f"Broken link in {doc.relative_to(ROOT)}: {match.group(1)}"

    for name in ["configuration", "generated-types", "response-handling", "diagnostics", "migration", "feature-support"]:
        assert f"references/{name}.md" in skill, f"Missing reference in SKILL.md: {name}"
        read(SKILL.parent / "references" / f"{name}.md")

    for value in [
        "ExcludeStatusCodesFromHandlers", "MinimalOpenApiHandler", "IncludeStatusCodes",
        "ExcludeStatusCodes", "OpenApiFile", "MOA015", "MOA016", "MOA017",
    ]:
        assert value in guide, f"Missing consumer guide feature: {value}"
    assert "1.0 supports:" not in guide, "Outdated supported-version heading"
    for value in ["oneOf", "anyOf", "inline", "nested"]:
        assert value in guide and value in skill + read(SKILL.parent / "references" / "feature-support.md") + read(SKILL.parent / "references" / "generated-types.md"), f"Missing feature guidance: {value}"
    for value in ["401", "403", "seven", "build", "middleware"]:
        assert value in scenario, f"Evaluation scenario missing {value}"
    print("Consumer skill structure, links, version guidance and evaluation fixture: OK")

if __name__ == "__main__":
    try:
        main()
    except AssertionError as error:
        print(f"Consumer skill validation failed: {error}", file=sys.stderr)
        sys.exit(1)

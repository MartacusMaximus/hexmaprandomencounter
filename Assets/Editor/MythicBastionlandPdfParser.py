#!/usr/bin/env python3
import json
import re
import sys
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import Optional

from pypdf import PdfReader


@dataclass
class Seer:
    seerName: str
    vigor: int
    clarity: int
    spirit: int
    guard: int
    traits: list[str] = field(default_factory=list)


@dataclass
class Steed:
    steedName: str
    vigor: int
    clarity: int
    spirit: int
    guard: int


@dataclass
class Equipment:
    name: str
    rulesText: str
    rarity: str
    displayCategory: str
    damageDiceNotation: str
    armorValue: int
    sourceTags: list[str] = field(default_factory=list)
    isBondedProperty: bool = False


@dataclass
class Knight:
    pageNumber: int
    knightName: str
    titleVerse: str
    passionTitle: str
    passionText: str
    abilityName: str
    abilityDescription: str
    linkedSeer: Seer
    steed: Steed
    propertyItems: list[Equipment] = field(default_factory=list)
    randomFlavorTableTitle: str = ""
    randomFlavorTableRows: list[str] = field(default_factory=list)
    personHook: str = ""
    objectHook: str = ""
    beastHook: str = ""
    stateHook: str = ""
    themeHook: str = ""


@dataclass
class CastEntry:
    name: str
    statBlock: str
    notes: str


@dataclass
class Myth:
    pageNumber: int
    mythName: str
    verse: str
    omens: list[str] = field(default_factory=list)
    castEntries: list[CastEntry] = field(default_factory=list)
    flavorTableTitle: str = ""
    flavorTableRows: list[str] = field(default_factory=list)
    dwelling: str = ""
    sanctum: str = ""
    monument: str = ""
    hazard: str = ""
    curse: str = ""
    ruin: str = ""


def clean_text(text: str) -> str:
    text = text.replace("\u2019", "'").replace("\u2018", "'").replace("\u201c", '"').replace("\u201d", '"')
    text = text.replace("\u2014", "-").replace("\u2026", "...")
    text = text.replace(" \n", "\n").replace("\n ", "\n")
    text = re.sub(r"[ \t]+", " ", text)
    text = re.sub(r"\n{2,}", "\n\n", text)
    return text.strip()


def block_between(text: str, start: str, end: Optional[str]) -> str:
    pattern = re.escape(start) + r"\n(.*)"
    match = re.search(pattern, text, re.S)
    if not match:
        return ""
    chunk = match.group(1)
    if end:
        stop = re.search(r"\n" + re.escape(end) + r"\n", chunk, re.S)
        if stop:
            chunk = chunk[:stop.start()]
    return chunk.strip()


def parse_statline(text: str) -> tuple[int, int, int, int]:
    match = re.search(r"VIG\s*(\d+),\s*CLA\s*(\d+),\s*SPI\s*(\d+),\s*(\d+)\s*GD", text)
    if not match:
        return 0, 0, 0, 0
    return tuple(int(group) for group in match.groups())


def split_top_level(text: str) -> list[str]:
    parts = []
    current = []
    depth = 0
    for char in text:
        if char == "(":
            depth += 1
        elif char == ")" and depth > 0:
            depth -= 1

        if char == "," and depth == 0:
            parts.append("".join(current).strip())
            current = []
            continue

        current.append(char)

    tail = "".join(current).strip()
    if tail:
        parts.append(tail)
    return parts


def normalize_item_name(text: str) -> str:
    text = text.strip().strip(".")
    if text.lower().startswith("and "):
        text = text[4:]
    return text.strip()


def infer_category(name: str, rules: str) -> str:
    probe = f"{name} {rules}".lower()
    if any(token in probe for token in ["shield"]):
        return "shield"
    if any(token in probe for token in ["mail", "plate", "helm", "gambeson", "armor", "armour", "greaves", "cloak", "vest", "hood", "gauntlet"]):
        return "armor"
    if any(token in probe for token in ["bow", "sword", "axe", "mace", "flail", "sickle", "staff", "lance", "javelin", "dagger", "hook", "punches", "talons", "beak", "spear"]):
        return "weapon"
    if "remedy" in probe or "medicine" in probe or "healing" in probe:
        return "remedy"
    if any(token in probe for token in ["amulet", "lantern", "harp", "flute", "horn", "coin", "ball"]):
        return "relic"
    return "tool"


def extract_damage(rules: str) -> str:
    match = re.search(r"(\d*d\d+)", rules.lower())
    if not match:
        return ""
    value = match.group(1)
    return value if not value.startswith("d") else f"1{value}"


def extract_armor(rules: str) -> int:
    match = re.search(r"A(\d+)", rules)
    return int(match.group(1)) if match else 0


def parse_property_items(block: str) -> tuple[list[Equipment], Optional[Steed]]:
    items: list[Equipment] = []
    steed = None
    for raw_bullet in re.findall(r"•\s+(.*?)(?=\n• |\nABILITY -|\Z)", block, re.S):
        bullet = clean_text(raw_bullet)
        if " steed" in bullet.lower() or "warhorse" in bullet.lower() or "stallion" in bullet.lower() or "horse" in bullet.lower():
            stats = parse_statline(bullet)
            name = clean_text(re.sub(r"\(.*", "", bullet)).strip()
            steed = Steed(name, *stats)
            continue

        fragments = split_top_level(bullet)
        merged: list[str] = []
        for fragment in fragments:
            if " and " in fragment and "(" not in fragment.split(" and ")[-1]:
                bits = [part.strip() for part in fragment.split(" and ") if part.strip()]
                merged.extend(bits)
            else:
                merged.append(fragment)

        for fragment in merged:
            fragment = normalize_item_name(fragment)
            if not fragment:
                continue
            name = clean_text(re.sub(r"\(.*", "", fragment)).strip()
            rules = clean_text(fragment[len(name):].strip(" ,"))
            rules = rules.strip()
            items.append(Equipment(
                name=name or fragment,
                rulesText=rules,
                rarity="bonded",
                displayCategory=infer_category(name or fragment, rules),
                damageDiceNotation=extract_damage(fragment),
                armorValue=extract_armor(fragment),
                sourceTags=["MythicBastionland", "KnightProperty"],
                isBondedProperty=True
            ))
    return items, steed


def parse_knight(page_number: int, text: str, issues: list[str]) -> Knight:
    property_block = block_between(text, "PROPERTY", "ABILITY -")
    ability_name_match = re.search(r"ABILITY - (.*?)\n", text)
    passion_match = re.search(r"PASSION - (.*?)\n(.*?)\nKNIGHTED BY…", text, re.S)
    seer_match = re.search(r"KNIGHTED BY…\n(.*?)\nVIG\s*(\d+),\s*CLA\s*(\d+),\s*SPI\s*(\d+),\s*(\d+)\s*GD\n(.*?)(?=\n[A-Z][^\n]*\n[A-Z][^\n]*\n[A-Za-z][^\n]* Knight\n)", text, re.S)
    tail_match = re.search(r"\n([A-Z][^\n]*)\n([A-Z][^\n]*)\n([A-Za-z][^\n]* Knight)\n", text)
    hook_match = re.search(r"Person:\s*(.*?)\s*~ Name:.*?Object:\s*(.*?)\s*~ Beast:\s*(.*?)\s*~ State:\s*(.*?)\s*~ Theme:\s*(.*)", text, re.S)

    if not (ability_name_match and passion_match and seer_match and tail_match):
        issues.append(f"Knight page {page_number} could not be parsed cleanly.")

    property_items, steed = parse_property_items(property_block)
    if steed is None:
        steed = Steed("Knight Steed", 10, 10, 5, 3)
        issues.append(f"Knight page {page_number} used fallback steed parsing.")

    seer_traits = [clean_text(line[1:]) for line in seer_match.group(6).splitlines() if line.strip().startswith("•")] if seer_match else []
    return Knight(
        pageNumber=page_number,
        knightName=clean_text(tail_match.group(3) if tail_match else f"Knight {page_number}"),
        titleVerse=clean_text("\n".join([tail_match.group(1), tail_match.group(2)]) if tail_match else ""),
        passionTitle=clean_text(passion_match.group(1) if passion_match else ""),
        passionText=clean_text(passion_match.group(2) if passion_match else ""),
        abilityName=clean_text(ability_name_match.group(1) if ability_name_match else f"Ability {page_number}"),
        abilityDescription=clean_text(block_between(text, f"ABILITY - {ability_name_match.group(1) if ability_name_match else ''}", "PASSION -") if ability_name_match else ""),
        linkedSeer=Seer(
            seerName=clean_text(seer_match.group(1) if seer_match else f"Seer {page_number}"),
            vigor=int(seer_match.group(2)) if seer_match else 0,
            clarity=int(seer_match.group(3)) if seer_match else 0,
            spirit=int(seer_match.group(4)) if seer_match else 0,
            guard=int(seer_match.group(5)) if seer_match else 0,
            traits=seer_traits
        ),
        steed=steed,
        propertyItems=property_items,
        randomFlavorTableTitle=clean_text(re.search(r"\n([A-Z][A-Z &']+)\n(?:[A-Za-z].*?\n)?1 ", text, re.S).group(1)) if re.search(r"\n([A-Z][A-Z &']+)\n(?:[A-Za-z].*?\n)?1 ", text, re.S) else "",
        randomFlavorTableRows=[clean_text(match.group(1)) for match in re.finditer(r"\n([1-6]\s.*?)(?=\n[1-6]\s|\nKNIGHTED BY…|\nPerson:)", text, re.S)],
        personHook=clean_text(hook_match.group(1) if hook_match else ""),
        objectHook=clean_text(hook_match.group(2) if hook_match else ""),
        beastHook=clean_text(hook_match.group(3) if hook_match else ""),
        stateHook=clean_text(hook_match.group(4) if hook_match else ""),
        themeHook=clean_text(hook_match.group(5) if hook_match else "")
    )


def parse_cast(block: str) -> list[CastEntry]:
    entries: list[CastEntry] = []
    chunk = block.strip()
    pattern = re.compile(r"([^\n]+)\n(VIG.*?GD)\n(.*?)(?=\n[^\n]+\nVIG|\Z)", re.S)
    for match in pattern.finditer(chunk):
        body = clean_text(match.group(3))
        lines = body.splitlines()
        stat_notes = lines[0] if lines else ""
        notes = "\n".join(lines[1:]).strip()
        entries.append(CastEntry(
            name=clean_text(match.group(1)),
            statBlock=clean_text(f"{match.group(2)}\n{stat_notes}".strip()),
            notes=clean_text(notes)
        ))
    return entries


def parse_myth(page_number: int, text: str, issues: list[str]) -> Myth:
    omen_matches = re.findall(r"([1-6]\.\s.*?)(?=\n[1-6]\.\s|\nCast\n)", text, re.S)
    cast_block = block_between(text, "Cast", None)
    tail_match = re.search(r"\n([A-Za-z].*?)\n([A-Za-z].*?)\n([A-Za-z][A-Za-z' -]+)\n([A-Z][A-Z &' -]+)\n(.*?)(?=\nDwelling:)", text, re.S)
    realm_match = re.search(r"Dwelling:\s*(.*?)\s*~ Sanctum:\s*(.*?)\s*~ Monument:\s*(.*?)\nHazard:\s*(.*?)\s*~ Curse:\s*(.*?)\s*~ Ruin:\s*(.*?)(?:\n|$)", text, re.S)

    if len(omen_matches) != 6:
        issues.append(f"Myth page {page_number} expected 6 omens but parsed {len(omen_matches)}.")

    if not tail_match:
        issues.append(f"Myth page {page_number} could not locate verse or table title cleanly.")

    table_rows = []
    if tail_match:
        title = clean_text(tail_match.group(4))
        row_block = clean_text(tail_match.group(5))
        table_rows = [clean_text(match.group(1)) for match in re.finditer(r"([1-6]\s.*?)(?=\n[1-6]\s|\Z)", row_block, re.S)]
    else:
        title = ""

    if len(table_rows) != 6:
        issues.append(f"Myth page {page_number} expected 6 flavor rows but parsed {len(table_rows)}.")

    cast_source = cast_block
    if tail_match:
        cast_source = cast_source[:cast_source.find(tail_match.group(1))].strip()

    return Myth(
        pageNumber=page_number,
        mythName=clean_text(tail_match.group(3) if tail_match else f"Myth {page_number}"),
        verse=clean_text("\n".join([tail_match.group(1), tail_match.group(2)]) if tail_match else ""),
        omens=[clean_text(omen) for omen in omen_matches[:6]],
        castEntries=parse_cast(cast_source),
        flavorTableTitle=title,
        flavorTableRows=table_rows,
        dwelling=clean_text(realm_match.group(1) if realm_match else ""),
        sanctum=clean_text(realm_match.group(2) if realm_match else ""),
        monument=clean_text(realm_match.group(3) if realm_match else ""),
        hazard=clean_text(realm_match.group(4) if realm_match else ""),
        curse=clean_text(realm_match.group(5) if realm_match else ""),
        ruin=clean_text(realm_match.group(6) if realm_match else "")
    )


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: MythicBastionlandPdfParser.py <pdf> <output>", file=sys.stderr)
        return 1

    pdf_path = Path(sys.argv[1])
    output_path = Path(sys.argv[2])
    reader = PdfReader(str(pdf_path))
    issues: list[str] = []

    knights = []
    myths = []
    for page_number in range(28, 171, 2):
        text = clean_text(reader.pages[page_number - 1].extract_text() or "")
        knights.append(parse_knight(page_number, text, issues))

    for page_number in range(29, 172, 2):
        text = clean_text(reader.pages[page_number - 1].extract_text() or "")
        myths.append(parse_myth(page_number, text, issues))

    payload = {
        "knights": [asdict(entry) for entry in knights],
        "myths": [asdict(entry) for entry in myths],
        "issues": issues,
    }
    output_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

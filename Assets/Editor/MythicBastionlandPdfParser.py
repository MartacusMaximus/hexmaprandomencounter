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
    isSupportFragment: bool = False


@dataclass
class TableColumn:
    header: str
    values: list[str] = field(default_factory=list)


@dataclass
class RollTable:
    title: str = ""
    columns: list[TableColumn] = field(default_factory=list)


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
    randomFlavorTable: RollTable = field(default_factory=RollTable)
    personHook: str = ""
    nameHook: str = ""
    characteristicHook: str = ""
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
    flavorTable: RollTable = field(default_factory=RollTable)
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


def inline_text(text: str) -> str:
    return clean_text(text).replace("\n", " ").strip()


def block_between(text: str, start: str, end: Optional[str]) -> str:
    pattern = re.escape(start) + r"\n(.*)"
    match = re.search(pattern, text, re.S)
    if not match:
        return ""
    chunk = match.group(1)
    if end:
        stop = re.search(r"\n" + re.escape(end) + r"(?=[ \t]|\n|$)", chunk)
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
    in_quotes = False
    for char in text:
        if char == '"':
            in_quotes = not in_quotes

        if not in_quotes:
            if char == "(":
                depth += 1
            elif char == ")" and depth > 0:
                depth -= 1

        if char == "," and depth == 0 and not in_quotes:
            parts.append("".join(current).strip())
            current = []
            continue

        current.append(char)

    tail = "".join(current).strip()
    if tail:
        parts.append(tail)
    return parts


def split_top_level_items(text: str) -> list[str]:
    if text.count("(") < 2:
        return [text.strip()] if text.strip() else []

    parts = []
    current = []
    depth = 0
    index = 0

    while index < len(text):
        char = text[index]
        if char == "(":
            depth += 1
        elif char == ")" and depth > 0:
            depth -= 1

        if depth == 0 and text.startswith(" and ", index):
            fragment = "".join(current).strip()
            if fragment:
                parts.append(fragment)
            current = []
            index += 5
            continue

        current.append(char)
        index += 1

    tail = "".join(current).strip()
    if tail:
        parts.append(tail)
    return parts


def normalize_item_name(text: str) -> str:
    text = text.strip().strip(".")
    text = re.sub(r"^and\s+", "", text, flags=re.IGNORECASE)
    return text.strip()


def infer_category(name: str, rules: str) -> str:
    probe = f"{name} {rules}".lower()
    if any(token in probe for token in ["shield"]):
        return "shield"
    if any(token in probe for token in ["mail", "plate", "helm", "gambeson", "armor", "armour", "greaves", "cloak", "vest", "hood", "gauntlet"]):
        return "armor"
    if any(token in probe for token in ["bow", "sword", "axe", "mace", "flail", "sickle", "staff", "lance", "javelin", "dagger", "hook", "punches", "talons", "beak", "spear", "halberd", "glaive", "trident", "guisarme", "club"]):
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

        fragments: list[str] = []
        for fragment in split_top_level(bullet):
            fragments.extend(split_top_level_items(fragment))

        for fragment_index, fragment in enumerate(fragments):
            fragment = normalize_item_name(fragment)
            if not fragment:
                continue
            base_name = fragment.split("(", 1)[0].strip()
            name = inline_text(base_name)
            rules = inline_text(fragment[len(base_name):].strip(" ,"))
            rules = rules.strip()
            items.append(Equipment(
                name=name or fragment,
                rulesText=rules,
                rarity="bonded",
                displayCategory=infer_category(name or fragment, rules),
                damageDiceNotation=extract_damage(fragment),
                armorValue=extract_armor(fragment),
                sourceTags=["MythicBastionland", "KnightProperty"],
                isBondedProperty=True,
                isSupportFragment=fragment_index > 0
            ))
    return items, steed


@dataclass
class PositionedFragment:
    x: float
    text: str


@dataclass
class PositionedLine:
    y: float
    fragments: list[PositionedFragment] = field(default_factory=list)

    @property
    def ordered_fragments(self) -> list[PositionedFragment]:
        return sorted(self.fragments, key=lambda fragment: fragment.x)

    @property
    def text(self) -> str:
        return clean_text(" ".join(fragment.text for fragment in self.ordered_fragments))


def strip_page_artifacts(text: str) -> str:
    text = clean_text(text)
    text = re.sub(r"^\d+\n", "", text)
    text = re.sub(r"\n(?:\d+\n){0,2}The$", "", text)
    return text.strip()


def find_table_row_start(text: str) -> int:
    for match in re.finditer(r"\n1 ", text):
        tail = text[match.start():]
        if all(f"\n{number} " in tail for number in range(2, 7)):
            return match.start()
    return -1


def is_uppercase_table_title(line: str) -> bool:
    normalized = inline_text(line)
    letters = re.sub(r"[^A-Za-z]", "", normalized)
    return bool(letters) and normalized == normalized.upper()


def find_table_title_index(lines: list[str]) -> int:
    for index in range(len(lines) - 1, -1, -1):
        if is_uppercase_table_title(lines[index]):
            return index
    return -1


def extract_positioned_lines(page) -> list[PositionedLine]:
    fragments: list[tuple[float, float, str]] = []

    def visit(text: str, cm, tm, font_dict, font_size) -> None:
        if text.strip():
            fragments.append((cm[5], cm[4], clean_text(text)))

    page.extract_text(visitor_text=visit)
    fragments.sort(key=lambda item: (-item[0], item[1]))

    lines: list[PositionedLine] = []
    for y, x, text in fragments:
        if lines and abs(lines[-1].y - y) <= 1.5:
            lines[-1].fragments.append(PositionedFragment(x=x, text=text))
        else:
            lines.append(PositionedLine(y=y, fragments=[PositionedFragment(x=x, text=text)]))
    return lines


def is_number_fragment(text: str) -> bool:
    return bool(re.fullmatch(r"[1-6]\.?", text.strip()))


def line_matches_marker(line: PositionedLine, marker: str) -> bool:
    normalized_line = inline_text(line.text)
    normalized_marker = inline_text(marker)
    return normalized_line == normalized_marker or normalized_line.startswith(normalized_marker)


def parse_roll_table(page, title: str, stop_markers: list[str], issues: list[str], label: str) -> RollTable:
    lines = extract_positioned_lines(page)
    normalized_title = inline_text(title)
    title_index = -1
    content_min_x = 0.0

    for index, line in enumerate(lines):
        normalized_line = inline_text(line.text)
        if normalized_title not in normalized_line:
            continue

        title_fragments = [
            fragment for fragment in line.ordered_fragments
            if inline_text(fragment.text) and (
                inline_text(fragment.text) in normalized_title
                or normalized_title in inline_text(fragment.text)
            )
        ]
        if not title_fragments:
            continue

        title_index = index
        content_min_x = max(0.0, min(fragment.x for fragment in title_fragments) - 100.0)
        break

    if title_index == -1:
        issues.append(f"{label} could not locate table title '{title}' in positional page data.")
        return RollTable(title=title)

    index = title_index + 1
    header_lines: list[PositionedLine] = []
    while index < len(lines):
        line = lines[index]
        if any(line_matches_marker(line, marker) for marker in stop_markers):
            break
        fragments = [fragment for fragment in line.ordered_fragments if fragment.x >= content_min_x]
        if fragments and is_number_fragment(fragments[0].text):
            break
        if fragments:
            header_lines.append(PositionedLine(y=line.y, fragments=fragments))
        index += 1

    if not header_lines:
        issues.append(f"{label} could not locate a header row for table '{title}'.")
        return RollTable(title=title)

    header_groups = []
    for header_line in header_lines:
        for fragment in header_line.ordered_fragments:
            if is_number_fragment(fragment.text):
                continue

            group = next((candidate for candidate in header_groups if abs(candidate["x"] - fragment.x) <= 60.0), None)
            if group is None:
                group = {"x": fragment.x, "parts": []}
                header_groups.append(group)
            group["parts"].append(fragment.text)

    header_groups.sort(key=lambda group: group["x"])
    column_positions = [float(group["x"]) for group in header_groups]
    columns = [TableColumn(header=inline_text(" ".join(group["parts"]))) for group in header_groups]
    rows: list[list[str]] = []
    current_row: Optional[list[str]] = None
    pending_next_row_fragments: list[PositionedFragment] = []

    while index < len(lines):
        line = lines[index]
        if any(line_matches_marker(line, marker) for marker in stop_markers):
            break

        fragments = [fragment for fragment in line.ordered_fragments if fragment.x >= content_min_x]
        if not fragments:
            index += 1
            continue

        row_fragments = fragments
        if is_number_fragment(fragments[0].text):
            if current_row is not None:
                rows.append(current_row)
            current_row = ["" for _ in columns]
            row_fragments = pending_next_row_fragments + fragments[1:]
            pending_next_row_fragments = []
        elif current_row is None:
            index += 1
            continue
        else:
            next_index = index + 1
            next_fragments = []
            while next_index < len(lines):
                if any(line_matches_marker(lines[next_index], marker) for marker in stop_markers):
                    break
                next_fragments = [fragment for fragment in lines[next_index].ordered_fragments if fragment.x >= content_min_x]
                if next_fragments:
                    break
                next_index += 1

            if next_fragments and is_number_fragment(next_fragments[0].text):
                pending_next_row_fragments.extend(fragments)
                index += 1
                continue

        for fragment in row_fragments:
            column_index = min(range(len(column_positions)), key=lambda candidate: abs(column_positions[candidate] - fragment.x))
            current_row[column_index] = inline_text(f"{current_row[column_index]} {fragment.text}".strip())

        index += 1

    if current_row is not None:
        rows.append(current_row)

    for column_index, column in enumerate(columns):
        column.values = [inline_text(row[column_index]) for row in rows if any(cell.strip() for cell in row)]

    if any(len(column.values) != 6 for column in columns):
        issues.append(f"{label} expected 6 rows in table '{title}' but parsed {[len(column.values) for column in columns]}.")

    return RollTable(title=title, columns=columns)


def parse_seer(block: str, page_number: int, issues: list[str]) -> Seer:
    lines = [line for line in block.splitlines() if line.strip()]
    if len(lines) < 2:
        issues.append(f"Knight page {page_number} could not parse its linked Seer block.")
        return Seer(f"Seer {page_number}", 0, 0, 0, 0, [])

    body = "\n".join(lines[2:]).strip()
    traits = [clean_text(match.group(1)) for match in re.finditer(r"•\s*(.*?)(?=\n• |\Z)", body, re.S)]
    vigor, clarity, spirit, guard = parse_statline(lines[1])
    return Seer(
        seerName=clean_text(lines[0]),
        vigor=vigor,
        clarity=clarity,
        spirit=spirit,
        guard=guard,
        traits=traits,
    )


def parse_knight(page_number: int, page, issues: list[str]) -> Knight:
    text = strip_page_artifacts(page.extract_text() or "")
    hook_match = re.search(
        r"Person:\s*(.*?)\s*~\s*Name:\s*(.*?)\s*~\s*Characteristic:\s*(.*?)\nObject:\s*(.*?)\s*~\s*Beast:\s*(.*?)\s*~\s*State:\s*(.*?)\s*~\s*Theme:\s*(.*)$",
        text,
        re.S,
    )
    text_without_hooks = text[:hook_match.start()].strip() if hook_match else text

    if "\nKNIGHTED BY...\n" not in text_without_hooks:
        issues.append(f"Knight page {page_number} could not locate the KNIGHTED BY section.")
        pre_seer = text_without_hooks
        seer_block = ""
    else:
        pre_seer, seer_block = text_without_hooks.split("\nKNIGHTED BY...\n", 1)

    row_start = find_table_row_start(pre_seer)
    head_lines = pre_seer[:row_start].strip().splitlines() if row_start != -1 else []
    title_index = find_table_title_index(head_lines)
    if title_index == -1:
        issues.append(f"Knight page {page_number} could not locate verse, name, and table metadata cleanly.")
        table_title = ""
        before_table_lines: list[str] = []
    else:
        table_title = clean_text(head_lines[title_index])
        before_table_lines = head_lines[:title_index]

    if len(before_table_lines) < 3:
        title_verse = ""
        knight_name = f"Knight {page_number}"
        prefix_before_verse = pre_seer
    else:
        title_verse = clean_text("\n".join(before_table_lines[-3:-1]))
        knight_name = clean_text(before_table_lines[-1])
        prefix_before_verse = "\n".join(before_table_lines[:-3]).strip()

    ability_match = re.search(r"ABILITY -\s*(.*?)\n(.*?)\nPASSION -", prefix_before_verse, re.S)
    passion_match = re.search(r"PASSION -\s*(.*?)\n(.*)$", prefix_before_verse, re.S)
    property_block = block_between(prefix_before_verse, "PROPERTY", "ABILITY -")

    property_items, steed = parse_property_items(property_block)
    if steed is None:
        steed = Steed("Knight Steed", 10, 10, 5, 3)
        issues.append(f"Knight page {page_number} used fallback steed parsing.")

    random_table = parse_roll_table(page, table_title, ["KNIGHTED BY...", "Person"], issues, f"Knight page {page_number}") if table_title else RollTable()
    linked_seer = parse_seer(seer_block, page_number, issues)

    if not ability_match or not passion_match:
        issues.append(f"Knight page {page_number} could not parse ability and passion sections cleanly.")

    return Knight(
        pageNumber=page_number,
        knightName=knight_name,
        titleVerse=title_verse,
        passionTitle=clean_text(passion_match.group(1) if passion_match else ""),
        passionText=clean_text(passion_match.group(2) if passion_match else ""),
        abilityName=clean_text(ability_match.group(1) if ability_match else f"Ability {page_number}"),
        abilityDescription=clean_text(ability_match.group(2) if ability_match else ""),
        linkedSeer=linked_seer,
        steed=steed,
        propertyItems=property_items,
        randomFlavorTable=random_table,
        personHook=clean_text(hook_match.group(1) if hook_match else ""),
        nameHook=clean_text(hook_match.group(2) if hook_match else ""),
        characteristicHook=clean_text(hook_match.group(3) if hook_match else ""),
        objectHook=clean_text(hook_match.group(4) if hook_match else ""),
        beastHook=clean_text(hook_match.group(5) if hook_match else ""),
        stateHook=clean_text(hook_match.group(6) if hook_match else ""),
        themeHook=clean_text(hook_match.group(7) if hook_match else ""),
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


def parse_myth(page_number: int, page, issues: list[str]) -> Myth:
    text = strip_page_artifacts(page.extract_text() or "")
    omen_matches = re.findall(r"([1-6]\.\s.*?)(?=\n[1-6]\.\s|\nCast\n)", text, re.S)
    realm_match = re.search(r"Dwelling:\s*(.*?)\s*~ Sanctum:\s*(.*?)\s*~ Monument:\s*(.*?)\nHazard:\s*(.*?)\s*~ Curse:\s*(.*?)\s*~ Ruin:\s*(.*?)(?:\n|$)", text, re.S)

    if len(omen_matches) != 6:
        issues.append(f"Myth page {page_number} expected 6 omens but parsed {len(omen_matches)}.")

    if not realm_match:
        issues.append(f"Myth page {page_number} could not locate realm flavor metadata.")
        core_text = text
    else:
        core_text = text[:realm_match.start()].strip()

    core_lines = core_text.splitlines()
    myth_name = clean_text(core_lines[-1]) if core_lines else f"Myth {page_number}"
    before_name = "\n".join(core_lines[:-1]).strip() if len(core_lines) > 1 else ""
    row_start = find_table_row_start(before_name)
    head_lines = before_name[:row_start].strip().splitlines() if row_start != -1 else []
    title_index = find_table_title_index(head_lines)
    if title_index == -1:
        issues.append(f"Myth page {page_number} could not locate verse and table metadata cleanly.")
        verse = ""
        table_title = ""
        cast_source = block_between(before_name, "Cast", None)
    else:
        verse_lines = head_lines[:title_index]
        verse = clean_text("\n".join(verse_lines[-2:])) if len(verse_lines) >= 2 else ""
        table_title = clean_text(head_lines[title_index])
        cast_and_omens = "\n".join(verse_lines[:-2]).strip() if len(verse_lines) >= 2 else ""
        cast_source = block_between(cast_and_omens, "Cast", None)

    flavor_table = parse_roll_table(page, table_title, ["Dwelling"], issues, f"Myth page {page_number}") if table_title else RollTable()

    return Myth(
        pageNumber=page_number,
        mythName=myth_name,
        verse=verse,
        omens=[clean_text(omen) for omen in omen_matches[:6]],
        castEntries=parse_cast(cast_source),
        flavorTable=flavor_table,
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
        page = reader.pages[page_number - 1]
        knights.append(parse_knight(page_number, page, issues))

    for page_number in range(29, 172, 2):
        page = reader.pages[page_number - 1]
        myths.append(parse_myth(page_number, page, issues))

    payload = {
        "knights": [asdict(entry) for entry in knights],
        "myths": [asdict(entry) for entry in myths],
        "issues": issues,
    }
    output_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

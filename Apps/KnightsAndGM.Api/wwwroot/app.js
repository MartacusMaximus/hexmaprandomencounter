const svgNs = "http://www.w3.org/2000/svg";

const state = {
  token: null,
  user: null,
  campaigns: [],
  activeCampaignId: null,
  map: null,
  characters: [],
  selectedHexId: null,
  activePartyId: null,
  moveMode: false,
  connection: null,
  selectedCharacterId: null,
  characterDraft: null,
  characterDirty: false
};

const ui = {
  loginForm: document.getElementById("login-form"),
  refreshButton: document.getElementById("refresh-button"),
  statusText: document.getElementById("status-text"),
  campaignTitle: document.getElementById("campaign-title"),
  campaignSubtitle: document.getElementById("campaign-subtitle"),
  mapSvg: document.getElementById("map-svg"),
  partySummary: document.getElementById("party-summary"),
  notesList: document.getElementById("notes-list"),
  noteForm: document.getElementById("note-form"),
  noteBody: document.getElementById("note-body"),
  selectedHexTitle: document.getElementById("selected-hex-title"),
  selectedHexLabel: document.getElementById("selected-hex-label"),
  selectedHexDetails: document.getElementById("selected-hex-details"),
  gmEncounters: document.getElementById("gm-encounters"),
  legendList: document.getElementById("legend-list"),
  charactersList: document.getElementById("characters-list"),
  characterSheet: document.getElementById("character-sheet"),
  newCharacterButton: document.getElementById("new-character-button")
};

const terrainPalette = {
  Unknown: { fill: "#ddd7cf", stroke: "#b4aa9a", label: "Unknown" },
  City: { fill: "#cbc5bd", stroke: "#81786e", label: "City" },
  Plains: { fill: "#f1edc9", stroke: "#38c762", label: "Plains" },
  Forest: { fill: "#0b8d37", stroke: "#14d76b", label: "Forest" },
  Swamp: { fill: "#68999a", stroke: "#f2ae17", label: "Swampland" },
  Water: { fill: "#4b78bc", stroke: "#dceafe", label: "Fresh Water" },
  Mountain: { fill: "#9fa78d", stroke: "#6f7761", label: "Mountain" },
  Tundra: { fill: "#e6e5e6", stroke: "#f5f5f5", label: "Tundra" },
  Desert: { fill: "#fff0b7", stroke: "#f7a83a", label: "Desert" },
  Badlands: { fill: "#c8dc38", stroke: "#7d9240", label: "Chaparral" },
  Tar: { fill: "#7d7c80", stroke: "#4d4a4c", label: "Tarred Ground" },
  Deadwood: { fill: "#763d4d", stroke: "#7952e0", label: "Poison Woods" },
  Ice: { fill: "#fcfdff", stroke: "#c2ddff", label: "Ice" }
};

const landmarkLabels = {
  temple: "TMP",
  city: "CTY",
  fortress: "FRT",
  village: "VIL",
  ruins: "RUN",
  dungeon: "DGN",
  lair: "LAR"
};

ui.loginForm.addEventListener("submit", onLogin);
ui.refreshButton.addEventListener("click", refreshAll);
ui.noteForm.addEventListener("submit", onCreateNote);
ui.newCharacterButton.addEventListener("click", onNewCharacter);

renderLegend();

async function onLogin(event) {
  event.preventDefault();
  await authenticate("/auth/login", {
    username: document.getElementById("login-username").value,
    role: document.getElementById("login-role").value
  });
}

async function authenticate(path, payload) {
  setStatus("Working...");
  const response = await fetch(path, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  const data = await readJsonSafely(response);
  if (!response.ok) {
    setStatus(data?.error || "Request failed.");
    return;
  }

  state.token = data.token;
  state.user = data.user;
  state.campaigns = data.campaigns || [];
  state.activeCampaignId = state.campaigns[0]?.campaignId || null;
  setStatus(`Signed in as ${state.user.displayName}.`);
  await connectRealtime();
  await refreshAll();
}

async function refreshAll() {
  if (!state.token || !state.activeCampaignId) {
    return;
  }

  const preservedCharacterId = state.selectedCharacterId;
  const map = await apiGet(`/campaigns/${state.activeCampaignId}/map`);
  state.map = map;
  state.activePartyId = state.activePartyId && map.parties.some(party => party.id === state.activePartyId)
    ? state.activePartyId
    : map.parties[0]?.id || null;

  if (!state.selectedHexId || !map.tiles.some(tile => tile.id === state.selectedHexId)) {
    state.selectedHexId = state.activePartyId
      ? map.tiles.find(tile => {
          const party = map.parties.find(candidate => candidate.id === state.activePartyId);
          return party && sameCoordinate(tile.coordinate, party.currentHex);
        })?.id || map.tiles[0]?.id || null
      : map.tiles[0]?.id || null;
  }

  state.characters = await apiGet(`/campaigns/${state.activeCampaignId}/characters`);
  if (preservedCharacterId && state.characters.some(character => character.character.id === preservedCharacterId)) {
    selectCharacter(preservedCharacterId, false);
  } else if (!state.characterDraft || state.selectedCharacterId !== null) {
    const firstCharacterId = state.characters[0]?.character.id || null;
    if (firstCharacterId) {
      selectCharacter(firstCharacterId, false);
    } else {
      state.selectedCharacterId = null;
      state.characterDraft = null;
      state.characterDirty = false;
    }
  }

  renderPartySummary();
  renderMap();
  renderNotes();
  renderCharactersList();
  renderCharacterSheet();
  await refreshEncounters();
}

async function refreshEncounters() {
  if (!state.map || state.map.role !== "GameMaster") {
    ui.gmEncounters.innerHTML = "<p class='muted'>GM-only encounter prompts appear here after travel.</p>";
    return;
  }

  const encounters = await apiGet(`/campaigns/${state.activeCampaignId}/gm/encounters`);
  ui.gmEncounters.innerHTML = "";
  if (!encounters.length) {
    ui.gmEncounters.innerHTML = "<p class='muted'>No encounters logged yet.</p>";
    return;
  }

  encounters.forEach(encounter => {
    const card = document.createElement("article");
    card.className = "encounter-card";
    card.innerHTML = `
      <strong>${escapeHtml(encounter.partyName)}</strong>
      <p>${escapeHtml(encounter.prompt.summary)}</p>
      <p class="muted">${escapeHtml(encounter.prompt.secretDetails)}</p>
    `;
    ui.gmEncounters.appendChild(card);
  });
}

function renderLegend() {
  ui.legendList.innerHTML = "";
  const orderedTerrain = ["Tundra", "Ice", "Water", "Swamp", "Deadwood", "Forest", "Plains", "Badlands", "Mountain", "Tar", "Desert", "City"];
  orderedTerrain.forEach(key => {
    const terrain = terrainPalette[key];
    const row = document.createElement("div");
    row.className = "legend-entry";
    row.innerHTML = `
      <span class="legend-swatch" style="background:${terrain.fill}; border-color:${terrain.stroke};"></span>
      <div>
        <strong>${terrain.label}</strong>
      </div>
    `;
    ui.legendList.appendChild(row);
  });
}

function renderPartySummary() {
  ui.partySummary.innerHTML = "";
  if (!state.map) {
    ui.partySummary.innerHTML = "<p class='muted'>Log in to load the active party.</p>";
    return;
  }

  const activeParty = getActiveParty();
  if (!activeParty) {
    ui.partySummary.innerHTML = "<p class='muted'>No active party is present on this campaign.</p>";
    return;
  }

  const partyTile = state.map.tiles.find(tile => sameCoordinate(tile.coordinate, activeParty.currentHex));
  const card = document.createElement("article");
  card.className = "summary-card";
  card.innerHTML = `
    <strong>${escapeHtml(activeParty.name)}</strong>
    <p class="muted">Current hex: Q ${activeParty.currentHex.q}, R ${activeParty.currentHex.r}</p>
    <p>Effort: ${activeParty.remainingEffort} / ${activeParty.totalEffort}</p>
    <p>${partyTile?.publicLandmarkName ? escapeHtml(partyTile.publicLandmarkName) : "No public landmark discovered in this hex yet."}</p>
  `;
  ui.partySummary.appendChild(card);
}

function renderMap() {
  ui.mapSvg.innerHTML = "";
  if (!state.map) {
    return;
  }

  ui.campaignTitle.textContent = state.map.name;
  ui.campaignSubtitle.textContent = `${state.user.displayName} • ${state.map.role}`;

  const size = 44;
  const positionedTiles = state.map.tiles.map(tile => {
    const point = axialToPoint(tile.coordinate, size);
    return { tile, x: point.x, y: point.y };
  });

  const minX = Math.min(...positionedTiles.map(item => item.x)) - size * 2;
  const maxX = Math.max(...positionedTiles.map(item => item.x)) + size * 2;
  const minY = Math.min(...positionedTiles.map(item => item.y)) - size * 2;
  const maxY = Math.max(...positionedTiles.map(item => item.y)) + size * 2;
  const width = maxX - minX;
  const height = maxY - minY;

  ui.mapSvg.setAttribute("viewBox", `0 0 ${width} ${height}`);

  const activeParty = getActiveParty();

  positionedTiles.forEach(({ tile, x, y }) => {
    const translatedX = x - minX;
    const translatedY = y - minY;
    const isSelected = state.selectedHexId === tile.id;
    const isReachable = state.moveMode && activeParty && hexDistance(activeParty.currentHex, tile.coordinate) === 1;
    const partyHere = state.map.parties.find(party => sameCoordinate(party.currentHex, tile.coordinate));
    const terrain = terrainPalette[tile.terrain] || terrainPalette.Unknown;

    const group = createSvg("g");
    group.classList.add("hex-cell");
    if (isSelected) {
      group.classList.add("is-selected");
    }
    if (isReachable) {
      group.classList.add("is-reachable");
    }
    if (!tile.isExploredPublic && state.map.role !== "GameMaster") {
      group.classList.add("is-hidden");
    }

    const polygon = createSvg("polygon");
    polygon.setAttribute("points", createHexPoints(translatedX, translatedY, size));
    polygon.setAttribute("fill", terrain.fill);
    polygon.setAttribute("stroke", terrain.stroke);
    polygon.classList.add("hex-outline");
    group.appendChild(polygon);

    if (!tile.isExploredPublic && state.map.role !== "GameMaster") {
      const fog = createSvg("polygon");
      fog.setAttribute("points", createHexPoints(translatedX, translatedY, size));
      fog.classList.add("hex-fog");
      group.appendChild(fog);
    }

    if (tile.landmarkKind) {
      const landmark = createSvg("text");
      landmark.setAttribute("x", translatedX);
      landmark.setAttribute("y", translatedY + 6);
      landmark.classList.add("landmark-code");
      landmark.textContent = landmarkLabels[tile.landmarkKind] || tile.landmarkKind.slice(0, 3).toUpperCase();
      group.appendChild(landmark);
    }

    const coordLabel = createSvg("text");
    coordLabel.setAttribute("x", translatedX);
    coordLabel.setAttribute("y", translatedY + size - 9);
    coordLabel.classList.add("hex-coord");
    coordLabel.textContent = `${tile.coordinate.q},${tile.coordinate.r}`;
    group.appendChild(coordLabel);

    if (tile.isExploredPublic && tile.publicLandmarkName) {
      const shortLabel = createSvg("text");
      shortLabel.setAttribute("x", translatedX);
      shortLabel.setAttribute("y", translatedY - size + 18);
      shortLabel.classList.add("hex-label");
      shortLabel.textContent = shorten(tile.publicLandmarkName, 14);
      group.appendChild(shortLabel);
    } else if (state.map.role === "GameMaster" && tile.secretLandmarkName) {
      const shortLabel = createSvg("text");
      shortLabel.setAttribute("x", translatedX);
      shortLabel.setAttribute("y", translatedY - size + 18);
      shortLabel.classList.add("hex-label");
      shortLabel.textContent = shorten(tile.secretLandmarkName, 14);
      group.appendChild(shortLabel);
    }

    if (partyHere) {
      group.appendChild(createPartyMarker(translatedX, translatedY, partyHere.id === state.activePartyId));
    }

    group.addEventListener("click", async () => {
      if (partyHere) {
        state.activePartyId = partyHere.id;
        state.moveMode = !(state.moveMode && partyHere.id === state.activePartyId);
        state.selectedHexId = tile.id;
        renderPartySummary();
        renderMap();
        renderNotes();
        return;
      }

      state.selectedHexId = tile.id;
      renderNotes();

      if (isReachable) {
        await moveParty(tile.coordinate);
      } else {
        renderMap();
      }
    });

    ui.mapSvg.appendChild(group);
  });
}

function renderNotes() {
  ui.notesList.innerHTML = "";
  ui.selectedHexDetails.innerHTML = "";

  if (!state.map) {
    return;
  }

  const selectedTile = getSelectedTile();
  if (!selectedTile) {
    ui.selectedHexTitle.textContent = "Hex Details";
    ui.selectedHexLabel.textContent = "Select a hex to inspect terrain and notes.";
    return;
  }

  const isGm = state.map.role === "GameMaster";
  ui.selectedHexTitle.textContent = selectedTile.publicLandmarkName || selectedTile.secretLandmarkName || `Hex ${selectedTile.coordinate.q}, ${selectedTile.coordinate.r}`;
  ui.selectedHexLabel.textContent = `Terrain: ${terrainPalette[selectedTile.terrain]?.label || selectedTile.terrain} • ${selectedTile.isExploredPublic ? "Discovered" : "Undiscovered"}`;

  const details = document.createElement("article");
  details.className = "hex-card";
  details.innerHTML = `
    <strong>Q ${selectedTile.coordinate.q}, R ${selectedTile.coordinate.r}</strong>
    <p>${selectedTile.publicLandmarkName ? escapeHtml(selectedTile.publicLandmarkName) : "No public landmark sign has been uncovered here yet."}</p>
    ${isGm && selectedTile.secretDetails ? `<p class="muted">${escapeHtml(selectedTile.secretDetails)}</p>` : ""}
  `;
  ui.selectedHexDetails.appendChild(details);

  const notes = state.map.notes.filter(note => note.hexTileId === selectedTile.id);
  if (!notes.length) {
    ui.notesList.innerHTML = "<p class='muted'>No public notes have been posted on this hex yet.</p>";
  } else {
    notes.forEach(note => {
      const card = document.createElement("article");
      card.className = "note-card";
      card.innerHTML = `
        <strong>${escapeHtml(note.authorName)}</strong>
        <p>${escapeHtml(note.body)}</p>
        <p class="muted">${new Date(note.createdAtUtc).toLocaleString()}</p>
      `;
      ui.notesList.appendChild(card);
    });
  }

  const canPostNote = isGm || selectedTile.isExploredPublic;
  ui.noteBody.disabled = !canPostNote;
  const noteButton = ui.noteForm.querySelector("button[type=submit]");
  noteButton.disabled = !canPostNote;
  ui.noteBody.placeholder = canPostNote
    ? "Write a field report for future adventurers"
    : "Players can post notes only on discovered hexes.";
}

function renderCharactersList() {
  ui.charactersList.innerHTML = "";

  if (state.characterDraft && state.selectedCharacterId === null) {
    const draftTab = document.createElement("button");
    draftTab.type = "button";
    draftTab.className = "character-tab active";
    draftTab.innerHTML = `<strong>${escapeHtml(state.characterDraft.name || "Unsaved Character")}</strong><span class="muted">Unsaved</span>`;
    ui.charactersList.appendChild(draftTab);
  }

  state.characters.forEach(entry => {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `character-tab${entry.character.id === state.selectedCharacterId ? " active" : ""}`;
    button.innerHTML = `
      <strong>${escapeHtml(entry.character.name)}</strong>
      <span class="muted">${entry.pointsLeft} pts left</span>
    `;
    button.addEventListener("click", () => selectCharacter(entry.character.id, true));
    ui.charactersList.appendChild(button);
  });
}

function renderCharacterSheet() {
  if (!state.characterDraft) {
    ui.characterSheet.innerHTML = "<p class='muted'>No character sheet is available yet.</p>";
    return;
  }

  const sourceEntry = getSelectedCharacterResponse();
  const creation = sourceEntry?.creation || defaultCreationConfig();
  const derived = calculateCharacterDerived(state.characterDraft, creation);
  const chunkEntries = Object.entries(derived.chunkCounts);
  const inventory = Array.isArray(state.characterDraft.inventory) ? state.characterDraft.inventory : [];

  ui.characterSheet.innerHTML = `
    <div class="sheet-grid">
      <div class="sheet-row">
        <label for="character-name"><strong>Name</strong></label>
        <input id="character-name" value="${escapeAttribute(state.characterDraft.name || "")}" />
        <p class="muted">${state.characterDirty ? "Unsaved changes" : "Saved to campaign state"}</p>
      </div>

      <div class="virtue-grid">
        ${renderVirtueCard("Vigor", "vigor", state.characterDraft.vigor)}
        ${renderVirtueCard("Clarity", "clarity", state.characterDraft.clarity)}
        ${renderVirtueCard("Spirit", "spirit", state.characterDraft.spirit)}
        <article class="derived-card">
          <header>
            <strong>Creation Pool</strong>
            <span>${derived.pointsLeft}</span>
          </header>
          <p class="muted">This uses the same virtue, flaw, core ability, deed, skill, and inventory point logic as the Unity creation rules.</p>
        </article>
      </div>

      <div class="sheet-checks">
        <label>
          <span>Flaw 1</span>
          <input id="flaw-one" type="checkbox" ${state.characterDraft.flawCount > 0 ? "checked" : ""} />
        </label>
        <label>
          <span>Flaw 2</span>
          <input id="flaw-two" type="checkbox" ${state.characterDraft.flawCount > 1 ? "checked" : ""} />
        </label>
        <label>
          <span>Core Ability</span>
          <input id="core-ability" type="checkbox" ${state.characterDraft.hasCoreAbility ? "checked" : ""} />
        </label>
        <div class="counter-line">
          <span>Deeds</span>
          <div class="counter-controls">
            <button type="button" class="secondary" data-deed="-1">-</button>
            <strong>${state.characterDraft.deedCount || 0}</strong>
            <button type="button" class="secondary" data-deed="1">+</button>
          </div>
        </div>
      </div>

      <div class="derived-grid">
        <article class="derived-card"><header><strong>Damage Bonus</strong><span>${derived.damageBonus}</span></header></article>
        <article class="derived-card"><header><strong>Guard Bonus</strong><span>${derived.guardBonus}</span></header></article>
        <article class="derived-card"><header><strong>Clarity Save Mod</strong><span>${derived.claritySaveModifier}</span></header></article>
        <article class="derived-card"><header><strong>Armor</strong><span>${derived.armorTotal}</span></header></article>
      </div>

      <div class="sheet-row">
        <strong>Skills</strong>
        <div class="skills-list">
          ${state.characterDraft.skills.map((skill, index) => `
            <div class="skill-row" data-skill-index="${index}">
              <input data-skill-name="${index}" value="${escapeAttribute(skill.name)}" />
              <button type="button" class="secondary" data-skill-delta="${index}:-1">-</button>
              <strong>${skill.value}</strong>
              <button type="button" class="secondary" data-skill-delta="${index}:1">+</button>
              <button type="button" class="secondary" data-skill-remove="${index}">Remove</button>
            </div>
          `).join("") || "<p class='muted'>No skills yet.</p>"}
        </div>
        <div class="skill-create">
          <input id="new-skill-name" placeholder="New skill name" />
          <button id="add-skill-button" type="button" class="secondary">Add Skill</button>
        </div>
      </div>

      <div class="sheet-row">
        <strong>Inventory Grid</strong>
        <div class="inventory-grid">
          ${inventory.map(slot => `
            <div class="inventory-slot">
              <div>
                <strong>${escapeHtml(slot.equipment?.name || "Empty")}</strong>
                <div class="muted">Slot ${slot.slotIndex + 1}</div>
              </div>
            </div>
          `).join("")}
        </div>
      </div>

      <div class="sheet-row">
        <strong>Chunk Counts</strong>
        <div class="chunk-grid">
          ${chunkEntries.map(([name, count]) => `<div class="chunk-chip">${escapeHtml(name)}: ${count}</div>`).join("")}
        </div>
      </div>

      <div class="sheet-actions">
        <button id="save-character-button" type="button">Save Character</button>
      </div>
    </div>
  `;

  document.getElementById("character-name").addEventListener("input", event => {
    state.characterDraft.name = event.target.value;
    state.characterDirty = true;
    renderCharactersList();
  });

  document.querySelectorAll("[data-virtue]").forEach(button => {
    button.addEventListener("click", () => {
      changeVirtue(button.dataset.virtue, Number(button.dataset.delta));
    });
  });

  document.getElementById("flaw-one").addEventListener("change", onFlawChange);
  document.getElementById("flaw-two").addEventListener("change", onFlawChange);
  document.getElementById("core-ability").addEventListener("change", event => {
    state.characterDraft.hasCoreAbility = event.target.checked;
    state.characterDirty = true;
    renderCharacterSheet();
  });

  document.querySelectorAll("[data-deed]").forEach(button => {
    button.addEventListener("click", () => {
      state.characterDraft.deedCount = Math.max(0, (state.characterDraft.deedCount || 0) + Number(button.dataset.deed));
      state.characterDirty = true;
      renderCharacterSheet();
    });
  });

  document.querySelectorAll("[data-skill-name]").forEach(input => {
    input.addEventListener("input", event => {
      const index = Number(event.target.dataset.skillName);
      state.characterDraft.skills[index].name = event.target.value;
      state.characterDirty = true;
      renderCharactersList();
    });
  });

  document.querySelectorAll("[data-skill-delta]").forEach(button => {
    button.addEventListener("click", () => {
      const [indexText, deltaText] = button.dataset.skillDelta.split(":");
      const index = Number(indexText);
      const delta = Number(deltaText);
      const creationConfig = getSelectedCharacterResponse()?.creation || defaultCreationConfig();
      const next = (state.characterDraft.skills[index].value || creationConfig.newSkillCost) + delta;
      state.characterDraft.skills[index].value = Math.max(creationConfig.newSkillCost, Math.min(creationConfig.virtueMax, next));
      state.characterDirty = true;
      renderCharacterSheet();
    });
  });

  document.querySelectorAll("[data-skill-remove]").forEach(button => {
    button.addEventListener("click", () => {
      const index = Number(button.dataset.skillRemove);
      state.characterDraft.skills.splice(index, 1);
      state.characterDirty = true;
      renderCharacterSheet();
    });
  });

  document.getElementById("add-skill-button").addEventListener("click", () => {
    const creationConfig = getSelectedCharacterResponse()?.creation || defaultCreationConfig();
    const input = document.getElementById("new-skill-name");
    const skillName = input.value.trim();
    if (!skillName) {
      setStatus("Skill name is required.");
      return;
    }

    if ((state.characterDraft.skills || []).length >= creationConfig.maxSkills) {
      setStatus("This sheet is already at the maximum number of skills.");
      return;
    }

    state.characterDraft.skills.push({ name: skillName, value: creationConfig.newSkillCost });
    input.value = "";
    state.characterDirty = true;
    setStatus("");
    renderCharacterSheet();
  });

  document.getElementById("save-character-button").addEventListener("click", persistCharacter);
}

function renderVirtueCard(label, key, value) {
  return `
    <article class="virtue-card">
      <header>
        <strong>${label}</strong>
        <span>${value}</span>
      </header>
      <div class="virtue-controls">
        <button type="button" class="secondary" data-virtue="${key}" data-delta="-1">-</button>
        <button type="button" class="secondary" data-virtue="${key}" data-delta="1">+</button>
      </div>
    </article>
  `;
}

function changeVirtue(key, delta) {
  const creation = getSelectedCharacterResponse()?.creation || defaultCreationConfig();
  const current = state.characterDraft[key] || creation.baseVirtueStart;
  const next = current + delta;
  if (next < creation.baseVirtueStart || next > creation.virtueMax) {
    return;
  }

  state.characterDraft[key] = next;
  state.characterDirty = true;
  renderCharacterSheet();
}

function onFlawChange() {
  const first = document.getElementById("flaw-one").checked;
  const second = document.getElementById("flaw-two").checked;
  state.characterDraft.flawCount = (first ? 1 : 0) + (second ? 1 : 0);
  state.characterDirty = true;
  renderCharacterSheet();
}

async function persistCharacter() {
  const payload = {
    character: normalizeCharacterForSave(state.characterDraft)
  };

  let result;
  if (state.selectedCharacterId) {
    result = await apiPut(`/campaigns/${state.activeCampaignId}/characters/${state.selectedCharacterId}`, payload);
  } else {
    result = await apiPost(`/campaigns/${state.activeCampaignId}/characters`, payload);
  }

  if (!result) {
    return;
  }

  state.characterDirty = false;
  state.selectedCharacterId = result.character.id;
  await refreshAll();
  selectCharacter(result.character.id, false);
  renderCharactersList();
  renderCharacterSheet();
  setStatus("Character saved.");
}

function onNewCharacter() {
  state.selectedCharacterId = null;
  state.characterDraft = createBlankCharacter();
  state.characterDirty = true;
  renderCharactersList();
  renderCharacterSheet();
}

function selectCharacter(characterId, resetMoveMode) {
  const entry = state.characters.find(character => character.character.id === characterId);
  if (!entry) {
    return;
  }

  state.selectedCharacterId = characterId;
  state.characterDraft = deepClone(entry.character);
  state.characterDirty = false;
  if (resetMoveMode) {
    state.moveMode = false;
  }
  renderCharactersList();
  renderCharacterSheet();
}

async function moveParty(targetCoordinate) {
  if (!state.activePartyId) {
    return;
  }

  const response = await apiPost(`/campaigns/${state.activeCampaignId}/parties/${state.activePartyId}/move`, {
    targetQ: targetCoordinate.q,
    targetR: targetCoordinate.r
  });

  if (!response) {
    return;
  }

  state.moveMode = false;
  await refreshAll();
}

async function onCreateNote(event) {
  event.preventDefault();
  if (!state.selectedHexId || !ui.noteBody.value.trim()) {
    return;
  }

  const note = await apiPost(`/campaigns/${state.activeCampaignId}/hexes/${state.selectedHexId}/notes`, {
    body: ui.noteBody.value
  });

  if (!note) {
    return;
  }

  ui.noteBody.value = "";
  await refreshAll();
}

async function apiGet(path) {
  const response = await fetch(path, {
    headers: { Authorization: `Bearer ${state.token}` }
  });
  const data = await readJsonSafely(response);
  if (!response.ok) {
    setStatus(data?.error || "Request failed.");
    throw new Error(data?.error || "Request failed.");
  }

  return data;
}

async function apiPost(path, payload) {
  const response = await fetch(path, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${state.token}`
    },
    body: JSON.stringify(payload)
  });

  const data = await readJsonSafely(response);
  if (!response.ok) {
    setStatus(data?.error || "Request failed.");
    return null;
  }

  setStatus("");
  return data;
}

async function apiPut(path, payload) {
  const response = await fetch(path, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${state.token}`
    },
    body: JSON.stringify(payload)
  });

  const data = await readJsonSafely(response);
  if (!response.ok) {
    setStatus(data?.error || "Request failed.");
    return null;
  }

  setStatus("");
  return data;
}

async function connectRealtime() {
  if (!window.signalR || !state.token || !state.activeCampaignId) {
    return;
  }

  if (state.connection) {
    await state.connection.stop();
  }

  state.connection = new signalR.HubConnectionBuilder()
    .withUrl(`/hubs/campaign?access_token=${state.token}`)
    .withAutomaticReconnect()
    .build();

  state.connection.on("PartyMoved", refreshAll);
  state.connection.on("HexDiscovered", refreshAll);
  state.connection.on("NoteCreated", refreshAll);

  await state.connection.start();
  await state.connection.invoke("JoinCampaignGroup", state.activeCampaignId);
}

function createPartyMarker(x, y, isActive) {
  const group = createSvg("g");

  const ring = createSvg("circle");
  ring.setAttribute("cx", x);
  ring.setAttribute("cy", y - 10);
  ring.setAttribute("r", isActive ? 18 : 14);
  ring.setAttribute("class", "party-marker-core");
  group.appendChild(ring);

  const banner = createSvg("rect");
  banner.setAttribute("x", x - 24);
  banner.setAttribute("y", y + 10);
  banner.setAttribute("width", 48);
  banner.setAttribute("height", 18);
  banner.setAttribute("rx", 9);
  banner.setAttribute("class", "party-marker-banner");
  if (isActive) {
    banner.setAttribute("fill", "#235854");
  }
  group.appendChild(banner);

  const text = createSvg("text");
  text.setAttribute("x", x);
  text.setAttribute("y", y + 23);
  text.setAttribute("class", "party-marker-text");
  text.textContent = "PARTY";
  group.appendChild(text);

  return group;
}

function createHexPoints(x, y, size) {
  const points = [];
  for (let index = 0; index < 6; index += 1) {
    const angle = (60 * index - 30) * Math.PI / 180;
    const px = x + size * Math.cos(angle);
    const py = y + size * Math.sin(angle);
    points.push(`${px},${py}`);
  }
  return points.join(" ");
}

function axialToPoint(coordinate, size) {
  return {
    x: size * Math.sqrt(3) * (coordinate.q + coordinate.r / 2),
    y: size * 1.5 * coordinate.r
  };
}

function getActiveParty() {
  return state.map?.parties.find(party => party.id === state.activePartyId) || state.map?.parties[0] || null;
}

function getSelectedTile() {
  return state.map?.tiles.find(tile => tile.id === state.selectedHexId) || state.map?.tiles[0] || null;
}

function getSelectedCharacterResponse() {
  return state.selectedCharacterId
    ? state.characters.find(character => character.character.id === state.selectedCharacterId) || null
    : null;
}

function calculateCharacterDerived(character, creation) {
  const normalized = normalizeCharacterForSave(character);
  const virtuesSpent =
    clampVirtueSpend(normalized.vigor, creation) +
    clampVirtueSpend(normalized.clarity, creation) +
    clampVirtueSpend(normalized.spirit, creation);

  const skillsSpent = (normalized.skills || []).reduce((total, skill) => total + (skill.value < creation.newSkillCost ? creation.newSkillCost : skill.value), 0);
  const inventorySpent = (normalized.inventory || []).reduce((total, slot) => {
    if (slot.equipment && slot.equipment.costsCreationPoints) {
      return total + (slot.equipment.pointCost || 0);
    }
    return total;
  }, 0);

  const pointsLeft =
    creation.startingPoints +
    ((normalized.flawCount || 0) * creation.flawGrant) +
    ((normalized.deedCount || 0) * creation.deedPointsPerDeed) -
    virtuesSpent -
    skillsSpent -
    inventorySpent -
    (normalized.hasCoreAbility ? creation.coreAbilityCost : 0);

  return {
    pointsLeft,
    damageBonus: Math.floor(normalized.vigor / 6),
    guardBonus: Math.floor(normalized.spirit / 3),
    claritySaveModifier: Math.floor(normalized.clarity / 8),
    armorTotal: calculateArmorTotal(normalized.inventory || []),
    chunkCounts: countChunks(normalized.inventory || [])
  };
}

function countChunks(inventory) {
  const counts = { Red: 0, Green: 0, Blue: 0, Rainbow: 0 };
  inventory.forEach((slot, index) => {
    const equipment = slot?.equipment;
    if (!equipment) {
      return;
    }

    if (equipment.centerChunk && equipment.centerChunk !== "None") {
      counts[equipment.centerChunk] = (counts[equipment.centerChunk] || 0) + 1;
    }

    const row = Math.floor(index / 3);
    const column = index % 3;

    if (equipment.rightHalf && equipment.rightHalf !== "None" && column < 2) {
      const rightNeighbor = inventory[index + 1]?.equipment;
      if (rightNeighbor && rightNeighbor.leftHalf === equipment.rightHalf) {
        counts[equipment.rightHalf] = (counts[equipment.rightHalf] || 0) + 1;
      }
    }

    if (equipment.bottomHalf && equipment.bottomHalf !== "None" && row < 2) {
      const bottomNeighbor = inventory[index + 3]?.equipment;
      if (bottomNeighbor && bottomNeighbor.topHalf === equipment.bottomHalf) {
        counts[equipment.bottomHalf] = (counts[equipment.bottomHalf] || 0) + 1;
      }
    }
  });

  return counts;
}

function calculateArmorTotal(inventory) {
  const bestByLocation = {};
  inventory.forEach(slot => {
    const equipment = slot?.equipment;
    if (!equipment || !equipment.isArmor) {
      return;
    }

    const currentBest = bestByLocation[equipment.armorLocation] || 0;
    if ((equipment.armorValue || 0) > currentBest) {
      bestByLocation[equipment.armorLocation] = equipment.armorValue || 0;
    }
  });

  return Object.values(bestByLocation).reduce((total, value) => total + value, 0);
}

function normalizeCharacterForSave(character) {
  const base = defaultCreationConfig();
  const draft = deepClone(character || createBlankCharacter());
  const skills = Array.isArray(draft.skills) ? draft.skills : [];
  const inventory = Array.isArray(draft.inventory) ? draft.inventory : [];

  return {
    id: draft.id || "00000000-0000-0000-0000-000000000000",
    name: (draft.name || "New Character").trim() || "New Character",
    vigor: Math.min(base.virtueMax, Math.max(base.baseVirtueStart, Number(draft.vigor || base.baseVirtueStart))),
    clarity: Math.min(base.virtueMax, Math.max(base.baseVirtueStart, Number(draft.clarity || base.baseVirtueStart))),
    spirit: Math.min(base.virtueMax, Math.max(base.baseVirtueStart, Number(draft.spirit || base.baseVirtueStart))),
    flawCount: Math.min(2, Math.max(0, Number(draft.flawCount || 0))),
    hasCoreAbility: Boolean(draft.hasCoreAbility),
    deedCount: Math.max(0, Number(draft.deedCount || 0)),
    cachedPointsLeft: 0,
    skills: skills
      .filter(skill => (skill.name || "").trim())
      .slice(0, base.maxSkills)
      .map(skill => ({
        name: skill.name.trim(),
        value: Math.min(base.virtueMax, Math.max(base.newSkillCost, Number(skill.value || base.newSkillCost)))
      })),
    inventory: Array.from({ length: 9 }, (_, index) => {
      const source = inventory.find(slot => slot.slotIndex === index) || inventory[index] || null;
      return {
        slotIndex: index,
        equipment: source?.equipment || null
      };
    })
  };
}

function createBlankCharacter() {
  return {
    id: null,
    name: "New Character",
    vigor: 6,
    clarity: 6,
    spirit: 6,
    flawCount: 0,
    hasCoreAbility: false,
    deedCount: 0,
    cachedPointsLeft: 0,
    skills: [],
    inventory: Array.from({ length: 9 }, (_, index) => ({ slotIndex: index, equipment: null }))
  };
}

function defaultCreationConfig() {
  return {
    baseVirtueStart: 6,
    virtueMax: 18,
    startingPoints: 50,
    deedPointsPerDeed: 3,
    flawGrant: 5,
    coreAbilityCost: 15,
    newSkillCost: 3,
    maxSkills: 10
  };
}

function clampVirtueSpend(value, creation) {
  return value > creation.baseVirtueStart ? value - creation.baseVirtueStart : 0;
}

function createSvg(tag) {
  return document.createElementNS(svgNs, tag);
}

function deepClone(value) {
  return JSON.parse(JSON.stringify(value));
}

function hexDistance(a, b) {
  const aS = -a.q - a.r;
  const bS = -b.q - b.r;
  return (Math.abs(a.q - b.q) + Math.abs(a.r - b.r) + Math.abs(aS - bS)) / 2;
}

function sameCoordinate(a, b) {
  return a && b && a.q === b.q && a.r === b.r;
}

function shorten(text, maxLength) {
  return text.length <= maxLength ? text : `${text.slice(0, maxLength - 1)}…`;
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#39;");
}

function escapeAttribute(value) {
  return escapeHtml(value);
}

async function readJsonSafely(response) {
  const text = await response.text();
  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return { error: text };
  }
}

function setStatus(message) {
  ui.statusText.textContent = message;
}

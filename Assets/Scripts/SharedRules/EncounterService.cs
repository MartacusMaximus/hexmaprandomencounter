using System;
using System.Collections.Generic;

namespace KnightsAndGM.Shared
{
    public sealed class EncounterPrompt
    {
        public string PromptId { get; set; }
        public TerrainType Terrain { get; set; }
        public HexCoordinate Coordinate { get; set; }
        public string Summary { get; set; }
        public string SecretDetails { get; set; }
    }

    public sealed class EncounterTemplate
    {
        public EncounterTemplate(string summary, string secretDetails)
        {
            Summary = summary;
            SecretDetails = secretDetails;
        }

        public string Summary { get; }
        public string SecretDetails { get; }
    }

    public sealed class EncounterService
    {
        private readonly Dictionary<TerrainType, List<EncounterTemplate>> tables;

        public EncounterService()
        {
            tables = new Dictionary<TerrainType, List<EncounterTemplate>>
            {
                [TerrainType.City] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("A faction runner arrives with urgent public news.", "Tie the news to one unresolved city intrigue or a returning expedition."),
                    new EncounterTemplate("A guild representative requests a logistical favor.", "Offer a choice between political leverage and expedition supplies.")
                },
                [TerrainType.Forest] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("Tracks and broken branches suggest something large crossed recently.", "Add a territorial beast or evidence of another party moving through the same hex."),
                    new EncounterTemplate("A quiet shrine or old marker hints at forgotten travelers.", "Reward careful investigation with lore, danger, or a future shortcut.")
                },
                [TerrainType.Plains] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("The open ground gives long sightlines and nowhere to hide.", "Use weather, scouts, or exposed movement to pressure the party."),
                    new EncounterTemplate("A salvageable campsite or cart remains in the grasslands.", "Place a useful clue beside a threat that arrives if the party lingers.")
                },
                [TerrainType.Water] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("The route forces a choice between slow caution and risky passage.", "Make the consequence about supplies, delay, or ambush positioning."),
                    new EncounterTemplate("The water itself shows an omen of something ahead.", "Foreshadow the next dangerous hex or dungeon entrance.")
                },
                [TerrainType.Mountain] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("Steep terrain exposes the party to falling stone and distant eyes.", "Add elevation, attrition, or a chokepoint encounter."),
                    new EncounterTemplate("A defensible ledge offers a chance to regroup.", "Attach a reward only if the party commits time or consumes supplies.")
                },
                [TerrainType.Desert] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("The landscape hides both tracks and water.", "Tie the scene to endurance, navigation, or a misleading opportunity."),
                    new EncounterTemplate("Heat and glare reveal something only at the edge of vision.", "Turn the mirage into a clue, monster, or false approach route.")
                },
                [TerrainType.Swamp] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("Travel slows through fetid ground and uncertain footing.", "Force the group to choose between noise, speed, and exhaustion."),
                    new EncounterTemplate("The swamp holds signs of something patient and territorial.", "Escalate only if players disturb the ground or pursue treasure.")
                },
                [TerrainType.Tundra] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("Cold exposure becomes the immediate enemy.", "Let the weather reveal both danger and a hidden route."),
                    new EncounterTemplate("Sparse cover makes the party visible from far away.", "Use a stalking enemy or signal fire to create pressure.")
                },
                [TerrainType.Badlands] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("Broken ground makes the approach difficult to read.", "Present multiple paths with different risks and incomplete information."),
                    new EncounterTemplate("Ruined remnants suggest something here survived disaster.", "Pair salvage with an awakened threat or cursed consequence.")
                },
                [TerrainType.Deadwood] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("The dead trees creak like a warning in the wind.", "Use sound and visibility to build a slow predator or cursed patrol."),
                    new EncounterTemplate("Old fire scars still shape the route forward.", "Turn the burned terrain into tactical cover and danger.")
                },
                [TerrainType.Tar] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("The ground itself threatens to trap the unwary.", "Pressure movement order, rescue choices, and pursuit."),
                    new EncounterTemplate("Black residue marks an earlier loss.", "Let the remains reveal what not to do and what might still be recovered.")
                },
                [TerrainType.Ice] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("The surface cracks under bad choices.", "Tie risk to formation, weight, or combat positioning."),
                    new EncounterTemplate("Reflections under the ice hint at danger below.", "Foreshadow a creature, relic, or buried route.")
                },
                [TerrainType.Unknown] = new List<EncounterTemplate>
                {
                    new EncounterTemplate("Something about the hex resists easy interpretation.", "Use it to reveal the tone of a new frontier instead of a full fight.")
                }
            };
        }

        public EncounterPrompt CreatePrompt(string campaignSeed, HexCoordinate coordinate, TerrainType terrain, int moveIndex)
        {
            var table = tables.TryGetValue(terrain, out var terrainTable) ? terrainTable : tables[TerrainType.Unknown];
            var hash = StableHash(campaignSeed, coordinate, moveIndex, terrain);
            var selected = table[Math.Abs(hash) % table.Count];

            return new EncounterPrompt
            {
                PromptId = $"{coordinate}-{moveIndex}",
                Terrain = terrain,
                Coordinate = coordinate,
                Summary = selected.Summary,
                SecretDetails = selected.SecretDetails
            };
        }

        private static int StableHash(string campaignSeed, HexCoordinate coordinate, int moveIndex, TerrainType terrain)
        {
            unchecked
            {
                var hash = 17;
                var seed = campaignSeed ?? string.Empty;
                for (var i = 0; i < seed.Length; i++)
                {
                    hash = (hash * 31) + seed[i];
                }
                hash = (hash * 31) + coordinate.GetHashCode();
                hash = (hash * 31) + moveIndex;
                hash = (hash * 31) + (int)terrain;
                return hash;
            }
        }
    }
}

using Lumina.Excel.Sheets;

namespace ChatAlerts;

public enum ZoneType : byte
{
    All            = 0,
    Overworld      = 1,
    Dungeon        = 2,
    Trial          = 3,
    Raid           = 4,
    AllianceRaid   = 5,
    DeepDungeon    = 6,
    PvP            = 7,
    Housing        = 8,
    FieldOperation = 9,
}

public static class ZoneTypeExtensions
{
    public static string ToDisplayName(this ZoneType type) => type switch
    {
        ZoneType.Overworld    => "Overworld",
        ZoneType.Dungeon      => "Dungeon",
        ZoneType.Trial        => "Trial",
        ZoneType.Raid         => "Raid",
        ZoneType.AllianceRaid => "Alliance Raid",
        ZoneType.DeepDungeon  => "Deep Dungeon",
        ZoneType.PvP            => "PvP",
        ZoneType.Housing        => "Housing",
        ZoneType.FieldOperation => "Field Operation",
        _                       => "All",
    };

    public static ZoneType FromTerritoryIntendedUse(uint useId) => useId switch
    {
        // Overworld
        0  => ZoneType.Overworld, // Town
        1  => ZoneType.Overworld, // OpenWorld
        2  => ZoneType.Overworld, // Inn
        5  => ZoneType.Overworld, // Jail
        6  => ZoneType.Overworld, // OpeningArea
        7  => ZoneType.Overworld, // LobbyArea
        9  => ZoneType.Overworld, // OpenWorldInstanceBattle
        15 => ZoneType.Overworld, // SoloOverworldInstance
        20 => ZoneType.Overworld, // ChocoboRacing
        21 => ZoneType.Overworld, // IshgardRestoration
        22 => ZoneType.Overworld, // Wedding
        23 => ZoneType.Overworld, // GoldSaucer
        26 => ZoneType.Overworld, // ExploratoryMissions
        27 => ZoneType.Overworld, // HallOfTheNovice
        29 => ZoneType.Overworld, // SoloDuty
        30 => ZoneType.Overworld, // FreeCompanyGarrison
        32 => ZoneType.Overworld, // Seasonal
        34 => ZoneType.Overworld, // SeasonalInstancedArea
        35 => ZoneType.Overworld, // TripleTriadBattleHall
        40 => ZoneType.Overworld, // PrivateEventArea
        44 => ZoneType.Overworld, // LeapOfFaith
        45 => ZoneType.Overworld, // MaskedCarnival
        46 => ZoneType.Overworld, // OceanFishing
        49 => ZoneType.Overworld, // IslandSanctuary
        51 => ZoneType.Overworld, // TripleTriadInvitationalParlor
        56 => ZoneType.Overworld, // Elysion
        59 => ZoneType.Overworld, // Blunderville
        60 => ZoneType.Overworld, // CosmicExploration
        63 => ZoneType.Overworld, // SprigCleaning / Lilyswim

        // Dungeon
        3  => ZoneType.Dungeon, // Dungeon
        4  => ZoneType.Dungeon, // VariantDungeon
        33 => ZoneType.Dungeon, // TreasureDungeon
        57 => ZoneType.Dungeon, // CriterionDungeon
        58 => ZoneType.Dungeon, // SavageCriterionDungeon

        // Trial
        10 => ZoneType.Trial, // Trial (all difficulties)

        // Raid
        16 => ZoneType.Raid, // Raid (normal)
        17 => ZoneType.Raid, // Raid (savage)

        // Alliance Raid
        8  => ZoneType.AllianceRaid, // AllianceRaid
        36 => ZoneType.AllianceRaid, // ChaoticRaid

        // Deep Dungeon
        31 => ZoneType.DeepDungeon, // DeepDungeon

        // Housing
        13 => ZoneType.Housing, // HousingOutdoor
        14 => ZoneType.Housing, // HousingIndoor

        // PvP
        18 => ZoneType.PvP, // Frontline
        28 => ZoneType.PvP, // CrystallineConflict
        37 => ZoneType.PvP, // CrystallineConflictCustomMatch
        39 => ZoneType.PvP, // RivalWings

        // Field Operation
        41 => ZoneType.FieldOperation, // Eureka
        48 => ZoneType.FieldOperation, // Bozja / Save the Queen
        61 => ZoneType.FieldOperation, // OccultCrescent

        _ => ZoneType.Overworld,
    };

    public static ZoneType GetCurrentZoneType(uint? territoryId = null)
    {
        var id = territoryId ?? Dalamud.ClientState.TerritoryType;
        if (id == 0)
            return ZoneType.Overworld;

        if (Dalamud.ClientState.IsPvP)
            return ZoneType.PvP;

        var sheet = Dalamud.GameData.Excel.GetSheet<TerritoryType>();
        if (sheet.TryGetRow(id, out var territory))
            return FromTerritoryIntendedUse(territory.TerritoryIntendedUse.RowId);

        return ZoneType.Overworld;
    }
}

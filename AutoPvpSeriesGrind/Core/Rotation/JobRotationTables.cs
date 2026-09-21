using static AutoPvpSeriesGrind.Core.Rotation.Rule;
using static AutoPvpSeriesGrind.Core.Rotation.When;

namespace AutoPvpSeriesGrind.Core.Rotation;

internal static class JobRotationTables
{
    private const float EndingSoonSec = 2.5f;
    private const float MeleeYalms = 5f;
    private const float MaxRangeYalms = 25f;

    private static readonly Dictionary<uint, RotationTable> Tables = new()
    {
        [19] = Paladin.Table,
        [21] = Warrior.Table,
        [32] = DarkKnight.Table,
        [37] = Gunbreaker.Table,
        [20] = Monk.Table,
        [22] = Dragoon.Table,
        [30] = Ninja.Table,
        [34] = Samurai.Table,
        [39] = Reaper.Table,
        [41] = Viper.Table,
        [23] = Bard.Table,
        [31] = Machinist.Table,
        [38] = Dancer.Table,
        [25] = BlackMage.Table,
        [27] = Summoner.Table,
        [35] = RedMage.Table,
        [42] = Pictomancer.Table,
        [24] = WhiteMage.Table,
        [28] = Scholar.Table,
        [33] = Astrologian.Table,
        [40] = Sage.Table,
    };

    public static bool TryGet(uint jobId, out RotationTable table)
    {
        if (Tables.TryGetValue(jobId, out var found))
        {
            table = found;
            return true;
        }
        table = null!;
        return false;
    }

    private static class Paladin
    {
        private const uint Combo = 29058;
        private const uint HolySpirit = 29062;
        private const uint Intervene = 29065;
        private const uint Guardian = 29066;
        private const uint HolySheltron = 29067;
        private const uint ShieldSmite = 41430;
        private const uint Imperator = 41431;
        private const uint HallowedGround = 1302;

        public static readonly RotationTable Table = new(0,
        [
            Use(Imperator),
            Use(ShieldSmite),
            Use(HolySpirit, ChargesAtLeast(2)),
            Use(Combo),
            Use(HolySpirit),
            Use(Guardian, SelfHas(HallowedGround)),
            UseOnSelf(HolySheltron, SelfHpBelow(0.5f)),
            Use(Intervene, TargetBeyond(6f)),
        ]);
    }

    private static class Warrior
    {
        private const uint Combo = 29074;
        private const uint Onslaught = 29079;
        private const uint Orogeny = 29080;
        private const uint Blota = 29081;
        private const uint Bloodwhetting = 29082;
        private const uint PrimalRend = 29084;
        private const uint PrimalWrath = 41433;

        public static readonly RotationTable Table = new(0,
        [
            Use(PrimalRend),
            Use(Combo),
            UseAs(Orogeny, PrimalWrath),
            UseAs(Onslaught, PrimalWrath),
            UseAs(Bloodwhetting, PrimalWrath),
            Use(Blota),
            Use(Onslaught),
            Use(Orogeny, EnemiesWithin(6f)),
            UseOnSelf(Bloodwhetting, SelfHpBelow(0.6f)),
        ]);
    }

    private static class DarkKnight
    {
        private const uint Combo = 29085;
        private const uint Shadowbringer = 29091;
        private const uint Plunge = 29092;
        private const uint TheBlackestNight = 29093;
        private const uint SaltedEarth = 29094;
        private const uint SaltAndDarkness = 29095;
        private const uint SaltAndDarknessDetonate = 29096;
        private const uint Disesteem = 41437;
        private const uint Impalement = 41438;
        private const uint Blackblood = 3033;
        private const uint DarkArts = 3034;

        public static readonly RotationTable Table = new(0,
        [
            UseAs(Combo, Disesteem),
            UseOnSelf(Impalement, SelfHpBelow(0.6f), EnemiesWithin(6f)),
            Use(Combo),
            Use(TheBlackestNight, ChargesAtLeast(2), InCombat),
            UseAs(SaltedEarth, SaltAndDarkness),
            UseAs(SaltedEarth, SaltAndDarknessDetonate),
            UseOnSelf(SaltedEarth, EnemiesWithin(MeleeYalms)),
            Use(Plunge),
            Use(Shadowbringer, SelfLacks(Blackblood), SelfHpAbove(0.5f)),
            Use(Shadowbringer, SelfLacks(Blackblood), SelfHas(DarkArts)),
            UseOnSelf(TheBlackestNight, SelfHpBelow(0.5f)),
        ]);
    }

    private static class Gunbreaker
    {
        private const uint Combo = 29098;
        private const uint GnashingFang = 29102;
        private const uint Continuation = 29106;
        private const uint RoughDivide = 29123;
        private const uint BlastingZone = 29128;
        private const uint HeartOfCorundum = 41443;
        private const uint FatedCircle = 41511;
        private const uint NoMercy = 3042;

        public static readonly RotationTable Table = new(0,
        [
            UseOnSelf(HeartOfCorundum, SelfHpBelow(0.3f)),
            Use(RoughDivide, SelfLacks(NoMercy)),
            Use(BlastingZone, TargetHpBelow(0.5f)),
            Use(Continuation),
            Use(GnashingFang),
            UseOnSelf(FatedCircle, EnemiesWithin(6f)),
            Use(Combo),
            Use(HeartOfCorundum),
        ]);
    }

    private static class Monk
    {
        private const uint Combo = 29475;
        private const uint PhantomRush = 29478;
        private const uint RisingPhoenix = 29481;
        private const uint RiddleOfEarth = 29482;
        private const uint EarthsReply = 29483;
        private const uint FiresReply = 41448;
        private const uint WindsReply = 41509;
        private const uint EarthResonance = 3171;

        public static readonly RotationTable Table = new(0,
        [
            UseAsOnSelf(RiddleOfEarth, EarthsReply, SelfStatusEnding(EarthResonance, EndingSoonSec)),
            UseAsOnSelf(RiddleOfEarth, EarthsReply, SelfHpBelow(0.5f)),
            UseAsOnSelf(RiddleOfEarth, RiddleOfEarth, InCombat, SelfHpBelow(0.8f)),
            UseOnSelf(RisingPhoenix, EnemiesWithin(6f), InCombat),
            UseAsOnSelf(RiddleOfEarth, EarthsReply, EnemiesWithin(MeleeYalms)),
            UseAs(Combo, PhantomRush),
            Use(FiresReply),
            Use(WindsReply),
            Use(Combo),
        ]);
    }

    private static class Dragoon
    {
        private const uint Combo = 29486;
        private const uint HeavensThrust = 29489;
        private const uint ChaoticSpring = 29490;
        private const uint Geirskogul = 29491;
        private const uint HighJump = 29493;
        private const uint ElusiveJump = 29494;
        private const uint WyrmwindThrust = 29495;
        private const uint HorridRoar = 29496;
        private const uint Starcross = 41450;

        public static readonly RotationTable Table = new(0,
        [
            UseAs(ElusiveJump, WyrmwindThrust),
            UseAs(Combo, HeavensThrust),
            UseAs(Combo, Starcross),
            Use(ChaoticSpring),
            Use(Combo),
            UseOnSelf(HorridRoar, EnemiesWithin(10f)),
            Use(Geirskogul),
            Use(HighJump, EnemiesWithin(MeleeYalms)),
        ]);
    }

    private static class Ninja
    {
        private const uint Combo = 29500;
        private const uint Assassinate = 29503;
        private const uint GokaMekkyaku = 29504;
        private const uint FumaShuriken = 29505;
        private const uint HyoshoRanryu = 29506;
        private const uint ThreeMudra = 29507;
        private const uint Meisui = 29508;
        private const uint ForkedRaiju = 29510;
        private const uint Bunshin = 29511;
        private const uint Huton = 29512;
        private const uint FleetingRaiju = 29707;
        private const uint Dokumori = 41451;
        private const uint ZeshoMeppo = 41452;
        private const uint Hidden = 1316;
        private const uint ThreeMudraStatus = 1317;

        public static readonly RotationTable Table = new(0,
        [
            UseAs(Combo, Assassinate),
            UseAs(Combo, ZeshoMeppo, SelfLacks(Hidden)),
            UseAsOnSelf(ThreeMudra, Meisui, SelfHpBelow(0.5f), SelfLacks(Hidden)),
            UseAs(Combo, ForkedRaiju, SelfLacks(Hidden)),
            UseAs(Combo, FleetingRaiju, SelfLacks(Hidden)),
            UseAs(Dokumori, GokaMekkyaku, SelfLacks(Hidden)),
            UseAs(FumaShuriken, HyoshoRanryu, SelfLacks(Hidden)),
            UseAsOnSelf(Bunshin, Huton, SelfStatusEnding(ThreeMudraStatus, 1f), SelfLacks(Hidden)),
            Use(FumaShuriken, SelfLacks(Hidden)),
            Use(Combo, SelfLacks(Hidden)),
            UseAs(Dokumori, Dokumori, SelfLacks(Hidden)),
            UseAsOnSelf(Bunshin, Bunshin, SelfLacks(ThreeMudraStatus), EnemiesWithin(MaxRangeYalms), SelfLacks(Hidden)),
            UseAsOnSelf(ThreeMudra, ThreeMudra, SelfLacks(ThreeMudraStatus), EnemiesWithin(MaxRangeYalms), SelfLacks(Hidden)),
        ]);
    }

    private static class Samurai
    {
        private const uint Combo = 29523;
        private const uint OgiNamikiri = 29530;
        private const uint KaeshiNamikiri = 29531;
        private const uint Soten = 29532;
        private const uint Chiten = 29533;
        private const uint Mineuchi = 29535;
        private const uint MeikyoShisui = 29536;
        private const uint TendoSetsugekka = 41454;
        private const uint TendoKaeshiSetsugekka = 41455;
        private const uint Zanshin = 41577;
        private const uint Kuzushi = 3202;

        public static readonly RotationTable Table = new(0,
        [
            UseAsOnSelf(MeikyoShisui, MeikyoShisui, SelfHasAny(PvpStatuses.PurifyClears)),
            UseAs(MeikyoShisui, TendoKaeshiSetsugekka),
            UseAs(MeikyoShisui, TendoSetsugekka),
            UseAs(OgiNamikiri, KaeshiNamikiri),
            Use(OgiNamikiri),
            Use(Combo),
            UseAs(Chiten, Zanshin),
            Use(Soten),
            Use(Mineuchi, TargetHas(Kuzushi)),
            UseAsOnSelf(Chiten, Chiten, EnemiesWithin(MeleeYalms)),
            UseAsOnSelf(MeikyoShisui, MeikyoShisui, EnemiesWithin(MeleeYalms)),
        ]);
    }

    private static class Reaper
    {
        private const uint Combo = 29538;
        private const uint VoidReaping = 29543;
        private const uint CrossReaping = 29544;
        private const uint HarvestMoon = 29545;
        private const uint PlentifulHarvest = 29546;
        private const uint GrimSwathe = 29547;
        private const uint LemuresSlice = 29548;
        private const uint DeathWarrant = 29549;
        private const uint ArcaneCrest = 29552;
        private const uint Communio = 29554;
        private const uint ExecutionersGuillotine = 41456;
        private const uint FateSealed = 41457;
        private const uint Perfectio = 41458;
        private const uint Enshrouded = 2863;
        private const uint ImmortalSacrifice = 3204;
        private const uint DeathWarrantStatus = 4308;

        public static readonly RotationTable Table = new(0,
        [
            UseAs(DeathWarrant, FateSealed, SelfStatusEnding(DeathWarrantStatus, EndingSoonSec)),
            UseAs(PlentifulHarvest, Communio, SelfStacksAtMost(Enshrouded, 1)),
            UseAs(PlentifulHarvest, Communio, SelfStatusEnding(Enshrouded, EndingSoonSec)),
            UseAs(PlentifulHarvest, Perfectio, TargetHpBelow(0.25f)),
            UseAs(Combo, CrossReaping),
            UseAs(Combo, VoidReaping),
            UseAs(PlentifulHarvest, PlentifulHarvest, SelfStacksAtLeast(ImmortalSacrifice, 4)),
            Use(HarvestMoon),
            UseAs(Combo, ExecutionersGuillotine),
            Use(Combo),
            UseAs(GrimSwathe, LemuresSlice),
            UseAs(DeathWarrant, DeathWarrant),
            UseAs(GrimSwathe, GrimSwathe),
            UseOnSelf(ArcaneCrest, SelfHpBelow(0.5f)),
        ]);
    }

    private static class Viper
    {
        private const uint Combo = 39157;
        private const uint Bloodcoil = 39166;
        private const uint SanguineFeast = 39167;
        private const uint UncoiledFury = 39168;
        private const uint FirstGeneration = 39169;
        private const uint SecondGeneration = 39170;
        private const uint ThirdGeneration = 39171;
        private const uint FourthGeneration = 39172;
        private const uint Ouroboros = 39173;
        private const uint SerpentsTail = 39183;
        private const uint RattlingCoil = 39189;
        private const uint HardenedScales = 4096;

        public static readonly RotationTable Table = new(HardenedScales,
        [
            Use(SerpentsTail),
            UseOnSelf(RattlingCoil, InCombat),
            UseAs(Combo, FourthGeneration),
            UseAs(Combo, ThirdGeneration),
            UseAs(Combo, SecondGeneration),
            UseAs(Combo, FirstGeneration),
            UseAs(Bloodcoil, Ouroboros),
            UseAs(Bloodcoil, SanguineFeast),
            Use(Bloodcoil),
            Use(UncoiledFury, ChargesAtLeast(2)),
            Use(Combo),
            Use(UncoiledFury),
        ]);
    }

    private static class Bard
    {
        private const uint PowerfulShot = 29391;
        private const uint PitchPerfect = 29392;
        private const uint ApexArrow = 29393;
        private const uint BlastArrow = 29394;
        private const uint SilentNocturne = 29395;
        private const uint WardensPaean = 29400;
        private const uint HarmonicArrow = 41464;
        private const uint Repertoire = 3137;
        private const uint FrontlinersMarch = 3138;

        public static readonly RotationTable Table = new(0,
        [
            UseOnSelf(WardensPaean, SelfHasAny(PvpStatuses.PurifyClears)),
            UseAs(ApexArrow, ApexArrow, SelfLacks(FrontlinersMarch)),
            Use(HarmonicArrow),
            UseAs(PowerfulShot, PitchPerfect),
            UseAs(ApexArrow, BlastArrow),
            Use(ApexArrow),
            Use(PowerfulShot),
            Use(SilentNocturne, SelfLacks(Repertoire)),
        ]);
    }

    private static class Machinist
    {
        private const uint BlastCharge = 29402;
        private const uint Scattergun = 29404;
        private const uint Drill = 29405;
        private const uint Wildfire = 29409;
        private const uint BishopAutoturret = 29412;
        private const uint Analysis = 29414;
        private const uint BlazingShot = 41468;
        private const uint FullMetalField = 41469;
        private const uint Overheated = 3149;
        private const uint AnalysisStatus = 3158;
        private static readonly uint[] PrimedStatuses = [3150, 3151, 3152, 3153];

        public static readonly RotationTable Table = new(0,
        [
            Use(FullMetalField),
            UseAs(BlastCharge, BlazingShot, SelfHas(Overheated), SelfLacks(AnalysisStatus)),
            Use(Drill),
            Use(Scattergun, SelfLacks(Overheated)),
            Use(BlastCharge),
            UseOnSelf(Analysis, SelfLacks(AnalysisStatus), SelfHasAny(PrimedStatuses)),
            Use(Wildfire, SelfHas(Overheated)),
            Use(BishopAutoturret),
        ]);
    }

    private static class Dancer
    {
        private const uint Combo = 29416;
        private const uint StarfallDance = 29421;
        private const uint HoningDance = 29422;
        private const uint FanDance = 29428;
        private const uint CuringWaltz = 29429;
        private const uint ClosedPosition = 29431;
        private const uint DanceOfTheDawn = 41472;
        private const uint ClosedPositionStatus = 2026;
        private const uint EnAvant = 2048;
        private const uint HoningDanceStatus = 3162;

        public static readonly RotationTable Table = new(HoningDanceStatus,
        [
            UseOnAlly(ClosedPosition, SelfLacks(ClosedPositionStatus)),
            UseAs(Combo, DanceOfTheDawn),
            Use(StarfallDance),
            UseAsOnSelf(HoningDance, HoningDance, EnemiesWithin(6f), SelfLacks(EnAvant)),
            Use(Combo),
            Use(FanDance),
            UseOnSelf(CuringWaltz, SelfHpBelow(0.7f)),
            UseOnSelf(CuringWaltz, AllyBelow(0.6f)),
        ]);
    }

    private static class BlackMage
    {
        private const uint Fire = 29649;
        private const uint Flare = 29651;
        private const uint Blizzard = 29653;
        private const uint Freeze = 29655;
        private const uint Burst = 29657;
        private const uint Xenoglossy = 29658;
        private const uint Paradox = 29663;
        private const uint HighFireII = 41473;
        private const uint HighBlizzardII = 41474;
        private const uint ElementalWeave = 41475;
        private const uint WreathOfFire = 41476;
        private const uint WreathOfFireDetonate = 41477;
        private const uint WreathOfIce = 41478;
        private const uint FlareStar = 41480;
        private const uint FrostStar = 41481;
        private const uint Lethargy = 41510;

        public static readonly RotationTable Table = new(0,
        [
            UseAs(Fire, FlareStar),
            UseAs(Blizzard, FrostStar),
            UseAs(Fire, Flare),
            UseAs(Blizzard, Freeze),
            Use(Xenoglossy, SelfHpBelow(0.5f)),
            Use(Xenoglossy, SelfHpAbove(0.8f)),
            Use(Paradox),
            UseOnSelf(Burst, EnemiesWithin(6f)),
            UseAs(Fire, HighFireII),
            UseAs(Blizzard, HighBlizzardII),
            Use(Blizzard),
            Use(Fire),
            Use(Lethargy),
            UseAs(ElementalWeave, WreathOfFireDetonate),
            UseAsOnSelf(ElementalWeave, WreathOfFire, InCombat),
            UseAsOnSelf(ElementalWeave, WreathOfIce, SelfHpBelow(0.5f)),
        ]);
    }

    private static class Summoner
    {
        private const uint RuinIII = 29664;
        private const uint CrimsonCyclone = 29667;
        private const uint CrimsonStrike = 29668;
        private const uint Slipstream = 29669;
        private const uint RadiantAegis = 29670;
        private const uint MountainBuster = 29671;
        private const uint Necrotize = 41483;
        private const uint Deathflare = 41484;
        private const uint BrandOfPurgatory = 41485;
        private const uint DreadwyrmTrance = 3228;
        private const uint FirebirdTrance = 3229;

        public static readonly RotationTable Table = new(0,
        [
            Use(Slipstream),
            Use(MountainBuster),
            UseAs(CrimsonCyclone, CrimsonStrike),
            Use(RuinIII),
            UseAs(Necrotize, Deathflare),
            UseAs(Necrotize, BrandOfPurgatory),
            UseAs(Necrotize, Necrotize, SelfLacks(FirebirdTrance), SelfLacks(DreadwyrmTrance)),
            UseAs(CrimsonCyclone, CrimsonCyclone, TargetWithin(MeleeYalms)),
            UseOnSelf(RadiantAegis, SelfHpBelow(0.6f)),
        ]);
    }

    private static class RedMage
    {
        private const uint JoltIII = 41486;
        private const uint GrandImpact = 41487;
        private const uint EnchantedRiposte = 41488;
        private const uint Resolution = 41492;
        private const uint ViceOfThorns = 41493;
        private const uint Embolden = 41494;
        private const uint Prefulgence = 41495;
        private const uint Forte = 41496;

        public static readonly RotationTable Table = new(0,
        [
            UseAs(Resolution, Prefulgence),
            Use(Resolution),
            Use(EnchantedRiposte),
            UseAs(JoltIII, GrandImpact),
            Use(JoltIII),
            UseAs(Embolden, ViceOfThorns),
            UseAsOnSelf(Embolden, Embolden, InCombat),
            UseOnSelf(Forte, SelfHpBelow(0.5f)),
        ]);
    }

    private static class Pictomancer
    {
        private const uint FireInRed = 39191;
        private const uint HolyInWhite = 39198;
        private const uint CometInBlack = 39199;
        private const uint CreatureMotif = 39204;
        private const uint LivingMuse = 39209;
        private const uint TemperaCoat = 39211;
        private const uint SubtractivePalette = 39213;
        private const uint MogOfTheAges = 39782;

        public static readonly RotationTable Table = new(0,
        [
            Use(MogOfTheAges),
            UseAs(HolyInWhite, CometInBlack),
            UseOnSelf(CreatureMotif),
            Use(FireInRed),
            Use(LivingMuse),
            UseAsOnSelf(SubtractivePalette, SubtractivePalette),
            UseOnSelf(TemperaCoat, SelfHpBelow(0.8f)),
        ]);
    }

    private static class WhiteMage
    {
        private const uint GlareIII = 29223;
        private const uint CureII = 29224;
        private const uint CureIII = 29225;
        private const uint AfflatusMisery = 29226;
        private const uint Aquaveil = 29227;
        private const uint MiracleOfNature = 29228;
        private const uint SeraphStrike = 29229;

        public static readonly RotationTable Table = new(0,
        [
            UseOnSelf(Aquaveil, SelfHasAny(PvpStatuses.PurifyClears)),
            UseAs(CureII, CureIII),
            Use(CureII),
            Use(AfflatusMisery),
            Use(GlareIII),
            Use(SeraphStrike),
            Use(MiracleOfNature),
        ]);
    }

    private static class Scholar
    {
        private const uint BroilIV = 29231;
        private const uint Adloquium = 29232;
        private const uint Biolysis = 29233;
        private const uint DeploymentTactics = 29234;
        private const uint Expedient = 29236;
        private const uint SummonSeraph = 29237;
        private const uint ChainStratagem = 29716;
        private const uint Accession = 41501;
        private const uint BiolysisStatus = 3089;
        private const uint Recitation = 3094;

        public static readonly RotationTable Table = new(0,
        [
            Use(ChainStratagem, TargetHas(PvpStatuses.Guard)),
            Use(Biolysis, SelfHas(Recitation)),
            UseAsOnSelf(Adloquium, Accession, AllyBelow(0.6f)),
            UseAsOnSelf(Expedient, Accession, AllyBelow(0.6f)),
            Use(Adloquium),
            Use(BroilIV),
            Use(DeploymentTactics, TargetHas(BiolysisStatus), NotLastUsed),
            UseOnSelf(Expedient, InCombat),
            UseOnSelf(SummonSeraph, AllyBelow(0.6f)),
        ]);
    }

    private static class Astrologian
    {
        private const uint FallMalefic = 29242;
        private const uint AspectedBenefic = 29243;
        private const uint GravityII = 29244;
        private const uint DoubleCast = 29245;
        private const uint DoubleMalefic = 29246;
        private const uint DoubleBenefic = 29247;
        private const uint DoubleGravity = 29248;
        private const uint Macrocosmos = 29253;
        private const uint Microcosmos = 29254;
        private const uint MinorArcana = 41503;
        private const uint LadyOfCrowns = 41504;
        private const uint LordOfCrowns = 41505;
        private const uint Oracle = 41508;
        private const uint MacrocosmosStatus = 3104;
        private const uint LadyOfCrownsStatus = 4328;

        public static readonly RotationTable Table = new(0,
        [
            UseAsOnSelf(Macrocosmos, Microcosmos, SelfStatusEnding(MacrocosmosStatus, EndingSoonSec)),
            UseAsOnSelf(MinorArcana, LadyOfCrowns, SelfStatusEnding(LadyOfCrownsStatus, EndingSoonSec)),
            UseAs(DoubleCast, DoubleBenefic),
            UseAsOnSelf(MinorArcana, LadyOfCrowns, SelfHpBelow(0.6f)),
            UseAsOnSelf(Macrocosmos, Microcosmos, SelfHpBelow(0.6f)),
            UseAs(MinorArcana, Oracle),
            UseAs(Macrocosmos, Oracle),
            UseAsOnSelf(MinorArcana, MinorArcana),
            UseAsOnSelf(MinorArcana, LordOfCrowns),
            UseAs(DoubleCast, DoubleGravity),
            UseAs(DoubleCast, DoubleMalefic),
            UseAsOnSelf(Macrocosmos, Macrocosmos, EnemiesWithin(20f)),
            Use(GravityII),
            Use(AspectedBenefic),
            Use(FallMalefic),
            UseAsOnSelf(MinorArcana, LadyOfCrowns, AllyBelow(0.6f)),
            UseAsOnSelf(Macrocosmos, Microcosmos, AllyBelow(0.6f)),
        ]);
    }

    private static class Sage
    {
        private const uint DosisIII = 29256;
        private const uint Eukrasia = 29258;
        private const uint PhlegmaIII = 29259;
        private const uint Pneuma = 29260;
        private const uint Toxikon = 29262;
        private const uint Kardia = 29264;
        private const uint KardiaStatus = 2871;
        private const uint EukrasianDosisIII = 3108;

        public static readonly RotationTable Table = new(0,
        [
            UseOnAlly(Kardia, SelfLacks(KardiaStatus)),
            Use(Pneuma),
            Use(PhlegmaIII),
            UseOnSelf(Eukrasia, InCombat, TargetLacks(EukrasianDosisIII)),
            Use(DosisIII),
            Use(Toxikon),
        ]);
    }
}

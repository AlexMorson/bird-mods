using System.Collections.Generic;
using System.Linq;

namespace TasBird
{
    internal static class LevelNames
    {
        public static bool NameExists(string name)
        {
            return FileToNameMap.ContainsValue(name);
        }

        public static string NameToFile(string name)
        {
            return FileToNameMap.FirstOrDefault(x => x.Value == name).Key;
        }

        public static bool FileExists(string file)
        {
            return FileToNameMap.ContainsKey(file);
        }

        public static string FileToName(string file)
        {
            return FileToNameMap[file];
        }

        private static readonly Dictionary<string, string> FileToNameMap = new Dictionary<string, string>
        {
            {
                "Caged_Dian_Dream",
                "Caged Dream"
            },
            {
                "Caged_Dian_Kingdom_6.4",
                "Home"
            },
            {
                "Caged_Kevin_CloakForest_v1.3",
                "The Outer Forest"
            },
            {
                "Forest_Kersti_HUB_v0.2",
                "The Forest Kingdom"
            },
            {
                "Forest_Kersti_SubHUB1Easy",
                "Quiet Valley Woods"
            },
            {
                "Forest_Kersti_RootSlide_0.4",
                "Root Slides"
            },
            {
                "Forest_Kevin_TreeSwoosh_v0.3",
                "Forest Edge"
            },
            {
                "Forest_Andrew_RootHollow_0.2",
                "The Hollow"
            },
            {
                "Forest_Nick_TwinTrees_v0.1",
                "Twin Tree Village"
            },
            {
                "Forest_Kersti_SubHUB2Med",
                "Cliffdrop Woods"
            },
            {
                "Forest_Kevin_HiddenVillage_0.3",
                "Hidden Village"
            },
            {
                "Forest_Kevin_DashJumpIntro_V2",
                "Wild Orchard"
            },
            {
                "Forest_Kevin_AbandonedTreeVillage_v1.3",
                "Abandoned Market"
            },
            {
                "Forest_Kevin_walljumps_v2",
                "Thorny Grove"
            },
            {
                "Forest_Kersti_SubHUB3Hard",
                "Dripping Vine Woods"
            },
            {
                "Forest_Andrew_OvergrownRuins_0.2",
                "Briar"
            },
            {
                "Forest_Brock_FloatingPlatforms_v0.7",
                "The Grotto"
            },
            {
                "Forest_Kevin_HighCanopy_1.0",
                "High Canopy"
            },
            {
                "Forest_Kevin_FoggyForest_0.1",
                "Foggy Forest"
            },
            {
                "Forest_Kersti_SubHUB4Challenge",
                "Branchstep Woods"
            },
            {
                "Forest_Alicia_SwampForest_1.1",
                "Mangrove Village"
            },
            {
                "Forest_Jeremy_Redwoods_v0.4",
                "Redwood Branches"
            },
            {
                "Forest_Kersti_TreeTrunkClimb_v2.14",
                "Ancient Tree Climb"
            },
            {
                "Forest_Samuel_DunkMachine_1.0",
                "Root Caverns"
            },
            {
                "Forest_Kersti_SubHUB5DLC",
                "Carved Earth Woods"
            },
            {
                "Forest_Alicia_Chute_1.0",
                "Swamplands"
            },
            {
                "Forest_Nick_Understory_v0.1",
                "Underbrush"
            },
            {
                "Forest_Samuel_Humus_1.0",
                "Loamy Gardens"
            },
            {
                "Forest_Samuel_RootMire_1.0",
                "Shaded Mire"
            },
            {
                "Forest_Kersti_End_v0.4",
                "Owl's Shrine"
            },
            {
                "Forest_Dian_Dream",
                "Forest Dream"
            },
            {
                "Water_Kersti_Hub_0.4",
                "The Lake Kingdom"
            },
            {
                "Water_Kersti_SubHUB1Easy",
                "Riverways District"
            },
            {
                "Water_Kevin_OvergrownRuinsPT2_0.1",
                "The Old City"
            },
            {
                "Water_Kevin_OvergrownRuinsPT1_0.1",
                "Overgrown Ruins"
            },
            {
                "Water_Alicia_FloodedCave_2.0",
                "Underground Stream"
            },
            {
                "Water_Jeremy_FloodedVillage_v0.1",
                "Flooded Village"
            },
            {
                "Water_Kersti_SubHUB2Med",
                "Scholars' District"
            },
            {
                "Water_Kersti_TheAcademy_1.6",
                "The Academy"
            },
            {
                "Water_Andrew_Rooftopshop_2.2",
                "Rooftops"
            },
            {
                "Water_Z_TotemCaverns_v2.0",
                "Colonnade"
            },
            {
                "Water_Kevin_OpenCave_1.0",
                "Tunnel Labyrinth"
            },
            {
                "Water_Kersti_SubHUB3Hard",
                "Wallside District"
            },
            {
                "Water_Kevin_ForestEdge_2.1",
                "Lakeside Slides"
            },
            {
                "Water_Samuel_Planks_1.1",
                "Plaza Ruins"
            },
            {
                "Water_KevinChris_Mountains_1.4",
                "Waterfall Mountains"
            },
            {
                "Water_Kersti_Vault_v0.8",
                "The Cistern"
            },
            {
                "Water_Kersti_SubHUB4Challenge",
                "Shades District"
            },
            {
                "Water_Samuel_Halfpipes_1.0",
                "The Vault"
            },
            {
                "Water_Samuel_CeilingTiles_v1.1",
                "Cliffside Cavern"
            },
            {
                "Water_Kevin_CrumbledChurch_1.0",
                "Crumbled Church"
            },
            {
                "Water_Kevin_CrystalCave_2.2",
                "Crystal Cave"
            },
            {
                "Water_Kersti_SubHUB5DLC",
                "Greenhouse District"
            },
            {
                "Water_Nick_Sploosh_1.1",
                "Wild Courtyards"
            },
            {
                "Water_Samuel_CliffCave_1.0",
                "Cliff Gallery"
            },
            {
                "Water_Samuel_Library_1.1",
                "The Library"
            },
            {
                "Water_Samuel_Steps_1.1",
                "The Acropolis"
            },
            {
                "Water_Samuel_Tango_1.0",
                "Lake Castle"
            },
            {
                "Water_Samuel_End_1.2",
                "Heron's Shrine"
            },
            {
                "Water_Dian_Dream",
                "Lake Dream"
            },
            {
                "Flying_Samuel_Hub_2.0",
                "The Sky Kingdom"
            },
            {
                "Flying_Kersti_SubHUB1Easy",
                "Sunrise Gate Isles"
            },
            {
                "Flying_Kersti_Land Bridges_v1.4",
                "Lifted Arches"
            },
            {
                "Flying_Kersti_Fallen Ruins_0.8",
                "Fallen Towers"
            },
            {
                "Flying_Z_TheCataracts_v0.2",
                "The Cascades"
            },
            {
                "Flying_Kevin_IslandBottom",
                "The Lower City"
            },
            {
                "Flying_Kersti_SubHUB2Med",
                "Promenade Isles"
            },
            {
                "Flying_Samuel_Scoops_2.1",
                "The Colosseum"
            },
            {
                "Flying_Samuel_Garden_1.2",
                "Royal Garden"
            },
            {
                "Flying_Samuel_StuntCastle_1.1",
                "Aqueducts"
            },
            {
                "Flying_Kevin_FlyingIslands_v0.1",
                "Island Hamlet"
            },
            {
                "Flying_Kersti_SubHUB3Hard",
                "Keystone Isles"
            },
            {
                "Flying_Kevin_CollapsedBridge",
                "Collapsed Bridge"
            },
            {
                "Flying_Kersti_JumpLevel_v1.7",
                "Grand Portico"
            },
            {
                "Flying_Cam_MountainClimb_v0.1",
                "Prison Mines"
            },
            {
                "Flying_Cam_SlopedIslands_v0.1",
                "The Outskirts"
            },
            {
                "Flying_Kersti_SubHUB4Challenge",
                "Forgotten Isles"
            },
            {
                "Flying_Kersti_FoggyWindmills_0.4",
                "Foggy Windmills"
            },
            {
                "Flying_Cam_AsteroidGauntlet_v0.1",
                "Hanging Gardens"
            },
            {
                "Flying_Kevin_Slings",
                "Ruined Citadel"
            },
            {
                "Flying_Samuel_Breakdance_1.0",
                "The Agora"
            },
            {
                "Flying_Kersti_SubHUB5DLC",
                "Buried Isles"
            },
            {
                "Flying_Nick_Whoosh",
                "Crumbling Wells"
            },
            {
                "Flying_Samuel_Golf_1.0",
                "The Roosts"
            },
            {
                "Flying_Samuel_Bouldering_1.0",
                "The Gorge"
            },
            {
                "Flying_Z_ReactorFarm",
                "Windmill Graveyard"
            },
            {
                "Flying_Alicia_End_1.0",
                "Eagle's Shrine"
            },
            {
                "Flying_Dian_Dream",
                "Sky Dream"
            },
            {
                "Finale_Dian_Tomb_v3",
                "The Fallen Kingdom"
            },
            {
                "Finale_Alex_KingDream",
                "Fallen Dream"
            },
            {
                "Finale_Kersti_Return1Sky_v0.1",
                "Sky Ruins"
            },
            {
                "Finale_Kersti_Return2Water_v0.1",
                "Lake Ruins"
            },
            {
                "Finale_Kersti_Return3Forest_v0.1",
                "Forest Ruins"
            },
            {
                "Finale_Dian_ReturnKingdom_v0.2",
                "Home Ruins"
            },
            {
                "Finale_Dian_Bossfight",
                "Battle for the Last Kingdom"
            },
            {
                "Finale_Dian_Conclusion",
                "The Aftermath"
            },
            {
                "Finale_Dian_CageCredits",
                "Cage Credits"
            },
            {
                "Finale_Dian_CloaksCredits",
                "Cloaks Credits"
            },
            {
                "testscene",
                "testscene"
            },
            {
                "shaderlessscene",
                "shaderlessscene"
            },
            {
                "shaderfulscene",
                "shaderfulscene"
            }
        };
    }
}

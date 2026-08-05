using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WIMassCharacterSeeder
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        // 인종별 한글/영문 이름 데이터 풀
        private static readonly string[][] RaceFirstNamesKR = new string[][]
        {
            // 0: Human (인간)
            new string[] { "가브리엘", "루카스", "아드리안", "테오도르", "올리버", "아서", "윌리엄", "에드워드", "롤랜드", "로데릭", "세드릭", "베르나르", "도미닉", "빈센트", "펠릭스", "비토르", "레오폴트", "고드프리", "지그프리트", "발렌틴", "에반", "오스틴", "크리스토프", "제랄드", "험프리", "제레미", "루카", "세바스티안", "티모시", "율리우스", "로건", "해리슨", "클라우드", "맥스", "알렉산더", "다니엘", "아치볼드", "헤롤드", "월터", "레이놀드" },
            // 1: Elf (엘프)
            new string[] { "에스텔", "레골라스", "아란도르", "엘론드", "세레나", "실비아", "페아놀", "아리엘", "티리온", "이실도르", "파라미르", "라리엘", "갈라드리엘", "에해보르", "올로린", "시르단", "엘라단", "로리엔", "세란드라", "린디르", "엘로윈", "피노르", "아스테리아", "실비엘", "아에린", "테네브리스", "리가르", "아에놀", "이실리아", "샤라엘" },
            // 2: Dwarf (드워프)
            new string[] { "토린", "발린", "드왈린", "글로인", "빔부르", "발드릭", "토르간", "브로도르", "그로인", "카자드", "듀린", "브룬하르트", "그룬디르", "바린", "크라니크", "오르만", "룬도르", "에리크", "토르발트", "볼룬트", "모르단", "구드문드", "헐크", "하랄트", "두르간", "바르딘", "고그림", "브로크", "스카그", "툰드라" },
            // 3: Beast (수인)
            new string[] { "바르크", "페키르", "칼루스", "바그라트", "그롤", "레오르", "발카르", "가루다", "우르사", "타우론", "코라그", "장고", "볼트", "라자르", "펜리르", "티그리스", "팡", "라이칸", "그릴스", "보리스", "샤카", "자카르", "크롤", "바르가스", "티가", "샤르칸", "울프", "레오파드", "판테라", "하운드" },
            // 4: Orc (오크)
            new string[] { "고르다크", "스카르", "그로그", "부르차", "크라그", "다구르", "모그라쉬", "오그림", "가로쉬", "굴단", "두로탄", "차르", "록하르", "보그락", "주르가", "굴라크", "그림가르", "바르도크", "아즈골", "우르그락", "타르가르", "코르그락", "드라그", "바골", "투르크", "우르간", "그림스컬", "볼진", "샤그라트", "우그룩" },
            // 5: Undead (언데드)
            new string[] { "모데카이", "차론", "베리엘", "라자루스", "말라카이", "보르텍스", "네크로스", "사다르", "칼리반", "나자르", "드라쿨", "발타자르", "모르단", "리치몬드", "타나토스", "그레이브스", "카론", "하데스", "레브난트", "모티스", "에레보스", "노스페라투", "오시리스", "아자젤", "베리알", "볼드머", "스컬마스터", "그림자", "세이클", "모르그" },
            // 6: DarkElf (다크엘프)
            new string[] { "드리즈트", "자크나페인", "말리스", "베르나", "바이렌", "샬리스", "일리디안", "발키스", "질린", "모란드", "네르갈", "아자젤", "발카라", "루시엔", "바엘", "키리스", "바리스", "제라투스", "말라키스", "셀라노", "자카리아스", "타이라", "발레리아", "베라실", "다크세이더", "섀도우스텝", "나이트스피릿", "벨라토르", "크로노스", "아스트라" }
        };

        private static readonly string[][] RaceFirstNamesEN = new string[][]
        {
            // 0: Human
            new string[] { "Gabriel", "Lucas", "Adrian", "Theodore", "Oliver", "Arthur", "William", "Edward", "Roland", "Roderick", "Cedric", "Bernard", "Dominic", "Vincent", "Felix", "Victor", "Leopold", "Godfrey", "Siegfried", "Valentin", "Evan", "Austin", "Christoph", "Gerald", "Humphrey", "Jeremy", "Luca", "Sebastian", "Timothy", "Julius", "Logan", "Harrison", "Claude", "Max", "Alexander", "Daniel", "Archibald", "Harold", "Walter", "Reynold" },
            // 1: Elf
            new string[] { "Estelle", "Legolas", "Arandor", "Elrond", "Serena", "Sylvia", "Feanor", "Ariel", "Tyrion", "Isildur", "Faramir", "Lariel", "Galadriel", "Erebor", "Olorin", "Cirdan", "Eladan", "Lorien", "Serandra", "Lindir", "Elowen", "Finor", "Asteria", "Silviel", "Aerin", "Tenebris", "Rigar", "Aenol", "Isilia", "Sharael" },
            // 2: Dwarf
            new string[] { "Thorin", "Balin", "Dwalin", "Gloin", "Bimbur", "Baldric", "Thorgan", "Brodor", "Groin", "Khazad", "Durin", "Brunhart", "Grundir", "Barin", "Kranik", "Orman", "Rundor", "Erik", "Thorvald", "Volund", "Mordan", "Gudmund", "Hulk", "Harald", "Durgan", "Bardin", "Gogrim", "Brok", "Skag", "Tundra" },
            // 3: Beast
            new string[] { "Bark", "Fekir", "Calus", "Bagrat", "Groll", "Leor", "Valkar", "Garuda", "Ursa", "Tauron", "Korag", "Jango", "Bolt", "Lazar", "Fenrir", "Tigris", "Fang", "Lycan", "Grylls", "Boris", "Shaka", "Zakar", "Kroll", "Vargas", "Tiga", "Sharkan", "Wolf", "Leopard", "Panthera", "Hound" },
            // 4: Orc
            new string[] { "Gordak", "Skar", "Grog", "Barcha", "Krag", "Dagur", "Mogrash", "Ogrim", "Garrosh", "Guldan", "Durotan", "Char", "Lokhar", "Bograk", "Jurga", "Gulak", "Grimgar", "Bardok", "Azgol", "Urgrak", "Targar", "Korgrak", "Drag", "Bagol", "Turk", "Urgan", "Grimskull", "Voljin", "Shagrat", "Ugluk" },
            // 5: Undead
            new string[] { "Mordecai", "Charon", "Beriel", "Lazarus", "Malachai", "Vortex", "Necros", "Sadar", "Caliban", "Nazar", "Drakul", "Balthazar", "Mordan", "Richmond", "Thanatos", "Graves", "Caron", "Hades", "Revenant", "Mortis", "Erebos", "Nosferatu", "Osiris", "Azazel", "Belial", "Voldmer", "Skullmaster", "Shadow", "Cycle", "Morg" },
            // 6: DarkElf
            new string[] { "Drizzt", "Zaknafein", "Malice", "Viren", "Shalis", "Illidian", "Valkis", "Zilin", "Morand", "Nergal", "Azazel", "Valkara", "Lucien", "Bael", "Kyris", "Varis", "Zeratus", "Malekith", "Celano", "Zacharias", "Tyra", "Valeria", "Verasil", "Darksader", "Shadowstep", "Nightspirit", "Bellator", "Chronos", "Astra" }
        };

        private static readonly string[][] RaceSurnamesKR = new string[][]
        {
            // 0: Human
            new string[] { "브라이트우드", "밸리언트", "스톰가드", "아이언하트", "크로스필드", "스카이워커", "로즈몬트", "윈드미어", "하이타워", "나이트폴", "레이븐우드", "골든실드", "크라운가드", "섀도우브룩", "킹스턴", "세인트클레어", "스톤월", "윈드포스" },
            // 1: Elf
            new string[] { "실버리프", "스타라이트", "셰이드위스퍼", "문스프링", "선스트라이크", "블라섬펠", "리버송", "에버그린", "미스트웨이브", "달빛가르기", "바람노래", "별빛줄기", "이슬물결" },
            // 2: Dwarf
            new string[] { "아이언포지", "스틸브레이커", "락스플리터", "해머폴", "마운틴피크", "골드베인", "브론즈비어드", "오어크러셔", "썬더스트라이크", "망치발굽", "강철바위", "구리수염" },
            // 3: Beast
            new string[] { "클로스트라이크", "피어스팡", "사베지헌트", "블러드메인", "스톰프로울", "다크후프", "와일드체이스", "썬더클로", "나이트스트라이커", "맹수발톱", "사나운발굽" },
            // 4: Orc
            new string[] { "스컬크러셔", "아이언하이드", "블러드엑스", "본브레이커", "스톰마울", "파이어스매시", "워하운드", "둠해머", "샤드파이어", "피의도끼", "해골파쇄" },
            // 5: Undead
            new string[] { "둠위스퍼", "본웨이크", "섀도우소울", "칠그레이브", "프로스트스컬", "보이드워커", "다크블루드", "헬바운드", "소울벱", "나이트메어", "죽음영혼", "서리무덤" },
            // 6: DarkElf
            new string[] { "나이트블레이드", "보이드스토커", "섀도우위버", "푸리스트라이크", "다크스타", "블러드문", "크림슨셰이드", "사일런트스탭", "베놈웹", "어둠검성", "붉은달" }
        };

        private static readonly string[][] RaceSurnamesEN = new string[][]
        {
            // 0: Human
            new string[] { "Brightwood", "Valiant", "Stormguard", "Ironheart", "Crossfield", "Skywalker", "Rosemont", "Windmere", "Hightower", "Nightfall", "Ravenwood", "Goldshield", "Crownguard", "Shadowbrook", "Kingston", "St.Clair", "Stonewall", "Windforce" },
            // 1: Elf
            new string[] { "Silverleaf", "Starlight", "Shadewhisper", "Moonspring", "Sunstrike", "Blossomfell", "Riversong", "Evergreen", "Mistwave", "Mooncleave", "Windsong", "Starbeam", "Dewrip" },
            // 2: Dwarf
            new string[] { "Ironforge", "Steelbreaker", "Rocksplitter", "Hammerfall", "Mountainpeak", "Goldvein", "Bronzebeard", "Orecrusher", "Thunderstrike", "Hammerhoof", "Steelrock", "Copperbeard" },
            // 3: Beast
            new string[] { "Clawstrike", "Fiercepang", "Savagehunt", "Bloodmane", "Stormprowl", "Darkhoof", "Wildchase", "Thunderclaw", "Nightstriker", "Beastclaw", "Fiercehoof" },
            // 4: Orc
            new string[] { "Skullcrusher", "Ironhide", "Bloodaxe", "Bonebreaker", "Stormmaul", "Firesmash", "Warhound", "Doomhammer", "Shardfire", "Bloodaxe", "Skullsmash" },
            // 5: Undead
            new string[] { "Doomwhisper", "Bonewake", "Shadowsoul", "Chillgrave", "Frostskull", "Voidwalker", "Darkblood", "Hellbound", "Soulvep", "Nightmare", "Deathsoul", "Frostgrave" },
            // 6: DarkElf
            new string[] { "Nightblade", "Voidstalker", "Shadowweaver", "Furystrike", "Darkstar", "Bloodmoon", "Crimsonshade", "Silentstab", "Venomweb", "Darksword", "Crimsonmoon" }
        };

        [MenuItem("ProjectWI/Data/Seed Mass Character Roster (100 Heroes, 400 Commons)")]
        // Hero 100종, Common 400종 (Human 60%, 기타 6종 각 6.67%) 무작위 원본 인물 로스터를 생성하고 내정 데이터베이스를 갱신합니다.
        public static void SeedMassRoster()
        {
            UnityEngine.Object databaseAsset = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            if (databaseAsset == null)
            {
                Debug.LogError($"ProjectWI 데이터베이스를 찾을 수 없습니다: {DatabasePath}");
                return;
            }

            SerializedObject serializedDatabase = new SerializedObject(databaseAsset);
            SerializedProperty heroes = serializedDatabase.FindProperty("heroes");

            // 고유 8영웅 및 6일반 캐릭터 원본 보존을 위한 맵핑
            heroes.ClearArray();

            System.Random rand = new System.Random(20260806); // 결정론적 시드 생성

            int totalHeroesTarget = 100;
            int totalCommonsTarget = 400;

            // 종족 비율 계산: Human 60%, 나머지 6종족 각 6.67%
            int[] heroRaceCounts = CalculateRaceDistribution(totalHeroesTarget);
            int[] commonRaceCounts = CalculateRaceDistribution(totalCommonsTarget);

            // 1. Hero 100명 생성 (시작 영웅 ID 보존)
            string[] startingHeroIds = { "ares", "lyria", "elwyn", "selene", "brom", "morrigan", "kael", "theron" };
            string[] startingHeroNamesKR = { "아레스", "리리아", "엘윈", "셀레네", "브롬", "모리건", "케일", "테론" };
            string[] startingHeroNamesEN = { "Ares", "Lyria", "Elwyn", "Selene", "Brom", "Morrigan", "Kael", "Theron" };

            int heroIdIndex = 1;
            for (int raceIdx = 0; raceIdx < 7; raceIdx++)
            {
                int count = heroRaceCounts[raceIdx];
                for (int i = 0; i < count; i++)
                {
                    string id;
                    string overrideKR = null;
                    string overrideEN = null;

                    if (heroIdIndex <= startingHeroIds.Length)
                    {
                        id = startingHeroIds[heroIdIndex - 1];
                        overrideKR = startingHeroNamesKR[heroIdIndex - 1];
                        overrideEN = startingHeroNamesEN[heroIdIndex - 1];
                    }
                    else
                    {
                        id = $"hero_{heroIdIndex:D3}";
                    }

                    int heroClass = (heroIdIndex - 1) % 12;
                    AddCharacter(heroes, id, true, raceIdx, heroClass, rand, heroIdIndex, overrideKR, overrideEN);
                    heroIdIndex++;
                }
            }

            // 2. Common 400명 생성 (시작 일반 인물 ID 보존)
            string[] startingCommonIds = { "common_alden", "common_sable", "common_gareth", "common_varek", "common_mira", "common_thane", "common_nym", "common_raska" };
            string[] startingCommonNamesKR = { "알덴", "세이블", "가레스", "바렉", "미라", "테인", "님", "라스카" };
            string[] startingCommonNamesEN = { "Alden", "Sable", "Gareth", "Varek", "Mira", "Thane", "Nym", "Raska" };

            int commonIdIndex = 1;
            for (int raceIdx = 0; raceIdx < 7; raceIdx++)
            {
                int count = commonRaceCounts[raceIdx];
                for (int i = 0; i < count; i++)
                {
                    string id;
                    string overrideKR = null;
                    string overrideEN = null;

                    if (commonIdIndex <= startingCommonIds.Length)
                    {
                        id = startingCommonIds[commonIdIndex - 1];
                        overrideKR = startingCommonNamesKR[commonIdIndex - 1];
                        overrideEN = startingCommonNamesEN[commonIdIndex - 1];
                    }
                    else
                    {
                        id = $"common_{commonIdIndex:D3}";
                    }

                    int heroClass = (commonIdIndex - 1) % 12;
                    AddCharacter(heroes, id, false, raceIdx, heroClass, rand, commonIdIndex, overrideKR, overrideEN);
                    commonIdIndex++;
                }
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(databaseAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"ProjectWI 대규모 인물 로스터 생성 완료! 총 {heroes.arraySize}명 (영웅 100명, 일반 400명 | Human 60%)");
        }

        // 종족별 수량 계산 (Human 60%, 나머지 6개 종족 동등 분배)
        private static int[] CalculateRaceDistribution(int totalTarget)
        {
            int[] counts = new int[7];
            int humanCount = Mathf.RoundToInt(totalTarget * 0.60f); // 60%
            counts[0] = humanCount;

            int remaining = totalTarget - humanCount;
            int perOtherRace = remaining / 6;
            int remainder = remaining % 6;

            for (int r = 1; r < 7; r++)
            {
                counts[r] = perOtherRace + (r <= remainder ? 1 : 0);
            }

            return counts;
        }

        // 단일 캐릭터 생성 및 프로퍼티 세팅
        private static void AddCharacter(SerializedProperty heroes, string id, bool isHero, int raceIndex, int heroClassIndex, System.Random rand, int sequenceIndex, string overrideKR = null, string overrideEN = null)
        {
            int arrayIdx = heroes.arraySize;
            heroes.InsertArrayElementAtIndex(arrayIdx);
            SerializedProperty charProp = heroes.GetArrayElementAtIndex(arrayIdx);

            // ID
            charProp.FindPropertyRelative("id").stringValue = id;

            // 이름 (인종별 퍼스트네임 + 성씨 무작위 조합 또는 오버라이드 이름)
            string krName;
            string enName;

            if (string.IsNullOrEmpty(overrideKR) == false && string.IsNullOrEmpty(overrideEN) == false)
            {
                krName = overrideKR;
                enName = overrideEN;
            }
            else
            {
                string[] firstKR = RaceFirstNamesKR[raceIndex];
                string[] firstEN = RaceFirstNamesEN[raceIndex];
                string[] surnameKR = RaceSurnamesKR[raceIndex];
                string[] surnameEN = RaceSurnamesEN[raceIndex];

                int fIdx = rand.Next(firstKR.Length);
                int sIdx = rand.Next(surnameKR.Length);

                krName = $"{firstKR[fIdx]} {surnameKR[sIdx]}";
                enName = $"{firstEN[fIdx]} {surnameEN[sIdx]}";
            }

            SerializedProperty nameProp = charProp.FindPropertyRelative("displayName");
            nameProp.FindPropertyRelative("uid").stringValue = $"CHARACTER_{id.ToUpperInvariant()}";
            nameProp.FindPropertyRelative("korean").stringValue = krName;
            nameProp.FindPropertyRelative("english").stringValue = enName;

            // 등급 & 종족 & 클래스
            charProp.FindPropertyRelative("grade").enumValueIndex = isHero ? 0 : 1;
            charProp.FindPropertyRelative("race").enumValueIndex = raceIndex;
            charProp.FindPropertyRelative("heroClass").enumValueIndex = heroClassIndex;

            // 능력치 (클래스 주/보조 특성에 맞춰 적절한 밸런스 분배)
            int leadership, might, intelligence, politics, charisma;
            GenerateBalancedStats(isHero, heroClassIndex, rand, out leadership, out might, out intelligence, out politics, out charisma);

            charProp.FindPropertyRelative("leadership").intValue = leadership;
            charProp.FindPropertyRelative("might").intValue = might;
            charProp.FindPropertyRelative("intelligence").intValue = intelligence;
            charProp.FindPropertyRelative("politics").intValue = politics;
            charProp.FindPropertyRelative("charisma").intValue = charisma;

            // 충성도 & 등용 조건
            charProp.FindPropertyRelative("loyaltyState").enumValueIndex = 0; // Stable
            int requiredRep = isHero ? rand.Next(15, 75) : rand.Next(0, 20);
            charProp.FindPropertyRelative("requiredReputation").intValue = requiredRep;

            // 등용 이벤트 ID
            charProp.FindPropertyRelative("recruitmentEventId").stringValue = requiredRep >= 30
                ? "recruit_faction"
                : (requiredRep >= 10 ? "recruit_service" : "recruit_trust");

            SerializedProperty requestProp = charProp.FindPropertyRelative("recruitmentRequest");
            requestProp.FindPropertyRelative("uid").stringValue = $"CHARACTER_{id.ToUpperInvariant()}_REQUEST";
            requestProp.FindPropertyRelative("korean").stringValue = "세력의 명성과 실력을 증명";
            requestProp.FindPropertyRelative("english").stringValue = "Prove the faction's reputation and strength";

            // 스킬 사용 가능 여부
            charProp.FindPropertyRelative("activeSkillAvailable").boolValue = isHero;

            // 특기 및 초상화 초기화
            charProp.FindPropertyRelative("traits").arraySize = 0;
            charProp.FindPropertyRelative("portrait").objectReferenceValue = null;
        }

        // 클래스 직업 특성에 맞는 적절한 5대 능력치 밸런스 분배
        private static void GenerateBalancedStats(bool isHero, int heroClass, System.Random rand,
            out int leadership, out int might, out int intelligence, out int politics, out int charisma)
        {
            // 기본 베이스 (Hero: 총합 320~400 / Common: 총합 200~300)
            int baseMin = isHero ? 55 : 30;
            int baseMax = isHero ? 75 : 50;

            leadership = rand.Next(baseMin, baseMax);
            might = rand.Next(baseMin, baseMax);
            intelligence = rand.Next(baseMin, baseMax);
            politics = rand.Next(baseMin, baseMax);
            charisma = rand.Next(baseMin, baseMax);

            // 클래스 주/보조 능력치 보너스 (+15 ~ +25)
            int mainBonus = isHero ? rand.Next(18, 26) : rand.Next(12, 18);
            int subBonus = isHero ? rand.Next(10, 18) : rand.Next(8, 14);

            switch (heroClass)
            {
                case 0: // MagicSwordsman (Might + Intelligence)
                    might += mainBonus;
                    intelligence += subBonus;
                    break;
                case 1: // Guardian (Leadership + Might)
                    leadership += mainBonus;
                    might += subBonus;
                    break;
                case 2: // Crusader (Leadership + Politics)
                    leadership += mainBonus;
                    politics += subBonus;
                    break;
                case 3: // SwordMaster (Might + Leadership)
                    might += mainBonus;
                    leadership += subBonus;
                    break;
                case 4: // Archer (Might + Intelligence)
                    might += mainBonus;
                    intelligence += subBonus;
                    break;
                case 5: // Assassin (Might + Charisma)
                    might += mainBonus;
                    charisma += subBonus;
                    break;
                case 6: // Archmage (Intelligence + Charisma)
                    intelligence += mainBonus;
                    charisma += subBonus;
                    break;
                case 7: // Priest (Intelligence + Politics)
                    intelligence += mainBonus;
                    politics += subBonus;
                    break;
                case 8: // Druid (Intelligence + Leadership)
                    intelligence += mainBonus;
                    leadership += subBonus;
                    break;
                case 9: // Strategist (Intelligence + Politics)
                    intelligence += mainBonus;
                    politics += subBonus;
                    break;
                case 10: // Alchemist (Intelligence + Politics)
                    intelligence += mainBonus;
                    politics += subBonus;
                    break;
                case 11: // Warlock (Intelligence + Might)
                    intelligence += mainBonus;
                    might += subBonus;
                    break;
            }

            // 0~99 상한 제한
            leadership = Mathf.Clamp(leadership, 1, 99);
            might = Mathf.Clamp(might, 1, 99);
            intelligence = Mathf.Clamp(intelligence, 1, 99);
            politics = Mathf.Clamp(politics, 1, 99);
            charisma = Mathf.Clamp(charisma, 1, 99);
        }
    }
}

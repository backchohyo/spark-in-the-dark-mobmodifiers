// MobModifiers: 몬스터에게 접두사(강화 효과)를 붙이고, 접두사 몬스터를 쓰러뜨리면 장비·스킬북을 보상으로 준다.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Creatures;
using HandItems;
using Management;
using ModLoader;
using UnityEngine;
using UnityEngine.UI;

[assembly: ModInfo("MobModifiers", "1.3.0", Description = "몬스터에게 접두사(거대한·역병의 등)를 붙이고, 쓰러뜨리면 티어에 따라 장비·스킬북을 보상으로 줍니다.",
    SettingsNote = "바꾼 값은 바로 저장·적용됩니다. 등장 확률 설정은 앞으로 새로 나타나는 몬스터부터, 수치·보상 설정은 이미 접두사가 붙은 몬스터에도 바로 반영됩니다.")]

namespace MobModifiers
{
    public enum MobAffix
    {
        None = 0, Giant = 1, Swift = 2, Hardened = 3, Plague = 4, Sharp = 5, Elite = 6, Regen = 7,
        Agile = 8, Mighty = 9, Vampiric = 10, Thorny = 11, Shadow = 12, Frenzied = 13, Undying = 14
    }

    public static class Cfg
    {
        const string S0 = "일반", SH = "머리 위 표시 (접두사 · 체력바)", A1 = "1티어 접두사 (체크 · 등장 비중)", A2 = "2티어 접두사 (체크 · 등장 비중)", A3 = "3티어 접두사 (체크 · 등장 비중)",
            V1 = "1티어 접두사 수치", V2 = "2티어 접두사 수치", V3 = "3티어 접두사 수치",
            R1 = "보상 - 1티어 몬스터", R2 = "보상 - 2티어 몬스터", R3 = "보상 - 3티어 몬스터";
        const string Strength = "약함|보통|강함|매우 강함";

        [Setting(S0, "접두사가 붙을 확률", Unit = "%", Min = 0, Max = 100, Desc = "몬스터가 처음 나타날 때 접두사가 붙을 확률")] public static int SpawnChance = 15;
        [Setting(S0, "다중 접두사 허용", Desc = "켜면 한 몬스터에 접두사가 여러 개 붙을 수 있습니다.")] public static bool AllowMultiple = true;
        [Setting(S0, "최대 접두사 개수", Unit = "개", Min = 1, Max = 5, Desc = "다중 접두사를 허용할 때 한 몬스터에 붙을 수 있는 최대 개수")] public static int MaxPrefixes = 3;
        [Setting(S0, "접두사가 하나 더 붙을 확률", Unit = "%", Min = 0, Max = 100, Desc = "접두사가 붙은 몬스터에 추가로 하나 더 붙을 확률. 최대 개수까지 매번 다시 굴립니다.")] public static int ExtraChance = 25;
        [Setting(SH, "접두사 이름표 표시", Desc = "접두사가 붙은 몬스터 머리 위에 【접두사】를 표시합니다. 색: 1티어 흰색, 2티어 주황, 3티어 빨강")] public static bool ShowLabels = true;
        [Setting(SH, "체력바 표시", Options = "끔|접두사 몬스터만|모든 몬스터", Desc = "몬스터 머리 위에 체력바를 표시할 대상")] public static int HealthBarMode = 2;
        [Setting(SH, "체력이 가득 찬 몬스터는 체력바 숨기기", Desc = "켜면 한 번이라도 피해를 입은 몬스터만 체력바가 보입니다.")] public static bool HideFullHealth = false;
        [Setting(SH, "체력 수치 표시", Desc = "체력바 위에 현재/최대 체력을 숫자로 표시합니다.")] public static bool ShowHpNumbers = false;
        [Setting(SH, "표시 거리", Unit = "m", Min = 3, Max = 50, Desc = "이 거리 안에 있는 몬스터만 이름표·체력바를 보여줍니다.")] public static int LabelDistance = 15;
        [Setting(SH, "크기", Unit = "%", Min = 50, Max = 200, Step = 10, Desc = "이름표 글자와 체력바 크기")] public static int LabelScale = 100;
        [Setting(SH, "벽 너머 몬스터는 숨기기", Desc = "끄면 벽 뒤에 있는 몬스터도 표시합니다.")] public static bool HideBehindWalls = true;

        [Setting(A1, "신속한", Desc = "이동 속도가 크게, 공격 속도가 조금 오릅니다.")] public static bool SwiftOn = true;
        [Setting(A1, "비중", Min = 0, Max = 100, Inline = true)] public static int SwiftWeight = 12;
        [Setting(A1, "단단한", Desc = "받는 피해가 크게 줄어듭니다 (방어력).")] public static bool HardenedOn = true;
        [Setting(A1, "비중", Min = 0, Max = 100, Inline = true)] public static int HardenedWeight = 12;
        [Setting(A1, "날카로운", Desc = "공격에 맞으면 출혈이 생깁니다.")] public static bool SharpOn = true;
        [Setting(A1, "비중", Min = 0, Max = 100, Inline = true)] public static int SharpWeight = 10;
        [Setting(A1, "재생의", Desc = "매초 최대 체력의 일정 %를 회복합니다.")] public static bool RegenOn = true;
        [Setting(A1, "비중", Min = 0, Max = 100, Inline = true)] public static int RegenWeight = 10;
        [Setting(A1, "유연한", Desc = "근접 공격을 일정 확률로 완전히 피합니다.")] public static bool AgileOn = true;
        [Setting(A1, "비중", Min = 0, Max = 100, Inline = true)] public static int AgileWeight = 10;
        [Setting(A2, "거대한", Desc = "체력과 공격력이 크게 오르고 몸집이 커집니다.")] public static bool GiantOn = true;
        [Setting(A2, "비중", Min = 0, Max = 100, Inline = true)] public static int GiantWeight = 10;
        [Setting(A2, "역병의", Desc = "공격에 맞으면 중독됩니다.")] public static bool PlagueOn = true;
        [Setting(A2, "비중", Min = 0, Max = 100, Inline = true)] public static int PlagueWeight = 9;
        [Setting(A2, "흡혈의", Desc = "영웅에게 입힌 피해의 일정 %만큼 체력을 회복합니다.")] public static bool VampiricOn = true;
        [Setting(A2, "비중", Min = 0, Max = 100, Inline = true)] public static int VampiricWeight = 7;
        [Setting(A2, "가시의", Desc = "영웅이 근접 공격으로 입힌 피해의 일정 %가 영웅에게 되돌아옵니다. (영웅을 죽이지는 않음)")] public static bool ThornyOn = true;
        [Setting(A2, "비중", Min = 0, Max = 100, Inline = true)] public static int ThornyWeight = 7;
        [Setting(A2, "어둠의", Desc = "공격에 맞으면 일정 확률로 실명(시야·통찰력 감소)에 걸립니다.")] public static bool ShadowOn = true;
        [Setting(A2, "비중", Min = 0, Max = 100, Inline = true)] public static int ShadowWeight = 6;
        [Setting(A2, "광폭한", Desc = "체력이 기준 이하로 떨어지면 공격력과 공격 속도가 오릅니다.")] public static bool FrenziedOn = true;
        [Setting(A2, "비중", Min = 0, Max = 100, Inline = true)] public static int FrenziedWeight = 5;
        [Setting(A3, "강력한", Desc = "공격력이 매우 크게 오릅니다.")] public static bool MightyOn = true;
        [Setting(A3, "비중", Min = 0, Max = 100, Inline = true)] public static int MightyWeight = 5;
        [Setting(A3, "엘리트", Desc = "공격력·체력·이동 속도·방어력이 모두 크게 오릅니다.")] public static bool EliteOn = true;
        [Setting(A3, "비중", Min = 0, Max = 100, Inline = true)] public static int EliteWeight = 9;
        [Setting(A3, "불사의", Desc = "처음 쓰러질 때 한 번 체력 일부를 회복하며 다시 일어납니다.")] public static bool UndyingOn = true;
        [Setting(A3, "비중", Min = 0, Max = 100, Inline = true)] public static int UndyingWeight = 4;

        [Setting(V1, "신속한  이동", Unit = "%", Min = 0, Max = 200, Step = 5, Desc = "신속한: 이동 속도와 공격 속도 증가량")] public static int SwiftMove = 40;
        [Setting(V1, "공속", Unit = "%", Min = 0, Max = 100, Inline = true)] public static int SwiftAtk = 10;
        [Setting(V1, "단단한  피해 감소", Unit = "%", Min = 0, Max = 90, Desc = "단단한: 받는 피해 감소량")] public static int HardenedDef = 35;
        [Setting(V1, "날카로운  출혈 세기", Options = Strength, Desc = "날카로운: 걸리는 출혈의 세기와, 공격이 맞았을 때 출혈이 생길 확률")] public static int SharpStrength = 0;
        [Setting(V1, "확률", Unit = "%", Min = 0, Max = 100, Inline = true)] public static int SharpChance = 100;
        [Setting(V1, "재생의  초당 회복", Unit = "%", Min = 0.5f, Max = 20, Step = 0.5f, Desc = "재생의: 매초 최대 체력의 몇 %를 회복하는지")] public static float RegenPct = 2f;
        [Setting(V1, "유연한  회피 확률", Unit = "%", Min = 1, Max = 90, Desc = "유연한: 근접 공격을 완전히 피할 확률")] public static int AgileDodge = 30;
        [Setting(V2, "거대한  체력", Unit = "%", Min = 0, Max = 500, Step = 5, Desc = "거대한: 체력 증가량과 공격력 증가량")] public static int GiantHp = 80;
        [Setting(V2, "공격력", Unit = "%", Min = 0, Max = 500, Step = 5, Inline = true)] public static int GiantDmg = 30;
        [Setting(V2, "역병의  중독 세기", Options = Strength, Desc = "역병의: 걸리는 중독의 세기와, 공격이 맞았을 때 중독될 확률")] public static int PlagueStrength = 0;
        [Setting(V2, "확률", Unit = "%", Min = 0, Max = 100, Inline = true)] public static int PlagueChance = 100;
        [Setting(V2, "흡혈의  흡혈량", Unit = "%", Min = 1, Max = 200, Desc = "흡혈의: 영웅에게 입힌 피해 중 회복하는 비율")] public static int VampiricPct = 30;
        [Setting(V2, "가시의  반사량", Unit = "%", Min = 1, Max = 200, Desc = "가시의: 영웅이 입힌 피해 중 되돌려받는 비율")] public static int ThornyPct = 20;
        [Setting(V2, "어둠의  실명 확률", Unit = "%", Min = 0, Max = 100, Desc = "어둠의: 공격이 맞았을 때 실명에 걸릴 확률")] public static int ShadowChance = 40;
        [Setting(V2, "광폭한  기준", Unit = "%", Min = 1, Max = 100, Desc = "광폭한: 체력이 기준 % 이하일 때 공격력·공격 속도 증가")] public static int FrenziedHp = 50;
        [Setting(V2, "공격력", Unit = "%", Min = 0, Max = 300, Step = 5, Inline = true)] public static int FrenziedDmg = 40;
        [Setting(V2, "공속", Unit = "%", Min = 0, Max = 200, Step = 5, Inline = true)] public static int FrenziedAtk = 30;
        [Setting(V3, "강력한  공격력", Unit = "%", Min = 0, Max = 500, Step = 5, Desc = "강력한: 공격력 증가량")] public static int MightyDmg = 80;
        [Setting(V3, "엘리트  모든 능력치", Unit = "%", Min = 0, Max = 200, Desc = "엘리트: 공격력·체력·이동 속도·방어력(피해 감소)이 각각 이만큼 오릅니다.")] public static int ElitePct = 30;
        [Setting(V3, "불사의  부활 체력", Unit = "%", Min = 1, Max = 100, Desc = "불사의: 다시 일어날 때 회복하는 체력 비율")] public static int UndyingHp = 50;

        [Setting(R1, "장비를 줄 확률", Unit = "%", Min = 0, Max = 100, Desc = "가장 높은 접두사 티어가 1인 몬스터를 영웅이 쓰러뜨렸을 때")] public static int T1EquipChance = 50;
        [Setting(R1, "장비 개수  최소", Unit = "개", Min = 1, Max = 5)] public static int T1EquipMin = 1;
        [Setting(R1, "최대", Unit = "개", Min = 1, Max = 5, Inline = true)] public static int T1EquipMax = 1;
        [Setting(R1, "스킬북을 줄 확률", Unit = "%", Min = 0, Max = 100, Desc = "아직 배우지 않은, 직업에 맞는 스킬북 중 하나")] public static int T1BookChance = 3;
        [Setting(R1, "등급 비중  빛바랜", Min = 0, Max = 100, Desc = "보상 장비 등급 비율 (Modifiers 모드가 켜져 있을 때)")] public static int T1W0 = 45;
        [Setting(R1, "은은한", Min = 0, Max = 100, Inline = true)] public static int T1W1 = 40;
        [Setting(R1, "찬란한", Min = 0, Max = 100, Inline = true)] public static int T1W2 = 15;
        [Setting(R1, "유니크 확률", Unit = "%", Min = 0, Max = 100, Step = 0.5f, Desc = "보상 장비가 유니크일 확률 (Modifiers 모드가 켜져 있을 때)")] public static float T1Unique = 1f;

        [Setting(R2, "장비를 줄 확률", Unit = "%", Min = 0, Max = 100, Desc = "가장 높은 접두사 티어가 2인 몬스터를 영웅이 쓰러뜨렸을 때")] public static int T2EquipChance = 85;
        [Setting(R2, "장비 개수  최소", Unit = "개", Min = 1, Max = 5)] public static int T2EquipMin = 1;
        [Setting(R2, "최대", Unit = "개", Min = 1, Max = 5, Inline = true)] public static int T2EquipMax = 2;
        [Setting(R2, "스킬북을 줄 확률", Unit = "%", Min = 0, Max = 100)] public static int T2BookChance = 8;
        [Setting(R2, "등급 비중  빛바랜", Min = 0, Max = 100, Desc = "보상 장비 등급 비율 (Modifiers 모드가 켜져 있을 때)")] public static int T2W0 = 25;
        [Setting(R2, "은은한", Min = 0, Max = 100, Inline = true)] public static int T2W1 = 45;
        [Setting(R2, "찬란한", Min = 0, Max = 100, Inline = true)] public static int T2W2 = 30;
        [Setting(R2, "유니크 확률", Unit = "%", Min = 0, Max = 100, Step = 0.5f)] public static float T2Unique = 3f;

        [Setting(R3, "장비를 줄 확률", Unit = "%", Min = 0, Max = 100, Desc = "가장 높은 접두사 티어가 3인 몬스터를 영웅이 쓰러뜨렸을 때")] public static int T3EquipChance = 100;
        [Setting(R3, "장비 개수  최소", Unit = "개", Min = 1, Max = 5)] public static int T3EquipMin = 2;
        [Setting(R3, "최대", Unit = "개", Min = 1, Max = 5, Inline = true)] public static int T3EquipMax = 3;
        [Setting(R3, "스킬북을 줄 확률", Unit = "%", Min = 0, Max = 100)] public static int T3BookChance = 20;
        [Setting(R3, "등급 비중  빛바랜", Min = 0, Max = 100, Desc = "보상 장비 등급 비율 (Modifiers 모드가 켜져 있을 때)")] public static int T3W0 = 5;
        [Setting(R3, "은은한", Min = 0, Max = 100, Inline = true)] public static int T3W1 = 35;
        [Setting(R3, "찬란한", Min = 0, Max = 100, Inline = true)] public static int T3W2 = 60;
        [Setting(R3, "유니크 확률", Unit = "%", Min = 0, Max = 100, Step = 0.5f)] public static float T3Unique = 10f;

        // ---- 이름으로 읽기
        static readonly Dictionary<string, FieldInfo> Cache = new Dictionary<string, FieldInfo>();
        public static object F(string name)
        {
            FieldInfo f;
            if (!Cache.TryGetValue(name, out f)) { f = typeof(Cfg).GetField(name); Cache[name] = f; }
            return f == null ? null : f.GetValue(null);
        }
        public static int I(string name) { object v = F(name); return v is int ? (int)v : 0; }
        public static float Fl(string name) { object v = F(name); return v is float ? (float)v : (v is int ? (int)v : 0f); }
        public static bool B(string name) { object v = F(name); return v is bool && (bool)v; }
    }

    public class MobData
    {
        public List<MobAffix> Affixes = new List<MobAffix>();
        public bool UndyingUsed;
        public bool Rewarded;

        public bool Any { get { return Affixes.Count > 0; } }
        string _label; int _labelN = -1;
        public string Label
        {
            get
            {
                if (_labelN != Affixes.Count || _label == null)
                {
                    _label = "【" + string.Join(" · ", Affixes.Select(Mod.NameOf).ToArray()) + "】";
                    _labelN = Affixes.Count;
                }
                return _label;
            }
        }
        public bool Has(MobAffix a) { return Affixes.Contains(a); }
        public int Tier
        {
            get
            {
                int t = 0;
                foreach (var a in Affixes) t = Math.Max(t, Mod.TierOf(a));
                return t;
            }
        }

        public string Serialize()
        {
            return "m1;a=" + string.Join(",", Affixes.Select(a => ((int)a).ToString(CultureInfo.InvariantCulture)).ToArray())
                + ";u=" + (UndyingUsed ? 1 : 0) + ";r=" + (Rewarded ? 1 : 0);
        }

        public static MobData Parse(string s)
        {
            if (string.IsNullOrEmpty(s) || !s.StartsWith("m1;", StringComparison.Ordinal)) return null;
            var d = new MobData();
            foreach (var part in s.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                string k = part.Substring(0, eq), v = part.Substring(eq + 1);
                if (k == "a")
                {
                    foreach (var x in v.Split(','))
                    {
                        int n;
                        if (int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) && n > 0 && n <= (int)MobAffix.Undying && !d.Affixes.Contains((MobAffix)n))
                            d.Affixes.Add((MobAffix)n);
                    }
                }
                else if (k == "u") d.UndyingUsed = v == "1";
                else if (k == "r") d.Rewarded = v == "1";
            }
            return d;
        }
    }

    public static class Mod
    {
        const string MeleeDamaged = "CharacterBase,Weapon,EnemySector,Armor,Shield,HandItem";
        static readonly System.Random Rng = new System.Random();
        internal static readonly Dictionary<CharacterBase, MobData> Data = new Dictionary<CharacterBase, MobData>();
        static float _nextCleanup;
        static int _logged;

        public static readonly MobAffix[] All =
        {
            MobAffix.Swift, MobAffix.Hardened, MobAffix.Sharp, MobAffix.Regen, MobAffix.Agile,
            MobAffix.Giant, MobAffix.Plague, MobAffix.Vampiric, MobAffix.Thorny, MobAffix.Shadow, MobAffix.Frenzied,
            MobAffix.Mighty, MobAffix.Elite, MobAffix.Undying
        };

        public static string NameOf(MobAffix a)
        {
            switch (a)
            {
                case MobAffix.Giant: return "거대한";
                case MobAffix.Swift: return "신속한";
                case MobAffix.Hardened: return "단단한";
                case MobAffix.Plague: return "역병의";
                case MobAffix.Sharp: return "날카로운";
                case MobAffix.Elite: return "엘리트";
                case MobAffix.Regen: return "재생의";
                case MobAffix.Agile: return "유연한";
                case MobAffix.Mighty: return "강력한";
                case MobAffix.Vampiric: return "흡혈의";
                case MobAffix.Thorny: return "가시의";
                case MobAffix.Shadow: return "어둠의";
                case MobAffix.Frenzied: return "광폭한";
                case MobAffix.Undying: return "불사의";
                default: return "";
            }
        }

        static string Key(MobAffix a) { return a.ToString(); }
        public static bool IsOn(MobAffix a) { return Cfg.B(Key(a) + "On"); }
        public static int WeightOf(MobAffix a) { return Math.Max(0, Cfg.I(Key(a) + "Weight")); }
        public static int TierOf(MobAffix a)
        {
            switch (a)
            {
                case MobAffix.Swift: case MobAffix.Hardened: case MobAffix.Sharp: case MobAffix.Regen: case MobAffix.Agile: return 1;
                case MobAffix.Mighty: case MobAffix.Elite: case MobAffix.Undying: return 3;
                case MobAffix.None: return 0;
                default: return 2;
            }
        }

        static void Log(string m) { if (_logged++ < 40) Loader.Log("[MobModifiers] " + m); }
        static void Warn(Exception e) { Log("오류: " + e.Message); }

        // ------------------------------------------------------------------ 데이터
        public static MobData Get(CharacterBase ch)
        {
            MobData d;
            if ((object)ch != null && Data.TryGetValue(ch, out d)) return d;
            return null;
        }

        static bool IsHero(CharacterBase c)
        {
            return (bool)c && (bool)MainChar.Instance && c == MainChar.Instance.Character;
        }

        static bool Eligible(CharacterBase ch, out Creature cr)
        {
            cr = null;
            if (!ch || IsHero(ch) || ch.IsDead) return false;
            cr = ch.GetComponent<Creature>();
            if (!cr || cr.IsEnvironment || cr.IsDead) return false;
            return true;
        }

        static MobData Roll()
        {
            var d = new MobData();
            if (Rng.Next(100) >= Cfg.SpawnChance) return d;
            int max = Cfg.AllowMultiple ? Mathf.Clamp(Cfg.MaxPrefixes, 1, 5) : 1;
            MobAffix first = Pick(d);
            if (first == MobAffix.None) return d;
            d.Affixes.Add(first);
            while (d.Affixes.Count < max && Rng.Next(100) < Cfg.ExtraChance)
            {
                MobAffix a = Pick(d);
                if (a == MobAffix.None) break;
                d.Affixes.Add(a);
            }
            return d;
        }

        static MobAffix Pick(MobData exclude)
        {
            int total = 0;
            foreach (var a in All) if (IsOn(a) && !exclude.Has(a)) total += WeightOf(a);
            if (total <= 0) return MobAffix.None;
            int r = Rng.Next(total);
            foreach (var a in All)
            {
                if (!IsOn(a) || exclude.Has(a)) continue;
                r -= WeightOf(a);
                if (r < 0) return a;
            }
            return MobAffix.None;
        }

        // ------------------------------------------------------------------ 저장
        [ModInit]
        public static void Init()
        {
            SaveExt.Register("MobMod",
                o =>
                {
                    Component c = o as Component;
                    if (!c || !c.GetComponent<Creature>()) return null;
                    MobData d = Get(c.GetComponent<CharacterBase>());
                    return d == null ? null : d.Serialize();
                },
                (o, v) =>
                {
                    if (v == null) return;
                    Component c = o as Component;
                    if (!c || !c.GetComponent<Creature>()) return;
                    CharacterBase ch = c.GetComponent<CharacterBase>();
                    MobData d = MobData.Parse(v);
                    if (ch && d != null) Data[ch] = d;
                });
        }

        // ------------------------------------------------------------------ 배율 계산
        static float Pct(int p) { return p / 100f; }

        public static float HpMult(MobData d)
        {
            float m = 1f;
            if (d.Has(MobAffix.Giant)) m *= 1f + Pct(Cfg.GiantHp);
            if (d.Has(MobAffix.Elite)) m *= 1f + Pct(Cfg.ElitePct);
            return m;
        }

        public static float TakenMult(MobData d)
        {
            float m = 1f;
            if (d.Has(MobAffix.Hardened)) m *= 1f - Mathf.Clamp01(Pct(Cfg.HardenedDef));
            if (d.Has(MobAffix.Elite)) m *= 1f / (1f + Pct(Cfg.ElitePct));
            return m;
        }

        public static float DmgMult(MobData d, CharacterBase ch)
        {
            float m = 1f;
            if (d.Has(MobAffix.Giant)) m *= 1f + Pct(Cfg.GiantDmg);
            if (d.Has(MobAffix.Elite)) m *= 1f + Pct(Cfg.ElitePct);
            if (d.Has(MobAffix.Mighty)) m *= 1f + Pct(Cfg.MightyDmg);
            if (d.Has(MobAffix.Frenzied) && IsFrenzy(ch)) m *= 1f + Pct(Cfg.FrenziedDmg);
            return m;
        }

        public static float MoveMult(MobData d)
        {
            float m = 1f;
            if (d.Has(MobAffix.Swift)) m *= 1f + Pct(Cfg.SwiftMove);
            if (d.Has(MobAffix.Elite)) m *= 1f + Pct(Cfg.ElitePct);
            return m;
        }

        public static float AtkMult(MobData d, CharacterBase ch)
        {
            float m = 1f;
            if (d.Has(MobAffix.Swift)) m *= 1f + Pct(Cfg.SwiftAtk);
            if (d.Has(MobAffix.Frenzied) && IsFrenzy(ch)) m *= 1f + Pct(Cfg.FrenziedAtk);
            return m;
        }

        static bool IsFrenzy(CharacterBase ch)
        {
            try { return ch && ch.GetHealthPercent() <= Cfg.FrenziedHp; } catch { return false; }
        }

        // ------------------------------------------------------------------ 훅
        // CharacterBase.CalculateSpecs() 끝 (0.2초마다): 접두사 결정, 재생, 효과 컴포넌트
        [Hook(HookKind.Postfix, "CharacterBase", "CalculateSpecs", Params = 0, Args = new[] { 0 })]
        public static void Tick(CharacterBase ch)
        {
            long perf = Perf.Begin();
            try
            {
                if (Game.IsPaused || IsHero(ch) || !ch) return;
                if (Time.unscaledTime > _nextCleanup)
                {
                    _nextCleanup = Time.unscaledTime + 20f;
                    foreach (var k in Data.Keys.Where(k => !k).ToList()) Data.Remove(k);
                }
                MobData d = Get(ch);
                if (d == null)
                {
                    Creature cr;
                    if (!Eligible(ch, out cr)) return;
                    d = Roll();
                    Data[ch] = d;
                    if (d.Any) Log("접두사 몬스터 등장: " + ch.CharacterName + " [" + string.Join(", ", d.Affixes.Select(NameOf).ToArray()) + "]");
                }
                Runner.Ensure();
                if (!d.Any || ch.IsDead) return;
                if (!ch.GetComponent<MobFx>()) ch.gameObject.AddComponent<MobFx>().Init(ch);
                if (d.Has(MobAffix.Regen))
                {
                    CharSpec hp = ch.GetSpec(CharSpecType.Health);
                    if (hp != null && hp.CurrentValue > 0f && hp.CurrentValue < hp.MaxValue)
                        hp.CurrentValue = Mathf.Min(hp.MaxValue, hp.CurrentValue + hp.MaxValue * (Cfg.RegenPct / 100f) * 0.2f);
                }
            }
            catch (Exception e) { Warn(e); }
            finally { Perf.End("MobModifiers 몬스터 능력치(0.2초마다)", perf); }
        }

        // CharacterBase.ReduceHealth(float) 앞부분: 체력 증가(받는 피해를 나눔), 방어력, 불사
        [Hook(HookKind.PrefixArg, "CharacterBase", "ReduceHealth", Params = 1, ArgIndex = 1, Args = new[] { 0, 1 })]
        public static float Incoming(CharacterBase ch, float dmg)
        {
            try
            {
                MobData d = Get(ch);
                if (d == null || !d.Any || dmg <= 0f) return dmg;
                dmg = dmg / HpMult(d) * TakenMult(d);
                if (d.Has(MobAffix.Undying) && !d.UndyingUsed)
                {
                    CharSpec hp = ch.GetSpec(CharSpecType.Health);
                    if (hp != null && hp.CurrentValue - dmg <= 0f)
                    {
                        d.UndyingUsed = true;
                        hp.CurrentValue = Mathf.Max(1f, hp.MaxValue * Mathf.Clamp01(Cfg.UndyingHp / 100f));
                        Log(ch.CharacterName + " (불사의) 다시 일어남");
                        return 0f;
                    }
                }
            }
            catch (Exception e) { Warn(e); }
            return dmg;
        }

        // CharacterBase.GetWeaponDamage(Weapon, bool) 뒷부분: 몬스터 공격력
        [Hook(HookKind.Postfix, "CharacterBase", "GetWeaponDamage", Params = 2, Args = new[] { 0 })]
        public static float OutgoingDamage(float result, CharacterBase ch)
        {
            try
            {
                MobData d = Get(ch);
                if (d == null || !d.Any) return result;
                return result * DmgMult(d, ch);
            }
            catch (Exception e) { Warn(e); return result; }
        }

        // CharacterBase.Damaged(근접) 앞부분: 유연한 → 공격을 완전히 피함
        [Hook(HookKind.PrefixSkipReturn, "CharacterBase", "Damaged", ParamTypes = MeleeDamaged, Args = new[] { 0, 1 })]
        public static bool Dodge(CharacterBase victim, CharacterBase attacker, out CharacterBase.DamageInfo info)
        {
            info = default(CharacterBase.DamageInfo);
            try
            {
                MobData d = Get(victim);
                if (d == null || !d.Has(MobAffix.Agile) || victim.IsDead) return false;
                if (Rng.Next(100) >= Cfg.AgileDodge) return false;
                info.DamageType = DamageType.None;
                info.Damage = 0f;
                return true;
            }
            catch (Exception e) { Warn(e); return false; }
        }

        // CharacterBase.Damaged(근접) 뒷부분: 역병/날카로운/어둠/흡혈/가시
        [Hook(HookKind.Postfix, "CharacterBase", "Damaged", ParamTypes = MeleeDamaged, Args = new[] { 0, 1, 2 })]
        public static CharacterBase.DamageInfo AfterHit(CharacterBase.DamageInfo result, CharacterBase victim, CharacterBase attacker, Weapon weapon)
        {
            try
            {
                if (result.DamageType == DamageType.None || result.Damage <= 0f) return result;
                MobData ad = Get(attacker);
                if (ad != null && ad.Any && IsHero(victim) && !victim.IsDead)
                {
                    Generator g = Generator.Instance;
                    if (ad.Has(MobAffix.Plague) && g && Rng.Next(100) < Cfg.PlagueChance)
                    {
                        Buff poison = g.BottleNegativeBuffs.FirstOrDefault(b => b && b.SpecialEffect == BuffSpecialEffect.Poison);
                        if (poison) victim.AddBuffDuplicate(poison, false, (BuffStrength)Mathf.Clamp(Cfg.PlagueStrength, 0, 3), attacker, SkillType.None);
                    }
                    if (ad.Has(MobAffix.Sharp) && g && g.BleedingBuff && Rng.Next(100) < Cfg.SharpChance)
                        victim.AddBuffDuplicate(g.BleedingBuff, false, (BuffStrength)Mathf.Clamp(Cfg.SharpStrength, 0, 3), attacker, SkillType.None);
                    if (ad.Has(MobAffix.Shadow) && g && g.BlindnessBuff && Rng.Next(100) < Cfg.ShadowChance)
                        victim.AddBuffDuplicate(g.BlindnessBuff, false, BuffStrength.Low, attacker, SkillType.None);
                    if (ad.Has(MobAffix.Vampiric) && !attacker.IsDead)
                    {
                        CharSpec hp = attacker.GetSpec(CharSpecType.Health);
                        if (hp != null && hp.CurrentValue > 0f)
                            hp.CurrentValue = Mathf.Min(hp.MaxValue, hp.CurrentValue + result.Damage * Pct(Cfg.VampiricPct) / HpMult(ad));
                    }
                }
                MobData vd = Get(victim);
                if (vd != null && vd.Has(MobAffix.Thorny) && IsHero(attacker) && !attacker.IsDead)
                {
                    CharSpec hp = attacker.GetSpec(CharSpecType.Health);
                    float reflect = result.Damage * Pct(Cfg.ThornyPct);
                    if (hp != null && hp.CurrentValue > 1f && reflect > 0f)
                        attacker.ReduceHealth(Mathf.Min(reflect, hp.CurrentValue - 1f));
                }
            }
            catch (Exception e) { Warn(e); }
            return result;
        }

        // BestHero.AddKilledCreature 앞부분: 영웅이 접두사 몬스터를 쓰러뜨리면 보상
        [Hook(HookKind.Prefix, "Management.BestHero", "AddKilledCreature", Params = 2, Args = new[] { 1 })]
        public static void OnKill(CharacterBase ch)
        {
            try
            {
                MobData d = Get(ch);
                if (d == null || !d.Any || d.Rewarded) return;
                d.Rewarded = true;
                Runner.Ensure();
                Runner.Queue(ch, d.Tier, 0.6f);
            }
            catch (Exception e) { Warn(e); }
        }

        // ------------------------------------------------------------------ 보상
        static readonly Type ModifiersType = FindModifiers();
        static Type FindModifiers()
        {
            try
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    if (a.GetName().Name == "Modifiers") return a.GetType("Modifiers.Mod");
            }
            catch { }
            return null;
        }
        static MethodInfo _forceRoll;
        static bool _forceRollSearched;

        static void BiasItem(HandItem h, int tier)
        {
            if (!_forceRollSearched)
            {
                _forceRollSearched = true;
                Type t = ModifiersType ?? FindModifiers();
                if (t != null) _forceRoll = t.GetMethod("ForceRoll", BindingFlags.Public | BindingFlags.Static);
            }
            if (_forceRoll == null) return;
            string p = "T" + tier;
            try { _forceRoll.Invoke(null, new object[] { h, Cfg.I(p + "W0"), Cfg.I(p + "W1"), Cfg.I(p + "W2"), Cfg.Fl(p + "Unique") }); }
            catch (Exception e) { Warn(e); }
        }

        internal static void GiveRewards(CharacterBase ch, int tier)
        {
            if (!ch || tier < 1) return;
            string p = "T" + tier;
            Container box = ch.GetComponent<Container>();
            int given = 0;
            if (Rng.Next(100) < Cfg.I(p + "EquipChance"))
            {
                int a = Cfg.I(p + "EquipMin"), b = Cfg.I(p + "EquipMax");
                int n = Rng.Next(Math.Min(a, b), Math.Max(a, b) + 1);
                for (int i = 0; i < n; i++)
                {
                    GameObject prefab = PickEquipment(tier);
                    if (prefab && Spawn(prefab, ch, box, tier)) given++;
                }
            }
            if (Rng.Next(100) < Cfg.I(p + "BookChance"))
            {
                GameObject book = PickBook();
                if (book && Spawn(book, ch, box, 0)) given++;
            }
            if (box && given > 0) box.CheckInteraction();
            Log(ch.CharacterName + " 처치 보상 (티어 " + tier + "): " + given + "개");
        }

        static GameObject PickEquipment(int tier)
        {
            Generator g = Generator.Instance;
            if (!g || g.LevelItems == null || g.LevelItems.Count == 0) return null;
            int idx = Mathf.Clamp(Game.GetLevelNumber() - 1, 0, g.LevelItems.Count - 1);
            GeneratorItemSet set = g.LevelItems[idx];
            if (!set) return null;
            var weapons = (set.Weapons ?? new List<Weapon>()).Where(w => w && !w.IsBroken && !w.IsBone && !w.IsClaws && w.SkillType != SkillType.WeaponCrossbows && w.SkillType != SkillType.UnarmedCombat).Cast<HandItem>().ToList();
            var armors = (set.Armors ?? new List<Armor>()).Where(x => x && !x.NonUsable).Cast<HandItem>().ToList();
            var shields = (set.Shields ?? new List<Shield>()).Where(x => x).Cast<HandItem>().ToList();
            int r = Rng.Next(100);
            List<HandItem> pool = r < 45 ? weapons : (r < 80 ? armors : shields);
            if (pool.Count == 0) pool = weapons.Count > 0 ? weapons : (armors.Count > 0 ? armors : shields);
            if (pool.Count == 0) return null;
            // 티어가 높을수록 낡은 장비를 덜 준다
            if (tier >= 2)
            {
                var better = pool.Where(x => x.Quality != ItemQuality.Oldest).ToList();
                if (tier >= 3) { var best = better.Where(x => x.Quality == ItemQuality.New).ToList(); if (best.Count > 0 && Rng.Next(100) < 50) better = best; }
                if (better.Count > 0) pool = better;
            }
            return pool[Rng.Next(pool.Count)].gameObject;
        }

        static GameObject PickBook()
        {
            try
            {
                Generator g = Generator.Instance;
                if (!g) return null;
                MethodInfo m = typeof(Generator).GetMethod("GetKnowledgeBooksForGeneration", BindingFlags.NonPublic | BindingFlags.Instance);
                var items = m == null ? null : m.Invoke(g, null) as System.Collections.IEnumerable;
                if (items == null) return null;
                var list = new List<GameObject>();
                foreach (var it in items)
                {
                    FieldInfo f = it.GetType().GetField("Prefab");
                    GameObject go = f == null ? null : f.GetValue(it) as GameObject;
                    if (go) list.Add(go);
                }
                return list.Count == 0 ? null : list[Rng.Next(list.Count)];
            }
            catch (Exception e) { Warn(e); return null; }
        }

        static bool Spawn(GameObject prefab, CharacterBase ch, Container box, int tier)
        {
            Vector3 pos = ch.transform.position + Vector3.up * 0.8f + new Vector3((float)Rng.NextDouble() - 0.5f, 0f, (float)Rng.NextDouble() - 0.5f) * 0.6f;
            GameObject go = SaveGameManager.Instance.CreateNewSaveableObject(prefab.name, pos, null, box ? ch.transform : null);
            if (!go) return false;
            HandItem h = go.GetComponent<HandItem>();
            if (!h) return true;
            Weapon w = h as Weapon; Armor ar = h as Armor; Shield sh = h as Shield;
            if ((object)w != null) w.GenerateSpecs(1f, true);
            else if ((object)ar != null) ar.GenerateSpecs();
            else if ((object)sh != null) sh.GenerateSpecs();
            h.IsGenerated = true;
            if (tier > 0) BiasItem(h, tier);
            if (box) box.AddItemToContainer(h);
            else
            {
                go.transform.SetParent(null);
                go.SetActive(true);
                if ((object)ar != null && ar.ShellObject && ar.OriginalObject) { ar.ShellObject.SetActive(true); ar.OriginalObject.SetActive(false); }
                h.EnablePhysics(true, true);
            }
            return true;
        }
    }

    // 몬스터에 붙는 효과: 몸집(거대한), 애니메이션 속도(이동/공격)
    public class MobFx : MonoBehaviour
    {
        CharacterBase _ch;
        Animator _anim;
        bool _hasAtk;
        float _lastSet = -1f;
        Vector3 _baseScale;
        bool _scaled;

        public void Init(CharacterBase ch)
        {
            _ch = ch;
            _anim = GetComponent<Animator>();
            _baseScale = transform.localScale;
            if (_anim)
                foreach (var p in _anim.parameters)
                    if (p.name == "IsAttacking" && p.type == AnimatorControllerParameterType.Bool) { _hasAtk = true; break; }
        }

        void LateUpdate()
        {
            long perf = Perf.Begin();
            try { Step(); } finally { Perf.End("MobModifiers 몬스터 속도/크기", perf); }
        }

        void Step()
        {
            if (!_ch) { Destroy(this); return; }
            MobData d = Mod.Get(_ch);
            if (d == null || !d.Any) return;
            bool giant = d.Has(MobAffix.Giant);
            if (giant != _scaled)
            {
                transform.localScale = giant ? _baseScale * 1.15f : _baseScale;
                _scaled = giant;
            }
            if (!_anim || Game.IsPaused) return;
            if (_ch.IsDead)
            {
                if (_lastSet > 0f && Mathf.Approximately(_anim.speed, _lastSet)) _anim.speed = 1f;
                enabled = false;
                return;
            }
            float cur = _anim.speed;
            // 게임이 속도를 바꿨으면 그 값을 새 기준으로 삼는다 (곱셈이 쌓이지 않게)
            if (_lastSet < 0f || !Mathf.Approximately(cur, _lastSet)) _baseSpeed = cur;
            bool attacking = _hasAtk && _anim.GetBool("IsAttacking");
            float want = _baseSpeed * (attacking ? Mod.AtkMult(d, _ch) : Mod.MoveMult(d));
            _anim.speed = want;
            _lastSet = want;
        }
        float _baseSpeed = 1f;
    }

    // 보상 지연 처리 + 머리 위 이름표/체력바
    // 성능: 표시 대상과 위치는 0.25초마다 한 번만 계산하고, 매 프레임에는 그리기만 한다.
    //       글꼴은 게임 UI 와 공유하지 않는 전용 글꼴을 써서 UI 전체가 다시 그려지는 일을 막는다.
    public class Runner : MonoBehaviour
    {
        static Runner _inst;
        struct Pending { public CharacterBase Ch; public int Tier; public float At; }
        static readonly List<Pending> Queue_ = new List<Pending>();

        class Shown
        {
            public Creature Cr;
            public CharacterBase Ch;
            public MobData D;
            public Vector3 TopOff;
            public bool Label, Bar;
            public string Numbers;
        }
        readonly List<Shown> _shown = new List<Shown>();
        readonly List<Shown> _pool = new List<Shown>();
        float _nextScan;
        static readonly Dictionary<Creature, Renderer[]> RendererCache = new Dictionary<Creature, Renderer[]>();
        float _nextCacheClean;
        int _mask = -1;

        Font _font;
        GUIStyle _style, _shadow;
        Texture2D _texBg, _texFill, _texFillMob, _texEdge;
        int _guiErrors;
        static readonly Color[] TierColors = { new Color(0.85f, 0.83f, 0.78f), new Color(0.85f, 0.83f, 0.78f), new Color(0.95f, 0.68f, 0.30f), new Color(0.95f, 0.33f, 0.28f) };

        public static void Ensure()
        {
            if (_inst) return;
            var go = new GameObject("MobModifiersRunner");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<Runner>();
        }

        public static void Queue(CharacterBase ch, int tier, float delay)
        {
            Queue_.Add(new Pending { Ch = ch, Tier = tier, At = Time.time + delay });
        }

        void Update()
        {
            for (int i = Queue_.Count - 1; i >= 0; i--)
            {
                if (Time.time < Queue_[i].At) continue;
                var p = Queue_[i];
                Queue_.RemoveAt(i);
                try { Mod.GiveRewards(p.Ch, p.Tier); }
                catch (Exception e) { Loader.Log("[MobModifiers] 보상 오류: " + e.Message); }
            }
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.25f;
                long t = Perf.Begin();
                try { Scan(); }
                catch (Exception e) { if (_guiErrors++ < 5) Loader.Log("[MobModifiers] 표시 대상 계산 오류: " + e.Message); }
                Perf.End("MobModifiers 머리 위 표시 계산", t);
            }
        }

        static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c); t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        static bool GetBounds(Creature cr, out Bounds b)
        {
            Renderer[] rs;
            if (!RendererCache.TryGetValue(cr, out rs) || rs == null)
            {
                var list = new List<Renderer>();
                foreach (var r in cr.GetComponentsInChildren<Renderer>(false))
                    if (r is SkinnedMeshRenderer || r is MeshRenderer) list.Add(r);
                rs = list.ToArray();
                RendererCache[cr] = rs;
            }
            bool any = false;
            b = default(Bounds);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (!r || !r.enabled) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            if (!any || b.size.y > 6f) b = new Bounds(cr.transform.position + Vector3.up * 1.2f, new Vector3(1f, 2.4f, 1f));
            return true;
        }

        void Scan()
        {
            for (int i = 0; i < _shown.Count; i++) _pool.Add(_shown[i]);
            _shown.Clear();
            bool labels = Cfg.ShowLabels, bars = Cfg.HealthBarMode > 0;
            if (!labels && !bars) return;
            MainChar mc = MainChar.Instance;
            Camera cam = Camera.main;
            if (!mc || !cam) return;
            if (Time.unscaledTime > _nextCacheClean)
            {
                _nextCacheClean = Time.unscaledTime + 10f;
                var dead = new List<Creature>();
                foreach (var k in RendererCache.Keys) if (!k) dead.Add(k);
                foreach (var k in dead) RendererCache.Remove(k);
            }
            if (_mask < 0) _mask = LayerMask.GetMask("LD_WallsLayer", "LD_DoorsLayer");
            Vector3 heroPos = mc.transform.position, camPos = cam.transform.position;
            float maxSq = Cfg.LabelDistance * (float)Cfg.LabelDistance;
            foreach (Creature cr in Creature.ActiveCreatures)
            {
                if (!cr || cr.IsDead || cr.IsEnvironment) continue;
                if ((cr.transform.position - heroPos).sqrMagnitude > maxSq) continue;
                CharacterBase ch = cr.Character;
                if (!ch || ch.IsDead) continue;
                MobData d = Mod.Get(ch);
                bool affixed = d != null && d.Any;
                bool showLabel = labels && affixed;
                bool showBar = Cfg.HealthBarMode == 2 || (Cfg.HealthBarMode == 1 && affixed);
                CharSpec hp = showBar ? ch.GetSpec(CharSpecType.Health) : null;
                if (hp == null || hp.MaxValue <= 0f) showBar = false;
                if (showBar && Cfg.HideFullHealth && hp.CurrentValue >= hp.MaxValue - 0.01f) showBar = false;
                if (!showLabel && !showBar) continue;
                Bounds bb;
                GetBounds(cr, out bb);
                if (Cfg.HideBehindWalls && _mask != 0 && Physics.Linecast(camPos, bb.center, _mask)) continue;
                Shown sh;
                if (_pool.Count > 0) { sh = _pool[_pool.Count - 1]; _pool.RemoveAt(_pool.Count - 1); }
                else sh = new Shown();
                sh.Cr = cr; sh.Ch = ch; sh.D = d; sh.Label = showLabel; sh.Bar = showBar;
                sh.TopOff = new Vector3(bb.center.x, bb.max.y + 0.25f, bb.center.z) - cr.transform.position;
                sh.Numbers = null;
                if (showBar && Cfg.ShowHpNumbers)
                {
                    float mult = affixed ? Mod.HpMult(d) : 1f;
                    sh.Numbers = Mathf.CeilToInt(Mathf.Max(0f, hp.CurrentValue) * mult) + " / " + Mathf.CeilToInt(hp.MaxValue * mult);
                }
                _shown.Add(sh);
            }
        }

        // 게임 UI 와 따로 쓰는 이름표 전용 글꼴
        static Font MakeFont()
        {
            var installed = new HashSet<string>(Font.GetOSInstalledFontNames() ?? new string[0], StringComparer.OrdinalIgnoreCase);
            var names = new List<string>();
            foreach (var n in new[] { "Warhaven", "Warhaven Regular", "워헤이븐", "MaruBuri", "마루 부리", "Batang", "바탕" })
                if (installed.Contains(n)) names.Add(n);
            names.Add("Malgun Gothic"); names.Add("맑은 고딕"); names.Add("Segoe UI Symbol");
            return Font.CreateDynamicFontFromOSFont(names.ToArray(), 16);
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || _shown.Count == 0) return;
            long perf = Perf.Begin();
            try
            {
                Camera cam = Camera.main;
                if (!cam) return;
                if (_style == null)
                {
                    _font = MakeFont();
                    _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false, richText = false, font = _font };
                    _shadow = new GUIStyle(_style);
                    _shadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
                    _texBg = Tex(new Color(0f, 0f, 0f, 0.65f));
                    _texEdge = Tex(new Color(0.75f, 0.70f, 0.60f, 0.55f));
                    _texFill = Tex(new Color(0.62f, 0.10f, 0.08f, 0.95f));
                    _texFillMob = Tex(new Color(0.85f, 0.42f, 0.10f, 0.95f));
                }
                float k01 = Screen.height / 1080f * Mathf.Clamp(Cfg.LabelScale, 50, 200) / 100f;
                int size = Mathf.Max(8, Mathf.RoundToInt(17f * k01));
                if (_style.fontSize != size) { _style.fontSize = size; _shadow.fontSize = size; }
                for (int i = 0; i < _shown.Count; i++)
                {
                    Shown sh = _shown[i];
                    if (!sh.Cr || !sh.Ch || sh.Ch.IsDead) continue;
                    Vector3 sp = cam.WorldToScreenPoint(sh.Cr.transform.position + sh.TopOff);
                    if (sp.z <= 0f) continue;
                    float x = sp.x, y = Screen.height - sp.y;
                    if (sh.Bar)
                    {
                        CharSpec hp = sh.Ch.GetSpec(CharSpecType.Health);
                        if (hp != null && hp.MaxValue > 0f)
                        {
                            float bw = 110f * k01, bh = Mathf.Max(4f, 9f * k01);
                            GUI.DrawTexture(new Rect(x - bw / 2f - 1f, y - bh - 1f, bw + 2f, bh + 2f), _texEdge);
                            GUI.DrawTexture(new Rect(x - bw / 2f, y - bh, bw, bh), _texBg);
                            float frac = Mathf.Clamp01(hp.CurrentValue / hp.MaxValue);
                            GUI.DrawTexture(new Rect(x - bw / 2f, y - bh, bw * frac, bh), sh.Label ? _texFillMob : _texFill);
                            if (sh.Numbers != null)
                            {
                                var nr = new Rect(x - 150f, y - bh - size * 1.3f - 2f, 300f, size * 1.4f);
                                _style.normal.textColor = new Color(0.9f, 0.88f, 0.82f);
                                GUI.Label(new Rect(nr.x + 1f, nr.y + 1f, nr.width, nr.height), sh.Numbers, _shadow);
                                GUI.Label(nr, sh.Numbers, _style);
                                y -= size * 1.3f;
                            }
                            y -= bh + 4f;
                        }
                    }
                    if (sh.Label && sh.D != null)
                    {
                        string text = sh.D.Label;
                        var r = new Rect(x - 300f, y - size * 1.5f, 600f, size * 1.5f);
                        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, _shadow);
                        _style.normal.textColor = TierColors[Mathf.Clamp(sh.D.Tier, 0, 3)];
                        GUI.Label(r, text, _style);
                    }
                }
            }
            catch (Exception e)
            {
                if (_guiErrors++ < 5) Loader.Log("[MobModifiers] 머리 위 표시 오류: " + e);
            }
            finally { Perf.End("MobModifiers 머리 위 표시 그리기", perf); }
        }
    }
}

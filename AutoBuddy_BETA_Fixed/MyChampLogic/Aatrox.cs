using System;
using System.Linq;
using AutoBuddy.MainLogics;
using EloBuddy;
using EloBuddy.SDK;

namespace AutoBuddy.MyChampLogic
{
    class Aatrox : IChampLogic
    {
        public int[] SkillSequence => new[] {3, 2, 3, 1, 3, 4, 3, 2, 3, 2, 4, 2, 2, 1, 1, 4, 1, 1};
        public float MaxDistanceForAA => Player.Instance.AttackRange;
        public float OptimalMaxComboDistance => Player.GetSpell(SpellSlot.Q).SData.CastRangeDisplayOverride;
        public float HarassDistance => Player.GetSpell(SpellSlot.E).SData.CastRangeDisplayOverride;
        public LogicSelector Logic { get; set; }
        public string ShopSequence => "1055:Buy,2003:StartHpPot,3340:Buy,1006:Buy,1036:Buy,1036:Buy,3077:Buy,1036:Buy,1053:Buy,1037:Buy,3074:Buy,1036:Buy,1053:Buy,1036:Buy,3144:Buy,1042:Buy,1042:Buy,1043:Buy,3153:Buy,1029:Buy,1029:Buy,3082:Buy,1028:Buy,1011:Buy,3143:Buy,1033:Buy,2003:StopHpPot,1028:Buy,3211:Buy,1028:Buy,3067:Buy,3065:Buy,1029:Buy,1031:Buy,1055:Sell,1033:Buy,1057:Buy,3026:Buy,1001:Buy,3111:Buy";
        public int QDamage => (int)(new[] {10,35,60,95,120}[Player.GetSpell(SpellSlot.Q).Level + 1] + Player.Instance.TotalAttackDamage * 1.1f);
        public int EDamage => (int)(new[] {70,110,150,190,230}[Player.GetSpell(SpellSlot.E).Level + 1] + (Player.Instance.TotalAttackDamage - Player.Instance.BaseAttackDamage) * 0.7f);
        public int RDamage => (int)(new[] {200,300,400}[Player.GetSpell(SpellSlot.R).Level + 1] + Player.Instance.TotalMagicalDamage);

        public Aatrox()
        {
            Game.OnUpdate += delegate
            {
                if (Player.Instance.HealthPercent < 50 && Player.GetSpell(SpellSlot.W).ToggleState == 1)
                {
                    Player.CastSpell(SpellSlot.W);
                }
                if (Player.Instance.HealthPercent > 50 && Player.GetSpell(SpellSlot.W).ToggleState == 0)
                {
                    Player.CastSpell(SpellSlot.W);
                }
            };
        }

        public void Harass(AIHeroClient target)
        {
            var spelldata = Player.GetSpell(SpellSlot.E).SData;
            if (Player.CanUseSpell(SpellSlot.E) == SpellState.Ready)
            {
                var predictedPos = Prediction.Position.PredictUnitPosition(target,
                        (int)(spelldata.CastTime + Player.Instance.Distance(target) / spelldata.MissileSpeed * 1000))
                    .To3DWorld();
                if (predictedPos.Distance(Player.Instance) < spelldata.CastRangeDisplayOverride)
                {
                    Player.CastSpell(SpellSlot.E, predictedPos);
                }
            }
        }

        public void Survi()
        {
            var closestHero = EntityManager.Heroes.Enemies.OrderBy(x => x.Distance(Player.Instance)).FirstOrDefault();
            if (closestHero != null)
            {
                Harass(closestHero);
            }
            if (Player.CanUseSpell(SpellSlot.Q) == SpellState.Ready)
            {
                Player.CastSpell(SpellSlot.Q, Player.Instance.GetNearestTurret(false).ServerPosition);
            }
        }

        public void Combo(AIHeroClient target)
        {
            Func<SpellSlot, SpellDataInst> GetSpell = Player.GetSpell;
            if (Player.CanUseSpell(SpellSlot.R) == SpellState.Ready && Player.Instance.CountEnemyChampionsInRange(GetSpell(SpellSlot.R).SData.CastRangeDisplayOverride) >= 2)
            {
                Player.CastSpell(SpellSlot.R);
                return;
            }
            if (Player.CanUseSpell(SpellSlot.E) == SpellState.Ready)
            {
                var predictedPos = Prediction.Position.PredictUnitPosition(target, (int)(GetSpell(SpellSlot.E).SData.CastTime + Player.Instance.Distance(target) / GetSpell(SpellSlot.E).SData.MissileSpeed * 1000)).To3DWorld();
                if (predictedPos.Distance(Player.Instance) < GetSpell(SpellSlot.E).SData.CastRangeDisplayOverride)
                {
                    Player.CastSpell(SpellSlot.E, predictedPos);
                    return;
                }
            }
            if (Player.CanUseSpell(SpellSlot.Q) == SpellState.Ready &&
                Player.Instance.Distance(target) < GetSpell(SpellSlot.Q).SData.CastRangeDisplayOverride)
            {
                Player.CastSpell(SpellSlot.Q, Prediction.Position.PredictUnitPosition(target, 400).To3DWorld());
            }
        }

        public void UnkillableMinion()
        {
            if (!Orbwalker.ModeIsActive(Orbwalker.ActiveModes.LaneClear))
            {
                return;
            }
            var farms = Orbwalker.UnKillableMinionsList;
            foreach (var farm in farms.OrderBy(x => x.Distance(Player.Instance)).ThenBy(x => x.Health))
            {
                if (Player.Instance.CalculateDamageOnUnit(farm, DamageType.Physical, EDamage) < farm.Health)
                {
                    Player.CastSpell(SpellSlot.E, farm.ServerPosition);
                    return;
                }
            }
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Mechanics.cs" company="LeagueSharp">
//   Copyright (C) 2015 L33T
//   
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//   
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//   
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// <summary>
//   The <c>Ekko</c> Mechanics.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Ekko
{
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    /// <summary>
    ///     The <c>Ekko</c> Mechanics.
    /// </summary>
    public class Mechanics
    {
        #region Static Fields

        /// <summary>
        ///     Last Q Tick.
        /// </summary>
        private static int lastQCastTick;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the flee minion.
        /// </summary>
        public static Obj_AI_Minion FleeMinion { get; set; }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the player.
        /// </summary>
        private static Obj_AI_Hero Player
        {
            get
            {
                return Ekko.Player;
            }
        }

        /// <summary>
        ///     Gets the spells.
        /// </summary>
        private static IDictionary<SpellSlot, Spell> Spells
        {
            get
            {
                return Ekko.Spells;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Process the farming.
        /// </summary>
        public static void ProcessFarm()
        {
            if (Spells[SpellSlot.Q].IsReady())
            {
                if (Ekko.Orbwalker.ActiveMode.Equals(Orbwalking.OrbwalkingMode.LaneClear)
                    && Ekko.Menu.Item("l33t.ekko.farming.lcq").GetValue<bool>())
                {
                    var farmLocation =
                        MinionManager.GetBestLineFarmLocation(
                            GameObjects.EnemyMinions.Where(
                                m => m.Distance(Player.Position) <= Spells[SpellSlot.Q].Range)
                                .Select(m => m.Position.To2D())
                                .ToList(), 
                            Spells[SpellSlot.Q].Width, 
                            Spells[SpellSlot.Q].Range);

                    if (farmLocation.MinionsHit >= Ekko.Menu.Item("l33t.ekko.farming.lcqh").GetValue<Slider>().Value)
                    {
                        Spells[SpellSlot.Q].Cast(farmLocation.Position);
                    }
                }
                else if (Ekko.Menu.Item("l33t.ekko.farming.lhq").GetValue<bool>())
                {
                    var farmLocation =
                        GetBestLineFarmLocation(
                            GameObjects.EnemyMinions.Where(
                                m => m.Distance(Player.Position) <= Spells[SpellSlot.Q].Range).ToList(), 
                            Spells[SpellSlot.Q].Width, 
                            Spells[SpellSlot.Q].Range);

                    if (farmLocation.MinionsHit >= Ekko.Menu.Item("l33t.ekko.farming.lhqh").GetValue<Slider>().Value
                        && farmLocation.Minions.Count(m => m.Health <= Damages.GetDamageQ(m))
                        >= Ekko.Menu.Item("l33t.ekko.farming.lhqh").GetValue<Slider>().Value)
                    {
                        Spells[SpellSlot.Q].Cast(farmLocation.Position);
                    }
                }
            }
        }

        /// <summary>
        ///     Processes Flee.
        /// </summary>
        public static void ProcessFlee()
        {
            var targets =
                GameObjects.EnemyHeroes.Where(h => h.Distance(Player.Position) <= Spells[SpellSlot.Q].Range).ToList();
            if (targets.Any())
            {
                if (Spells[SpellSlot.Q].IsReady() && Ekko.Menu.Item("l33t.ekko.flee.q").GetValue<bool>())
                {
                    var line = MinionManager.GetBestLineFarmLocation(
                        targets.Select(h => h.Position.To2D()).ToList(), 
                        Spells[SpellSlot.Q].Width, 
                        Spells[SpellSlot.Q].Range);
                    if (line.MinionsHit >= targets.Count / 2 - 1 && line.MinionsHit >= 1)
                    {
                        Spells[SpellSlot.Q].Cast(line.Position);
                    }
                }

                if (Spells[SpellSlot.W].IsReady() && Ekko.Menu.Item("l33t.ekko.flee.w").GetValue<bool>())
                {
                    Spells[SpellSlot.W].Cast(Spells[SpellSlot.W].GetPrediction(Player, true).CastPosition);
                }
            }

            if (Spells[SpellSlot.E].IsReady() && Ekko.Menu.Item("l33t.ekko.flee.e").GetValue<bool>())
            {
                var closestTarget = targets.OrderBy(t => t.Distance(Player.Position)).FirstOrDefault();
                if (closestTarget != null)
                {
                    var farestMinion =
                        GameObjects.EnemyMinions.Where(
                            m => m.Distance(Player.Position) <= Spells[SpellSlot.E].Range + 425f)
                            .OrderByDescending(m => m.Distance(closestTarget.Position))
                            .FirstOrDefault();
                    if (farestMinion != null && farestMinion.IsValidTarget())
                    {
                        Spells[SpellSlot.E].Cast(farestMinion);
                        FleeMinion = farestMinion;
                    }
                }
            }

            if (Player.AttackRange > 125 && FleeMinion.IsValidTarget())
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, FleeMinion);
            }
        }

        /// <summary>
        ///     Processes combo.
        /// </summary>
        /// <param name="ultimate">
        ///     The ultimate.
        /// </param>
        public static void ProcessSpells(bool ultimate = false)
        {
            var target = TargetSelector.GetTarget(float.MaxValue, TargetSelector.DamageType.Magical);
            if (!target.IsValidTarget())
            {
                return;
            }

            var targetPos =
                Prediction.GetPrediction(target, Spells[SpellSlot.Q].Delay, target.BoundingRadius, target.MoveSpeed)
                    .UnitPosition;

            if (Spells[SpellSlot.Q].IsReady() && Ekko.Menu.Item("l33t.ekko.combo.q").GetValue<bool>()
                && targetPos.Distance(Player.Position) <= Spells[SpellSlot.Q].Range)
            {
                var pred = Spells[SpellSlot.Q].GetPrediction(target).CastPosition;
                Spells[SpellSlot.Q].Cast(pred + (pred - targetPos));
                lastQCastTick = Ekko.GameTime;
            }

            if (Spells[SpellSlot.E].IsReady() && Ekko.Menu.Item("l33t.ekko.combo.e").GetValue<bool>()
                && targetPos.Distance(Player.Position) <= Spells[SpellSlot.E].Range + 425f)
            {
                var dash = Player.Position.Extend(targetPos, Spells[SpellSlot.E].Range);
                if (dash.IsWall())
                {
                    var longestDash = Player.Position;
                    while (!longestDash.IsWall())
                    {
                        longestDash = longestDash.Extend(targetPos, 1f);
                    }

                    if (longestDash.Distance(targetPos) <= 425f)
                    {
                        Spells[SpellSlot.E].Cast(dash);
                    }
                }
                else
                {
                    if (dash.Distance(targetPos) <= 425f)
                    {
                        Spells[SpellSlot.E].Cast(dash);
                    }
                }
            }

            if (Spells[SpellSlot.W].IsReady() && Ekko.Menu.Item("l33t.ekko.combo.w").GetValue<bool>()
                && Player.Distance(targetPos) <= Spells[SpellSlot.Q].Range - Player.AttackRange
                && Ekko.GameTime - lastQCastTick < 7000 + 1000 * Spells[SpellSlot.Q].Level - 1)
            {
                var targetPosition =
                    Prediction.GetPrediction(
                        target, 
                        3f, 
                        target.BoundingRadius, 
                        target.MoveSpeed * new float[] { 40, 50, 60, 70, 80 }[Spells[SpellSlot.W].Level - 1])
                        .UnitPosition;
                if (targetPosition.Distance(Player.Position) <= Spells[SpellSlot.W].Range)
                {
                    Spells[SpellSlot.W].Cast(targetPos + (targetPos - targetPosition));
                }
            }

            if (ultimate)
            {
                if (Spells[SpellSlot.R].IsReady() && Ekko.Menu.Item("l33t.ekko.combo.r").GetValue<bool>()
                    && Ekko.EkkoGhost != null && Ekko.EkkoGhost.IsValid)
                {
                    if (Ekko.Menu.Item("l33t.ekko.combo.rkill").GetValue<bool>())
                    {
                        if (target.Distance(Ekko.EkkoGhost.Position) <= Spells[SpellSlot.R].Range)
                        {
                            var damage = Damages.GetDamageE(target) + Damages.GetDamageQ(target)
                                         + Damages.GetDamageR(target);
                            if (damage >= target.Health)
                            {
                                Spells[SpellSlot.R].Cast();
                            }
                        }
                    }

                    if (
                        GameObjects.EnemyHeroes.Count(
                            e => e.Distance(Ekko.EkkoGhost.Position) <= Spells[SpellSlot.Q].Range)
                        >= Ekko.Menu.Item("l33t.ekko.combo.rifhit").GetValue<Slider>().Value)
                    {
                        Spells[SpellSlot.R].Cast();
                    }

                    if (Ekko.Menu.Item("l33t.ekko.combo.rbackenable").GetValue<bool>()
                        && Ekko.OldHealthPercent.ContainsKey(Ekko.GameTime - 4000))
                    {
                        if (Player.HealthPercent - Ekko.OldHealthPercent[Ekko.GameTime - 4000]
                            > Ekko.Menu.Item("l33t.ekko.combo.rback").GetValue<Slider>().Value)
                        {
                            Spells[SpellSlot.R].Cast();
                        }
                    }
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Calculates and gets the best line farming location.
        /// </summary>
        /// <param name="minionPositions">
        ///     The Minions
        /// </param>
        /// <param name="width">
        ///     The Width
        /// </param>
        /// <param name="range">
        ///     The Range
        /// </param>
        /// <returns>
        ///     The Farming Location Container.
        /// </returns>
        private static FarmingLocation GetBestLineFarmLocation(
            IReadOnlyList<Obj_AI_Minion> minionPositions, 
            float width, 
            float range)
        {
            var result = new Vector2();
            var minionCount = 0;
            var startPos = ObjectManager.Player.ServerPosition.To2D();
            var minionsList = new List<Obj_AI_Minion>();
            var tracking = minionPositions.ToDictionary(minion => minion, minion => minion.Position.To2D());

            var max = minionPositions.Count;
            for (var i = 0; i < max; i++)
            {
                for (var j = 0; j < max; j++)
                {
                    if (minionPositions[j].Position.To2D() != minionPositions[i].Position.To2D())
                    {
                        tracking.Add(
                            minionPositions[j], 
                            (minionPositions[j].Position.To2D() + minionPositions[i].Position.To2D()) / 2);
                    }
                }
            }

            foreach (var pos in tracking)
            {
                if (pos.Value.Distance(startPos, true) <= range * range)
                {
                    var endPos = startPos + range * (pos.Value - startPos).Normalized();

                    var count =
                        tracking.Count(pos2 => pos2.Value.Distance(startPos, endPos, true, true) <= width * width);

                    if (count >= minionCount)
                    {
                        result = endPos;
                        minionCount = count;
                        minionsList =
                            tracking.Where(pos2 => pos2.Value.Distance(startPos, endPos, true, true) <= width * width)
                                .Select(m => m.Key)
                                .ToList();
                    }
                }
            }

            return new FarmingLocation(result, minionCount, minionsList);
        }

        #endregion

        /// <summary>
        ///     The Farming Location Container.
        /// </summary>
        public class FarmingLocation
        {
            #region Constructors and Destructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="FarmingLocation" /> class.
            /// </summary>
            /// <param name="result">
            ///     The result position
            /// </param>
            /// <param name="minionCount">
            ///     The Minion Count
            /// </param>
            /// <param name="list">
            ///     The Minions List
            /// </param>
            public FarmingLocation(Vector2 result, int minionCount, List<Obj_AI_Minion> list)
            {
                this.Position = result;
                this.MinionsHit = minionCount;
                this.Minions = list;
            }

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets the minions.
            /// </summary>
            public List<Obj_AI_Minion> Minions { get; private set; }

            /// <summary>
            ///     Gets the minions hit.
            /// </summary>
            public int MinionsHit { get; private set; }

            /// <summary>
            ///     Gets the position.
            /// </summary>
            public Vector2 Position { get; private set; }

            #endregion
        }
    }
}
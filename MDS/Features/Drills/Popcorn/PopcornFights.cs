using System;
using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Popcorn
{
    // Deals both teams into fights between the smallest and largest sizes: small fights are common and each size up is rarer, and most fights are one player apart.
    // Everyone fights: whoever is left over joins an even fight first, so it stays the uneven kind, even past the largest size.
    public sealed class PopcornFights
    {
        // Each player more makes a fight 0.55 times as likely, and an uneven size counts 1.65 times.
        // Together every size is rarer than the one below it, and about two fights in three end up uneven once leftovers have joined (Editor/PopcornSimulation).
        private const double SizeDecay = 0.55;
        private const double UnevenWeight = 1.65;

        private const int Sides = 2;

        private readonly Random _random;

        public PopcornFights(Random random)
        {
            _random = random;
        }

        public List<Lineup> Deal(Lineup field, FightSize smallest, FightSize largest)
        {
            var shuffled = new Lineup(PickRules.Shuffled(field.Attackers, _random), PickRules.Shuffled(field.Defenders, _random));
            var table = new Table(shuffled, smallest.Total, largest.Total);
            var fights = new List<Fight>();
            while (TryDrawFight(table, out Fight fight)) fights.Add(fight);

            if (fights.Count == 0) fights.Add(new Fight());
            SeatLeftovers(table, fights);
            return fights.ConvertAll(each => new Lineup(each.Attackers, each.Defenders));
        }

        private static void SeatLeftovers(Table table, List<Fight> fights)
        {
            while (table.Attackers.Count + table.Defenders.Count > 0)
            {
                bool attacker = table.Attackers.Count >= table.Defenders.Count;
                Fight fight = BestFightFor(fights, attacker);
                if (attacker) fight.Attackers.Add(table.Attackers.Dequeue());
                else fight.Defenders.Add(table.Defenders.Dequeue());
            }
        }

        // An even fight first, which the newcomer makes uneven, then one where their side is behind, then the smallest; the smaller fight wins a tie.
        private static Fight BestFightFor(List<Fight> fights, bool attacker)
        {
            Fight best = fights[0];
            foreach (Fight fight in fights)
            {
                int rank = fight.RankFor(attacker);
                int bestRank = best.RankFor(attacker);
                if (rank < bestRank || (rank == bestRank && fight.Total < best.Total)) best = fight;
            }
            return best;
        }

        // Every fight the players left can still form, each weighted by its size and evenness, so a short team narrows the choice instead of ending the deal.
        private bool TryDrawFight(Table table, out Fight fight)
        {
            fight = null;
            List<Shape> shapes = ShapesThatFit(table);
            if (shapes.Count == 0) return false;

            Shape shape = Pick(shapes);
            fight = new Fight();
            for (int i = 0; i < shape.Attackers; i++) fight.Attackers.Add(table.Attackers.Dequeue());
            for (int i = 0; i < shape.Defenders; i++) fight.Defenders.Add(table.Defenders.Dequeue());
            return true;
        }

        // Each total splits as evenly as it can, and the odd player goes to the team with more players left, so the teams stay level while dealing.
        private List<Shape> ShapesThatFit(Table table)
        {
            bool attackersAhead = table.Attackers.Count > table.Defenders.Count || (table.Attackers.Count == table.Defenders.Count && _random.Next(Sides) == 0);
            var shapes = new List<Shape>();
            for (int total = table.Smallest; total <= table.Largest; total++)
            {
                int bigger = (total + 1) / Sides;
                int smaller = total / Sides;
                double weight = Math.Pow(SizeDecay, total - table.Smallest) * (bigger > smaller ? UnevenWeight : 1.0);
                AddIfFits(shapes, table, attackersAhead ? new Shape(bigger, smaller, weight) : new Shape(smaller, bigger, weight));
            }
            return shapes;
        }

        private static void AddIfFits(List<Shape> shapes, Table table, Shape shape)
        {
            bool fits = shape.Attackers >= 1 && shape.Defenders >= 1 && shape.Attackers <= table.Attackers.Count && shape.Defenders <= table.Defenders.Count;
            if (fits) shapes.Add(shape);
        }

        private Shape Pick(List<Shape> shapes)
        {
            double sum = 0.0;
            foreach (Shape shape in shapes) sum += shape.Weight;
            double roll = _random.NextDouble() * sum;
            foreach (Shape shape in shapes)
            {
                roll -= shape.Weight;
                if (roll < 0.0) return shape;
            }
            return shapes[shapes.Count - 1];
        }

        private readonly struct Shape
        {
            public Shape(int attackers, int defenders, double weight)
            {
                Attackers = attackers;
                Defenders = defenders;
                Weight = weight;
            }

            public int Attackers { get; }

            public int Defenders { get; }

            public double Weight { get; }
        }

        private sealed class Table
        {
            public Table(Lineup field, int smallest, int largest)
            {
                Attackers = new Queue<IPlayer>(field.Attackers);
                Defenders = new Queue<IPlayer>(field.Defenders);
                Smallest = smallest;
                Largest = largest;
            }

            public Queue<IPlayer> Attackers { get; }

            public Queue<IPlayer> Defenders { get; }

            public int Smallest { get; }

            public int Largest { get; }
        }

        private sealed class Fight
        {
            private const int Even = 0;
            private const int Behind = 0;
            private const int Ahead = 2;

            public List<IPlayer> Attackers { get; } = new();

            public List<IPlayer> Defenders { get; } = new();

            public int Total => Attackers.Count + Defenders.Count;

            public int RankFor(bool attacker)
            {
                int lead = attacker ? Attackers.Count - Defenders.Count : Defenders.Count - Attackers.Count;
                return lead == 0 ? Even : lead < 0 ? Behind : Ahead;
            }
        }
    }
}

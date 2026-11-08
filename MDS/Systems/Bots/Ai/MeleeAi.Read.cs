using UnityEngine;

// The read half of MeleeAi: what this bot has worked out about the opponent it is fighting right now.

namespace MDS.Systems
{
    public partial class MeleeAi
    {
        // Where a rate sits before anything has been seen, so an unknown opponent gets a genuine mix.
        private const float ReadPrior = 0.5f;

        // How far a rate moves per observation. Higher forgets faster, which is what lets the read follow a
        // player who changes gear mid-duel rather than averaging both halves of the fight together.
        private const float ReadSmoothing = 0.3f;

        // A rate is never allowed to reach 0 or 1. This is what stops the bot becoming a puzzle with a single
        // solution, and it doubles as the sampling that keeps a collapsed rate able to recover.
        private const float ReadMinChance = 0.15f;
        private const float ReadMaxChance = 0.85f;

        // A guard back up inside this counts the drop as a bait rather than a real opening.
        private const float BaitWindow = 0.45f;

        // One learned fraction: how often the thing observed came back true.
        private struct Rate
        {
            private float _value;
            private bool _seeded;

            public float Value => _seeded ? _value : ReadPrior;

            // Always eased from where the rate already sits, the prior included. Seeding the first observation
            // straight in instead would let one failed hold slam the rate to the floor on a single sample.
            public void Observe(bool hit)
            {
                _value = Mathf.Lerp(Value, hit ? 1f : 0f, ReadSmoothing);
                _seeded = true;
            }

            public void Clear() { _value = 0f; _seeded = false; }

            public bool Roll() => Random.value < Mathf.Clamp(Value, ReadMinChance, ReadMaxChance);
        }

        private int? _readOf;                // whose reads these are; a new opponent is a new duel
        private Rate _holdPaysRate;          // how often holding this opponent makes them break first
        private Rate _baitRate;              // how often their guard-drops turn out to be baits

        private bool _wasGuarding;           // their guard last tick, for edge detection
        private bool _dropPending;           // a drop is inside its window, not yet judged
        private float _dropSeenAt;           // realtime that drop happened
        private bool _takeThisDrop = true;   // the latched roll: this drop is real, throw at it
        private bool _guardSeenThisChamber;  // they raised a guard at some point during our current chamber

        // The direction arms: did the stab after a feint get through when we changed direction, and when we did
        // not. Scored on what the stab achieved rather than on whether they chase the chamber, because every
        // competent player matches the incoming direction and a read on that would sit at 1 and decide nothing.
        private Rate _switchPays;
        private Rate _keepPays;

        // The feint-length arms, asked only when the wanted depth will not fit inside the range: shorten the
        // stab and keep it aimed, or aim it wide and keep the length. Scored on the same stab as the direction
        // arms, which is noisier than one bandit alone but keeps them independent and needs no new scoring.
        private Rate _longPays;
        private Rate _shortPays;

        private float _outcomeDueAt;         // realtime the last stab's fate is settled, 0 for none pending
        private float _outcomeCommitAt;      // realtime that stab was committed
        private bool _outcomeSwitched;       // which direction arm it is scoring
        private bool _outcomeLong;           // which length arm it is scoring

        // Stepped once per tick from Decide, not from inside the hold, because a drop is judged BaitWindow later
        // and by then the bot has often thrown. Watching through our own stab is what keeps the read honest: a
        // bot that took the bait still learns it was one, so the read improves without losing exchanges to it.
        private void StepRead(CombatTracker.MeleeState enemy, float now, int targetId)
        {
            if (_readOf != targetId) { ClearRead(); _readOf = targetId; }

            if (_attackPhase == AttackPhase.Chamber && enemy.Guarding) _guardSeenThisChamber = true;

            // Their guard fell. Roll here, once, for whether to believe it: ReasonToHold runs every tick, so a
            // roll living there would re-try the same decision tens of times a second and always end up throwing.
            // Only drops during a hold count: the rest are ordinary block cycling between exchanges, and reading
            // those swamps the handful of real baits in noise.
            if (_wasGuarding && !enemy.Guarding && _attackPhase == AttackPhase.Chamber && _holdChosen)
            {
                _dropSeenAt = now;
                _dropPending = true;
                _takeThisDrop = !_baitRate.Roll();
            }

            if (_dropPending && (enemy.Guarding || now - _dropSeenAt >= BaitWindow))
            {
                _baitRate.Observe(enemy.Guarding);

                // The window closed with the guard still down, so it was a real opening whatever we guessed.
                // Without this the bot sits on a suspected bait until holdMax even once it has been proven wrong.
                if (!enemy.Guarding) _takeThisDrop = true;
                _dropPending = false;
            }

            _wasGuarding = enemy.Guarding;
            StepStabOutcome(now);
        }

        // Settles the fate of a stab thrown out of a feint. OnBlock stamps SpentAt on the attacker, us included,
        // so a SpentAt later than our commit means they blocked it.
        private void StepStabOutcome(float now)
        {
            if (_outcomeDueAt <= 0f || now < _outcomeDueAt) return;

            bool blocked = CombatTracker.TryGet(_selfId, out CombatTracker.MeleeState self)
                           && self.SpentAt > _outcomeCommitAt;

            if (_outcomeSwitched) _switchPays.Observe(!blocked);
            else _keepPays.Observe(!blocked);

            if (_outcomeLong) _longPays.Observe(!blocked);
            else _shortPays.Observe(!blocked);

            _outcomeDueAt = 0f;
        }

        // Arms the check above. Only a stab that followed at least one cancel says anything about the direction.
        private void NoteStabThrown(float now, bool afterFeint, bool switched, bool wasLong)
        {
            if (!afterFeint) return;

            _outcomeCommitAt = now;
            _outcomeSwitched = switched;
            _outcomeLong = wasLong;
            _outcomeDueAt = now + CombatTracker.LethalWindowSeconds;
        }

        // Whether a stab that will not fit the range is aimed wide and held long, or shortened and kept aimed.
        private bool PickLongFeint() => PickArm(_longPays, _shortPays);

        // Whether the re-chamber changes direction. Two arms weighted by how each has actually been doing, so a
        // player who has learned that a feint always switches stops being handed that.
        private bool PickFeintSwitch() => PickArm(_switchPays, _keepPays);

        // Two arms weighted by how each has actually been doing. Clamped so neither ever stops being tried,
        // which is both what keeps the bot unpredictable and what lets a written-off arm come back.
        private static bool PickArm(Rate yes, Rate no)
        {
            float y = Mathf.Clamp(yes.Value, ReadMinChance, ReadMaxChance);
            float n = Mathf.Clamp(no.Value, ReadMinChance, ReadMaxChance);
            return Random.value < y / (y + n);
        }

        // Closes the book on one hold: did waiting actually make them break?
        private void NoteChamberEnded(bool brokeEarly)
        {
            // A hold that runs to the cap is a hold the bot lost: it blinked first and stabbed into a raised
            // guard. One with no guard up at all gained nothing either, since there was nothing to wait out.
            if (_holdChosen) _holdPaysRate.Observe(_guardSeenThisChamber && brokeEarly);
        }

        // Whether this chamber is worth holding at all, judged on whether holding has been working. Asking
        // instead whether they guard saturates at 1 against anyone who blocks, so it never decided anything.
        private bool WillHold() => _holdMax > 0f && _holdPaysRate.Roll();

        private void ClearRead()
        {
            _holdPaysRate.Clear();
            _baitRate.Clear();
            _wasGuarding = false;
            _dropPending = false;
            _takeThisDrop = true;
            _guardSeenThisChamber = false;
            _switchPays.Clear();
            _keepPays.Clear();
            _longPays.Clear();
            _shortPays.Clear();
            _outcomeDueAt = 0f;
        }
    }
}

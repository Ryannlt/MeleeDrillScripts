using UnityEngine;

// The feint half of MeleeAi: throwing a real stab, cancelling it in flight with a block, and attacking again.

namespace MDS.Systems
{
    public partial class MeleeAi
    {
        private float _feintUntil;           // realtime the cancelling block comes down and we re-chamber
        private string _feintBlockToken;     // the block holding the cancel up
        private string _feintNextDir;        // direction the re-chamber uses, null to pick normally
        private int _feintsDone;             // cancels spent on the attack being built
        private bool _feintSwitched;         // that re-chamber changed direction, which is the arm being scored
        private float _feintCancelAt;        // realtime the stab now in flight gets cancelled
        private bool _feintLong;             // this one is aimed to miss, so it can be held out past its reach
        private float _feintAimOff;          // degrees the long feint's aim is turned so the blade goes wide
        private float _feintRange;           // range at the throw, for the probe: the cap is derived from it

        // How long after a cancel the chain still counts as in progress. Past this the bot was kept out of the
        // attack by something else, so the next chamber is a new attack rather than a resume.
        private const float FeintResumeWindow = 1f;

        // The block a feint wants up this tick, or null. Called before the guard is decided, so the cancel goes
        // out on the tick it is chosen rather than the one after.
        private string FeintBlock(float now, CombatTracker.MeleeState enemy)
        {
            if (_feintCount <= 0f) return null;

            // The chain lapsed. Without this _feintsDone stays up and the bot keeps skipping its press, cooldown
            // and range checks forever on the strength of a cancel it made seconds ago. A stab still waiting for
            // its cancel is not lapsed however long feintDepth is, or a late feint resets its own chain.
            if (_attackPhase != AttackPhase.Feint && _attackPhase != AttackPhase.Swing
                && _feintsDone > 0 && now > _feintUntil + FeintResumeWindow)
                ResetFeints();

            // Our own stab was blocked, so the cancel came too late: they have the riposte and we do not have
            // priority. Bail out of the chain, keep the recovery we were charged, and let the guard come up.
            if (_attackPhase == AttackPhase.Swing && StabWasBlocked())
            {
                _attackPhase = AttackPhase.None;
                _strikeCommittedUntil = 0f;
                _bladeLiveUntil = 0f;
                ResetFeints();
                return null;
            }

            if (_attackPhase == AttackPhase.Feint)
            {
                if (now < _feintUntil) return _feintBlockToken;

                // Guard comes down before the next attack, which is the shape a player is forced into: release
                // block, then press attack. Going straight from the block token to the strike interrupts the
                // block pose before it animates, and that pose is what sells the feint. Lowering it costs a tick
                // of attacking, which is the gap. _blockIsFeint is left set so the minimum-hold rule does not
                // re-raise the guard on the way out; the StopMeleeBlock path clears it.
                _feintBlockToken = null;
                _attackPhase = AttackPhase.None;

                // A real threat outranks the chain. Hand the guard back to the defensive path and finish the
                // feint afterwards rather than re-chambering into an incoming stab.
                if (enemy.IsThreat(now)) _blockIsFeint = false;
                return null;
            }

            // The stab is out and flying. Cancelling it here is the whole mechanic: the enemy had to respect a
            // real strike, it never lands, and killing it this way costs none of the recovery a thrown stab does.
            if (_attackPhase != AttackPhase.Swing) return null;
            if (now < _feintCancelAt) return null;

            _feintsDone++;
            _attackPhase = AttackPhase.Feint;
            _feintUntil = now + _feintDwell;

            // That swing no longer exists, so neither does anything it was owed: not the recovery, and not the
            // refusal to guard that protects a strike in flight.
            _attackCooldownUntil = 0f;
            _strikeCommittedUntil = 0f;
            _bladeLiveUntil = 0f;

            // Aimed at whatever they are threatening, so the cancel doubles as a real guard. With nothing to
            // defend against, block where the stab went.
            _feintBlockToken = DesiredBlockToken(enemy, now) ?? ("MeleeBlock" + _attackDir);
            _blockIsFeint = true;

            _feintSwitched = PickFeintSwitch();
            _feintNextDir = _feintSwitched ? Opposite(_attackDir) : _attackDir;

            if (MeleeProbe.IsProbing(_selfId))
                MeleeProbe.LogFeint(_selfId, _feintsDone, Mathf.RoundToInt(_feintCount), now - _strikeThrownAt,
                    _feintDepth, _feintRange, _feintLong, _feintAimOff, _attackDir, _feintNextDir,
                    _longPays.Value, _shortPays.Value, _switchPays.Value, _keepPays.Value);

            return _feintBlockToken;
        }

        // Whether the stab about to be released is one we mean to cancel. Asked at the execute, so the swing
        // knows from the moment it starts whether it is a feint or a real attack.
        private bool FeintOwed() => _feintCount > 0f && _feintsDone < Mathf.RoundToInt(_feintCount);

        // OnBlock stamps SpentAt on the attacker, us included, so a stamp later than the throw means they
        // stopped it. A feint that gets blocked is a feint that was not cancelled in time.
        private bool StabWasBlocked() =>
            CombatTracker.TryGet(_selfId, out CombatTracker.MeleeState self) && self.SpentAt > _strikeThrownAt;

        // How long a stab can be left out before it reaches them. The blade extends over the swing, so the
        // further away they are the longer it can be held. Past the blade's length it can never connect at all.
        private float SafeFeintDepth(float range) =>
            range >= BladeReach ? float.MaxValue : (range / BladeReach) * StrikeCommitWindow;

        // Sets up the stab about to be thrown: how long it may be held, and whether it is aimed to miss so it
        // can be held longer than its reach allows. Called at the execute, where the range is known.
        private void BeginFeintedSwing(float now, float range)
        {
            _feintRange = range;
            float safe = SafeFeintDepth(range);

            // A depth that fits needs no decision: throw it at them and pull it back in time.
            if (_feintDepth <= safe) { _feintLong = false; _feintAimOff = 0f; }
            else
            {
                // It does not fit, so either shorten it and keep it aimed, or aim it wide and keep the length.
                _feintLong = PickLongFeint();
                _feintAimOff = _feintLong ? MissAimOffset(range) : 0f;
            }

            _feintCancelAt = now + (_feintLong ? _feintDepth : Mathf.Min(_feintDepth, safe));
        }

        // Degrees to turn the aim so the blade passes beside them rather than through. Same geometry the mate
        // clamp uses, since "miss this body" is the question it already answers.
        private float MissAimOffset(float range)
        {
            float off = MateConeHalfWidth(_gateRadius, Mathf.Max(range, 1e-4f), floored: false) + _bladeMargin;

            // Turned the way the blade already leads, so it reads as a stab going wide rather than one thrown
            // at nothing.
            return BladeBearing >= 0f ? off : -off;
        }

        // The aim offset in force this tick, 0 unless a long feint's stab is out.
        private float FeintAimOffset() => _attackPhase == AttackPhase.Swing ? _feintAimOff : 0f;

        // Clears the chain. Called when an attack is thrown for real, and whenever a chamber is abandoned.
        private void ResetFeints()
        {
            _feintsDone = 0;
            _feintNextDir = null;
            _feintBlockToken = null;
            _feintCancelAt = 0f;
            _feintLong = false;
            _feintAimOff = 0f;
        }

        private static string Opposite(string dir) => dir == "High" ? "Low" : "High";
    }
}

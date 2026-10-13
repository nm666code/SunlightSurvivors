using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional hook for scripts that need to react to the freeze transition itself
/// (swap to a frozen tint, pause a timer, cancel a wind-up, ...).
/// Anything simpler should just early-out on TimeFreeze.IsFrozen.
/// </summary>
public interface IFreezable
{
    void OnTimeFreezeStart();
    void OnTimeFreezeEnd();
}

/// <summary>
/// Global time-stop state: everything holds still except the exempt roots (the player).
///
/// Four kinds of motion exist in a scene, and each needs its own treatment:
///
///   1. Physics        -- every dynamic Rigidbody gets RigidbodyConstraints.FreezeAll with
///                        its velocity cached. Constraints rather than isKinematic on purpose:
///                        EnemyScript writes linearVelocity every FixedUpdate, which Unity
///                        rejects on kinematic bodies. With FreezeAll the body stays dynamic,
///                        those writes are simply cancelled out, and its collider stays solid.
///   2. Animation      -- every Animator has its speed set to 0.
///   3. Particles      -- every playing ParticleSystem is paused.
///   4. Script-driven  -- a script moving a transform by hand (or a kinematic body) cannot be
///                        detected from out here, so it has to opt in itself:
///
///                            void Update()
///                            {
///                                if (TimeFreeze.IsFrozen) return;
///                                ...
///                            }
///
/// Time.timeScale is deliberately left alone, so the player keeps moving under normal
/// physics, and GameOverUI's own timeScale = 0 keeps working.
/// </summary>
public static class TimeFreeze
{
    public static bool IsFrozen { get; private set; }

    private struct FrozenBody
    {
        public Rigidbody body;
        public RigidbodyConstraints constraints;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
    }

    private struct FrozenAnimator
    {
        public Animator animator;
        public float speed;
    }

    private static readonly List<FrozenBody> frozenBodies = new List<FrozenBody>();
    private static readonly List<FrozenAnimator> frozenAnimators = new List<FrozenAnimator>();
    private static readonly List<ParticleSystem> pausedParticles = new List<ParticleSystem>();
    private static readonly List<IFreezable> notified = new List<IFreezable>();

    /// <summary>
    /// Freezes the scene. Everything under one of <paramref name="exemptRoots"/> is left
    /// alone -- pass the player. Nulls are ignored.
    /// </summary>
    public static void Freeze(params Transform[] exemptRoots)
    {
        if (IsFrozen) return;

        FreezeBodies(exemptRoots);
        FreezeAnimators(exemptRoots);
        FreezeParticles(exemptRoots);

        IsFrozen = true;

        // Collected after IsFrozen is set, so listeners can query it from the callback.
        NotifyFreezables(exemptRoots);
    }

    /// <summary>Restores everything Freeze() touched.</summary>
    public static void Unfreeze()
    {
        if (!IsFrozen) return;

        foreach (FrozenBody frozen in frozenBodies)
        {
            if (frozen.body == null) continue; // destroyed while frozen

            // Constraints first: a velocity set while FreezeAll is still on would be zeroed.
            frozen.body.constraints = frozen.constraints;
            frozen.body.linearVelocity = frozen.linearVelocity;
            frozen.body.angularVelocity = frozen.angularVelocity;
        }
        frozenBodies.Clear();

        foreach (FrozenAnimator frozen in frozenAnimators)
        {
            if (frozen.animator == null) continue;
            frozen.animator.speed = frozen.speed;
        }
        frozenAnimators.Clear();

        foreach (ParticleSystem system in pausedParticles)
        {
            if (system == null) continue;
            system.Play(false); // resumes from where Pause() left it
        }
        pausedParticles.Clear();

        IsFrozen = false;

        foreach (IFreezable freezable in notified)
        {
            // A MonoBehaviour destroyed mid-freeze compares equal to null through Unity's
            // object identity, so test it as a Unity object rather than a plain reference.
            if (freezable is Object unityObject && unityObject == null) continue;
            freezable.OnTimeFreezeEnd();
        }
        notified.Clear();
    }

    /// <summary>Drops all state without restoring anything, e.g. when entering Play Mode with domain reload off.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetState()
    {
        frozenBodies.Clear();
        frozenAnimators.Clear();
        pausedParticles.Clear();
        notified.Clear();
        IsFrozen = false;
    }

    // ------------------------------------------------------------------ internals

    private static void FreezeBodies(Transform[] exemptRoots)
    {
        frozenBodies.Clear();

        foreach (Rigidbody body in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
        {
            // Kinematic bodies are moved by their own scripts; those opt in via IsFrozen.
            if (body == null || body.isKinematic || IsExempt(body.transform, exemptRoots)) continue;

            frozenBodies.Add(new FrozenBody
            {
                body = body,
                constraints = body.constraints,
                linearVelocity = body.linearVelocity,
                angularVelocity = body.angularVelocity
            });

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private static void FreezeAnimators(Transform[] exemptRoots)
    {
        frozenAnimators.Clear();

        foreach (Animator animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (animator == null || IsExempt(animator.transform, exemptRoots)) continue;

            frozenAnimators.Add(new FrozenAnimator { animator = animator, speed = animator.speed });
            animator.speed = 0f;
        }
    }

    private static void FreezeParticles(Transform[] exemptRoots)
    {
        pausedParticles.Clear();

        foreach (ParticleSystem system in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            if (system == null || IsExempt(system.transform, exemptRoots)) continue;
            if (!system.isPlaying) continue; // do not resurrect one that had already finished

            // withChildren: false, because FindObjectsByType already returns the
            // children separately and each is tracked on its own.
            system.Pause(false);
            pausedParticles.Add(system);
        }
    }

    private static void NotifyFreezables(Transform[] exemptRoots)
    {
        notified.Clear();

        foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (behaviour is IFreezable freezable && !IsExempt(behaviour.transform, exemptRoots))
            {
                notified.Add(freezable);
                freezable.OnTimeFreezeStart();
            }
        }
    }

    private static bool IsExempt(Transform candidate, Transform[] exemptRoots)
    {
        if (exemptRoots == null) return false;

        foreach (Transform root in exemptRoots)
        {
            if (root != null && candidate.IsChildOf(root)) return true;
        }
        return false;
    }
}

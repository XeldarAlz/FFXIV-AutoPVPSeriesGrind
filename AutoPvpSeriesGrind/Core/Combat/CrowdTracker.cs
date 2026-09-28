using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal sealed class CrowdTracker
{
    private const float SmoothingSeconds = 0.5f;
    private const float JumpYalms = 20f;
    private const float MaxSpeedYalmsPerSecond = 12f;
    private const float StartMovingSpeed = 2.5f;
    private const float StopMovingSpeed = 1.5f;

    private Vector3? smoothed;
    private Vector3 velocity;
    private long sampledAtMs;

    public Vector3? Position => smoothed;
    public bool Moving { get; private set; }

    public Vector3 Lead(float seconds) => Moving ? velocity * seconds : Vector3.Zero;

    public void Reset()
    {
        smoothed = null;
        Stall();
    }

    public void Stall()
    {
        velocity = Vector3.Zero;
        sampledAtMs = 0;
        Moving = false;
    }

    public void Update(Vector3 centroid)
    {
        var now = Environment.TickCount64;
        if (smoothed is not { } previous || sampledAtMs == 0 || Vector3.Distance(previous, centroid) > JumpYalms)
        {
            smoothed = centroid;
            velocity = Vector3.Zero;
            sampledAtMs = now;
            Moving = false;
            return;
        }

        var seconds = (now - sampledAtMs) / 1000f;
        if (seconds <= 0f)
        {
            return;
        }

        sampledAtMs = now;
        var blend = 1f - MathF.Exp(-seconds / SmoothingSeconds);
        var next = Vector3.Lerp(previous, centroid, blend);
        var stepVelocity = (next - previous) / seconds;
        stepVelocity.Y = 0f;
        velocity = Vector3.Lerp(velocity, stepVelocity, blend);
        var speed = velocity.Length();
        if (speed > MaxSpeedYalmsPerSecond)
        {
            velocity *= MaxSpeedYalmsPerSecond / speed;
            speed = MaxSpeedYalmsPerSecond;
        }

        Moving = Moving ? speed > StopMovingSpeed : speed > StartMovingSpeed;
        smoothed = next;
    }
}

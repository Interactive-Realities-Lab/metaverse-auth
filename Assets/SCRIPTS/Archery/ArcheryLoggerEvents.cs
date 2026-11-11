using System;

public static class ArcheryLoggerEvents
{
    public static event Action<Arrow> ArrowRetrieved;
    public static event Action<Arrow> ArrowFired;

    public static void NotifyArrowRetrieved(Arrow arrow)
    {
        ArrowRetrieved?.Invoke(arrow);
    }

    public static void NotifyArrowFired(Arrow arrow)
    {
        ArrowFired?.Invoke(arrow);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class BowlingScoreManager : MonoBehaviour
{
    public event Action OnScoreChanged;
    public event Action OnRackResetRequired;
    public event Action OnGameCompleted;

    // Each inner list contains the rolls of one frame.
    private readonly List<List<int>> frames = new();

    private int currentFrame = 0;
    private int pinsStanding = 10;

    public bool GameOver { get; private set; }
    public int CurrentFrame => Mathf.Min(currentFrame + 1, 10);
    public int PinsStanding => pinsStanding;

    private void Awake()
    {
        NewGame();
    }

    public void NewGame()
    {
        frames.Clear();

        for (int i = 0; i < 10; i++)
            frames.Add(new List<int>());

        currentFrame = 0;
        pinsStanding = 10;
        GameOver = false;

        OnScoreChanged?.Invoke();
        OnRackResetRequired?.Invoke();
    }

    public void RegisterRoll(int knockedDown)
    {
        if (GameOver)
            return;

        if (knockedDown < 0 || knockedDown > pinsStanding)
        {
            Debug.LogWarning("Invalid pin count: " + knockedDown);
            return;
        }

        var frame = frames[currentFrame];
        frame.Add(knockedDown);
        pinsStanding -= knockedDown;

        bool resetRack = false;
        bool advance = false;

        if (currentFrame < 9)
        {
            if (frame[0] == 10 || frame.Count == 2)
                advance = true;
        }
        else
        {
            // Special rules for frame 10.
            if (frame.Count == 1)
            {
                if (frame[0] == 10)
                    resetRack = true;
            }
            else if (frame.Count == 2)
            {
                if (frame[0] == 10)
                {
                    // Third roll allowed after a strike.
                    if (frame[1] == 10)
                        resetRack = true;
                }
                else if (frame[0] + frame[1] == 10)
                {
                    // Third roll allowed after a spare.
                    resetRack = true;
                }
                else
                {
                    GameOver = true;
                }
            }
            else if (frame.Count == 3)
            {
                GameOver = true;
            }
        }

        if (advance)
        {
            currentFrame++;
            resetRack = true;
        }

        if (resetRack)
        {
            pinsStanding = 10;
        }

        OnScoreChanged?.Invoke();

        if (GameOver)
            OnGameCompleted?.Invoke();
        else if (resetRack)
            OnRackResetRequired?.Invoke();
    }

    public int? GetFrameScore(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= 10)
            return null;

        List<int> frame = frames[frameIndex];

        if (frame.Count == 0)
            return null;

        if (frameIndex == 9)
        {
            if (frame.Count < 2)
                return null;

            bool bonus = frame[0] == 10 ||
                         frame[0] + frame[1] == 10;

            if (bonus && frame.Count < 3)
                return null;

            int total = 0;
            foreach (int roll in frame)
                total += roll;

            return total;
        }

        int first = frame[0];

        if (first == 10)
        {
            List<int> next = GetNextRolls(frameIndex, 2);

            if (next.Count < 2)
                return null;

            return 10 + next[0] + next[1];
        }

        if (frame.Count < 2)
            return null;

        int second = frame[1];

        if (first + second == 10)
        {
            List<int> next = GetNextRolls(frameIndex, 1);

            if (next.Count < 1)
                return null;

            return 10 + next[0];
        }

        return first + second;
    }

    private List<int> GetNextRolls(int frameIndex, int count)
    {
        List<int> result = new();

        for (int i = frameIndex + 1; i < 10; i++)
        {
            foreach (int roll in frames[i])
            {
                result.Add(roll);

                if (result.Count == count)
                    return result;
            }
        }

        return result;
    }

    public int GetCumulativeScore(int frameIndex)
    {
        int total = 0;

        for (int i = 0; i <= frameIndex && i < 10; i++)
        {
            int? score = GetFrameScore(i);

            if (!score.HasValue)
                break;

            total += score.Value;
        }

        return total;
    }

    public int? GetCumulativeScoreIfKnown(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= 10)
            return null;

        int total = 0;

        for (int i = 0; i <= frameIndex; i++)
        {
            int? score = GetFrameScore(i);

            if (!score.HasValue)
                return null;

            total += score.Value;
        }

        return total;
    }

    public List<int> GetFrameRolls(int frameIndex)
    {
        return new List<int>(frames[frameIndex]);
    }

    public int GetTotalScore()
    {
        return GetCumulativeScore(9);
    }
}
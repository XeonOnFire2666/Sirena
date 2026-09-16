using System;
using System.Collections.Generic;
using System.Text;

namespace SirenaDesktop.Application.Contracts.Ai;

public interface IVideoWatchAccumulator
{
    void Upsert(string videoTitle, int secondsWatched);

    IReadOnlyDictionary<string, int> Snapshot();

    IReadOnlyDictionary<string, int> Drain();

    void Merge(IReadOnlyDictionary<string, int> data);

    int TotalAccumulatedMinutes();

    void Clear();
}
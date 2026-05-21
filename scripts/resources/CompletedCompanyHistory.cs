using Godot;
using Godot.Collections;

namespace MobArena.Scripts.Resources;

public partial class CompletedCompanyHistory : Resource
{
    public const int DefaultRecordCap = 10;

    [Signal]
    public delegate void HistoryChangedEventHandler();

    [Export]
    public int RecordCap { get; private set; } = DefaultRecordCap;

    [Export]
    public Array<CompletedCompanyRecord> Records { get; private set; } = new();

    public int TotalCompletedRuns => Records.Count;

    public bool CanPlaceRecord(int fame)
    {
        if (Records.Count < RecordCap)
            return true;

        return fame > GetLowestFame();
    }

    public bool TryAddCompletedRun(CompanyLogoData logoData, CompanyCareerData careerData, CompanyRunData runData)
    {
        return TryAddCompletedRun(logoData, careerData, runData?.Fame ?? 0);
    }

    public bool TryAddCompletedRun(CompanyLogoData logoData, CompanyCareerData careerData, int finalFame)
    {
        return TryAddRecord(CompletedCompanyRecord.Create(logoData, careerData, finalFame));
    }

    public bool TryAddRecord(CompletedCompanyRecord record)
    {
        if (record == null || !CanPlaceRecord(record.FinalFame))
            return false;

        Records.Add(record);
        SortAndTrim();
        EmitSignal(SignalName.HistoryChanged);
        return true;
    }

    public bool TryReplaceRecord(int index, CompletedCompanyRecord record)
    {
        if (record == null || !HasRecordAt(index))
            return false;

        Records[index] = record;
        SortAndTrim();
        EmitSignal(SignalName.HistoryChanged);
        return true;
    }

    public bool TryDeleteRecord(int index)
    {
        if (!HasRecordAt(index))
            return false;

        Records.RemoveAt(index);
        EmitSignal(SignalName.HistoryChanged);
        return true;
    }

    public CompletedCompanyRecord GetRecordOrNull(int index)
    {
        return HasRecordAt(index) ? Records[index] : null;
    }

    public void SortAndTrim()
    {
        for (var leftIndex = 0; leftIndex < Records.Count - 1; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < Records.Count; rightIndex++)
            {
                if (ComesBefore(Records[rightIndex], Records[leftIndex]))
                    (Records[leftIndex], Records[rightIndex]) = (Records[rightIndex], Records[leftIndex]);
            }
        }

        while (Records.Count > RecordCap)
            Records.RemoveAt(Records.Count - 1);
    }

    private bool HasRecordAt(int index)
    {
        return index >= 0 && index < Records.Count;
    }

    private int GetLowestFame()
    {
        if (Records.Count == 0)
            return 0;

        SortAndTrim();
        return Records[^1]?.FinalFame ?? 0;
    }

    private static bool ComesBefore(CompletedCompanyRecord left, CompletedCompanyRecord right)
    {
        var leftFame = left?.FinalFame ?? 0;
        var rightFame = right?.FinalFame ?? 0;
        if (leftFame != rightFame)
            return leftFame > rightFame;

        return string.Compare(left?.CompanyName, right?.CompanyName, System.StringComparison.Ordinal) < 0;
    }
}

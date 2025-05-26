using BehaviorDesigner.Runtime;

[System.Serializable]
public class SharedDayPart : SharedVariable<DayPart>
{
    public static implicit operator SharedDayPart(DayPart value) { return new SharedDayPart { Value = value }; }
}
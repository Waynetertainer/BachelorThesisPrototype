
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

[JsonConverter(typeof(StringEnumConverter))]
public enum FingerPose
{
    Straight,
    Half,
    Full
}

public enum Room
{
    Command,
    Shield,
    Machine
}

public enum State
{
    Start,
    Room, 
    Problem,
    ProblemEnd,
    TrainingSel,
    Training,
    TrainingEnd,
    Victory
}

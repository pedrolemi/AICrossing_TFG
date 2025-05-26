using System.Collections.Generic;

public struct PlayerAnswer
{
    public string Text { get; set; }
    public int FriendshipPoints { get; set; }
}

public struct NPCQuestion
{
    public string Question { get; set; }
    public List<PlayerAnswer> Answers { get; set; }
}

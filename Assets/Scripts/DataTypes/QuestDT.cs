using System;

[Serializable]
public class Item
{
    public int Id { get; set; }
    public int Amount { get; set; }
    public BaseItem baseItem { get; set; }
}

[Serializable]
public class Reward
{
    public int FriendshipPoints { get; set; }
}


[Serializable]
public class Quest
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string QuestGiverName { get; set; }

    public Item Item { get; set; }
    public Reward Reward { get; set; }


    // Delivery/Request
    public string ItemReceiverName { get; set; } = "";
    // Retrieval
    public string ItemProviderName { get; set; } = "";
    // LostItem
    public string LocationName { get; set; } = "";

    public NPCQuestHandler QuestHandler { get; set; } = null;

}
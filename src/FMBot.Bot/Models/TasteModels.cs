namespace FMBot.Bot.Models
{
    public class TasteModels
    {
        public string Description { get; set; }

        public string LeftDescription { get; set; }

        public string RightDescription { get; set; }
    }

    public class TasteTwoUserModel
    {
        public string Artist { get; set; }

        public int OwnPlaycount { get; set; }

        public int OtherPlaycount { get; set; }
    }

    public enum TasteType
    {
        FullEmbed = 1,
        Table = 2
    }
}
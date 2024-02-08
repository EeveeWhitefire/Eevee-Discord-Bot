namespace EeveeBot.Interfaces
{
    public interface IEeveeEmote
    {
        ulong Id { get; }
        ulong AdderId { get; }
        ulong GuildId { get; }
        string Name { get; }
        string RelativePath { get; }
        bool IsAnimated { get; }
        string Url { get; }
    }
}

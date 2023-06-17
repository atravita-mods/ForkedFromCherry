using System;
using System.Linq;

using NetEscapades.EnumGenerators;

using StardewValley;
using StardewValley.GameData.Characters;

namespace ShopTileFramework.src.Queries;
internal static class GameStateQueries
{
    internal static bool HasSeenAnyEvent(string[] query, GameLocation location, Farmer player, Item targetItem, Item inputItem, Random random)
    {
        if (!ArgUtility.TryGet(query, 1, out var playerKey, out var error) || !ArgUtility.TryGet(query, 2, out _, out error))
        {
            return GameStateQuery.Helpers.ErrorResult(query, error);
        }
        return new ArraySegment<string>(query, 2, query.Length - 2).Any(evt => GameStateQuery.Helpers.WithPlayer(player, playerKey, target => target.eventsSeen.Contains(evt)));
    }

    internal static bool HasFriendshipPoints(string[] query, GameLocation location, Farmer player, Item targetItem, Item inputItem, Random random)
    {
        if (!ArgUtility.TryGet(query, 1, out var playerKey, out var error)
            || !ArgUtility.TryGet(query, 2, out var npcName, out error)
            || !ArgUtility.TryGetInt(query, 3, out var friendshipPoints, out error))
        {
            return GameStateQuery.Helpers.ErrorResult(query, error);
        }

        _ = NPCTypeExtensions.TryParse(npcName, out NPCType type, ignoreCase: true);

        switch (type)
        {
            case NPCType.Any:
            {
                return GameStateQuery.Helpers.WithPlayer(
                    player,
                    playerKey,
                    target =>
                    {
                        foreach (var (friend, friendship) in player.friendshipData.Pairs)
                        {
                            // double checking the npc hasn't been, like, removed from the game or something.
                            if (friendship.Points >= friendshipPoints && Game1.characterData.ContainsKey(friend))
                            {
                                return true;
                            }
                        }
                        return false;
                    });
            }
            case NPCType.AnyDateable:
            {
                return GameStateQuery.Helpers.WithPlayer(
                    player,
                    playerKey,
                    target =>
                    {
                        foreach (var (friend, friendship) in player.friendshipData.Pairs)
                        {
                            // double checking the npc hasn't been, like, removed from the game or something.
                            if (friendship.Points >= friendshipPoints && Game1.characterData.TryGetValue(friend, out var characterData) && characterData.CanBeRomanced)
                            {
                                return true;
                            }
                        }
                        return false;
                    });
            }
            default:
            {
                return GameStateQuery.Helpers.WithPlayer(
                player,
                playerKey,
                target => target.friendshipData.TryGetValue(npcName, out var friendship) && friendship.Points >= friendshipPoints);
            }
        }
    }

    internal static bool IsNPCInPlayerLocation(string[] query, GameLocation location, Farmer player, Item targetItem, Item inputItem, Random random)
    {
        if (!ArgUtility.TryGet(query, 1, out var playerKey, out var error) || !ArgUtility.TryGet(query, 2, out var npcName, out error))
        {
            return GameStateQuery.Helpers.ErrorResult(query, error);
        }
        _ = NPCTypeExtensions.TryParse(npcName, out NPCType type, ignoreCase: true);

        switch (type)
        {
            case NPCType.Any:
            {
                return GameStateQuery.Helpers.WithPlayer(
                    player,
                    playerKey,
                    target => target.currentLocation?.characters.Any(npc => npc.isVillager()) == true);
            }
            case NPCType.AnyDateable:
            {
                return GameStateQuery.Helpers.WithPlayer(
                    player,
                    playerKey,
                    target => target.currentLocation?.characters.Any(npc => npc.isVillager() && npc.datable.Value) == true);
            }
            default:
            {
                return GameStateQuery.Helpers.WithPlayer(
                    player,
                    playerKey,
                    target => target.currentLocation?.characters.Any(npc => npc.isVillager() && npc.Name == npcName) == true);
            }
        }
    }
}

[EnumExtensions]
internal enum NPCType
{
    Specific, // first so it's default.
    Any,
    AnyDateable,
}
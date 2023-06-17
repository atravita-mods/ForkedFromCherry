using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

using StardewModdingAPI;

using StardewValley;

namespace ShopTileFramework.src.Utility;
internal static class EventPreconditionToGSQ
{
    delegate bool EventPreconditionToGSQDelegate(ArraySegment<string> args, out string? GSQ);

    private static ConcurrentDictionary<string, EventPreconditionToGSQDelegate> map = new();

    internal static void Populate()
    {
        foreach (var method in typeof(Handlers).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            try
            {
                EventPreconditionToGSQDelegate del = method.CreateDelegate<EventPreconditionToGSQDelegate>();
                map[method.Name] = del;
            }
            catch (Exception ex)
            {
                ModEntry.monitor.Log(ex.ToString(), LogLevel.Error);
            }
        }
    }

    internal static string? TryGetMatchingGSQ(string eventPrecondition)
    {
        string[] parts = eventPrecondition.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || !map.TryGetValue(parts[0], out var @delegate))
        {
            ModEntry.monitor.LogOnce($"'{eventPrecondition}' cannot be matched to a GSQ");
            return null;
        }
        if (!@delegate(new ArraySegment<string>(parts, 1, parts.Length - 1), out var GSQ))
        {
            ModEntry.monitor.LogOnce($"'{eventPrecondition}' has matching GSQ but could not be parsed for some reason.");
            return null;
        }
        return GSQ;
    }

    internal static class Handlers
    {
        private static bool A(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1)
            {
                GSQ = null;
                return false;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_HAS_CONVERSATION_TOPIC)} Current {args[0]}";
            return true;
        }

        private static bool F(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            GSQ = $"!{nameof(GameStateQuery.DefaultResolvers.IS_FESTIVAL_DAY)}";
            return true;
        }

        // there are no festivals in the next N days.
        private static bool U(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1 || !int.TryParse(args[0], out int days))
            {
                GSQ = null;
                return false;
            }

            StringBuilder sb = new();
            for (int i = 0; i < days; i++)
            {
                sb.Append($"!{nameof(GameStateQuery.DefaultResolvers.IS_FESTIVAL_DAY)} ").Append(i).Append(',');
            }

            GSQ = sb.ToString(0, sb.Length - 1);
            return true;
        }

        private static bool d(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count == 0)
            {
                GSQ = null;
                return false;
            }

            GSQ = string.Join(',', args.Select(static a => $"!{nameof(GameStateQuery.DefaultResolvers.DAY_OF_WEEK)} {a}"));
            return true;
        }

        private static bool r(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1 || !double.TryParse(args[0], out double val))
            {
                GSQ = null;
                return false;
            }

            // to match previous behavior of shops only refreshing their randoms once per day.
            // we'll use SYNCED_RANDOM here, and give a random long string as a key.
            // hopefully no collisions?
            byte[] buffer = new byte[64];
            Random.Shared.NextBytes(buffer);
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.SYNCED_RANDOM)} day {Convert.ToBase64String(buffer)} {val:F2}";
            return true;
        }
        
        // v <- if NPC is visible has no matching GSQ.

        private static bool w(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1)
            {
                GSQ = null;
                return false;
            }
            switch (args[0])
            {
                case "rainy":
                    GSQ = $"ANY \"{nameof(GameStateQuery.DefaultResolvers.WEATHER)} Here Rain\" \"{nameof(GameStateQuery.DefaultResolvers.WEATHER)} Here Storm\"";
                    return true;
                case "sunny":
                    GSQ = $"!{nameof(GameStateQuery.DefaultResolvers.WEATHER)} Here Rain, !{nameof(GameStateQuery.DefaultResolvers.WEATHER)} Here Storm";
                    return true;
                default:
                    GSQ = null;
                    return false;
            }
        }

        // y precondition: y1 means MUST be y1, any number means "at least".
        private static bool y(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1 || !int.TryParse(args[0], out int year))
            {
                GSQ = null;
                return false;
            }

            if (year == 1)
            {
                GSQ = $"{nameof(GameStateQuery.DefaultResolvers.YEAR)} 1 1";
                return true;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.YEAR)} {year}";
            return true;
        }

        // Season.
        private static bool z(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1 || !StardewValley.Utility.TryParseEnum<Season>(args[0], out Season season))
            {
                GSQ = null;
                return false;
            }
            GSQ = $"!{nameof(GameStateQuery.DefaultResolvers.SEASON)} {args[0]}";
            return true;
        }

        // B - current player has a double bed, no relevant GSQ.

        // D - dating.
        private static bool D(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1)
            {
                GSQ = null;
                return false;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_IS_DATING)} Current {args[0]}";
            return true;
        }

        // is Joja complete?
        private static bool J(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            GSQ = nameof(GameStateQuery.DefaultResolvers.IS_JOJA_MART_COMPLETE);
            return true;
        }

        // this is a departure from previous code, because L used to never evaluate true.
        // because STF/EPU would use the Farm, not Farmhouse, as the location to check.
        private static bool L(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_FARMHOUSE_UPGRADE)} Current 2";
            return true;
        }

        private static bool M(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1 || !int.TryParse(args[0], out int amount))
            {
                GSQ = null;
                return false;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_CURRENT_MONEY)} {amount}";
            return true;
        }

        // Todo - crosscheck?
        private static bool N(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1 || !int.TryParse(args[0], out int amount))
            {
                GSQ = null;
                return false;
            }
            GSQ = $"!{nameof(GameStateQuery.DefaultResolvers.WORLD_STATE_FIELD)} GoldenWalnutsFound {amount}";
            return true;
        }

        private static bool O(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1)
            {
                GSQ = null;
                return false;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_IS_MARRIED)} Current {args[0]}";
            return true;
        }

        private static bool S(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1 || !int.TryParse(args[0], out int note))
            {
                GSQ = null;
                return false;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_HAS_SECRET_NOTE)} {note}";
            return true;
        }

        // a wasn't previously useful and there's no matching GSQ

        // b - number of times reached bottom of mines -- has no matching GSQ

        // c - number of inventory slots open - has no matching GSQ

        private static bool e(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count < 1)
            {
                GSQ = null;
                return false;
            }
            StringBuilder sb = new("ANY");
            foreach (string arg in args)
            {
                sb.Append(' ')
                  .Append('"')
                  .Append(nameof(GameStateQuery.DefaultResolvers.PLAYER_HAS_SEEN_EVENT))
                  .Append(" Current ")
                  .Append(arg)
                  .Append('"');
            }
            GSQ = sb.ToString();
            return true;
        }

        // friendship.

        private static bool g(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count == 1)
            {
                if (args[0].Equals("male", StringComparison.OrdinalIgnoreCase))
                {
                    GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_GENDER)} Male";
                    return true;
                }
                else if (args[0].Equals("female", StringComparison.OrdinalIgnoreCase))
                {
                    GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_GENDER)} Female";
                    return true;
                }
            }
            GSQ = null;
            return false;
        }

        // h - current player does not already have a pet, and their preference matches "cat" or "dog" and this is jank af in multiplayer.

        // i - player has specified item in inventory - this probably never worked right anyways.

        private static bool j(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1 || !int.TryParse(args[0], out int val))
            {
                GSQ = null;
                return false;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.DAYS_PLAYED)} {val + 1}";
            return true;
        }

        private static bool k(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count == 0)
            {
                GSQ = null;
                return false;
            }
            GSQ = string.Join(',', args.Select(x => $"!{nameof(GameStateQuery.DefaultResolvers.PLAYER_HAS_SEEN_EVENT)} {x}"));
            return true;
        }

        private static bool l(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (n(args, out string? val))
            {
                GSQ = "!" + val;
                return true;
            }
            GSQ = null;
            return false;
        }

        private static bool m(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1 || int.TryParse(args[0], out int val))
            {
                GSQ = null;
                return false;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_MONEY_EARNED)} {val}";
            return true;
        }

        private static bool n(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 1)
            {
                GSQ = null;
                return false;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_HAS_FLAG)} {args[0]}";
            return true;
        }

        private static bool o(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (O(args, out string? val))
            {
                GSQ = "!" + val;
                return true;
            }
            GSQ = null;
            return false;
        }

        // p - npc is in the player's current location.

        private static bool q(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count == 0)
            {
                GSQ = null;
                return false;
            }
            GSQ = string.Join(',', args.Select(x => $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_HAS_DIALOGUE_ANSWER)} {x}"));
            return true;
        }

        private static bool s(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 2 || !int.TryParse(args[1], out int count))
            {
                GSQ = null;
                return false;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.PLAYER_SHIPPED_BASIC_ITEM)} Current {args[0]} {count}";
            return true;
        }

        private static bool t(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count != 2 || !int.TryParse(args[0], out int min) || !int.TryParse(args[1], out int max))
            {
                GSQ = null;
                return false;
            }
            GSQ = $"{nameof(GameStateQuery.DefaultResolvers.TIME)} {min:D4} {max:D4}";
            return true;
        }

        private static bool u(ArraySegment<string> args, [NotNullWhen(true)] out string? GSQ)
        {
            if (args.Count == 0)
            {
                GSQ = null;
                return false;
            }

            StringBuilder sb = new("ANY");
            foreach (string x in args)
            {
                if (!int.TryParse(x, out _))
                {
                    GSQ = null;
                    return false;
                }
                sb.Append(' ').Append('"').Append(nameof(GameStateQuery.DefaultResolvers.DAY_OF_MONTH)).Append(' ').Append(x).Append('"');
            }
            GSQ = sb.ToString();
            return true;
        }

        // x is not useful

        // doing the host player ones later.
    }
}

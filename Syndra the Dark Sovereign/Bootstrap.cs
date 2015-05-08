using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using LeagueSharp;
using LeagueSharp.Common;

namespace Syndra
{
    public class Bootstrap
    {
        private static void Main(string[] args)
        {
            if (args != null)
            {
                CustomEvents.Game.OnGameLoad += eventArgs =>
                {
                    if (ObjectManager.Player.ChampionName == GetCurrentNamespace())
                    {
                        Menu menu = null;
                        try
                        {
                            menu = new Menu("Syndra the Dark Sovereign", "l33t.stdv", true);
                        }
                        catch (Exception)
                        {
                            // Ignored .ctor failure, if loaded to an outdated/updated reference not matching supported one.
                        }

                        var pointer = new EntryPoint(ObjectManager.Player, menu);
                        pointer.RegisterCallbacks();
                        pointer.RegisterMenu();
                        EntryPoint.RegisterSpells();

                        if (EntryPoint.Menu.Item("l33t.stds.misc.welcomesound").GetValue<bool>())
                        {
                            Audio.PlaySound(Sounds.Welcome);
                        }
                    }
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetCurrentNamespace()
        {
            var dType = Assembly.GetCallingAssembly().EntryPoint.DeclaringType;
            return dType == null ? typeof(Bootstrap).Namespace : dType.Namespace;
        }
    }
}
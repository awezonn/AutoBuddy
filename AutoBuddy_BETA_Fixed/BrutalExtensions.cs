using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using AutoBuddy.Humanizers;
using AutoBuddy.Properties;
using AutoBuddy.Utilities;
using AutoBuddy.Utilities.AutoShop;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace AutoBuddy
{
    internal static class BrutalExtensions
    {
        private static List<Champion> IntroBots = new List<Champion>
        {
            Champion.Ryze,
            Champion.Nasus,
            Champion.Ezreal,
            Champion.Alistar,
            Champion.Galio
        };

        public static string GetGameMode()
        {
            /*if (Player.HasBuff("Get this buff name first"))
            {
                return "URF";
            }*/
            if (Player.Instance.HasItem(3630, 3633, 3645, 3649))
            {
                return "Nexus Siege";
            }
            if (Player.Instance.HasItem(3462))
            {
                return "Definitely not Dominion";
            }
            if (Game.Type == GameType.KingPoro)
            {
                return "Legend of the Poro King";
            }
            if (Player.Instance.HasItem(3460, 3461))
            {
                return "Ascension";
            }
            if (EntityManager.Heroes.Enemies.Count == 6 || EntityManager.Heroes.Allies.Count == 6)
            {
                return "Hexakill";
            }
            if (EntityManager.Heroes.Enemies.All(x => x.IsBot == 1) ||
                EntityManager.Heroes.Enemies.All(x => x.Name.ToLower().EndsWith(" bot")))
            {
                if (EntityManager.Heroes.Enemies.Any(x => x.SkinId != 0))
                {
                    return "Medium Bots";
                }
                if (EntityManager.Heroes.Enemies.All(x => IntroBots.Contains(x.Hero)))
                {
                    return "Intro Bots";
                }
                return "Beginner Bots";
            }
            var hero = Champion.Unknown;
            var first = true;
            foreach (var ally in EntityManager.Heroes.Allies)
            {
                if (first)
                {
                    hero = ally.Hero;
                    first = false;
                }
                else
                {
                    if (ally.Hero == hero)
                    {
                        return "One for All";
                    }
                    break;
                }
            }
            return Game.Type.ToString();
        }

        public static byte[] GetResourceForGame()
        {
            if (Game.MapId == GameMapId.TwistedTreeline)
            {
                return Resources.NavGraphTwistedTreeline;
            }
            if (Game.MapId == GameMapId.HowlingAbyss)
            {
                return Resources.NavGraphHowlingAbyss;
            }
            if (Game.MapId == GameMapId.CrystalScar)
            {
                if (GetGameMode() == "Ascension")
                {
                    return Resources.NavGraphCrystalScar_Ascension;
                }
                if (GetGameMode() == "Definitely not Dominion")
                {
                    return Resources.NavGraphCrystalScar_DefinitelyNotDominion;
                }
            }
            if (Game.MapId == GameMapId.SummonersRift)
            {
                if (GetGameMode() == "Nexus Siege")
                {
                    if (Player.Instance.Team == GameObjectTeam.Order)
                    {
                        return Resources.NavGraphSummonersRift_NexusSiege_Blue;
                    }
                    return Resources.NavGraphSummonersRift_NexusSiege_Red;
                }
                if (Player.Instance.Team == GameObjectTeam.Order)
                {
                    return Resources.NavGraphSummonersRift_Blue;
                }
                return Resources.NavGraphSummonersRift_Red;
            }
            return Resources.NavGraphSummonersRiftOld;
        }

        public static Lane GetLane(this Obj_AI_Minion min)
        {
            try
            {
                if (min.Name == null || min.Name.Length < 13) return Lane.Unknown;
                if (min.Name[12] == '0') return Lane.Bot;
                if (min.Name[12] == '1') return Lane.Mid;
                if (min.Name[12] == '2') return Lane.Top;
            }
            catch (Exception e)
            {
                Console.WriteLine("GetLane:" + e.Message);
            }
            return Lane.Unknown;
        }

        public static Lane GetLane(this Obj_AI_Turret tur)
        {
            if (tur.Name.EndsWith("Shrine_A")) return Lane.Spawn;
            if (tur.Name.EndsWith("C_02_A") || tur.Name.EndsWith("C_01_A")) return Lane.HQ;
            if (tur.Name == null || tur.Name.Length < 12) return Lane.Unknown;
            if (tur.Name[10] == 'R') return Lane.Bot;
            if (tur.Name[10] == 'C') return Lane.Mid;
            if (tur.Name[10] == 'L') return Lane.Top;
            return Lane.Unknown;
        }

        public static int GetWave(this Obj_AI_Minion min)
        {
            if (min.Name == null || min.Name.Length < 17) return 0;
            int result;
            try
            {
                result = Int32.Parse(min.Name.Substring(14, 2));
            }
            catch (FormatException)
            {
                result = 0;
                Console.WriteLine("GetWave error, minion name: " + min.Name);
            }
            return result;
        }

        public static Vector3 RotatedAround(this Vector3 rotated, Vector3 around, float angle)
        {
            var s = Math.Sin(angle);
            var c = Math.Cos(angle);

            var ret = new Vector2(rotated.X - around.X, rotated.Y - around.Y);

            var xnew = ret.X*c - ret.Y*s;
            var ynew = ret.X*s + ret.Y*c;

            ret.X = (float) xnew + around.X;
            ret.Y = (float) ynew + around.Y;

            return ret.To3DWorld();
        }

        public static Vector3 Randomized(this Vector3 vec, float min = -300, float max = 300)
        {
            return new Vector3(vec.X + RandGen.r.NextFloat(min, max), vec.Y + RandGen.r.NextFloat(min, max), vec.Z);
        }

        public static Obj_AI_Turret GetNearestTurret(this Vector3 pos, bool enemy = true)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>()
                    .Where(tur => tur.Health > 0 && tur.IsAlly ^ enemy)
                    .OrderBy(tur => tur.Distance(pos))
                    .First();
        }

        public static Obj_AI_Turret GetNearestTurret(this Obj_AI_Base unit, bool enemy = true)
        {
            return unit.Position.GetNearestTurret(enemy);
        }

        public static bool IsVisible(this Obj_AI_Base unit)
        {
            return !unit.IsDead() && unit.IsHPBarRendered;
        }

        public static bool IsDead(this Obj_AI_Base unit)
        {
            return unit.Health <= 0;
        }

        public static float HealthPercent(this Obj_AI_Base unit)
        {
            return unit.Health/unit.MaxHealth*100f;
        }

        public static float ManaPercent(this Obj_AI_Base unit)
        {
            return unit.Mana/unit.MaxMana*100f;
        }

        public static string Concatenate<T>(this IEnumerable<T> source, string delimiter)
        {
            var s = new StringBuilder();
            var first = true;
            foreach (var t in source)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    s.Append(delimiter);
                }
                s.Append(t);
            }
            return s.ToString();
        }

        public static List<int> AllIndexesOf(string str, string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            var indexes = new List<int>();
            for (var index = 0;; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        public static string GetResponseText(this string address)
        {
            var request = (HttpWebRequest)WebRequest.Create(address);
            request.Proxy = null;
            using (var response = (HttpWebResponse) request.GetResponse())
            {
                var encoding = Encoding.GetEncoding(response.CharacterSet);

                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream, encoding))
                    return reader.ReadToEnd();
            }
        }

        public static string Post(this string address, Dictionary<string, string> data )
        {
            var request = (HttpWebRequest)WebRequest.Create(address);
            request.Method = "POST";
            request.Proxy = null;
            request.ContentType = "application/x-www-form-urlencoded";
            var postData = data.Aggregate("", (current, pair) => current + pair.Key+ "=" + pair.Value.ToBase64URL() + "&");
            postData = postData.Substring(0, postData.Length - 1);
            
            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentLength = byteArray.Length;

            var dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();


            using (var response = (HttpWebResponse)request.GetResponse())
            {
                var encoding = Encoding.GetEncoding(response.CharacterSet);

                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream, encoding))
                    return reader.ReadToEnd();
            }
        }

        public static string ToBase64URL(this string toEncode)
        {
            var toEncodeAsBytes
                  = Encoding.Default.GetBytes(toEncode);
            var returnValue
                  = Convert.ToBase64String(toEncodeAsBytes);
            return HttpUtility.UrlEncode(returnValue);
        }

        public static bool IsHealthlyConsumable(this IItem i)
        {
            return i.Id == 2003 || i.Id == 2009 || i.Id == 2010;
        }

        public static bool IsHealthlyConsumable(this ItemId i)
        {
            return (int)i == 2003 || (int)i == 2009 || (int)i == 2010;
        }

        public static bool IsElixir(this IItem i)
        {
            return i.Id == 2138 || i.Id == 2139 || i.Id == 2140;
        }

        public static bool IsElixir(this ItemId i)
        {
            return (int)i == 2138 || (int)i == 2139 || (int)i == 2140;
        }

        public static bool IsHPotion(this ItemId i)
        {
            return (int) i == 2003 || (int) i == 2009 || (int) i == 2010 || (int) i == 2031;
        }

        public static int GetItemSlot(this IItem it)
        {
            BrutalItemInfo.GetItemSlot(it.Id);
            return -1;
        }

        public static float GetDmg(this SpellSlot slot)
        {
            return 1;
        }

        public static Vector3 Away(this Vector3 myPos, Vector3 threatPos, float range, float add = 200,
            float resolution = 40)
        {
            var r = threatPos.Extend(myPos, range).To3D();
            var re = threatPos.Extend(myPos, range + add).To3D();
            if (!NavMesh.GetCollisionFlags(re).HasFlag(CollisionFlags.Wall)) return r;
            for (var i = 1; i < resolution; i++)
            {
                if (
                    !NavMesh.GetCollisionFlags(re.RotatedAround(threatPos, 3.14f/resolution*i))
                        .HasFlag(CollisionFlags.Wall)) return r.RotatedAround(threatPos, 3.14f/resolution*i);
                if (
                    !NavMesh.GetCollisionFlags(re.RotatedAround(threatPos, 3.14f/resolution*i*-1f))
                        .HasFlag(CollisionFlags.Wall)) return r.RotatedAround(threatPos, 3.14f/resolution*i*-1f);
            }
            return r;
        }

        public static Vector3 Copy(this Vector3 from)
        {
            return new Vector3(from.X, from.Y, from.Z);
        }

        public static Vector3[] Copy(this Vector3[] from)
        {
            var ar = new Vector3[from.Length];
            for (var i = 0; i < ar.Length; i++)
            {
                ar[i] = from[i].Copy();
            }
            return ar;

        }
    }
}

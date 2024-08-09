using System;
using System.Text.RegularExpressions;

namespace PoeTradeSearch
{
    internal static class Request
    {
        public static (string, bool) CreateJson(ItemOption itemOptions, bool useSaleType, FilterData[] mFilter, ParserData mParser, ConfigData mConfig)
        {
            string BeforeDayToString(int day)
            {
                if (day < 1)
                    return "any";
                else if (day < 3)
                    return "1day";
                else if (day < 7)
                    return "3days";
                else if (day < 14)
                    return "1week";
                return "2weeks";
            }

            JsonData jsonData = new JsonData
            {
                Query = new q_Query()
            };
            q_Query JQ = jsonData.Query;

            jsonData.Sort.Price = "asc";

            byte lang_index = (byte)itemOptions.LangIndex;
            string Inherit = itemOptions.Inherits.Length > 0 ? itemOptions.Inherits[0] : "any";

            JQ.Name = itemOptions.Name;
            JQ.Type = itemOptions.Type;

            JQ.Stats = new q_Stats[0];
            JQ.Status.Option = "online";

            JQ.Filters.Type.Filters.Category.Option = Inherit == "jewel" ? Inherit : itemOptions.Inherits.Join('.');
            JQ.Filters.Type.Filters.Rarity.Option = itemOptions.RarityAt > 0 ? mParser.Rarity.Entries[itemOptions.RarityAt - 1].Id : "any";
            //JQ.Filters.Type.Filters.Rarity.Option = itemOptions.RarityAt > 0 ? RS.lRarity.ElementAt(itemOptions.RarityAt - 1).Key.ToLower() : "any";

            JQ.Filters.Trade.Disabled = mConfig.Options.SearchBeforeDay == 0;
            JQ.Filters.Trade.Filters.Indexed.Option = BeforeDayToString(mConfig.Options.SearchBeforeDay);
            JQ.Filters.Trade.Filters.SaleType.Option = useSaleType ? "priced" : "any";
            JQ.Filters.Trade.Filters.Price.Option = "chaos_divine";
            JQ.Filters.Trade.Filters.Price.Max = 99999;
            JQ.Filters.Trade.Filters.Price.Min = itemOptions.PriceMin > 0 ? itemOptions.PriceMin : 99999;

            JQ.Filters.Socket.Disabled = itemOptions.ChkSocket != true;

            JQ.Filters.Socket.Filters.Links.Min = itemOptions.LinkMin;
            JQ.Filters.Socket.Filters.Links.Max = itemOptions.LinkMax;
            JQ.Filters.Socket.Filters.Sockets.Min = itemOptions.SocketMin;
            JQ.Filters.Socket.Filters.Sockets.Max = itemOptions.SocketMax;

            JQ.Filters.Misc.Filters.Quality.Min = itemOptions.ChkQuality == true ? itemOptions.QualityMin : 99999;
            JQ.Filters.Misc.Filters.Quality.Max = itemOptions.ChkQuality == true ? itemOptions.QualityMax : 99999;

            JQ.Filters.Misc.Filters.Ilvl.Min = itemOptions.ChkLv != true || Inherit == "gem" || Inherit == "map" ? 99999 : itemOptions.LvMin;
            JQ.Filters.Misc.Filters.Ilvl.Max = itemOptions.ChkLv != true || Inherit == "gem" || Inherit == "map" ? 99999 : itemOptions.LvMax;
            JQ.Filters.Misc.Filters.Gem_level.Min = itemOptions.ChkLv == true && Inherit == "gem" ? itemOptions.LvMin : 99999;
            JQ.Filters.Misc.Filters.Gem_level.Max = itemOptions.ChkLv == true && Inherit == "gem" ? itemOptions.LvMax : 99999;
            JQ.Filters.Misc.Filters.AlternateQuality.Option = Inherit == "gem" && itemOptions.AltQuality > 0 ? itemOptions.AltQuality.ToString() : "any";

            JQ.Filters.Misc.Filters.Shaper.Option = Inherit != "map" && (itemOptions.Influence1 == 1 || itemOptions.Influence2 == 1) ? "true" : "any";
            JQ.Filters.Misc.Filters.Elder.Option = Inherit != "map" && (itemOptions.Influence1 == 2 || itemOptions.Influence2 == 2) ? "true" : "any";
            JQ.Filters.Misc.Filters.Crusader.Option = Inherit != "map" && (itemOptions.Influence1 == 3 || itemOptions.Influence2 == 3) ? "true" : "any";
            JQ.Filters.Misc.Filters.Redeemer.Option = Inherit != "map" && (itemOptions.Influence1 == 4 || itemOptions.Influence2 == 4) ? "true" : "any";
            JQ.Filters.Misc.Filters.Hunter.Option = Inherit != "map" && (itemOptions.Influence1 == 5 || itemOptions.Influence2 == 5) ? "true" : "any";
            JQ.Filters.Misc.Filters.Warlord.Option = Inherit != "map" && (itemOptions.Influence1 == 6 || itemOptions.Influence2 == 6) ? "true" : "any";

            JQ.Filters.Misc.Filters.Synthesis.Option = Inherit != "map" && itemOptions.Synthesis == true ? "true" : "any";
            JQ.Filters.Misc.Filters.Corrupted.Option = itemOptions.Corrupt == 1 ? "true" : (itemOptions.Corrupt == 2 ? "false" : "any");

            JQ.Filters.Heist.Filters.HeistObjective.Option = "any";
            if (Inherit == "heistmission" && itemOptions.AltQuality > 0)
            {
                string[] tmp = new string[] { "moderate", "high", "precious", "priceless" };
                JQ.Filters.Heist.Filters.HeistObjective.Option = tmp[itemOptions.AltQuality - 1];
            }

            JQ.Filters.Heist.Disabled = JQ.Filters.Heist.Filters.HeistObjective.Option == "any";

            JQ.Filters.Misc.Disabled = !(
                itemOptions.ChkQuality == true || itemOptions.Corrupt > 0 || (Inherit == "gem" && itemOptions.AltQuality > 0)
                || (Inherit != "map" && (itemOptions.Influence1 != 0 || itemOptions.ChkLv == true || itemOptions.Synthesis == true))
            );

            JQ.Filters.Map.Disabled = !(
                Inherit == "map" && (itemOptions.AltQuality > 0 || itemOptions.ChkLv == true || itemOptions.Synthesis == true || itemOptions.Influence1 != 0)
            );

            JQ.Filters.Map.Filters.Tier.Min = itemOptions.ChkLv == true && Inherit == "map" ? itemOptions.LvMin : 99999;
            JQ.Filters.Map.Filters.Tier.Max = itemOptions.ChkLv == true && Inherit == "map" ? itemOptions.LvMax : 99999;
            JQ.Filters.Map.Filters.Shaper.Option = Inherit == "map" && itemOptions.Influence1 == 1 ? "true" : "any";
            JQ.Filters.Map.Filters.Elder.Option = Inherit == "map" && itemOptions.Influence1 == 2 ? "true" : "any";
            JQ.Filters.Map.Filters.Blight.Option = Inherit == "map" && itemOptions.Synthesis == true ? "true" : "any";

            JQ.Filters.Ultimatum.Disabled = !(itemOptions.AltQuality > 0 && itemOptions.Flags.IndexOf("ULTIMATUM|") == 0);
            if (!JQ.Filters.Ultimatum.Disabled && itemOptions.AltQuality > 0)
            {
                JQ.Filters.Ultimatum.Filters.Reward.Option = mParser.RewardUltimatum.Entries[itemOptions.AltQuality - 1].Id;
                JQ.Filters.Ultimatum.Filters.Output.Option = JQ.Filters.Ultimatum.Filters.Reward.Option == "ExchangeUnique" ? itemOptions.Flags.Split('|')[1] : "any";
            }

            bool error_filter = false;

            if (itemOptions.itemfilters.Count > 0)
            {
                error_filter = AddOptionsToJQ(itemOptions, mFilter, JQ, lang_index, Inherit, error_filter);
            }

            //if (!ckSocket.Dispatcher.CheckAccess())
            //else if (ckSocket.Dispatcher.CheckAccess())

            string sEntity = Json.Serialize<JsonData>(jsonData);

            if (itemOptions.ByCategory || JQ.Name == "" || !(JQ.Filters.Type.Filters.Rarity.Option == "unique"))
            {
                sEntity = sEntity.Replace("\"name\":\"" + JQ.Name + "\",", "");

                if (Inherit == "jewel" || itemOptions.ByCategory)
                    sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "");
                else if (Inherit == "prophecy" || JQ.Filters.Type.Filters.Category.Option == "monster.sample")
                    sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"" + (Inherit == "prophecy" ? "name" : "term") + "\":\"" + JQ.Type + "\",");
            }

            if (Inherit == "gem" && itemOptions.Name != "")
            {
                TransfiguredGemType gemType = new TransfiguredGemType
                {
                    Option = itemOptions.Type,
                    Discriminator = itemOptions.Flags // alt_x or alt_y
                };
                string transType = Json.Serialize<TransfiguredGemType>(gemType);
                sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"type\":" + transType + ",");
            }

            sEntity = sEntity.RepEx("\"(min|max)\":99999|\"option\":(0|\"any\"|null)", "").RepEx("\"[a-z_]+\":{[,]*}", "");
            sEntity = sEntity.RepEx(",{2,}", ",").RepEx("({),{1,}", "$1").RepEx(",{1,}(}|])", "$1");

            return (sEntity, error_filter);
        }

        private static bool AddOptionsToJQ(ItemOption itemOptions, FilterData[] mFilter, q_Query JQ, byte lang_index, string Inherit, bool error_filter)
        {
            JQ.Stats = new q_Stats[1];
            JQ.Stats[0] = new q_Stats
            {
                Type = "and",
                Filters = new q_Stats_filters[itemOptions.itemfilters.Count]
            };

            int idx = 0;

            foreach (Itemfilter itemFilter in itemOptions.itemfilters)
            {
                string input = itemFilter.text;
                string stat = itemFilter.stat;
                string type = itemFilter.type;

                if (input.Trim() != "" && RS.lFilterType.ContainsKey(type))
                {
                    string type_name = RS.lFilterType[type];

                    FilterDict filterDict = Array.Find(mFilter[lang_index].Result, x => x.Label == type_name);

                    if (filterDict != null)
                    {
                        // 무기에 경우 pseudo_adds_[a-lang]+_damage 옵션은 공격 시 가 붙음
                        if (Inherit == "weapon" && type == "pseudo" && Regex.IsMatch(stat, @"^pseudo_adds_[a-z]+_damage$"))
                        {
                            stat += "_to_attacks";
                        }

                        FilterDictItem dicItem = Array.Find(filterDict.Entries, x => x.Id == type + "." + stat);

                        JQ.Stats[0].Filters[idx] = new q_Stats_filters
                        {
                            Value = new q_Min_And_Max()
                        };

                        if (dicItem != null && (dicItem.Id ?? "").Trim() != "")
                        {
                            JQ.Stats[0].Filters[idx].Disabled = itemFilter.disabled == true;
                            JQ.Stats[0].Filters[idx].Value.Min = itemFilter.min;
                            JQ.Stats[0].Filters[idx].Value.Max = itemFilter.max;
                            JQ.Stats[0].Filters[idx].Value.Option = itemFilter.option;
                            JQ.Stats[0].Filters[idx++].Id = dicItem.Id;
                        }
                        else
                        {
                            error_filter = true;
                            itemFilter.isNull = true;

                            // 오류 방지를 위해 널값시 아무거나 추가 
                            JQ.Stats[0].Filters[idx].Id = "temp_ids";
                            JQ.Stats[0].Filters[idx].Value.Min = JQ.Stats[0].Filters[idx].Value.Max = 99999;
                            JQ.Stats[0].Filters[idx++].Disabled = true;
                        }
                    }
                }
            }

            return error_filter;
        }
    }
}

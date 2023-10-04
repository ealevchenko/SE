using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using static MINER_HUB_UPR_V2.Program;
using static System.Net.Mime.MediaTypeNames;
using static VRage.Game.MyObjectBuilder_CurveDefinition;
/// <summary>
/// v1.0
/// </summary>
namespace MINER_HUB_UPR_V2
{
    public sealed class Program : MyGridProgram
    {
        string NameObj = "[MINER_HUB]";
        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея
        const char green = '\uE001';
        const char blue = '\uE002';
        const char red = '\uE003';
        const char yellow = '\uE004';
        const char darkGrey = '\uE00F';
        static LCD lcd_storage;
        static LCD lcd_debug;
        static LCD lcd_debug1;
        static LCD lcd_name;

        static Connectors connectors;
        static Power power;
        static Program _scr;
        public class PText
        {
            static public string GetVector3D(Vector3D vector)
            {
                return "X: " + Math.Round(vector.GetDim(0), 2).ToString() + "Y: " + Math.Round(vector.GetDim(1), 2).ToString() + "Z: " + Math.Round(vector.GetDim(2), 2).ToString();
            }
            static public string GetPersent(double perse)
            {
                return " - " + Math.Round((perse * 100), 1) + "%";
            }
            static public string GetScalePersent(double perse, int scale)
            {
                string prog = "[";
                for (int i = 0; i < Math.Round((perse * scale), 0); i++)
                {
                    prog += "|";
                }
                for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++)
                {
                    prog += "'";
                }
                prog += "]" + GetPersent(perse);
                return prog;
            }
            static public string GetValueOfUnits(float value, string units)
            {
                if (value >= 1f)
                {
                    return Math.Round(value, 1).ToString() + "M" + units;
                }
                else if (value >= 0.001f)
                {
                    value = value * 1000; // K
                    return Math.Round(value, 1).ToString() + "k" + units;
                }
                else
                {
                    value = value * 1000000; // 
                    return Math.Round(value, 1).ToString() + units;
                }
            }
            static public string GetCurrentOfMax(float cur, float max, string units)
            {
                return "[ " + GetValueOfUnits(cur, units) + " / " + GetValueOfUnits(max, units) + " ]";
            }
            static public string GetSign(int x, int y)
            {
                if (x == y) { return "="; }
                if (x > y) { return ">"; }
                if (x < y) { return "<"; }
                return "";
            }
            static public string GetCountObj(int count, int count_on)
            {
                return count_on + GetSign(count_on, count) + count;
            }
            //------------------------------------------------------------------------
            static public string GetCapacityTanks(double perse, float capacity)
            {
                return "[ " + Math.Round(((perse * capacity) / 1000000), 1) + "МЛ / " + Math.Round((capacity / 1000000), 1) + "МЛ ]";
            }
            static public string GetThrust(float value)
            {
                return Math.Round(value / 1000000, 1) + "МН";
            }
            static public string GetCurrentThrust(float to, float cur, float max)
            {
                return "[ " + GetThrust(to) + " / " + GetThrust(cur) + " / " + GetThrust(max) + " ]";
            }
            static public string GetCurrentThrust(float cur, float max)
            {
                return "[ " + GetThrust(cur) + " / " + GetThrust(max) + " ]";
            }
            static public string GetCountDetaliThrust(int count_a, int count_h, int count_i, int count_on_a, int count_on_h, int count_on_i)
            {
                string result = "[";
                if (count_a > 0)
                {
                    result += "{А-" + count_on_a + "|" + count_a + "}";
                }
                if (count_h > 0)
                {
                    result += "{В-" + count_on_h + "|" + count_h + "}";
                }
                if (count_i > 0)
                {
                    result += "{И-" + count_on_i + "|" + count_i + "}";
                }
                result += "]";
                return result;
            }
            static public string GetGPS(string name, Vector3D target)
            {
                return "GPS:" + name + ":" + target.GetDim(0) + ":" + target.GetDim(1) + ":" + target.GetDim(2) + ":\n";
            }
        }
        public class InpResurs
        {
            public string SubtypeId { get; set; } = null;
            public float current { get; set; } = 0;
            public float max { get; set; } = 0;
            public int count_power_by { get; set; } = 0;
        }
        public class OutResurs
        {
            public string SubtypeId { get; set; } = null;
            public float current { get; set; } = 0;
            public float max { get; set; } = 0;
        }
        public class BaseListTerminalBlock<T> where T : class
        {
            public List<T> list_obj = new List<T>();
            public int Count { get { return list_obj.Count(); } }
            public BaseListTerminalBlock(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "]: " + list_obj.Count());
            }
            public BaseListTerminalBlock(string name_obj, string tag)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "],[" + tag + "]: " + list_obj.Count());
            }
            // Команды включения\выключения
            private void Off(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_Off");
                }
            }
            public void Off()
            {
                Off(list_obj);
            }
            private void OffOfTag(List<T> list, string tag)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(tag))
                    {
                        obj.ApplyAction("OnOff_Off");
                    }
                }
            }
            public void OffOfTag(string tag)
            {
                OffOfTag(list_obj, tag);
            }
            private void On(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_On");
                }
            }
            public void On()
            {
                On(list_obj);
            }
            private void OnOfTag(List<T> list, string tag)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(tag))
                    {
                        obj.ApplyAction("OnOff_On");
                    }
                }
            }
            public void OnOfTag(string tag)
            {
                OnOfTag(list_obj, tag);
            }
            public bool Enabled(string tag)
            {
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    if (obj.CustomName.Contains(tag) && !((IMyFunctionalBlock)obj).Enabled)
                    {
                        return false;
                    }
                }
                return true;
            }
            public bool Enabled()
            {
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    if (!((IMyFunctionalBlock)obj).Enabled)
                    {
                        return false;
                    }
                }
                return true;
            }
            public List<InpResurs> GetInpResurs(string name)
            {
                List<InpResurs> result = new List<InpResurs>();
                // Потребляемый ресурсы
                MyResourceSinkComponent sink;
                foreach (T obj in list_obj)
                {
                    ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSinkComponent>(out sink);
                    if (sink != null)
                    {
                        var list = sink.AcceptedResources;
                        foreach (MyDefinitionId def in list)
                        {
                            if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                            {
                                InpResurs resurs = result.Where(r => r.SubtypeId == def.SubtypeId.ToString()).FirstOrDefault();
                                if (resurs == null)
                                {
                                    resurs = new InpResurs()
                                    {
                                        SubtypeId = def.SubtypeId.ToString(),
                                        count_power_by = sink.IsPoweredByType(def) == true ? 1 : 0,
                                        max = sink.MaxRequiredInputByType(def),
                                        current = sink.CurrentInputByType(def)
                                    };
                                    result.Add(resurs);
                                }
                                else
                                {
                                    bool is_power_by = sink.IsPoweredByType(def);
                                    if (is_power_by == true) resurs.count_power_by++;
                                    resurs.max = sink.MaxRequiredInputByType(def);
                                    resurs.current = sink.CurrentInputByType(def);
                                }

                            }
                        }
                    }
                }
                return result;
            }
            public List<OutResurs> GetOutResurs(string name)
            {
                List<OutResurs> result = new List<OutResurs>();
                MyResourceSourceComponent source;
                foreach (T obj in list_obj)
                {
                    ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSourceComponent>(out source);
                    if (source != null)
                    {
                        var list = source.ResourceTypes;
                        foreach (MyDefinitionId def in list)
                        {
                            if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                            {
                                OutResurs resurs = result.Where(r => r.SubtypeId == def.SubtypeId.ToString()).FirstOrDefault();
                                if (resurs == null)
                                {
                                    resurs = new OutResurs()
                                    {
                                        SubtypeId = def.SubtypeId.ToString(),
                                        max = source.DefinedOutputByType(def),
                                        current = source.CurrentOutputByType(def)
                                    };
                                }
                                else
                                {
                                    resurs.max = source.DefinedOutputByType(def);
                                    resurs.current = source.CurrentOutputByType(def);
                                }
                                result.Add(resurs);
                            }
                        }
                    }
                }
                return result;
            }
        }
        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;
            public string CustomName { get { return ((IMyTerminalBlock)this.obj).CustomName; } set { ((IMyTerminalBlock)this.obj).CustomName = value; } }
            public BaseTerminalBlock(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as T;
                _scr.Echo("block:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public BaseTerminalBlock(T myobj)
            {
                obj = myobj;
                _scr.Echo("block:[" + obj.ToString() + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public Vector3D GetPosition()
            {
                return ((IMyEntity)obj).GetPosition();
            }
            // Команды включения\выключения
            public void Off()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off");
            }
            public void On()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On");
            }
            public List<InpResurs> GetInpResurs(string name)
            {
                List<InpResurs> result = new List<InpResurs>();
                // Потребляемый ресурсы
                MyResourceSinkComponent sink;
                ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSinkComponent>(out sink);
                if (sink != null)
                {
                    var list = sink.AcceptedResources;
                    foreach (MyDefinitionId def in list)
                    {
                        if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                        {
                            InpResurs resurs = new InpResurs()
                            {
                                SubtypeId = def.SubtypeId.ToString(),
                                count_power_by = sink.IsPoweredByType(def) == true ? 1 : 0,
                                max = sink.MaxRequiredInputByType(def),
                                current = sink.CurrentInputByType(def)
                            };
                            result.Add(resurs);
                        }
                    }
                }
                return result;
            }
            public List<OutResurs> GetOutResurs(string name)
            {
                List<OutResurs> result = new List<OutResurs>();
                MyResourceSourceComponent source;
                ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSourceComponent>(out source);
                if (source != null)
                {
                    var list = source.ResourceTypes;
                    foreach (MyDefinitionId def in list)
                    {
                        if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                        {
                            OutResurs resurs = new OutResurs()
                            {
                                SubtypeId = def.SubtypeId.ToString(),
                                max = source.DefinedOutputByType(def),
                                current = source.CurrentOutputByType(def)
                            };
                            result.Add(resurs);
                        }
                    }
                }
                return result;
            }
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name)
            {
                if (base.obj != null)
                {
                    base.obj.SetValue("Content", (Int64)1);
                }
            }
            public void OutText(StringBuilder values)
            {
                if (base.obj != null)
                {
                    base.obj.WriteText(values, false);
                }
            }
            public void OutText(string text, bool append)
            {
                if (base.obj != null)
                {
                    base.obj.WriteText(text, append);
                }
            }
            public StringBuilder GetText()
            {
                StringBuilder values = new StringBuilder();
                if (base.obj != null)
                {
                    base.obj.ReadText(values);
                }
                return values;
            }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            lcd_debug1 = new LCD(NameObj + "-LCD-DEBUG1");
            lcd_name = new LCD(NameObj + "-LCD-Name");
            connectors = new Connectors(NameObj);
            power = new Power(NameObj);
        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            //StringBuilder values_info = new StringBuilder();
            power.Logic(argument, updateSource);
            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update100)
            {

            }
            //lcd_debug.OutText(bats.TextInfo(), false);
        }
        public class Connectors : BaseListTerminalBlock<IMyShipConnector>
        {
            public Connectors(string name) : base(name) { }
            public Connectors(string name_obj, string tag) : base(name_obj, tag) { }
            public bool IsConected()
            {
                foreach (IMyShipConnector obj in base.list_obj)
                {
                    if (obj.Status == MyShipConnectorStatus.Connected)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public class Power
        {
            public string name_obj;
            public class power_result
            {
                public string TyepID { get; set; } = null;
                public int count { get; set; } = 0;
                public int working { get; set; } = 0;
                public int count_power_by { get; set; } = 0;
                public float int_cur { get; set; } = 0;
                public float int_max { get; set; } = 0;
                public float out_cur { get; set; } = 0;
                public float out_max { get; set; } = 0;
                public float power_cur { get; set; } = 0;
                public float power_max { get; set; } = 0;
            }

            private List<power_result> list_power_result = new List<power_result>();
            private List<power_result> list_ext_power_result = new List<power_result>();

            private List<IGrouping<string, IMyPowerProducer>> group_pp = new List<IGrouping<string, IMyPowerProducer>>(); // Источники внутрение
            private List<IGrouping<string, IMyTerminalBlock>> group_inp = new List<IGrouping<string, IMyTerminalBlock>>(); // Потребители внутрение
            //
            public float sum_cur_input { get; private set; } = 0;
            public float sum_max_input { get; private set; } = 0;
            public float sum_max_output { get; private set; } = 0;
            public float sum_cur_output { get; private set; } = 0;
            public Power(string name_obj)
            {
                this.name_obj = name_obj;
                List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(list, r => r.CustomName.Contains(name_obj));
                InitPower(list, ref group_pp, ref group_inp);
                _scr.Echo("Получено групп Out. : " + group_pp.Count());
                _scr.Echo("Получено групп Inp. : " + group_inp.Count());
            }
            public List<InpResurs> GetInpResurs(IMyTerminalBlock obj, string name)
            {
                List<InpResurs> result = new List<InpResurs>();
                // Потребляемый ресурсы
                MyResourceSinkComponent sink;
                obj.Components.TryGet<MyResourceSinkComponent>(out sink);
                if (sink != null)
                {
                    var list = sink.AcceptedResources;
                    foreach (MyDefinitionId def in list)
                    {
                        if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                        {
                            InpResurs resurs = new InpResurs()
                            {
                                SubtypeId = def.SubtypeId.ToString(),
                                count_power_by = sink.IsPoweredByType(def) == true ? 1 : 0,
                                max = sink.MaxRequiredInputByType(def),
                                current = sink.CurrentInputByType(def)
                            };
                            result.Add(resurs);
                        }
                    }
                }
                return result;
            }
            public List<OutResurs> GetOutResurs(IMyTerminalBlock obj, string name)
            {
                List<OutResurs> result = new List<OutResurs>();
                MyResourceSourceComponent source;
                obj.Components.TryGet<MyResourceSourceComponent>(out source);
                if (source != null)
                {
                    var list = source.ResourceTypes;
                    foreach (MyDefinitionId def in list)
                    {
                        if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                        {
                            OutResurs resurs = new OutResurs()
                            {
                                SubtypeId = def.SubtypeId.ToString(),
                                max = source.DefinedOutputByType(def),
                                current = source.CurrentOutputByType(def)
                            };
                            result.Add(resurs);
                        }
                    }
                }
                return result;
            }
            public List<InpResurs> GetInpResurs<T>(List<T> list_obj, string name)
            {
                List<InpResurs> result = new List<InpResurs>();
                // Потребляемый ресурсы
                MyResourceSinkComponent sink;
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSinkComponent>(out sink);
                    if (sink != null)
                    {
                        var list = sink.AcceptedResources;
                        foreach (MyDefinitionId def in list)
                        {
                            if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                            {
                                InpResurs resurs = result.Where(r => r.SubtypeId == def.SubtypeId.ToString()).FirstOrDefault();
                                bool is_power_by = sink.IsPoweredByType(def);
                                if (resurs == null)
                                {
                                    resurs = new InpResurs()
                                    {
                                        SubtypeId = def.SubtypeId.ToString(),
                                        count_power_by = is_power_by == true ? 1 : 0,
                                        max = sink.MaxRequiredInputByType(def),
                                        current = sink.CurrentInputByType(def)
                                    };
                                    result.Add(resurs);
                                }
                                else
                                {

                                    if (is_power_by == true)
                                        resurs.count_power_by++;
                                    resurs.max += sink.MaxRequiredInputByType(def);
                                    resurs.current += sink.CurrentInputByType(def);
                                }
                            }
                        }
                    }
                }
                return result;
            }
            public List<OutResurs> GetOutResurs<T>(List<T> list_obj, string name)
            {
                List<OutResurs> result = new List<OutResurs>();
                MyResourceSourceComponent source;
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSourceComponent>(out source);
                    if (source != null)
                    {
                        var list = source.ResourceTypes;
                        foreach (MyDefinitionId def in list)
                        {
                            if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                            {
                                OutResurs resurs = result.Where(r => r.SubtypeId == def.SubtypeId.ToString()).FirstOrDefault();
                                if (resurs == null)
                                {
                                    resurs = new OutResurs()
                                    {
                                        SubtypeId = def.SubtypeId.ToString(),
                                        max = source.DefinedOutputByType(def),
                                        current = source.CurrentOutputByType(def)
                                    };
                                    result.Add(resurs);
                                }
                                else
                                {
                                    resurs.max += source.DefinedOutputByType(def);
                                    resurs.current += source.CurrentOutputByType(def);
                                }

                            }
                        }
                    }
                }
                return result;
            }
            public void InitPower(List<IMyTerminalBlock> list, ref List<IGrouping<string, IMyPowerProducer>> group_pp, ref List<IGrouping<string, IMyTerminalBlock>> group_inp)
            {
                List<IMyPowerProducer> list_pp = new List<IMyPowerProducer>();
                List<IMyTerminalBlock> list_inp = new List<IMyTerminalBlock>();
                foreach (IMyTerminalBlock bl in list)
                {
                    if (bl is IMyPowerProducer)
                    {
                        list_pp.Add((IMyPowerProducer)bl);
                    }
                    else
                    {
                        List<InpResurs> inputs = GetInpResurs(bl, "Electricity");
                        if (inputs != null && inputs.Count() > 0 && inputs[0].max > 0f)
                        {
                            list_inp.Add(bl);
                        }
                    }
                }
                group_pp = list_pp.GroupBy(g => ((IMyTerminalBlock)g).BlockDefinition.TypeIdString).ToList();
                group_inp = list_inp.GroupBy(g => g.BlockDefinition.TypeIdString).ToList();
            }
            public void UpdatePower(List<IGrouping<string, IMyPowerProducer>> group_pp, List<IGrouping<string, IMyTerminalBlock>> group_inp, ref List<power_result> list_power)
            {
                list_power.Clear();
                foreach (IGrouping<string, IMyPowerProducer> gr_pp in group_pp)
                {
                    if (gr_pp.Count() > 0)
                    {
                        InpResurs inputs = GetInpResurs(gr_pp.ToList(), "Electricity").FirstOrDefault();
                        list_power.Add(new power_result()
                        {
                            TyepID = gr_pp.Key,
                            count = gr_pp.Count(),
                            working = gr_pp.Select(b => b.IsWorking == true).Count(),
                            count_power_by = inputs != null ? inputs.count_power_by : 0,
                            out_cur = gr_pp.Select(b => b.CurrentOutput).Sum(),
                            out_max = gr_pp.Select(b => b.MaxOutput).Sum(),
                            int_cur = gr_pp.Key == "MyObjectBuilder_BatteryBlock" ? gr_pp.Select(b => ((IMyBatteryBlock)b).CurrentInput).Sum() : 0,
                            int_max = gr_pp.Key == "MyObjectBuilder_BatteryBlock" ? gr_pp.Select(b => ((IMyBatteryBlock)b).MaxInput).Sum() : 0,
                            power_cur = gr_pp.Key == "MyObjectBuilder_BatteryBlock" ? gr_pp.Select(b => ((IMyBatteryBlock)b).CurrentStoredPower).Sum() : 0,
                            power_max = gr_pp.Key == "MyObjectBuilder_BatteryBlock" ? gr_pp.Select(b => ((IMyBatteryBlock)b).MaxStoredPower).Sum() : 0,
                        });
                    }
                }
                foreach (IGrouping<string, IMyTerminalBlock> gr_inp in group_inp)
                {
                    if (gr_inp.Count() > 0)
                    {
                        InpResurs inputs = GetInpResurs(gr_inp.ToList(), "Electricity").FirstOrDefault();
                        list_power.Add(new power_result()
                        {
                            TyepID = gr_inp.Key,
                            count = gr_inp.Count(),
                            working = gr_inp.Select(b => b.IsWorking == true).Count(),
                            int_cur = inputs.current,
                            int_max = inputs.max,
                        });
                    }
                }
            }
            public void UpdateExternal()
            {
                list_ext_power_result.Clear();
                if (connectors.IsConected())
                {
                    List<IMyTerminalBlock> list_ext = new List<IMyTerminalBlock>();
                    List<IGrouping<string, IMyPowerProducer>> group_pp = new List<IGrouping<string, IMyPowerProducer>>();
                    List<IGrouping<string, IMyTerminalBlock>> group_inp = new List<IGrouping<string, IMyTerminalBlock>>();
                    _scr.GridTerminalSystem.GetBlocksOfType(list_ext, r => r.CustomName.Contains(this.name_obj) != true);
                    InitPower(list_ext, ref group_pp, ref group_inp);
                    UpdatePower(group_pp, group_inp, ref list_ext_power_result);
                }
            }
            public void UpdateInternal()
            {
                //StringBuilder values = new StringBuilder();

                sum_cur_input = 0;
                sum_max_input = 0;
                sum_max_output = 0;
                sum_cur_output = 0;
                UpdatePower(group_pp, group_inp, ref list_power_result);

                sum_cur_input = list_power_result.Select(b => b.int_cur).Sum();
                sum_max_input = list_power_result.Select(b => b.int_max).Sum();
                sum_cur_output = list_power_result.Select(b => b.out_cur).Sum();
                sum_max_output = list_power_result.Select(b => b.out_max).Sum();
                //lcd_debug.OutText(values.ToString(), false);
            }
            public void Update()
            {
                //StringBuilder values = new StringBuilder();

                sum_cur_input = 0;
                sum_max_input = 0;
                sum_max_output = 0;
                sum_cur_output = 0;
                UpdateExternal();
                UpdateInternal();
                sum_cur_input = list_power_result.Select(b => b.int_cur).Sum();
                sum_max_input = list_power_result.Select(b => b.int_max).Sum();
                sum_cur_output = list_power_result.Select(b => b.out_cur).Sum();
                sum_max_output = list_power_result.Select(b => b.out_max).Sum();
                //lcd_debug.OutText(values.ToString(), false);
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {

                }
                if (updateSource == UpdateType.Update100)
                {
                    UpdateExternal();
                    Update();
                    lcd_debug.OutText(TextInfo(), false);
                    lcd_debug.OutText(TextBatterysInfo(), true);
                    lcd_debug.OutText(TextInfoDetali(list_power_result), true);
                    lcd_debug1.OutText(TextInfoDetali(list_ext_power_result), false);
                }
            }
            public string TextInfoDetali(List<power_result> list_power_result)
            {
                StringBuilder values = new StringBuilder();
                foreach (power_result pr in list_power_result)
                {
                    values.Append(pr.TyepID.Replace("MyObjectBuilder_", "") + " C-W-P: [" + pr.count + "-" + pr.working + "-" + pr.count_power_by + "]" + "\n");
                    if (pr.out_max > 0)
                    {
                        values.Append("|- OUT: [" + PText.GetCurrentOfMax(pr.out_cur, pr.out_max, "W") + "\n");
                        values.Append("|  " + PText.GetScalePersent(pr.out_max > 0f ? pr.out_cur / pr.out_max : 0f, 40) + "\n");
                    }
                    if (pr.int_max > 0)
                    {
                        values.Append("|- IN : [" + PText.GetCurrentOfMax(pr.int_cur, pr.int_max, "W") + "\n");
                        values.Append("|  " + PText.GetScalePersent(pr.int_max > 0f ? pr.int_cur / pr.int_max : 0f, 40) + "\n");
                    }

                }
                return values.ToString();
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ВЫРАБОТКА   : " + PText.GetCurrentOfMax(sum_cur_output, sum_max_output, "W") + "\n");
                values.Append("|  " + PText.GetScalePersent(sum_max_output > 0f ? sum_cur_output / sum_max_output : 0f, 40) + "\n");
                values.Append("ПОТРЕБЛЕНИЕ : " + PText.GetCurrentOfMax(sum_cur_input, sum_max_input, "W") + "\n");
                values.Append("|  " + PText.GetScalePersent(sum_max_input > 0f ? sum_cur_input / sum_max_input : 0f, 40) + "\n");
                values.Append("ДОСТУПНО : " + PText.GetValueOfUnits(sum_cur_output - sum_cur_input, "W") + "\n");
                return values.ToString();
            }
            public string TextBatterysInfo()
            {
                StringBuilder values = new StringBuilder();
                int count = list_power_result.Select(b => b.power_max > 0f).Count();
                values.Append("БАТАРЕЯ :[" + count + "]" + "\n");
                float cur = list_power_result.Select(b => b.power_cur).Sum();
                float max = list_power_result.Select(b => b.power_max).Sum();
                values.Append("|- ЗАРЯД: " + PText.GetCurrentOfMax(cur, max, "W") + "\n");
                values.Append("|  " + PText.GetScalePersent(max > 0f ? cur / max : 0f, 40) + "\n");
                return values.ToString();
            }
        }
    }
}

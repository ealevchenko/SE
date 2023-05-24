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
        static LCD lcd_name;

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
                if (value < 0.1)
                {
                    value = value * 1000; // K
                    return Math.Round(value, 1).ToString() + "k" + units;

                }
                else if (value < 0.001)
                {
                    value = value * 1000000; // K
                    return Math.Round(value, 1).ToString() + units;
                }
                return Math.Round(value, 1).ToString() + "M" + units;
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
            public bool is_power_by { get; set; } = false;
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
                                        is_power_by = sink.IsPoweredByType(def),
                                        max = sink.MaxRequiredInputByType(def),
                                        current = sink.CurrentInputByType(def)
                                    };
                                }
                                else
                                {
                                    bool is_power_by = sink.IsPoweredByType(def);
                                    resurs.max = sink.MaxRequiredInputByType(def);
                                    resurs.current = sink.CurrentInputByType(def);
                                }
                                result.Add(resurs);
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
                                is_power_by = sink.IsPoweredByType(def),
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
            lcd_name = new LCD(NameObj + "-LCD-Name");
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
        public class Power
        {
            public string name_obj;
            public class power_result
            {
                public string TyepID { get; set; } = null;
                public int count { get; set; } = 0;
                public int working { get; set; } = 0;
                public float int_cur { get; set; } = 0;
                public float int_max { get; set; } = 0;
                public float out_cur { get; set; } = 0;
                public float out_max { get; set; } = 0;
            }

            private List<power_result> list_power_result = new List<power_result>();
            //
            private List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
            private List<IMyTerminalBlock> lis_inp = new List<IMyTerminalBlock>();

            private List<IMyBatteryBlock> batterys = new List<IMyBatteryBlock>();
            private List<IMySolarPanel> solar_panels = new List<IMySolarPanel>();
            private List<IMyReactor> reactors = new List<IMyReactor>();
            private List<IMyPowerProducer> hydrogen_engines = new List<IMyPowerProducer>();
            private List<IMyPowerProducer> wind_turbine = new List<IMyPowerProducer>();

            private List<IGrouping<string, IMyTerminalBlock>> group_inp = new List<IGrouping<string, IMyTerminalBlock>>(); // Потребители
            //
            //List<IMyGasGenerator> gas_generators = new List<IMyGasGenerator>();
            //List<IMyGasTank> gas_tank = new List<IMyGasTank>();
            //List<IMyRefinery> refinery = new List<IMyRefinery>();
            public float sum_cur_input { get; private set; } = 0;
            public float sum_max_input { get; private set; } = 0;
            public float sum_max_output { get; private set; } = 0;
            public float sum_cur_output { get; private set; } = 0;
            public Power(string name_obj)
            {
                this.name_obj = name_obj;
                _scr.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(list, r => r.CustomName.Contains(name_obj));
                foreach (IMyTerminalBlock bl in list)
                {
                    if (bl is IMyBatteryBlock) { batterys.Add((IMyBatteryBlock)bl); }
                    else if (bl is IMySolarPanel) { solar_panels.Add((IMySolarPanel)bl); }
                    else if (bl is IMyReactor) { reactors.Add((IMyReactor)bl); }
                    else if (bl is IMyPowerProducer)
                    {
                        if (bl.BlockDefinition.TypeIdString.Contains("HydrogenEngine"))
                            hydrogen_engines.Add((IMyPowerProducer)bl);
                        if (bl.BlockDefinition.TypeIdString.Contains("WindTurbine"))
                            wind_turbine.Add((IMyPowerProducer)bl);
                    }
                    //
                    //else if (bl is IMyGasGenerator) { gas_generators.Add((IMyGasGenerator)bl); }
                    //else if (bl is IMyGasTank) { gas_tank.Add((IMyGasTank)bl); }
                    //else if (bl is IMyRefinery) { refinery.Add((IMyRefinery)bl); }
                    else
                    {
                        List<InpResurs> inputs = GetInpResurs(bl, "Electricity");
                        if (inputs != null && inputs.Count() > 0)
                        {
                            lis_inp.Add(bl);
                        }
                    }
                }
                List<IGrouping<string, IMyTerminalBlock>> group_inp = lis_inp.GroupBy(g => g.BlockDefinition.TypeIdString).ToList();

                _scr.Echo("Найдено hydrogen_engines : " + hydrogen_engines.Count());
                _scr.Echo("Найдено wind_turbine : " + wind_turbine.Count());
                _scr.Echo("Найдено solar_panels : " + solar_panels.Count());
                _scr.Echo("Найдено reactors : " + reactors.Count());
                //_scr.Echo("Найдено gas_generators : " + gas_generators.Count());
                //_scr.Echo("Найдено gas_tank : " + gas_tank.Count());
                //_scr.Echo("Найдено refinery : " + refinery.Count());
                _scr.Echo("Найдено др. потр. : " + lis_inp.Count());
                _scr.Echo("Получено групп потр. : " + group_inp.Count());
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
                                is_power_by = sink.IsPoweredByType(def),
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
                                if (resurs == null)
                                {
                                    resurs = new InpResurs()
                                    {
                                        SubtypeId = def.SubtypeId.ToString(),
                                        is_power_by = sink.IsPoweredByType(def),
                                        max = sink.MaxRequiredInputByType(def),
                                        current = sink.CurrentInputByType(def)
                                    };
                                }
                                else
                                {
                                    bool is_power_by = sink.IsPoweredByType(def);
                                    resurs.max = sink.MaxRequiredInputByType(def);
                                    resurs.current = sink.CurrentInputByType(def);
                                }
                                result.Add(resurs);
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
            public void Update()
            {
                sum_cur_input = 0;
                sum_max_input = 0;
                sum_max_output = 0;
                sum_cur_output = 0;

                list_power_result.Clear();
                // Добавим источники электричества
                if (batterys.Count() > 0)
                {
                    list_power_result.Add(new power_result()
                    {
                        TyepID = batterys.FirstOrDefault().BlockDefinition.TypeIdString,
                        count = batterys.Count(),
                        working = batterys.Select(b => b.IsWorking == true).Count(),
                        out_cur = batterys.Select(b => b.CurrentOutput).Sum(),
                        out_max = batterys.Select(b => b.MaxOutput).Sum(),
                        int_cur = batterys.Select(b => b.CurrentInput).Sum(),
                        int_max = batterys.Select(b => b.MaxInput).Sum(),
                    });
                }
                if (hydrogen_engines.Count() > 0)
                {
                    list_power_result.Add(new power_result()
                    {
                        TyepID = hydrogen_engines.FirstOrDefault().BlockDefinition.TypeIdString,
                        count = hydrogen_engines.Count(),
                        working = hydrogen_engines.Select(b => b.IsWorking == true).Count(),
                        out_cur = hydrogen_engines.Select(b => b.CurrentOutput).Sum(),
                        out_max = hydrogen_engines.Select(b => b.MaxOutput).Sum(),
                    });
                }
                if (solar_panels.Count() > 0)
                {
                    list_power_result.Add(new power_result()
                    {
                        TyepID = solar_panels.FirstOrDefault().BlockDefinition.TypeIdString,
                        count = solar_panels.Count(),
                        working = solar_panels.Select(b => b.IsWorking == true).Count(),
                        out_cur = solar_panels.Select(b => b.CurrentOutput).Sum(),
                        out_max = solar_panels.Select(b => b.MaxOutput).Sum(),
                    });
                }
                if (wind_turbine.Count() > 0)
                {
                    list_power_result.Add(new power_result()
                    {
                        TyepID = wind_turbine.FirstOrDefault().BlockDefinition.TypeIdString,
                        count = wind_turbine.Count(),
                        working = wind_turbine.Select(b => b.IsWorking == true).Count(),
                        out_cur = wind_turbine.Select(b => b.CurrentOutput).Sum(),
                        out_max = wind_turbine.Select(b => b.MaxOutput).Sum(),
                    });
                }
                if (reactors.Count() > 0)
                {
                    list_power_result.Add(new power_result()
                    {
                        TyepID = reactors.FirstOrDefault().BlockDefinition.TypeIdString,
                        count = reactors.Count(),
                        working = reactors.Select(b => b.IsWorking == true).Count(),
                        out_cur = reactors.Select(b => b.CurrentOutput).Sum(),
                        out_max = reactors.Select(b => b.MaxOutput).Sum(),
                    });
                }
                // Добавим потребителей электричества
                foreach (IGrouping<string, IMyTerminalBlock> gr_inp in group_inp)
                {
                    if (gr_inp.Count() > 0)
                    {
                        InpResurs inputs = GetInpResurs(gr_inp.ToList(), "Electricity").FirstOrDefault();
                        list_power_result.Add(new power_result()
                        {
                            TyepID = gr_inp.Key,
                            count = gr_inp.Count(),
                            working = gr_inp.Select(b => b.IsWorking == true).Count(),
                            int_cur = inputs.current,
                            int_max = inputs.max,
                        });
                    }
                }
                sum_cur_input = list_power_result.Select(b => b.int_cur).Sum();
                sum_max_input = list_power_result.Select(b => b.int_max).Sum();
                sum_cur_output = list_power_result.Select(b => b.out_cur).Sum();
                sum_max_output = list_power_result.Select(b => b.out_max).Sum();

            }
            public void Logic(string argument, UpdateType updateSource)
            {
                //bats.Logic(argument, updateSource);
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
                    Update();
                    lcd_debug.OutText(TextInfo(), false);
                }

            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                foreach (power_result pr in list_power_result)
                {
                    values.Append(pr.TyepID.Replace("MyObjectBuilder_", "") + " [" + pr.count + "-" + pr.working + "]" + "\n");
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
                values.Append("ВЫРАБОТКА   : " + PText.GetCurrentOfMax(sum_cur_output, sum_max_output, "W") + "\n");
                values.Append("|  " + PText.GetScalePersent(sum_max_output > 0f ? sum_cur_output / sum_max_output : 0f, 40) + "\n");
                values.Append("ПОТРЕБЛЕНИЕ : " + PText.GetCurrentOfMax(sum_cur_input, sum_max_input, "W") + "\n");
                values.Append("|  " + PText.GetScalePersent(sum_max_input > 0f ? sum_cur_input / sum_max_input : 0f, 40) + "\n");
                values.Append("ДОСТУПНО : " + PText.GetValueOfUnits(sum_cur_output- sum_cur_input, "W") + "\n");
                return values.ToString();
            }
        }
    }
}


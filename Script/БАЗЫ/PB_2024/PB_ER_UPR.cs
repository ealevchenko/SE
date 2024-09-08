using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Scripting;
using VRageMath;

/// <summary>
/// v1.0
/// Управление планетарной станцией
/// </summary>
namespace PB_ER_UPR
{
    public sealed class Program : MyGridProgram
    {
        // v1.
        static string NameObj = "[PB-ER-1]";
        static float SP_PW_PRS_0 = 90.0f;
        static float SP_PW_PRS_1 = 40.0f;
        static float SP_PW_PRS_2 = 30.0f;
        static float SP_PW_PRS_3 = 20.0f;
        static float SP_PW_PRS_4 = 10.0f;
        static float SP_PW_PRS_CR = 5.0f;
        public enum room : int
        {
            space = 0,
            operators = 1,
            station = 2,
            gateway = 3,
        };
        public static string[] name_room = { "КОСМОС", "ОПЕРАТОРСКАЯ", "СТАНЦИЯ", "ШЛЮЗ" };
        public static int[] count_room = { 0, 0, 0, 0 };

        public static Color red = new Color(255, 0, 0);
        public static Color yellow = new Color(255, 255, 0);
        public static Color green = new Color(0, 128, 0);

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static LCD lcd_storage;
        static LCD lcd_debug;
        //static LCD lcd_nav1;
        //static LCD lcd_st1, lcd_st2, lcd_st3, lcd_st4;
        //static LCD lcd_cntr1, lcd_cntr2;
        //static LCD lcd_con_forw, lcd_con_back, lcd_con_info;
        //static LCD lcd_base1, lcd_base2;
        static Batterys bats;
        static GasGenerators gas_gen1, gas_gen2, gas_gen3, gas_gen4;

        //static Connector connector_forw, connector_back, connector_l1, connector_l2, connector_r1, connector_r2, connector_pl1, connector_pl2, connector_work;
        //static ReflectorsLight reflectors_light;
        //static Lightings room_light;
        static MyStorage storage;
        //static SolarPanels solar_panels_left, solar_panels_right;
        static ControlPower control_power;
        //int clock = 0;

        static Program _scr;
        public class PText
        {
            static public string GetPersent(double perse) { return " - " + Math.Round((perse * 100), 1) + "%"; }
            static public string GetScalePersent(double perse, int scale) { string prog = "["; for (int i = 0; i < Math.Round((perse * scale), 0); i++) { prog += "|"; } for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++) { prog += "'"; } prog += "]" + GetPersent(perse); return prog; }
            static public string GetCurrentOfMax(float cur, float max, string units) { return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetCurrentOfMinMax(float min, float cur, float max, string units) { return "[ " + Math.Round(min, 1) + units + " / " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetThrust(float value) { return Math.Round(value / 1000000, 1) + "МН"; }
            static public string GetFarm(float value) { return Math.Round(value, 1) + "L"; }
            static public string GetGPS(string name, Vector3D target) { return "GPS:" + name + ":" + target.GetDim(0) + ":" + target.GetDim(1) + ":" + target.GetDim(2) + ":\n"; }
            static public string GetGPSMatrixD(string name, MatrixD target) { return "MatrixD:" + name + "\n" + "M11:" + target.M11 + "M12:" + target.M12 + "M13:" + target.M13 + "M14:" + target.M14 + ":\n" + "M21:" + target.M21 + "M22:" + target.M22 + "M23:" + target.M23 + "M24:" + target.M24 + ":\n" + "M31:" + target.M31 + "M32:" + target.M32 + "M33:" + target.M33 + "M34:" + target.M34 + ":\n" + "M41:" + target.M41 + "M42:" + target.M42 + "M43:" + target.M43 + "M44:" + target.M44 + ":\n"; }
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
            public BaseListTerminalBlock(string name_obj) { _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj)); _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "]: " + list_obj.Count()); }
            public BaseListTerminalBlock(string name_obj, string tag) { _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj)); if (!String.IsNullOrWhiteSpace(tag)) { list_obj = list_obj.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag)).ToList(); } _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "],[" + tag + "]: " + list_obj.Count()); }
            private void Off(List<T> list) { foreach (IMyTerminalBlock obj in list) { obj.ApplyAction("OnOff_Off"); } }
            public void Off() { Off(list_obj); }
            private void OffOfTag(List<T> list, string tag) { foreach (IMyTerminalBlock obj in list) { if (obj.CustomName.Contains(tag)) { obj.ApplyAction("OnOff_Off"); } } }
            public void OffOfTag(string tag) { OffOfTag(list_obj, tag); }
            private void On(List<T> list) { foreach (IMyTerminalBlock obj in list) { obj.ApplyAction("OnOff_On"); } }
            public void On() { On(list_obj); }
            public void OnOff(bool on_off) { if (on_off) On(); else Off(); }
            private void OnOfTag(List<T> list, string tag)
            { foreach (IMyTerminalBlock obj in list) { if (obj.CustomName.Contains(tag)) { obj.ApplyAction("OnOff_On"); } } }
            public void OnOfTag(string tag) { OnOfTag(list_obj, tag); }
            public bool Enabled(string tag) { foreach (IMyTerminalBlock obj in list_obj) { if (obj.CustomName.Contains(tag) && !((IMyFunctionalBlock)obj).Enabled) { return false; } } return true; }
            public bool Enabled() { foreach (IMyTerminalBlock obj in list_obj) { if (!((IMyFunctionalBlock)obj).Enabled) { return false; } } return true; }
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
            public BaseTerminalBlock(string name) { obj = _scr.GridTerminalSystem.GetBlockWithName(name) as T; _scr.Echo("block:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block"))); }
            public BaseTerminalBlock(T myobj) { obj = myobj; _scr.Echo("block:[" + obj.ToString() + "]: " + ((obj != null) ? ("Ок") : ("not Block"))); }
            public Vector3D GetPosition() { return ((IMyEntity)obj).GetPosition(); }
            public void Off() { if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off"); }
            public void On() { if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On"); }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            //lcd_nav1 = new LCD(NameObj + "-LCD Nav1");
            //lcd_st1 = new LCD(NameObj + "-LCD ST1");
            //lcd_st2 = new LCD(NameObj + "-LCD ST2");
            //lcd_st3 = new LCD(NameObj + "-LCD ST3");
            //lcd_st4 = new LCD(NameObj + "-LCD ST4");
            //lcd_cntr1 = new LCD(NameObj + "-LCD CNTR1");
            //lcd_cntr2 = new LCD(NameObj + "-LCD CNTR2 [LCD]");
            //lcd_con_forw = new LCD(NameObj + "-LCD CNTR [forw] [LCD]");
            //lcd_con_back = new LCD(NameObj + "-LCD CNTR [back] [LCD]");
            //lcd_con_info = new LCD(NameObj + "-LCD [connectors-info]");
            //lcd_base1 = new LCD(NameObj + "-LCD BASE1 [LCD]");
            //lcd_base2 = new LCD(NameObj + "-LCD BASE2 [LCD]");
            bats = new Batterys(NameObj, "[base]");
            gas_gen1 = new GasGenerators(NameObj + "-Генератор [АВР-1]");
            gas_gen2 = new GasGenerators(NameObj + "-Генератор [АВР-2]");
            gas_gen3 = new GasGenerators(NameObj + "-Генератор [АВР-3]");
            gas_gen4 = new GasGenerators(NameObj + "-Генератор [АВР-4]");
            //connector_forw = new Connector(NameObj + "-Коннектор forw");
            //connector_back = new Connector(NameObj + "-Коннектор back");
            //connector_l1 = new Connector(NameObj + "-Коннектор left-1");
            //connector_l2 = new Connector(NameObj + "-Коннектор left-2");
            //connector_r1 = new Connector(NameObj + "-Коннектор right-1");
            //connector_r2 = new Connector(NameObj + "-Коннектор right-2");
            //connector_pl1 = new Connector(NameObj + "-Коннектор pl-1");
            //connector_pl2 = new Connector(NameObj + "-Коннектор pl-2");
            //connector_work = new Connector(NameObj + "-Коннектор work");
            //room_light.Off();
            //reflectors_light = new ReflectorsLight(NameObj);
            //reflectors_light.Off();
            //solar_panels_left = new SolarPanels(NameObj, "[left]");
            //solar_panels_right = new SolarPanels(NameObj, "[right]");
            control_power = new ControlPower();
            storage = new MyStorage();
            storage.LoadFromStorage();
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument) { default: break; }
            count_room[(int)room.space] = 0;// В космосе людей не считаем
            //room_light.Logic(argument, updateSource);// Логика отработки включения и выключения освещения
            control_power.Logic(argument, updateSource);// Логика системы контроля питания
            if (updateSource == UpdateType.Update100)
            {

            }
            StringBuilder values = new StringBuilder();
            values.Append(bats.TextInfo());
            values.Append(control_power.TextStatus());
            lcd_debug.OutText(values);
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name) { if (base.obj != null) { base.obj.SetValue("Content", (Int64)1); } }
            public void OutText(StringBuilder values) { if (base.obj != null) { base.obj.WriteText(values, false); } }
            public void OutText(string text, bool append) { if (base.obj != null) { base.obj.WriteText(text, append); } }
            public StringBuilder GetText() { StringBuilder values = new StringBuilder(); if (base.obj != null) { base.obj.ReadText(values); } return values; }
        }
        public class Batterys : BaseListTerminalBlock<IMyBatteryBlock>
        {
            //         public int count_work_batterys { get { return list_obj.Where(n => !((IMyTerminalBlock)n).CustomName.Contains(tag_batterys_duty)).Count(); } }
            public bool charger = false;
            public Batterys(string name_obj) : base(name_obj)
            {
                Init();
            }
            public Batterys(string name_obj, string tag) : base(name_obj, tag)
            {
                Init();
            }
            public void Init()
            {
                base.On();
                charger = IsCharger();
            }
            public float MaxPower()
            {
                return base.list_obj.Select(b => b.MaxStoredPower).Sum();
            }
            public float CurrentPower()
            {
                return base.list_obj.Select(b => b.CurrentStoredPower).Sum();
            }
            public float CurrentPersent()
            {
                return base.list_obj.Select(b => b.CurrentStoredPower).Sum() / base.list_obj.Select(b => b.MaxStoredPower).Sum();
            }
            public int CountCharger()
            {
                List<IMyBatteryBlock> res = base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Recharge).ToList();
                return res.Count();
            }
            public int CountAuto()
            {
                List<IMyBatteryBlock> res = base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Auto).ToList();
                return res.Count();
            }
            public bool IsCharger()
            {
                int count_charger = CountCharger();
                return count_charger > 0;
                //return count_work_batterys > 0 && count_charger > 0 && count_work_batterys == count_charger ? true : false;
            }
            public bool IsAuto()
            {
                int count_auto = CountAuto();
                return Count > 0 && count_auto > 0 && Count == count_auto ? true : false;
            }
            public void Charger()
            {
                foreach (IMyBatteryBlock obj in base.list_obj)
                {
                    // проверка батарея дежурного режима
                    //if (!obj.CustomName.Contains(tag_batterys_duty))
                    //{
                    //    obj.ChargeMode = ChargeMode.Recharge;
                    //}
                    obj.ChargeMode = ChargeMode.Recharge;
                }
                charger = IsCharger();
            }
            public void Auto()
            {
                foreach (IMyBatteryBlock obj in base.list_obj)
                {
                    obj.ChargeMode = ChargeMode.Auto;
                }
                charger = IsCharger();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "bat_charger":
                        Charger();
                        break;
                    case "bat_auto":
                        Auto();
                        break;
                    case "bat_toggle":
                        if (charger) { Auto(); } else { Charger(); }
                        break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {

                }

            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("БАТАРЕЯ: [" + Count + "] [А-" + CountAuto() + " З-" + CountCharger() + "]" + PText.GetCurrentOfMax(CurrentPower(), MaxPower(), "MW") + "\n");
                values.Append("|- ЗАР:  " + PText.GetScalePersent(CurrentPower() / MaxPower(), 20) + "\n");
                return values.ToString();
            }
        }
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorsLight(string name_obj) : base(name_obj) { }
            public ReflectorsLight(string name_obj, string tag) : base(name_obj, tag) { }
        }
        public class Lightings : BaseListTerminalBlock<IMyInteriorLight>
        {
            public Lightings(string name_obj, string tag) : base(name_obj)
            {
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено Lighting:[" + tag + "]: " + list_obj.Count());
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
                    foreach (room rm in Enum.GetValues(typeof(room)))
                    {
                        if (count_room[(int)rm] > 0)
                        {
                            OnOfTag(rm.ToString());
                        }
                        else
                        {
                            OffOfTag(rm.ToString());
                        }
                    }
                }
            }
        }
        public class SolarPanels : BaseListTerminalBlock<IMySolarPanel>
        {
            public SolarPanels(string name_obj) : base(name_obj) { }
            public SolarPanels(string name_obj, string tag) : base(name_obj, tag) { }
            public float MaxOutput { get { return this.list_obj.Sum(s => s.MaxOutput); } }
            public float CurrentOutput { get { return this.list_obj.Sum(s => s.CurrentOutput); } }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append("СОЛН. ПАНЕЛЬ " + name + " : [" + Count + "] " + PText.GetCurrentOfMax(CurrentOutput, MaxOutput, "MW") + "\n");
                values.Append("|- ВЫХ:  " + PText.GetScalePersent(CurrentOutput / MaxOutput, 20) + "\n");
                return values.ToString();
            }
        }
        public class Connector : BaseTerminalBlock<IMyShipConnector>
        {
            public MyShipConnectorStatus Status { get { return base.obj.Status; } }
            public bool Connected { get { return base.obj.Status == MyShipConnectorStatus.Connected ? true : false; } }
            public bool Unconnected { get { return base.obj.Status == MyShipConnectorStatus.Unconnected ? true : false; } }
            public bool Connectable { get { return base.obj.Status == MyShipConnectorStatus.Connectable ? true : false; } }
            public Connector(string name) : base(name)
            {
                if (base.obj != null)
                {

                }
            }
            public string GetInfoStatus()
            {
                switch (base.obj.Status)
                {
                    case MyShipConnectorStatus.Connected:
                        {
                            return "ПОДКЛЮЧЕН";
                        }
                    case MyShipConnectorStatus.Connectable:
                        {
                            return "ГОТОВ";
                        }
                    case MyShipConnectorStatus.Unconnected:
                        {
                            return "НЕПОДКЛЮЧЕН";
                        }
                    default:
                        {
                            return "";
                        }
                }
            }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append(name + ": " + (Connected ? green.ToString() : (Connectable ? yellow.ToString() : red.ToString())) + "\n");
                return values.ToString();
            }
            public void Connect()
            {
                obj.Connect();
            }
            public void Disconnect()
            {
                obj.Disconnect();
            }
        }
        public class GasGenerators : BaseListTerminalBlock<IMyPowerProducer>
        {
            public InpResurs res_inp { get; private set; }
            public GasGenerators(string name_obj) : base(name_obj)
            {
            }
            public GasGenerators(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public void Update()
            {
                res_inp = GetInpResurs("Electricity").FirstOrDefault();
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ГЕНЕРАТОР ГАЗА :[" + Count + "]" + "\n");
                values.Append("|- IN: " + PText.GetCurrentOfMax(res_inp.current, res_inp.max, "W") + "\n");
                values.Append("   " + PText.GetScalePersent(res_inp.max > 0f ? res_inp.current / res_inp.max : 0f, 40) + "\n");
                return values.ToString();
            }
        }
        public class MyStorage
        {
            public MyStorage() { }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                for (int i = 0; i < count_room.Length; i++)
                {
                    int count = GetValInt("count_room_" + i, str.ToString());
                    count_room[i] = count;
                }
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                for (int i = 0; i < count_room.Length; i++)
                {
                    values.Append("count_room_" + i + ": " + (count_room[i]).ToString() + ";\n");
                }
                lcd_storage.OutText(values);
            }
            private string GetVal(string Key, string str, string val) { string pattern = @"(" + Key + "):([^:^;]+);"; System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern); if (match.Success) { val = match.Groups[2].Value; } return val; }
            public string GetValString(string Key, string str) { return GetVal(Key, str, ""); }
            public double GetValDouble(string Key, string str) { return Convert.ToDouble(GetVal(Key, str, "0")); }
            public int GetValInt(string Key, string str) { return Convert.ToInt32(GetVal(Key, str, "0")); }
            public long GetValInt64(string Key, string str) { return Convert.ToInt64(GetVal(Key, str, "0")); }
            public bool GetValBool(string Key, string str) { return Convert.ToBoolean(GetVal(Key, str, "False")); }
        }
        public class ControlPower
        {
            public bool paused = false;
            public bool stop_dreel = false;
            public float curr_power_per { get; set; }

            public ControlPower()
            {
                //storage.LoadFromStorage();
                //LoadFromStorageJSON();
            }
            //-------------------------------------------------


            public string TextStatus()
            {
                StringBuilder values = new StringBuilder();
                values.Append("--------------------------------------\n");
                values.Append("Текущий заряд: " + Math.Round(curr_power_per, 2).ToString() + "\n");

                //values.Append("ЭТАП      : " + name_mode[(int)curent_mode] + "\n");
                //values.Append("ПАУЗА     : " + (paused ? green.ToString() : red.ToString()) + ", ");
                //values.Append("СТОП      : " + (stop_dreel ? green.ToString() : red.ToString()) + "\n");
                //values.Append("--------------------------------------\n");
                //values.Append("ЗАД/ДИСТ  : " + Math.Round(TackDistance).ToString() + " / " + Math.Round(CalcDistance).ToString() + "\n");
                //values.Append("--------------------------------------\n");
                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "load":
                        storage.LoadFromStorage();
                        break;
                    case "save":
                        storage.SaveToStorage();
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update100)
                {
                    curr_power_per = (bats.CurrentPower() / bats.MaxPower() * 100.0f);
                    if (curr_power_per < SP_PW_PRS_1)
                    {
                        gas_gen1.On();
                    }
                    if (curr_power_per < SP_PW_PRS_2)
                    {
                        gas_gen2.On();
                    }
                    if (curr_power_per < SP_PW_PRS_3)
                    {
                        gas_gen3.On();
                    }
                    if (curr_power_per < SP_PW_PRS_4)
                    {
                        gas_gen4.On();
                    }
                    if (curr_power_per > SP_PW_PRS_0)
                    {
                        gas_gen4.Off();
                        gas_gen3.Off();
                        gas_gen2.Off();
                    }
                    if (curr_power_per == 100.0f)
                    {
                        gas_gen4.Off();
                        gas_gen3.Off();
                        gas_gen2.Off();
                        gas_gen1.Off();
                    }
                }
            }
        }
    }
}


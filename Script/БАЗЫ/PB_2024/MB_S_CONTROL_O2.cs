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
using static MUL_H1_NAV.Program;
using static VRage.Game.MyObjectBuilder_CurveDefinition;

/// <summary>
/// v1.0
/// База метеор (Контроллер управления дверями, содержанием кислорода)
/// </summary>
namespace MB_S_CONTROL_O2
{
    public sealed class Program : MyGridProgram
    {
        // v1.
        static string NameObj = "[MB-S01]";
        public enum room : int
        {
            space = 0,
            cabin = 1,
            relaxation = 2,
            tech_ladder = 3,
            operators = 4,
            gateway = 5,
            angar_tech = 6,
            angar_work = 7,
            transition_left = 8,
            transition_right = 9,
            waiting = 10,
            medical = 11,
            operators_work = 12,
            operators_fabric = 13,
            fabric = 14,
            sg_work = 15,
            operators_dors = 16,
            operators_weapon = 17,
        };
        public static string[] name_room = { "КОСМОС", "КАЮТА",
            "КОМ. ОТДЫХА", "ТЕХ-ЛЕСТНИЦА", "ОПЕРАТОРСКАЯ", "ШЛЮЗ",
            "ТЕХ-АНГАР", "АНГАР-СБОРЩИК", "ТЕХ-ПЕРЕХОД-Л", "ТЕХ-ПЕРЕХОД-П",
            "КОМ. ОЖИДАНИЯ", "МЕД-БЛОК", "ОПЕР. СБОРЩИК", "ОПЕР. ФАБРИКА",
            "ФАБРИКА", "СБОРЩИК-МС", "ОПЕР. К-ДВ-О2" , "ОПЕР. К-ОРУЖИЯ" };
        public static int[] count_room = { 0, 0, 0, 0, 0, 0, 0 };

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
        static Batterys bats;

        static Lightings lightings;
        static Gateway gateway;

        static O2Tanks o2_tanks_base;
        static BaseShipController cockpit;
        static MyStorage storage;
        static Control control;

        //int clock = 0;

        static Program _scr;

        public class RoomStatus
        {
            public room room { get; set; }
            public string name { get; set; }
            public float ox_level { get; set; }
            public VentStatus vent_status { get; set; }
            public int pipel_count { get; set; }
            public List<IMyDoor> doors = new List<IMyDoor>();
            public List<IMyAirVent> vents = new List<IMyAirVent>();
            public List<IMyInteriorLight> light = new List<IMyInteriorLight>();
            public List<IMyTextPanel> panel = new List<IMyTextPanel>();
        }
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
            bats = new Batterys(NameObj);
            //connector_base = new Connector(NameObj + "-Коннектор base");
            //gateway = new Gateway(NameObj);
            lightings = new Lightings(NameObj, "[lighting]");
            lightings.Off();
            o2_tanks_base = new O2Tanks(NameObj);
            cockpit = new BaseShipController(NameObj + "-Cocpit O2 Locked [LCD]");
            control = new Control(NameObj);
            storage = new MyStorage();
            storage.LoadFromStorage();
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            //gateway.Logic();
            switch (argument) { default: break; }
            count_room[(int)room.space] = 0;// В космосе людей не считаем
            control.Logic(argument, updateSource);// Логика системы контроля питания
            if (updateSource == UpdateType.Update100)
            {

            }
            StringBuilder values = new StringBuilder();
            values.Append(bats.TextInfo());
            values.Append(o2_tanks_base.TextInfo("O2-НОСИТЕЛЯ"));
            //values.Append(connector_base.TextInfo("К:Base") + "\n");
            //values.Append(mergeblock_trusk1.TextInfo("СОЕД-НОСИТ-1"));
            //values.Append(mergeblock_trusk2.TextInfo("СОЕД-НОСИТ-2"));
            values.Append(control.TextStatus());
            cockpit.OutText(values, 0);
            //lcd_debug.OutText(values);
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
        public class Gateway
        {
            string name;
            public int count_external = 0;
            public int count_internal = 0;
            IMySensorBlock sn1;
            IMySensorBlock sn2;
            IMyDoor dr1;
            IMyDoor dr2;
            bool sn1_active = false;    // датчик входа
            bool sn2_active = false;   // датчик выхода
            public Gateway(string name, IMyDoor dr1, IMySensorBlock sn1, string rm1, IMyDoor dr2, IMySensorBlock sn2, string rm2)
            {
                this.name = name;
                this.dr1 = dr1; this.dr2 = dr2; this.sn1 = sn1; this.sn2 = sn2;
                this.dr1.ApplyAction("OnOff_On");
                this.dr2.ApplyAction("OnOff_On");
                this.dr1.CloseDoor();
                this.dr2.CloseDoor();
            }
            public void Logic()
            {
                if (!sn1.IsActive && dr1.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    dr1.CloseDoor();
                }
                if (sn1.IsActive && dr1.Status == DoorStatus.Closed && dr2.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    dr1.OpenDoor();
                }
                if (!sn2.IsActive && dr2.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    dr2.CloseDoor();
                }
                if (sn2.IsActive && dr2.Status == DoorStatus.Closed && dr1.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    dr2.OpenDoor();
                }
                // Логика направдения движения
                if (sn1_active && !sn2_active && sn2.IsActive)
                {
                    // Выход
                    sn2_active = true;
                    count_external--;
                    count_internal++;
                }
                if (sn2_active && !sn1_active && sn1.IsActive)
                {
                    // Вход
                    sn1_active = true;
                    count_external++;
                    count_internal--;
                }
                if (sn2_active && sn1_active && !sn2.IsActive && !sn1.IsActive)
                {
                    // Вход
                    sn1_active = false;
                    sn2_active = false;
                }

                if (!sn1_active && !sn2_active)
                {
                    // Выход
                    sn1_active = sn1.IsActive;
                    sn2_active = sn2.IsActive;
                }
                if (count_external < 0) count_external = 0;
                if (count_internal < 0) count_internal = 0;
            }
        }
        public class O2Tanks : BaseListTerminalBlock<IMyGasTank>
        {
            public O2Tanks(string name_obj) : base(name_obj) { AutoRefillBottles(true); }
            public O2Tanks(string name_obj, string tag) : base(name_obj, tag) { AutoRefillBottles(true); }
            public float MaxCapacity() { return base.list_obj.Select(b => b.Capacity).Sum(); }
            public double AverageFilledRatio { get { return base.list_obj.Average(t => t.FilledRatio); } }
            public double CountAutoRefillBottles { get { return base.list_obj.Count(t => t.AutoRefillBottles); } }
            public double CountStockpile { get { return base.list_obj.Count(t => t.Stockpile); } }
            public double Capacity { get { return base.list_obj.Sum(t => t.Capacity); } }
            public void AutoRefillBottles(bool on) { foreach (IMyGasTank obj in base.list_obj) { obj.AutoRefillBottles = on; } }
            public void Stockpile(bool on) { foreach (IMyGasTank obj in base.list_obj) { obj.Stockpile = on; } }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append((name != null ? name : "БАКИ") + " : [" + base.list_obj.Count() + "] [А-" + CountAutoRefillBottles + " З-" + CountStockpile + "]" + PText.GetCurrentOfMax((float)(Capacity * AverageFilledRatio) / 1000000, (float)Capacity / 1000000, "МЛ") + "\n");
                values.Append("|- ЗАП:  " + PText.GetScalePersent(AverageFilledRatio, 20) + "\n");
                return values.ToString();
            }
        }
        public class BaseShipController
        {
            public IMyShipController obj;
            public BaseShipController(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipController;
                _scr.Echo("base_controller:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }

            public BaseShipController(string name_obj, string tag)
            {
                List<IMyShipController> list_obj = new List<IMyShipController>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyShipController>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo("Найдено base_ship_controller : " + list_obj.Count());
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    obj = list_obj.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag)).FirstOrDefault();
                }
                _scr.Echo("Выбран base_ship_controller: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public void OutText(StringBuilder values, int num_lcd)
            {
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(values, false);
                    }
                }
            }
            public void OutText(string text, bool append, int num_lcd)
            {
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(text, append);
                    }
                }
            }
            public StringBuilder GetText(int num_lcd)
            {
                StringBuilder values = new StringBuilder();
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return null;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.ReadText(values);
                    }
                }
                return values;
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
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
            public MatrixD GetValMatrixD(string Key, string str)
            {
                return new MatrixD(GetValDouble(Key + "11", str.ToString()), GetValDouble(Key + "12", str.ToString()), GetValDouble(Key + "13", str.ToString()), GetValDouble(Key + "14", str.ToString()),
                GetValDouble(Key + "21", str.ToString()), GetValDouble(Key + "22", str.ToString()), GetValDouble(Key + "23", str.ToString()), GetValDouble(Key + "24", str.ToString()),
                GetValDouble(Key + "31", str.ToString()), GetValDouble(Key + "32", str.ToString()), GetValDouble(Key + "33", str.ToString()), GetValDouble(Key + "34", str.ToString()),
                GetValDouble(Key + "41", str.ToString()), GetValDouble(Key + "42", str.ToString()), GetValDouble(Key + "43", str.ToString()), GetValDouble(Key + "44", str.ToString()));
            }
            public Vector3D GetValVector3D(string Key, string str) { return new Vector3D(GetValDouble(Key + "X", str.ToString()), GetValDouble(Key + "Y", str.ToString()), GetValDouble(Key + "Z", str.ToString())); }
            public string SetValVector3D(string Key, Vector3D val) { return val.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", Key + "X").Replace("Y", Key + "Y").Replace("Z", Key + "Z"); }
            public string SetValMatrixD(string Key, MatrixD val) { return val.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", Key + "M"); }
        }
        public class Control
        {
            public float curr_power_per { get; set; }
            List<RoomStatus> list_rs = new List<RoomStatus>();

            private List<IMyDoor> doors = new List<IMyDoor>();
            private List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            private List<IMyAirVent> vents = new List<IMyAirVent>();
            private List<IMyInteriorLight> lights = new List<IMyInteriorLight>();
            private List<IMyTextPanel> panels = new List<IMyTextPanel>();
            private List<IMyTextPanel> ipanels = new List<IMyTextPanel>();
            private List<Gateway> gateways = new List<Gateway>();
            public Control(string name)
            {
                // Создадим структуру состояния помещений
                foreach (room group in Enum.GetValues(typeof(room)))
                {
                    RoomStatus rs = new RoomStatus()
                    {
                        room = group,
                        pipel_count = 0,
                        name = name_room[(int)group],
                        ox_level = 0,
                        vent_status = 0,
                    };
                    list_rs.Add(rs);
                }




                StringBuilder values = new StringBuilder();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, r => r.CustomName.Contains(name));
                _scr.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, r => r.CustomName.Contains(name));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents, r => r.CustomName.Contains(name));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(lights, r => r.CustomName.Contains(name));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, r => r.CustomName.Contains(name));
                ipanels = panels.Where(x => x.CustomName.Contains("[lcd-info]")).ToList();
                // gateway - получим шлюзы
                List<IGrouping<string, IMyDoor>> dr_gr = doors.Where(d => d.CustomName.Contains("[dr-gateway-")).GroupBy(g => GetNameOfTemplate(g.CustomName, "[dr-gateway-")).ToList();
                lcd_debug.OutText("Старт ->", false);
                lcd_debug.OutText("\nlist_rs -> " + list_rs.Count(), true);
                lcd_debug.OutText("\ndoors -> " + doors.Count(), true);
                lcd_debug.OutText("\nsensors -> " + sensors.Count(), true);
                lcd_debug.OutText("\nvents -> " + vents.Count(), true);
                lcd_debug.OutText("\nvents -> " + vents.Count(), true);
                lcd_debug.OutText("\nipanels.Count() -> " + ipanels.Count(), true);
                // Настройка Gateway
                foreach (IGrouping<string, IMyDoor> gtw in dr_gr)
                {
                    lcd_debug.OutText("\nIGrouping -> " + gtw.Key, true);
                    lcd_debug.OutText("\ngtw.Count() -> " + gtw.Count(), true);
                    if (gtw.Count() == 2)
                    {
                        // Первая дверь
                        IMyDoor dr1 = gtw.First();
                        lcd_debug.OutText("\ndr1 -> " + dr1.CustomName, true);
                        string rm1 = GetNameOfTemplate(dr1.CustomName, "[rm-");
                        lcd_debug.OutText("\nrm1 -> " + rm1, true);
                        IMySensorBlock sn1 = sensors.Where(s => s.CustomName.Contains(gtw.Key) && s.CustomName.Contains(rm1)).FirstOrDefault();
                        lcd_debug.OutText("\nsn1 -> " + sn1.CustomName, true);
                        // Вторая дверь
                        IMyDoor dr2 = gtw.Last();
                        lcd_debug.OutText("\ndr2 -> " + dr2.CustomName, true);
                        string rm2 = GetNameOfTemplate(dr2.CustomName, "[rm-");
                        lcd_debug.OutText("\nrm2 -> " + rm2, true);
                        IMySensorBlock sn2 = sensors.Where(s => s.CustomName.Contains(gtw.Key) && s.CustomName.Contains(rm2)).FirstOrDefault();
                        lcd_debug.OutText("\nsn2 -> " + sn2.CustomName, true);
                        Gateway dr_gtw = new Gateway(gtw.Key, dr1, sn1, rm1, dr2, sn2, rm2);
                        gateways.Add(dr_gtw);
                    }
                }
                lcd_debug.OutText("\ngateways.Count() -> " + gateways.Count(), true);
                // Настройка info-panel


                //foreach (room group in Enum.GetValues(typeof(room)))
                //{

                //}

            }
            public string GetNameOfTemplate(string str, string tmp)
            {
                int istart = str.IndexOf(tmp);
                string result = null;
                if (istart >= 0)
                {
                    for (var i = istart; i < str.Length; i++)
                    {
                        result += str[i];
                        if (str[i] == ']') return result;
                    }
                }
                return result;
            }
            public void SetTextLCDInfo(room rm, Color color)
            {
                List<IMyTextPanel> objs = panels.Where(x => x.CustomName.Contains("[rm-" + rm.ToString() + "]")).ToList();
                foreach (IMyTextPanel obj in objs)
                {
                    obj.SetValue("Content", (Int64)1);
                    obj.SetValueColor("FontColor", color);
                    obj.SetValueFloat("FontSize", 7.0f);
                    obj.SetValue("alignment", (Int64)2);
                    obj.WriteText(name_room[(int)rm].ToUpper(), false);
                }
            }
            //-------------------------------------------------
            public string TextStatus()
            {
                StringBuilder values = new StringBuilder();
                values.Append("------------------------\n");
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
                // Проверим двери
                foreach (Gateway gtw in gateways)
                {
                    gtw.Logic();
                }
                // Переберем таблички и покажем кислород
                foreach (room group in Enum.GetValues(typeof(room)))
                {
                    IMyAirVent vent = vents.Where(x => x.CustomName.Contains("[rm-" + group.ToString() + "]")).FirstOrDefault();
                    if (vent != null)
                    {
                        float Ol = vent.GetOxygenLevel();
                        if (Ol > 0.9)
                        {
                            SetTextLCDInfo(group, green);
                        }
                        else if (Ol == 0)
                        {
                            SetTextLCDInfo(group, red);
                        }
                        else
                        {
                            SetTextLCDInfo(group, yellow);
                        }
                    }
                }
                if (updateSource == UpdateType.Update100)
                {
                    curr_power_per = (bats.CurrentPower() / bats.MaxPower() * 100.0f);
                }
            }
        }
    }
}

// [rm-operators]
// [dr-gateway]
// [dr-inner]
// [dr-gate]
// [sn-gateway]
// [sn-inner]
// [sn-gate]
//


//[rm-space]
//[rm-cabin]
//[rm-relaxation]
//[rm-tech_ladder]
//[rm-operators_fabric]
//[rm-fabric]

// [MB-S01]-Gate [dr-gate-sg-work] [rm-sg_work]
// [MB-S01]-Gate [dr-gate-angar_work] [rm-angar_work]

// [MB-S01]-[lcd-info] [rm-cabin]
// [MB-S01]-[lcd-info] [rm-relaxation]
// [MB-S01]-[lcd-info] [rm-angar_tech]
// [MB-S01]-[lcd-info] [rm-waiting]
// [MB-S01]-[lcd-info] [rm-medical]
// [MB-S01]-[lcd-info] [rm-tech_ladder]
// [MB-S01]-[lcd-info] [rm-operators_work]
// [MB-S01]-[lcd-info] [rm-operators_fabric]
// [MB-S01]-[lcd-info] [rm-fabric]
// [MB-S01]-[lcd-info] [rm-sg_work]
// [MB-S01]-[lcd-info] [rm-angar_work]
// [MB-S01]-[lcd-info] [rm-operators_dors]
// [MB-S01]-[lcd-info] [rm-operators_weapon]

// [MB-S01]-Vent [rm-angar_work]
// [MB-S01]-Vent [rm-cabin]
// [MB-S01]-Vent [rm-relaxation]
// [MB-S01]-Vent [rm-waiting]
// [MB-S01]-Vent [rm-angar_tech]
// [MB-S01]-Vent [rm-medical]
// [MB-S01]-Vent [rm-tech_ladder]
// [MB-S01]-Vent [rm-operators_work]
// [MB-S01]-Vent [rm-operators_fabric]
// [MB-S01]-Vent [rm-fabric]
// [MB-S01]-Vent [rm-sg_work]
// [MB-S01]-Vent [rm-operators_dors]
// [MB-S01]-Vent [rm-operators_weapon]

// [MB-S01]-sn [dr-inner-cabin-relaxation] [rm-cabin]
// [MB-S01]-dr [dr-inner-cabin-relaxation] [rm-cabin] [rm-relaxation]
// [MB-S01]-sn [dr-inner-cabin-relaxation] [rm-relaxation]

// [MB-S01]-sn [dr-inner-tladder-relaxation] [rm-tech_ladder]
// [MB-S01]-dr [dr-inner-tladder-relaxation] [rm-tech_ladder] [rm-relaxation]
// [MB-S01]-sn [dr-inner-tladder-relaxation] [rm-relaxation]

// [MB-S01]-sn [dr-inner-tladder-ofabric] [rm-tech_ladder]
// [MB-S01]-dr [dr-inner-tladder-ofabric] [rm-tech_ladder] [rm-operators_fabric]
// [MB-S01]-sn [dr-inner-tladder-ofabric] [rm-operators_fabric]

// [MB-S01]-sn [dr-inner-tladder-fabric] [rm-tech_ladder]
// [MB-S01]-dr [dr-inner-tladder-fabric] [rm-tech_ladder] [rm-fabric]
// [MB-S01]-sn [dr-inner-tladder-fabric] [rm-fabric]

// [MB-S01]-sn [dr-inner-tladder-owork] [rm-tech_ladder]
// [MB-S01]-dr [dr-inner-tladder-owork] [rm-tech_ladder] [rm-operators_work]
// [MB-S01]-sn [dr-inner-tladder-owork] [rm-operators_work]

// [MB-S01]-sn [dr-inner-ofabric-fabric] [rm-operators_fabric]
// [MB-S01]-dr [dr-inner-ofabric-fabric] [rm-operators_fabric] [rm-fabric]
// [MB-S01]-sn [dr-inner-ofabric-fabric] [rm-fabric]


// [MB-S01]-sn [dr-inner-waiting-relaxation] [rm-waiting]
// [MB-S01]-dr [dr-inner-waiting-relaxation] [rm-waiting] [rm-relaxation]
// [MB-S01]-sn [dr-inner-waiting-relaxation] [rm-relaxation]

// [MB-S01]-sn [dr-inner-waiting-medical] [rm-waiting]
// [MB-S01]-dr [dr-inner-waiting-medical] [rm-waiting] [rm-medical]
// [MB-S01]-sn [dr-inner-waiting-medical] [rm-medical]

// [MB-S01]-sn [dr-inner-sgwork-owork] [rm-sg_work]
// [MB-S01]-dr [dr-inner-sgwork-owork] [rm-sg_work] [rm-operators_work]
// [MB-S01]-sn [dr-inner-sgwork-owork] [rm-operators_work]

// [MB-S01]-sn [dr-inner-odors-awork] [rm-operators_dors]
// [MB-S01]-dr [dr-inner-odors-awork] [rm-operators_dors] [rm-angar_work]
// [MB-S01]-sn [dr-inner-odors-awork] [rm-angar_work]

// [MB-S01]-sn [dr-inner-oweapon-awork] [rm-operators_weapon]
// [MB-S01]-dr [dr-inner-oweapon-awork] [rm-operators_weapon] [rm-angar_work]
// [MB-S01]-sn [dr-inner-oweapon-awork] [rm-angar_work]

// [MB-S01]-sn [dr-gateway-cabin] [rm-cabin]
// [MB-S01]-dr [dr-gateway-cabin] [rm-cabin]
// [MB-S01]-dr [dr-gateway-cabin] [rm-space]
// [MB-S01]-sn [dr-gateway-cabin] [rm-space]

// [MB-S01]-sn [dr-gateway-relaxation] [rm-relaxation]
// [MB-S01]-dr [dr-gateway-relaxation] [rm-relaxation]
// [MB-S01]-dr [dr-gateway-relaxation] [rm-angar_tech]
// [MB-S01]-sn [dr-gateway-relaxation] [rm-angar_tech]

// [MB-S01]-sn [dr-gateway-owork] [rm-operators_work]
// [MB-S01]-dr [dr-gateway-owork] [rm-operators_work]
// [MB-S01]-dr [dr-gateway-owork] [rm-space]
// [MB-S01]-sn [dr-gateway-owork] [rm-space]

// [MB-S01]-sn [dr-gateway-sg_work2] [rm-sg_work]
// [MB-S01]-dr [dr-gateway-sg_work2] [rm-sg_work]
// [MB-S01]-dr [dr-gateway-sg_work2] [rm-angar_work]
// [MB-S01]-sn [dr-gateway-sg_work2] [rm-angar_work]

// [MB-S01]-sn [dr-gateway-angar_work] [rm-space]
// [MB-S01]-dr [dr-gateway-angar_work] [rm-space]
// [MB-S01]-dr [dr-gateway-angar_work] [rm-angar_work]
// [MB-S01]-sn [dr-gateway-angar_work] [rm-angar_work]

// [MB-S01]-sn [dr-gateway-sg_work_odors] [rm-sg_work]
// [MB-S01]-dr [dr-gateway-sg_work_odors] [rm-sg_work]
// [MB-S01]-dr [dr-gateway-sg_work_odors] [rm-operators_dors]
// [MB-S01]-sn [dr-gateway-sg_work_odors] [rm-operators_dors]

// [MB-S01]-sn [dr-gateway-sg_work_oweapon] [rm-sg_work]
// [MB-S01]-dr [dr-gateway-sg_work_oweapon] [rm-sg_work]
// [MB-S01]-dr [dr-gateway-sg_work_oweapon] [rm-operators_weapon]
// [MB-S01]-sn [dr-gateway-sg_work_oweapon] [rm-operators_weapon]

//sn [dr-inner-01] [rm-operators] - sn
//dr [dr-inner-01] [rm-operators] [rm-angar_tech] - door
//sn [dr-inner-01] [rm-angar_tech] - sn
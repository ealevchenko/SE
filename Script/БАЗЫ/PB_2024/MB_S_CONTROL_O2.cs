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
            fabric_tech = 18,
        };
        public static string[] name_room = { "КОСМОС", "КАЮТА",
            "КОМ. ОТДЫХА", "ТЕХ-ЛЕСТНИЦА", "ОПЕРАТОРСКАЯ", "ШЛЮЗ",
            "ТЕХ-АНГАР", "АНГАР-СБОРЩИК", "ТЕХ-ПЕРЕХОД-Л", "ТЕХ-ПЕРЕХОД-П",
            "КОМ. ОЖИДАНИЯ", "МЕД-БЛОК", "ОПЕР. СБОРЩИК", "ОПЕР. ФАБРИКА",
            "ФАБРИКА", "СБОРЩИК-МС", "ОПЕР. К-ДВ-О2" , "ОПЕР. К-ОРУЖИЯ", "ТЕХ-КОР-СБОРЩИК" };
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
        static LCD lcd_debug1;
        static LCD lcd_debug2;
        static LCD lcd_info1, lcd_info2;
        static LCD lcd_gate_angar_work1, lcd_gate_angar_work2;
        static LCD lcd_gt_angar_work1, lcd_gt_sg_work1, lcd_gt_big_angar_tech1, lcd_gt_small_angar_tech1;
        static LCD lcd_gt_angar_work2, lcd_gt_sg_work2, lcd_gt_big_angar_tech2, lcd_gt_small_angar_tech2;
        static Batterys bats;

        static Lightings lightings;
        static Gateway gateway;
        static Gate gt_angar_work, gt_sg_work, gt_big_angar_tech, gt_small_angar_tech;
        static O2Tanks o2_tanks_base;
        static BaseShipController cockpit;
        static MyStorage storage;
        static Control control;

        //int clock = 0;

        static Program _scr;

        class Help
        {
            static public string GetNameOfTemplate(string str, string tmp)
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
            static public float GetOxygenLevel(List<IMyAirVent> obj)
            {
                return obj != null && obj.Count() > 0 ? obj.ToList().Average(c => c.GetOxygenLevel()) : 0;
            }
            // Помещение разермитизированно
            static public bool isPressurizationEnabled(List<IMyAirVent> obj)
            {
                if (obj != null)
                {
                    foreach (IMyAirVent vn in obj)
                    {
                        if (!vn.PressurizationEnabled)
                        {
                            return false;
                        }
                    }

                }
                return true;
            }
            // Можно закачать кислород
            static public bool isCanPressurize(List<IMyAirVent> obj)
            {
                if (obj != null)
                {
                    foreach (IMyAirVent vn in obj)
                    {
                        if (!vn.CanPressurize)
                        {
                            return false;
                        }
                    }

                }
                return true;
            }
            // Режим откачки кислорода из помещения (вкл выкл)
            static public void Depressurize(List<IMyAirVent> obj, bool on)
            {
                if (obj != null)
                {
                    foreach (IMyAirVent vn in obj)
                    {
                        vn.Depressurize = on;
                    }
                }
            }
            static public void OpenClose(List<IMyDoor> obj, bool open)
            {
                if (obj != null)
                {
                    foreach (IMyDoor dr in obj)
                    {
                        if (open) dr.OpenDoor(); else dr.CloseDoor();
                    }
                }
            }
            static public bool isCloseDoors(List<IMyDoor> obj)
            {
                if (obj != null)
                {
                    foreach (IMyDoor dr in obj)
                    {
                        if (dr.Status != DoorStatus.Closed)
                        {
                            return false;
                        }
                    }

                }
                return true;
            }
            static public bool isOpenDoors(List<IMyDoor> obj)
            {
                if (obj != null)
                {
                    foreach (IMyDoor dr in obj)
                    {
                        if (dr.Status != DoorStatus.Open)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            static public float LevelDoors(List<IMyDoor> obj)
            {
                return obj != null && obj.Count() > 0 ? obj.ToList().Average(c => c.OpenRatio) : 0;
            }
        }
        public class RoomStatus
        {
            public room room { get; set; }
            public string name { get; set; }
            public float ox_level { get; set; }
            public VentStatus vent_status { get; set; }
            public int pipel_count { get; set; }
            public List<IMyDoor> doors = new List<IMyDoor>();
            public List<IMyAirVent> vents = new List<IMyAirVent>();
            public List<IMyInteriorLight> lights = new List<IMyInteriorLight>();
            public List<IMyTextPanel> panels = new List<IMyTextPanel>();
            public float cur_ox_level { get { return vents != null && vents.Count() > 0 ? vents.ToList().Average(c => c.GetOxygenLevel()) : 0; } }
            //public void PowerInner(bool on)
            //{
            //    if (doors != null && doors.Count() > 0)
            //    {
            //        foreach (IMyDoor dr in doors.Where(d => d.CustomName.Contains("[dr-inner-")).ToList())
            //        {
            //            if (on)
            //            {
            //                dr.ApplyAction("OnOff_On");
            //            }
            //            else
            //            {
            //                dr.ApplyAction("OnOff_Off");
            //            }
            //        }
            //    }
            //}
            public void Close()
            {
                if (doors != null && doors.Count() > 0)
                {
                    foreach (IMyDoor dr in doors)
                    {
                        dr.CloseDoor();
                    }
                }
            }
            public void Open()
            {
                if (doors != null && doors.Count() > 0)
                {
                    foreach (IMyDoor dr in doors)
                    {
                        dr.OpenDoor();
                    }
                }
            }
            public void Depressurize(bool on)
            {
                if (vents != null && vents.Count() > 0)
                {
                    foreach (IMyAirVent vn in vents)
                    {
                        vn.Depressurize = on;
                    }
                }
            }
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
            lcd_debug1 = new LCD(NameObj + "-LCD-DEBUG1");
            lcd_debug2 = new LCD(NameObj + "-LCD-DEBUG2");
            lcd_info1 = new LCD(NameObj + "-LCD-INFO-O2");
            lcd_info2 = new LCD(NameObj + "-LCD-INFO-RM");
            lcd_gate_angar_work1 = new LCD(NameObj + "-LCD-GATE-ANGAR-WORK1");
            lcd_gate_angar_work2 = new LCD(NameObj + "-LCD-GATE-ANGAR-WORK2");
            lcd_gt_angar_work1 = new LCD(NameObj + "-LCD-GATE-AW1");
            lcd_gt_angar_work2 = new LCD(NameObj + "-LCD-GATE-AW2");
            lcd_gt_sg_work1 = new LCD(NameObj + "-LCD-GATE-SW1");
            lcd_gt_sg_work2 = new LCD(NameObj + "-LCD-GATE-SW2");
            lcd_gt_big_angar_tech1 = new LCD(NameObj + "-LCD-GATE-BAT1");
            lcd_gt_big_angar_tech2 = new LCD(NameObj + "-LCD-GATE-BAT2");
            lcd_gt_small_angar_tech1 = new LCD(NameObj + "-LCD-GATE-SAT1");
            lcd_gt_small_angar_tech2 = new LCD(NameObj + "-LCD-GATE-SAT2");
            bats = new Batterys(NameObj);
            gt_angar_work = new Gate(NameObj, "dr-gate-angar_work");
            gt_sg_work = new Gate(NameObj, "dr-gate-sg-work");
            gt_big_angar_tech = new Gate(NameObj, "dr-gate-big-angar_tech");
            gt_small_angar_tech = new Gate(NameObj, "dr-gate-small-angar_tech");
            //connector_base = new Connector(NameObj + "-Коннектор base");
            lightings = new Lightings(NameObj, "[lighting]");
            lightings.Off();
            o2_tanks_base = new O2Tanks(NameObj, "[tank-O2]");
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
            //gt_angar_work.Logic(argument, updateSource);


            switch (argument) { default: break; }
            count_room[(int)room.space] = 0;// В космосе людей не считаем
            control.Logic(argument, updateSource);// Логика системы контроля питания
            if (updateSource == UpdateType.Update100)
            {

            }
            StringBuilder values = new StringBuilder();
            values.Append(bats.TextInfo(null));
            values.Append(o2_tanks_base.TextInfo("O2-НОСИТЕЛЯ"));
            //values.Append(connector_base.TextInfo("К:Base") + "\n");
            //values.Append(mergeblock_trusk1.TextInfo("СОЕД-НОСИТ-1"));
            //values.Append(mergeblock_trusk2.TextInfo("СОЕД-НОСИТ-2"));
            values.Append(control.TextStatus());
            cockpit.OutText(values, 0);

            lcd_info1.OutText(control.TextInfoO2(), false);
            lcd_info2.OutText(control.TextInfoDetali(), false);
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
            public float MaxPower { get { return base.list_obj.Select(b => b.MaxStoredPower).Sum(); } }
            public float CurrentPower { get { return base.list_obj.Select(b => b.CurrentStoredPower).Sum(); } }
            public float CurrentPersent { get { return base.list_obj.Select(b => b.CurrentStoredPower).Sum() / base.list_obj.Select(b => b.MaxStoredPower).Sum(); } }
            public float CountCharger { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Recharge).ToList().Count(); } }
            public float CountAuto { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Auto).ToList().Count(); } }
            public bool IsCharger { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Recharge).ToList().Count() > 0; } }
            public bool IsAuto { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Auto).ToList().Count() > 0; } }
            public Batterys(string name_obj) : base(name_obj) { }
            public Batterys(string name_obj, string tag) : base(name_obj, tag) { }
            public void Charger() { foreach (IMyBatteryBlock obj in base.list_obj) { obj.ChargeMode = ChargeMode.Recharge; } }
            public void Auto() { foreach (IMyBatteryBlock obj in base.list_obj) { obj.ChargeMode = ChargeMode.Auto; } }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append((!String.IsNullOrWhiteSpace(name) ? name : "БАТАРЕИ") + ": [" + Count + "] [А-" + CountAuto + " З-" + CountCharger + "]" + PText.GetCurrentOfMax(CurrentPower, MaxPower, "MW") + "\n");
                values.Append("|- ЗАР:  " + PText.GetScalePersent(CurrentPower / MaxPower, 20) + "\n");
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
        public class Inner
        {
            public string name;
            public string rm1 = null;
            public string rm2 = null;
            public int count1 = 0;
            public int count2 = 0;
            IMySensorBlock sn1;
            IMySensorBlock sn2;
            public IMyDoor dr;
            bool sn1_active = false;    // датчик входа
            bool sn2_active = false;   // датчик выхода
            public Inner(string name, IMyDoor dr, IMySensorBlock sn1, string rm1, IMySensorBlock sn2, string rm2)
            {
                this.name = name;
                this.dr = dr;
                this.rm1 = rm1;
                this.rm2 = rm2;
                this.sn1 = sn1;
                this.sn2 = sn2;
                this.dr.ApplyAction("OnOff_On");
                this.dr.CloseDoor();
                this.dr.CloseDoor();
            }
            public void Logic()
            {
                if (!sn1.IsActive && !sn2.IsActive && dr.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    dr.CloseDoor();
                }
                if ((sn1.IsActive || sn2.IsActive) && dr.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    dr.OpenDoor();
                }
                // Логика направдения движения
                if (sn1_active && !sn2_active && sn2.IsActive)
                {
                    // Выход
                    sn2_active = true;
                    count1--;
                    count2++;
                }
                if (sn2_active && !sn1_active && sn1.IsActive)
                {
                    // Вход
                    sn1_active = true;
                    count1++;
                    count2--;
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
                if (count1 < 0) count1 = 0;
                if (count2 < 0) count2 = 0;
            }


        }
        public class Gate
        {
            public bool open { get; set; } = false;
            public bool close { get; set; } = false;

            public string name;
            public string rm = null;
            //public int count1 = 0;
            //public int count2 = 0;
            public List<IMyDoor> drs;
            public List<IMyDoor> drs_inr;
            public List<IMyDoor> drs_gtw;
            List<IMyAirVent> vents;
            //bool sn1_active = false;    // датчик входа
            //bool sn2_active = false;   // датчик выхода
            public Gate(string name, List<IMyDoor> drs, string rm, List<IMyAirVent> vents, List<IMyDoor> drs_gtw, List<IMyDoor> drs_inr)
            {
                this.name = name;
                this.drs = drs;
                this.drs_inr = drs_inr;
                this.drs_gtw = drs_gtw;
                this.vents = vents;
                this.rm = rm;
                OnOff(true);
                this.Close();
            }
            public Gate(string NameObj, string name)
            {
                this.name = name;
                List<IMyDoor> dors = new List<IMyDoor>();
                List<IMyAirVent> vents = new List<IMyAirVent>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(dors, r => r.CustomName.Contains(NameObj));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents, r => r.CustomName.Contains(NameObj));
                this.drs = dors.Where(d => d.CustomName.Contains("[" + name + "]")).ToList();
                if (this.drs != null && this.drs.Count() > 0)
                {
                    this.rm = Help.GetNameOfTemplate(this.drs.ToList()[0].CustomName, "[rm-");
                    this.vents = vents.Where(d => d.CustomName.Contains(this.rm)).ToList();
                    this.drs_inr = dors.Where(d => d.CustomName.Contains("[dr-inner-") && d.CustomName.Contains(this.rm)).ToList();
                    this.drs_gtw = dors.Where(d => d.CustomName.Contains("[dr-gateway-") && d.CustomName.Contains(this.rm)).ToList();
                }
            }
            public void OnOff(bool on)
            {
                foreach (IMyDoor dr in drs)
                {
                    if (on) dr.ApplyAction("OnOff_On");
                    else dr.ApplyAction("OnOff_Off");
                }
            }
            private void Close() { Help.OpenClose(drs, false); }
            private void Open() { Help.OpenClose(drs, true); }
            public bool OpenGate()
            {
                //lcd_debug1.OutText("\nstart - OpenGate ", false);
                //lcd_debug1.OutText("\nthis.rm " + this.rm, true);
                bool result = false;
                bool o_gtw = Help.isOpenDoors(this.drs_gtw);
                //lcd_debug1.OutText("\no_gtw =" + o_gtw.ToString(), true);
                bool o_inr = Help.isOpenDoors(this.drs_inr);
                //lcd_debug1.OutText("\no_inr =" + o_inr.ToString(), true);
                float ol = Help.GetOxygenLevel(this.vents);
                //lcd_debug1.OutText("\nol =" + ol.ToString(), true);
                bool press = Help.isPressurizationEnabled(this.vents);
                //lcd_debug1.OutText("\npress =" + press.ToString(), true);
                // закрыть
                if (o_gtw)
                {
                    Help.OpenClose(this.drs_gtw, false);
                }
                if (o_inr)
                {
                    Help.OpenClose(this.drs_inr, false);
                }
                if (!o_gtw && !o_inr)
                {
                    if (ol > 0f && press && o2_tanks_base.AverageFilledRatio < 1.0)
                    {
                        Help.Depressurize(this.vents, true); // включить разгермитизацию
                    }
                    else
                    {
                        if (ol == 0f || (ol > 0f && o2_tanks_base.AverageFilledRatio == 1.0))
                        {
                            // Дверь открываем если кис. в помещении нет или баки полны и его некуда запускать
                            Help.OpenClose(this.drs, true);
                        }
                    }
                }
                result = Help.isOpenDoors(this.drs);
                //lcd_debug1.OutText("\nresult =" + result.ToString(), true);
                return result;
            }
            public bool CloseGate()
            {
                lcd_debug1.OutText("\nstart - OpenGate ", false);
                bool result = false;
                bool c_drs = Help.isCloseDoors(this.drs);
                bool c_gtw = Help.isCloseDoors(this.drs_gtw);
                bool c_inr = Help.isCloseDoors(this.drs_inr);
                bool o2_start = Help.isCanPressurize(this.vents);
                //bool press = Help.isPressurizationEnabled(this.vents);
                float ol = Help.GetOxygenLevel(this.vents);
                lcd_debug1.OutText("\nol =" + ol.ToString(), true);
                if (!c_drs)
                {
                    Help.OpenClose(this.drs, false);
                }
                if (!c_gtw)
                {
                    Help.OpenClose(this.drs_gtw, false);
                }
                if (!c_inr)
                {
                    Help.OpenClose(this.drs_inr, false);
                }
                if (c_drs && c_gtw && c_inr)
                {
                    Help.Depressurize(this.vents, !o2_start); // выключить подачу кислорода
                }
                if (ol >= 0.99f || (ol < 1f && o2_tanks_base.AverageFilledRatio == 0.0))
                {
                    // В помещение закачан кисл. или в баке нет кислорода
                    result = true;
                }
                lcd_debug1.OutText("\nresult =" + result.ToString(), true);
                return result;
            }
            public string TextInfoDetali(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append(("O2-БАКИ") + " : [" + o2_tanks_base.Count + "] [А-" + o2_tanks_base.CountAutoRefillBottles + " З-" + o2_tanks_base.CountStockpile + "]" + PText.GetCurrentOfMax((float)(o2_tanks_base.Capacity * o2_tanks_base.AverageFilledRatio) / 1000000, (float)o2_tanks_base.Capacity / 1000000, "МЛ") + "\n");
                values.Append("+- ЗАП:  " + PText.GetScalePersent(o2_tanks_base.AverageFilledRatio, 20) + "\n");
                if (this.vents != null && this.vents.Count() > 0)
                {
                    float ol = Help.GetOxygenLevel(this.vents);
                    values.Append("+-О2 в помещении" + PText.GetPersent(ol) + " Герметично " + (Help.isCanPressurize(this.vents) ? igreen.ToString() : ired.ToString()) + "\n");
                    values.Append("| " + PText.GetScalePersent(ol, 20) + "\n");
                    values.Append("+-Вентиляторы [" + this.vents.Count() + "]" + "\n");
                    foreach (IMyAirVent vnt in this.vents)
                    {
                        values.Append("  |-" + vnt.Status.ToString() + " - " + (vnt.Depressurize ? iblue.ToString() : igreen.ToString()) + "\n");
                    }
                }
                values.Append("=GATE " + name + " =\n");
                values.Append("+-Команда [open] " + (this.open ? igreen.ToString() : ired.ToString()) + " [close] " + (this.close ? igreen.ToString() : ired.ToString()) + " =\n");
                if (this.drs != null && this.drs.Count() > 0)
                {
                    values.Append("+- [O]:" + (Help.isOpenDoors(drs) ? igreen.ToString() : ired.ToString()) + " " + PText.GetScalePersent(Help.LevelDoors(drs), 20) + " " + (Help.isCloseDoors(drs) ? igreen.ToString() : ired.ToString()) + "[З] \n");
                }
                if (this.drs_gtw != null && this.drs_gtw.Count() > 0)
                {
                    values.Append("|\n");
                    values.Append("+-Двери 'GATEWAY' [" + drs_gtw.Count() + "]" + "\n");
                    foreach (IMyDoor dr in drs_gtw)
                    {
                        values.Append("| |-" + Help.GetNameOfTemplate(dr.CustomName, "[dr-") + " - " + dr.Status + " E:" + (dr.Enabled ? igreen.ToString() : ired.ToString()) +
                            " O:" + (dr.Status == DoorStatus.Open ? igreen.ToString() : (dr.Status == DoorStatus.Closed ? ired.ToString() : iyellow.ToString())) + "\n");
                    }
                }
                if (this.drs_inr != null && this.drs_inr.Count() > 0)
                {
                    values.Append("+-Двери 'INNER' [" + drs_gtw.Count() + "]" + "\n");
                    foreach (IMyDoor dr in drs_inr)
                    {
                        //IMyFunctionalBlock
                        values.Append("| |-" + Help.GetNameOfTemplate(dr.CustomName, "[dr-") + " - " + dr.Status + " E:" + (dr.Enabled ? igreen.ToString() : ired.ToString()) +
                            " O:" + (dr.Status == DoorStatus.Open ? igreen.ToString() : (dr.Status == DoorStatus.Closed ? ired.ToString() : iyellow.ToString())) + "\n");
                    }
                }
                return values.ToString();
            }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append("Ворота [" + name + "]\n");
                bool od = Help.isOpenDoors(drs);
                bool cd = Help.isCloseDoors(drs);
                values.Append("<<" + (this.open ? igreen.ToString() : idarkGrey.ToString()) +
                    "<<[Open]:" + (od ? igreen.ToString() : ired.ToString()) +
                    " " + PText.GetScalePersent(Help.LevelDoors(drs), 20) +
                    " [Close]" + (cd ? igreen.ToString() : ired.ToString()) +
                    ">>" + (this.close ? igreen.ToString() : idarkGrey.ToString()) + ">>" +
                    "\n");
                return values.ToString();
            }
            public void ToggleOpenClose()
            {
                if (!this.open && !this.close)
                {
                    this.close = Help.isOpenDoors(this.drs);
                    this.open = Help.isCloseDoors(this.drs);
                }
                else
                {
                    this.close = !this.close;
                    this.open = !this.open;
                }
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "open-gate": { this.open = true; this.close = false; break; }
                    case "close-gate": { this.open = false; this.close = true; ; break; }
                    default: break;
                }
                if (open && OpenGate()) open = false;
                if (close && CloseGate()) close = false;
            }
        }
        public class O2Tanks : BaseListTerminalBlock<IMyGasTank>
        {
            public O2Tanks(string name_obj) : base(name_obj) { AutoRefillBottles(true); }
            public O2Tanks(string name_obj, string tag) : base(name_obj, tag) { AutoRefillBottles(true); }
            public float MaxCapacity() { return base.list_obj.Select(b => b.Capacity).Sum(); }
            public double AverageFilledRatio { get { return base.list_obj != null && base.list_obj.Count() > 0 ? base.list_obj.Average(t => t.FilledRatio) : 0; } }
            public double CountAutoRefillBottles { get { return base.list_obj != null ? base.list_obj.Count(t => t.AutoRefillBottles) : 0; } }
            public double CountStockpile { get { return base.list_obj != null ? base.list_obj.Count(t => t.Stockpile) : 0; } }
            public double Capacity { get { return base.list_obj != null ? base.list_obj.Sum(t => t.Capacity) : 0; } }
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
            int index_rm = 1;
            public float curr_power_per { get; set; }
            List<RoomStatus> list_rs = new List<RoomStatus>();
            private List<IMyDoor> doors = new List<IMyDoor>();
            private List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            private List<IMyAirVent> vents = new List<IMyAirVent>();
            private List<IMyInteriorLight> lights = new List<IMyInteriorLight>();
            private List<IMyTextPanel> panels = new List<IMyTextPanel>();
            private List<IMyTextPanel> ipanels = new List<IMyTextPanel>();
            private List<Gateway> gateways = new List<Gateway>(); // Классы дверей шлюз (2дв 2датч.)
            private List<Inner> inners = new List<Inner>(); // Классы дверей межкоютных с учетом разницы O2 (1дв 2датч.)
            private List<Gate> gates = new List<Gate>(); // Классы ангарных дверей с учетом O2 (1...дв)

            public Control(string name)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, r => r.CustomName.Contains(name));
                _scr.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, r => r.CustomName.Contains(name));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents, r => r.CustomName.Contains(name));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(lights, r => r.CustomName.Contains(name));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, r => r.CustomName.Contains(name));
                ipanels = panels.Where(x => x.CustomName.Contains("[lcd-info]")).ToList();
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
                        doors = doors.Where(d => d.CustomName.Contains("[rm-" + group)).ToList(),
                        vents = vents.Where(d => d.CustomName.Contains("[rm-" + group)).ToList(),
                        lights = lights.Where(d => d.CustomName.Contains("[rm-" + group)).ToList(),
                        panels = ipanels.Where(d => d.CustomName.Contains("[rm-" + group)).ToList(),
                    };
                    rs.Close();
                    list_rs.Add(rs);
                }
                StringBuilder values = new StringBuilder();
                // gateway - получим шлюзы
                List<IGrouping<string, IMyDoor>> dr_gr = doors.Where(d => d.CustomName.Contains("[dr-gateway-")).GroupBy(g => Help.GetNameOfTemplate(g.CustomName, "[dr-gateway-")).ToList();
                List<IGrouping<string, IMyDoor>> dr_grin = doors.Where(d => d.CustomName.Contains("[dr-inner-")).GroupBy(g => Help.GetNameOfTemplate(g.CustomName, "[dr-inner-")).ToList();
                List<IGrouping<string, IMyDoor>> dr_gate = doors.Where(d => d.CustomName.Contains("[dr-gate-")).GroupBy(g => Help.GetNameOfTemplate(g.CustomName, "[dr-gate-")).ToList();
                lcd_debug.OutText("Старт ->", false);
                //lcd_debug.OutText("\nlist_rs -> " + list_rs.Count(), true);
                //lcd_debug.OutText("\ndoors -> " + doors.Count(), true);
                //lcd_debug.OutText("\nsensors -> " + sensors.Count(), true);
                //lcd_debug.OutText("\nvents -> " + vents.Count(), true);
                //lcd_debug.OutText("\nvlights -> " + lights.Count(), true);
                //lcd_debug.OutText("\npanels -> " + panels.Count(), true);
                //lcd_debug.OutText("\nipanels -> " + ipanels.Count(), true);
                lcd_debug.OutText("\ndr_gr -> " + dr_gr.Count(), true);
                lcd_debug.OutText("\ndr_grin -> " + dr_grin.Count(), true);
                lcd_debug.OutText("\ndr_gate -> " + dr_gate.Count(), true);
                // Настройка дверей Gateway
                foreach (IGrouping<string, IMyDoor> gtw in dr_gr)
                {
                    //lcd_debug.OutText("\nIGrouping -> " + gtw.Key, true);
                    //lcd_debug.OutText("\ngtw.Count() -> " + gtw.Count(), true);
                    if (gtw.Count() == 2)
                    {
                        // Первая дверь
                        IMyDoor dr1 = gtw.First();
                        //lcd_debug.OutText("\ndr1 -> " + dr1.CustomName, true);
                        string rm1 = Help.GetNameOfTemplate(dr1.CustomName, "[rm-");
                        //lcd_debug.OutText("\nrm1 -> " + rm1, true);
                        IMySensorBlock sn1 = sensors.Where(s => s.CustomName.Contains(gtw.Key) && s.CustomName.Contains(rm1)).FirstOrDefault();
                        //lcd_debug.OutText("\nsn1 -> " + sn1.CustomName, true);
                        // Вторая дверь
                        IMyDoor dr2 = gtw.Last();
                        //lcd_debug.OutText("\ndr2 -> " + dr2.CustomName, true);
                        string rm2 = Help.GetNameOfTemplate(dr2.CustomName, "[rm-");
                        //lcd_debug.OutText("\nrm2 -> " + rm2, true);
                        IMySensorBlock sn2 = sensors.Where(s => s.CustomName.Contains(gtw.Key) && s.CustomName.Contains(rm2)).FirstOrDefault();
                        //lcd_debug.OutText("\nsn2 -> " + sn2.CustomName, true);
                        Gateway dr_gtw = new Gateway(gtw.Key, dr1, sn1, rm1, dr2, sn2, rm2);
                        gateways.Add(dr_gtw);
                    }
                }
                // Настройка дверей inner
                foreach (IGrouping<string, IMyDoor> inr in dr_grin)
                {
                    //lcd_debug.OutText("\nIGrouping -> " + inr.Key, true);
                    //lcd_debug.OutText("\ngtw.Count() -> " + inr.Count(), true);
                    if (inr.Count() == 1)
                    {
                        // Первая дверь
                        IMyDoor dr = inr.First();
                        //lcd_debug.OutText("\ndr1 -> " + dr1.CustomName, true);

                        //lcd_debug.OutText("\nrm1 -> " + rm1, true);
                        List<IMySensorBlock> sns = sensors.Where(s => s.CustomName.Contains(inr.Key)).ToList();
                        IMySensorBlock sn1 = null;
                        IMySensorBlock sn2 = null;
                        string rm1 = null;
                        string rm2 = null;
                        if (sns.Count() == 2)
                        {
                            sn1 = sns.First();
                            rm1 = Help.GetNameOfTemplate(sn1.CustomName, "[rm-");
                            sn2 = sns.Last();
                            rm2 = Help.GetNameOfTemplate(sn2.CustomName, "[rm-");
                        }
                        //lcd_debug.OutText("\nrm2 -> " + rm2, true);
                        //lcd_debug.OutText("\nsn2 -> " + sn2.CustomName, true);
                        if (sn1 != null && sn2 != null && rm1 != null && rm2 != null)
                        {
                            Inner dr_inr = new Inner(inr.Key, dr, sn1, rm1, sn2, rm2);
                            inners.Add(dr_inr);
                        }
                    }
                }
                // Настройка ангарных дверей gates
                //foreach (IGrouping<string, IMyDoor> gts in dr_gate)
                //{
                //    if (gts != null && gts.Count() > 0)
                //    {
                //        string rm = Help.GetNameOfTemplate(gts.ToList()[0].CustomName, "[rm-");
                //        List<IMyAirVent> vnts = vents.Where(d => d.CustomName.Contains(rm)).ToList();
                //        List<IMyDoor> drinr = doors.Where(d => d.CustomName.Contains("[dr-inner-") && d.CustomName.Contains(rm)).ToList();
                //        List<IMyDoor> drgtw = doors.Where(d => d.CustomName.Contains("[dr-gateway-") && d.CustomName.Contains(rm)).ToList();
                //        Gate gt = new Gate(gts.Key, gts.ToList(), rm, vnts, drgtw, drinr);
                //        gates.Add(gt);
                //    }
                //}
                //lcd_debug.OutText("\ngateways.Count() -> " + gateways.Count(), true);
                lcd_debug.OutText("\ninners.Count() -> " + inners.Count(), true);
                lcd_debug.OutText("\ngates.Count() -> " + gates.Count(), true);
            }

            public void SetTextLCDInfo(room rm, Color color)
            {
                List<IMyTextPanel> objs = panels.Where(x => x.CustomName.Contains("[rm-" + rm.ToString() + "]")).ToList();
                SetTextLCDInfo(objs, rm, color);
            }
            public void SetTextLCDInfo(List<IMyTextPanel> objs, room rm, Color color)
            {
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
                gt_angar_work.Logic(argument, updateSource);
                gt_sg_work.Logic(argument, updateSource);
                gt_big_angar_tech.Logic(argument, updateSource);
                gt_small_angar_tech.Logic(argument, updateSource);
                lcd_gate_angar_work1.OutText(gt_angar_work.TextInfoDetali("Ангар-сборщика"), false);
                lcd_gt_angar_work1.OutText(gt_angar_work.TextInfo("АНГАР-СБОРЩИК"), false);
                lcd_gt_angar_work2.OutText(gt_angar_work.TextInfo("АНГАР-СБОРЩИК"), false);
                lcd_gt_sg_work1.OutText(gt_sg_work.TextInfo("СБОРЩИК"), false);
                lcd_gt_sg_work2.OutText(gt_sg_work.TextInfo("СБОРЩИК"), false);
                lcd_gt_big_angar_tech1.OutText(gt_big_angar_tech.TextInfo("БВ-ТЕХ-АНГАР"), false);
                lcd_gt_big_angar_tech2.OutText(gt_big_angar_tech.TextInfo("БВ-ТЕХ-АНГАР"), false);
                lcd_gt_small_angar_tech1.OutText(gt_small_angar_tech.TextInfo("МВ-ТЕХ-АНГАР"), false);
                lcd_gt_small_angar_tech2.OutText(gt_small_angar_tech.TextInfo("МВ-ТЕХ-АНГАР"), false);
                switch (argument)
                {
                    case "rm+":
                        {
                            if (index_rm > list_rs.Count() - 1) index_rm = 1;
                            else index_rm++;
                            break;
                        }
                    case "rm-":
                        {
                            if (index_rm < 1) index_rm = list_rs.Count() - 1;
                            else index_rm--;
                            break;
                        }
                    case "gt_aw_open":
                        {
                            gt_angar_work.open = true; gt_angar_work.close = false; break;
                        }
                    case "gt_aw_close":
                        {
                            gt_angar_work.open = false; gt_angar_work.close = true; break;
                        }
                    case "gt_aw_toggle":
                        {
                            gt_angar_work.ToggleOpenClose(); break;
                        }
                    case "gt_sw_open":
                        {
                            gt_sg_work.open = true; gt_sg_work.close = false; break;
                        }
                    case "gt_sw_close":
                        {
                            gt_sg_work.open = false; gt_sg_work.close = true; break;
                        }
                    case "gt_sw_toggle":
                        {
                            gt_sg_work.ToggleOpenClose(); break;
                        }
                    case "gt_bat_open":
                        {
                            gt_big_angar_tech.open = true; gt_big_angar_tech.close = false; break;
                        }
                    case "gt_bat_close":
                        {
                            gt_big_angar_tech.open = false; gt_big_angar_tech.close = true; break;
                        }
                    case "gt_bat_toggle":
                        {
                            gt_big_angar_tech.ToggleOpenClose(); break;
                        }
                    case "gt_sat_open":
                        {
                            gt_small_angar_tech.open = true; gt_small_angar_tech.close = false; break;
                        }
                    case "gt_sat_close":
                        {
                            gt_small_angar_tech.open = false; gt_small_angar_tech.close = true; break;
                        }
                    case "gt_sat_toggle":
                        {
                            gt_small_angar_tech.ToggleOpenClose(); break;
                        }
                    case "load":
                        storage.LoadFromStorage();
                        break;
                    case "save":
                        storage.SaveToStorage();
                        break;
                    default:
                        break;
                }
                //StringBuilder values = new StringBuilder();
                //lcd_debug1.OutText("Кислород------", false);
                foreach (RoomStatus rs in list_rs)
                {
                    float Ol = rs.cur_ox_level;
                    //lcd_debug1.OutText("\nПомещение " + rs.room + ", Ol=" + Ol, true);
                    //lcd_debug1.OutText("\ndoors " + rs.doors.Count(), true);
                    //rs.PowerInner(Ol > 0.9); // Отключить двери нет воздуха
                    if (Ol > 0.9)
                    {
                        SetTextLCDInfo(rs.panels, rs.room, green);
                    }
                    else if (Ol == 0)
                    {
                        SetTextLCDInfo(rs.room, red);
                    }
                    else
                    {
                        SetTextLCDInfo(rs.room, yellow);
                    }
                }
                // Проверим двери
                foreach (Gateway gtw in gateways)
                {
                    gtw.Logic();
                }
                //StringBuilder values1 = new StringBuilder();
                lcd_debug2.OutText("ДВЕРИ------", false);
                foreach (Inner inr in inners)
                {

                    RoomStatus rs1 = list_rs.Where(l => "[rm-" + l.room.ToString() + "]" == inr.rm1).FirstOrDefault();
                    RoomStatus rs2 = list_rs.Where(l => "[rm-" + l.room.ToString() + "]" == inr.rm2).FirstOrDefault();
                    lcd_debug2.OutText("\ninr -" + inr.name + ", rm1 -" + inr.rm1 + ", rm2 -" + inr.rm2, true);
                    bool bol1 = rs1 != null ? rs1.cur_ox_level > 0.9 : false;
                    bool bol2 = rs2 != null ? rs2.cur_ox_level > 0.9 : false;
                    lcd_debug2.OutText("\nbol1=" + bol1.ToString() + "bol2=" + bol2.ToString(), true);
                    if (bol1 != bol2)
                    {
                        inr.dr.ApplyAction("OnOff_Off");
                        lcd_debug2.OutText("\nOFF", true);
                    }
                    else
                    {
                        inr.dr.ApplyAction("OnOff_On");
                        lcd_debug2.OutText("\nON", true);
                        inr.Logic();
                    }
                }
                if (updateSource == UpdateType.Update100)
                {
                    curr_power_per = (bats.CurrentPower / bats.MaxPower * 100.0f);
                }
            }
            public string TextInfoO2()
            {
                StringBuilder values = new StringBuilder();
                values.Append("==СПИСОК ПОМЕЩЕНИЙ==\n");
                foreach (RoomStatus rs in list_rs)
                {
                    if ((int)rs.room > 0)
                    {
                        float Ol = rs.cur_ox_level;
                        if ((int)rs.room == index_rm)
                        {

                        }
                        values.Append(((int)rs.room == index_rm ? "->" : "  ") + "[" + ((Ol > 0.9) ? igreen.ToString() : ((Ol == 0) ? ired.ToString() : iyellow.ToString())) + "]" + rs.name + "\t" + PText.GetPersent(Ol) + "\n");
                    }
                }
                return values.ToString();
            }
            public string TextInfoDetali()
            {
                StringBuilder values = new StringBuilder();
                RoomStatus rs = list_rs.Where(l => l.room.ToString() == ((room)index_rm).ToString()).FirstOrDefault();
                if (rs != null)
                {
                    values.Append("==ПОМЕЩЕНИЕ [ " + rs.name + " ]==\n");
                    values.Append("+-О2 в помещении" + PText.GetPersent(rs.cur_ox_level) + "\n");
                    values.Append("| " + PText.GetScalePersent(rs.cur_ox_level, 20) + "\n");
                    values.Append("+-Вентиляторы [" + rs.vents.Count() + "]" + "\n");
                    foreach (IMyAirVent vnt in rs.vents)
                    {
                        values.Append("| |-" + vnt.Status.ToString() + " - " + (vnt.Depressurize ? iblue.ToString() : igreen.ToString()) + "\n");
                    }
                    if (rs.doors != null && rs.doors.Count() > 0)
                    {
                        List<IMyDoor> drs_gtw = rs.doors.Where(d => d.CustomName.Contains("[dr-gateway-")).ToList();
                        List<IMyDoor> drs_inr = rs.doors.Where(d => d.CustomName.Contains("[dr-inner-")).ToList();
                        List<IMyDoor> drs_gate = rs.doors.Where(d => d.CustomName.Contains("[dr-gate-")).ToList();
                        if (drs_gtw != null)
                        {
                            values.Append("+-Двери 'GATEWAY' [" + drs_gtw.Count() + "]" + "\n");
                            foreach (IMyDoor dr in drs_gtw)
                            {
                                //IMyFunctionalBlock
                                values.Append("| |-" + Help.GetNameOfTemplate(dr.CustomName, "[dr-") + " - " + dr.Status + " E:" + (dr.Enabled ? igreen.ToString() : ired.ToString()) +
                                    " O:" + (dr.Status == DoorStatus.Open ? igreen.ToString() : (dr.Status == DoorStatus.Closed ? ired.ToString() : iyellow.ToString())) + "\n");
                            }
                        }
                        if (drs_inr != null)
                        {
                            values.Append("+-Двери 'INNER' [" + drs_gtw.Count() + "]" + "\n");
                            foreach (IMyDoor dr in drs_inr)
                            {
                                //IMyFunctionalBlock
                                values.Append("| |-" + Help.GetNameOfTemplate(dr.CustomName, "[dr-") + " - " + dr.Status + " E:" + (dr.Enabled ? igreen.ToString() : ired.ToString()) +
                                    " O:" + (dr.Status == DoorStatus.Open ? igreen.ToString() : (dr.Status == DoorStatus.Closed ? ired.ToString() : iyellow.ToString())) + "\n");
                            }
                        }
                        if (drs_gate != null)
                        {
                            values.Append("+-Двери 'GATE' [" + drs_gate.Count() + "]" + "\n");
                            //foreach (IMyDoor dr in drs_inr)
                            //{
                            //    //IMyFunctionalBlock
                            //    values.Append("| |-" + GetNameOfTemplate(dr.CustomName, "[dr-") + " - " + dr.Status + " E:" + (dr.Enabled ? igreen.ToString() : ired.ToString()) +
                            //        " O:" + (dr.Status == DoorStatus.Open ? igreen.ToString() : (dr.Status == DoorStatus.Closed ? ired.ToString() : iyellow.ToString())) + "\n");
                            //}
                        }
                    }
                }
                return values.ToString();
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

// [MB-S01] -К1 [connect] [rm-angar_tech]

//[rm-space]
//[rm-cabin]
//[rm-relaxation]
//[rm-tech_ladder]
//[rm-operators_fabric]
//[rm-fabric]

// [MB-S01]-Gate [dr-gate-sg-work] [rm-sg_work]
// [MB-S01]-Gate [dr-gate-angar_work] [rm-angar_work]
// [MB-S01]-Gate [dr-gate-big-angar_tech] [rm-angar_tech]
// [MB-S01]-Gate [dr-gate-small-angar_tech] [rm-angar_tech]

// [MB-S01]-Вн. турель [rm-angar_work]


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
// [MB-S01]-[lcd-info] [rm-gateway]
// [MB-S01]-[lcd-info] [rm-transition_right]
// [MB-S01]-[lcd-info] [rm-transition_left]
// [MB-S01]-[lcd-info] [rm-fabric_tech]

// [MB-S01]-Кнопка gate-angar_work [internal]

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
// [MB-S01]-Vent [rm-gateway]
// [MB-S01]-Vent [rm-transition_right]
// [MB-S01]-Vent [rm-transition_left]
// [MB-S01]-Vent [rm-fabric_tech]

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

// [MB-S01]-sn [dr-inner-gateway-rtransition] [rm-gateway]
// [MB-S01]-dr [dr-inner-gateway-rtransition] [rm-gateway] [rm-transition_right]
// [MB-S01]-sn [dr-inner-gateway-rtransition] [rm-transition_right]

// [MB-S01]-sn [dr-inner-tfabric-rtransition] [rm-fabric_tech]
// [MB-S01]-dr [dr-inner-tfabric-rtransition] [rm-fabric_tech] [rm-transition_right]
// [MB-S01]-sn [dr-inner-tfabric-rtransition] [rm-transition_right]

// [MB-S01]-sn [dr-inner-tfabric-fabric] [rm-fabric_tech]
// [MB-S01]-dr [dr-inner-tfabric-fabric] [rm-fabric_tech] [rm-fabric]
// [MB-S01]-sn [dr-inner-tfabric-fabric] [rm-fabric]

// [MB-S01]-sn [dr-inner-doperators-rtransition] [rm-operators_dors]
// [MB-S01]-dr [dr-inner-doperators-rtransition] [rm-operators_dors] [rm-transition_right]
// [MB-S01]-sn [dr-inner-doperators-rtransition] [rm-transition_right]

// [MB-S01]-sn [dr-inner-oweapon-ltransition] [rm-operators_weapon]
// [MB-S01]-dr [dr-inner-oweapon-ltransition] [rm-operators_weapon] [rm-transition_left]
// [MB-S01]-sn [dr-inner-oweapon-ltransition] [rm-transition_left]

// [MB-S01]-sn [dr-inner-tfabric-ltransition] [rm-fabric_tech]
// [MB-S01]-dr [dr-inner-tfabric-ltransition] [rm-fabric_tech] [rm-transition_left]
// [MB-S01]-sn [dr-inner-tfabric-ltransition] [rm-transition_left]

// [MB-S01]-sn [dr-inner-gateway-ltransition] [rm-gateway]
// [MB-S01]-dr [dr-inner-gateway-ltransition] [rm-gateway] [rm-transition_left]
// [MB-S01]-sn [dr-inner-gateway-ltransition] [rm-transition_left]

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

// [MB-S01]-sn [dr-gateway-gateway] [rm-gateway]
// [MB-S01]-dr [dr-gateway-gateway] [rm-gateway]
// [MB-S01]-dr [dr-gateway-gateway] [rm-space]
// [MB-S01]-sn [dr-gateway-gateway] [rm-space]

// [MB-S01]-sn [dr-gateway-rtransition] [rm-transition_right]
// [MB-S01]-dr [dr-gateway-rtransition] [rm-transition_right]
// [MB-S01]-dr [dr-gateway-rtransition] [rm-angar_tech]
// [MB-S01]-sn [dr-gateway-rtransition] [rm-angar_tech]

// [MB-S01]-sn [dr-gateway-ltransition] [rm-transition_left]
// [MB-S01]-dr [dr-gateway-ltransition] [rm-transition_left]
// [MB-S01]-dr [dr-gateway-ltransition] [rm-angar_tech]
// [MB-S01]-sn [dr-gateway-ltransition] [rm-angar_tech]

// [MB-S01]-sn [dr-gateway-rtransition-space] [rm-transition_right]
// [MB-S01]-dr [dr-gateway-rtransition-space] [rm-transition_right]
// [MB-S01]-dr [dr-gateway-rtransition-space] [rm-space]
// [MB-S01]-sn [dr-gateway-rtransition-space] [rm-space]

// [MB-S01]-sn [dr-gateway-ltransition-space] [rm-transition_left]
// [MB-S01]-dr [dr-gateway-ltransition-space] [rm-transition_left]
// [MB-S01]-dr [dr-gateway-ltransition-space] [rm-space]
// [MB-S01]-sn [dr-gateway-ltransition-space] [rm-space]

//sn [dr-inner-01] [rm-operators] - sn
//dr [dr-inner-01] [rm-operators] [rm-angar_tech] - door
//sn [dr-inner-01] [rm-angar_tech] - sn
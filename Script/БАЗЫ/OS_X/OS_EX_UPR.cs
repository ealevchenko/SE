using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using static SPB_MARS_DORS.Program;

/// <summary>
/// v1.0
/// Управление дверями и освещением на станции.
/// </summary>
namespace OS_EX_UPR
{
    public sealed class Program : MyGridProgram
    {
        // v1.
        string NameObj = "[OS-E1]";

        string tag_door_gateway = "[door-gateway]";
        string tag_lighting_room = "[lighting_room]";
        string tag_info_tablo = "[door-info]";
        public enum room : int
        {
            space = 0,
            operators = 1,
            station = 2,
            gateway = 3,
        };
        public static string[] name_room = { "КОСМОС", "ОПЕРАТОРСКАЯ", "СТАНЦИЯ", "ШЛЮЗ" };
        public static int[] count_room = { 0, 0, 0, 0 };
        public enum doors_gareways : int
        {
            operators_space_left_down = 0,
            operators_space_right_down = 1,
            operators_space_left_up = 2,
            operators_space_right_up = 3,
        }

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

        static ReflectorsLight reflectors_light;
        static Gateways gateways_doors;
        static Lightings room_light;
        static AirInfo air_info;
        static AirVent air_vent;
        static MyStorage storage;
        static Camera camera_course;
        static ShipController cockpit_nav;
        static Gyros gyros;
        static Thrusts thrusts;
        static SolarPower solar_power;

        static Program _scr;
        public class PText
        {
            static public string GetPersent(double perse) { return " - " + Math.Round((perse * 100), 1) + "%"; }
            static public string GetScalePersent(double perse, int scale) { string prog = "["; for (int i = 0; i < Math.Round((perse * scale), 0); i++) { prog += "|"; } for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++) { prog += "'"; } prog += "]" + GetPersent(perse); return prog; }
            static public string GetCurrentOfMax(float cur, float max, string units) { return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetThrust(float value) { return Math.Round(value / 1000000, 1) + "МН"; }
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
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            air_vent = new AirVent(NameObj);
            air_vent.On();
            air_info = new AirInfo(NameObj, tag_info_tablo);
            gateways_doors = new Gateways(NameObj, tag_door_gateway);
            room_light = new Lightings(NameObj, tag_lighting_room); // Освещение
            room_light.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            camera_course = new Camera(NameObj + "-Камера [curse]");
            cockpit_nav = new ShipController(NameObj + "-Cocpit navigation [LCD]");
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
            thrusts.InitThrusts(cockpit_nav);
            solar_power = new SolarPower();
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
            solar_power.Logic(argument, updateSource);
            air_info.Logic(argument, updateSource);
            gateways_doors.Logic(argument, updateSource);// Логика отработки шлюзовых дверей
            count_room[(int)room.space] = 0;// В космосе людей не считаем
            room_light.Logic(argument, updateSource);// Логика отработки включения и выключения освещения
            if (updateSource == UpdateType.Update10)
            {
                
            }
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
            public int count_work_batterys { get { return list_obj.Count(); } }
            public Batterys(string name_obj) : base(name_obj) { base.On(); }
            public Batterys(string name_obj, string tag) : base(name_obj, tag) { base.On(); }
            public float MaxPower() { return base.list_obj.Select(b => b.MaxStoredPower).Sum(); }
            public float CurrentPower() { return base.list_obj.Select(b => b.CurrentStoredPower).Sum(); }
            public float CurrentPersent() { return base.list_obj.Select(b => b.CurrentStoredPower).Sum() / base.list_obj.Select(b => b.MaxStoredPower).Sum(); }
            public int CountCharger() { return base.list_obj.Select(b => b.ChargeMode == ChargeMode.Recharge).Count(); }
            public int CountAuto() { return base.list_obj.Select(b => b.ChargeMode == ChargeMode.Auto).Count(); }
            public bool IsCharger() { int count_charger = CountCharger(); return count_work_batterys > 0 && count_charger > 0 && count_work_batterys == count_charger ? true : false; }
            public bool IsAuto() { int count_auto = CountAuto(); return Count > 0 && count_auto > 0 && Count == count_auto ? true : false; }
            public void Charger() { foreach (IMyBatteryBlock obj in base.list_obj) { obj.ChargeMode = ChargeMode.Recharge; } }
            public void Auto() { foreach (IMyBatteryBlock obj in base.list_obj) { obj.ChargeMode = ChargeMode.Auto; } }
            public void Logic(string argument, UpdateType updateSource) { switch (argument) { default: break; } if (updateSource == UpdateType.Update10) { } }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("БАТАРЕЯ: [" + Count + "] [А-" + CountAuto() + " З-" + CountCharger() + "]" + PText.GetCurrentOfMax(CurrentPower(), MaxPower(), "MW") + "\n");
                values.Append("|- ЗАР:  " + PText.GetScalePersent(CurrentPower() / MaxPower(), 20) + "\n");
                return values.ToString();
            }
        }
        public class Connector : BaseTerminalBlock<IMyShipConnector>
        {
            public MyShipConnectorStatus Status { get { return base.obj.Status; } }
            public bool Connected { get { return base.obj.Status == MyShipConnectorStatus.Connected ? true : false; } }
            public bool Unconnected { get { return base.obj.Status == MyShipConnectorStatus.Unconnected ? true : false; } }
            public bool Connectable { get { return base.obj.Status == MyShipConnectorStatus.Connectable ? true : false; } }
            public Connector(string name) : base(name) { if (base.obj != null) { } }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("КОННЕКТОР: " + (Connected ? igreen.ToString() : (Connectable ? iyellow.ToString() : ired.ToString())) + "\n");
                return values.ToString();
            }
            public long? getEntityIdRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn.EntityId; } return null; }
            public IMyShipConnector getRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn; } return null; }
        }
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorsLight(string name_obj) : base(name_obj) { }
            public ReflectorsLight(string name_obj, string tag) : base(name_obj, tag) { }
        }
        public class Gateway
        {
            doors_gareways door_gtw;
            IMySensorBlock sn1;
            IMySensorBlock sn2;
            room rm1;
            IMyDoor door1;
            IMyDoor door2;
            room rm2;
            bool sn1_active = false;    // датчик входа
            bool sn2_active = false;   // датчик выхода
            public Gateway(doors_gareways dg, IMyDoor door1, IMySensorBlock sn1, room rm1, IMyDoor door2, IMySensorBlock sn2, room rm2)
            {
                this.door_gtw = dg;
                this.rm1 = rm1;
                this.rm2 = rm2;
                this.sn1 = sn1;
                string sn1_cd = sn1.CustomData; // 1.0f, 1.0f, 2.5f, 1.0f, 0.1f, 2.5f
                this.sn2 = sn2;
                this.door1 = door1;
                this.door2 = door2;
                this.door1.ApplyAction("OnOff_On");
                this.door2.ApplyAction("OnOff_On");
                this.door1.CloseDoor();
                this.door2.CloseDoor();
            }
            public void Logic()
            {
                if (!sn1.IsActive && door1.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    door1.CloseDoor();
                }
                if (sn1.IsActive && door1.Status == DoorStatus.Closed && door2.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    door1.OpenDoor();
                }
                if (!sn2.IsActive && door2.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    door2.CloseDoor();
                }
                if (sn2.IsActive && door2.Status == DoorStatus.Closed && door1.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    door2.OpenDoor();
                }
                // Логика направления движения
                if (sn1_active && !sn2_active && sn2.IsActive)
                {
                    // Выход
                    sn2_active = true;
                    count_room[(int)rm1]--;
                    count_room[(int)rm2]++;
                    storage.SaveToStorage();
                }
                if (sn2_active && !sn1_active && sn1.IsActive)
                {
                    // Вход
                    sn1_active = true;
                    count_room[(int)rm1]++;
                    count_room[(int)rm2]--;
                    storage.SaveToStorage();
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
                if (count_room[(int)rm1] < 0) { count_room[(int)rm1] = 0; storage.SaveToStorage(); }

                if (count_room[(int)rm2] < 0) { count_room[(int)rm2] = 0; storage.SaveToStorage(); }
               
            }
        }
        public class Gateways
        {
            private List<IMyDoor> doors = new List<IMyDoor>();
            private List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            List<Gateway> list_gtw = new List<Gateway>();
            public Gateways(string name_obj, string tag)
            {
                //lcd_debug.OutText("Start" + "\n", false);
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                _scr.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                //lcd_debug.OutText("doors:" + doors.Count() + "\n", true);
                //lcd_debug.OutText("sensors:" + doors.Count() + "\n", true);
                IMyDoor door1;
                IMySensorBlock sensor1;
                room room1;
                IMyDoor door2;
                IMySensorBlock sensor2;
                room room2;
                //lcd_debug.OutText("Поиск дверей:" + "\n", true);
                foreach (doors_gareways gw in Enum.GetValues(typeof(doors_gareways)))
                {
                    door1 = null;
                    sensor1 = null;
                    room1 = room.space;
                    door2 = null;
                    sensor2 = null;
                    room2 = room.space;

                    List<IMyDoor> l_drs = doors.Where(d => d.CustomName.Contains("[" + gw.ToString() + "]")).ToList();
                    List<IMySensorBlock> l_sns = sensors.Where(d => d.CustomName.Contains("[" + gw.ToString() + "]")).ToList();
                    //lcd_debug.OutText("l_drs:" + l_drs.Count() + "\n", true);
                    //lcd_debug.OutText("l_sns:" + l_sns.Count() + "\n", true);
                    if (l_drs != null && l_drs.Count() == 2 && l_sns != null && l_sns.Count() == 2)
                    {
                        foreach (room rm in Enum.GetValues(typeof(room)))
                        {
                            //lcd_debug.OutText("room:" + rm + "\n", true);
                            IMyDoor dr = l_drs.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
                            IMySensorBlock sn = l_sns.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
                            //lcd_debug.OutText("dr:" + (dr != null ? "ok" : "not") + "\n", true);
                            //lcd_debug.OutText("sn:" + (sn != null ? "ok" : "not") + "\n", true);
                            if (dr != null && sn != null)
                            {
                                if (door1 != null && door2 == null) { door2 = dr; room2 = rm; }
                                if (door1 == null) { door1 = dr; room1 = rm; }
                                if (sensor1 != null && sensor2 == null) { sensor2 = sn; }
                                if (sensor1 == null) { sensor1 = sn; }
                            }
                        }
                        if (door1 != null && door2 != null && sensor1 != null && sensor2 != null)
                        {
                            //lcd_debug.OutText("door1:" + door1.CustomName + "\n", true);
                            //lcd_debug.OutText("door2:" + door2.CustomName + "\n", true);
                            //lcd_debug.OutText("sensor1:" + sensor1.CustomName + "\n", true);
                            //lcd_debug.OutText("sensor2:" + sensor2.CustomName + "\n", true);
                            list_gtw.Add(new Gateway(gw, door1, sensor1, room1, door2, sensor2, room2));
                        }
                    }
                }
                _scr.Echo("Найдено Gateways:[" + tag + "]: " + list_gtw.Count());
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
                    foreach (Gateway gateway in list_gtw)
                    {
                        gateway.Logic();
                    }
                }

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
        public class InfoTablo : BaseListTerminalBlock<IMyTextPanel>
        {
            string tag;
            //List<IMyTextPanel> list = new List<IMyTextPanel>();
            public InfoTablo(string name_obj, string tag) : base(name_obj)
            {
                this.tag = tag;
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    base.list_obj = list_obj.Where(x => x.CustomName.Contains(this.tag)).ToList();
                }
                _scr.Echo("Найдено TextPanel:[" + tag + "]: " + base.list_obj.Count());
                //list = list_obj.Where(x => x.CustomName.Contains(this.tag)).ToList();
            }
            public void InitPanel()
            {
                // Пройдемся по помещениям и настроим панели
                foreach (room group in Enum.GetValues(typeof(room)))
                {
                    SetText(group, green);
                }
            }
            public void SetText(room rm, Color color)
            {
                List<IMyTextPanel> objs = base.list_obj.Where(x => x.CustomName.Contains("[" + rm.ToString() + "]")).ToList();
                foreach (IMyTextPanel obj in objs)
                {
                    obj.SetValue("Content", (Int64)1);
                    obj.SetValueColor("FontColor", color);
                    obj.SetValueFloat("FontSize", 7.0f);
                    obj.SetValue("alignment", (Int64)2);
                    obj.WriteText(name_room[(int)rm].ToUpper(), false);
                }
            }
        }
        // Вентиляторы
        public class AirVent : BaseListTerminalBlock<IMyAirVent>
        {
            public AirVent(string name_obj) : base(name_obj)
            {

            }
            public VentStatus? getStatus(string tag)
            {
                IMyAirVent obj = list_obj.Where(x => x.CustomName.Contains(tag)).FirstOrDefault();
                return obj != null ? (VentStatus?)obj.Status : null;
            }
            public float? GetOxygenLevel(string tag)
            {
                IMyAirVent obj = list_obj.Where(x => x.CustomName.Contains(tag)).FirstOrDefault();
                return obj != null ? (float?)obj.GetOxygenLevel() : null;
            }
            public bool isOxygenLevelNull(string tag)
            {
                float? ox = GetOxygenLevel(tag);
                return ox != null && ox < 0.8f ? true : false;
            }
            public bool isOxygenLevelNull(string[] tags)
            {
                foreach (string tag in tags)
                {
                    if (isOxygenLevelNull(tag))
                    {
                        return true;
                    }
                }
                return false;
            }

        }
        // Класс формирования подписей над дверями с учетом кислорода в помещении
        public class AirInfo
        {
            InfoTablo info_tablo;
            AirVent air_vant;
            public AirInfo(string name_obj, string tag)
            {
                info_tablo = new InfoTablo(name_obj, tag);
                info_tablo.InitPanel();
                air_vant = new AirVent(name_obj);
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    //case "connected_on":
                    //    break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {
                    //test_lcd.WriteText("Старт" + "\n", false);
                    foreach (room group in Enum.GetValues(typeof(room)))
                    {
                        float? o2 = air_vant.GetOxygenLevel("[" + group.ToString() + "]");
                        if (o2 != null)
                        {
                            if (o2 > 0.9)
                            {
                                info_tablo.SetText(group, green);
                            }
                            else if (o2 == 0)
                            {
                                info_tablo.SetText(group, red);
                            }
                            else
                            {
                                info_tablo.SetText(group, yellow);
                            }
                        }
                    }
                }

            }
        }
        public class Camera : BaseTerminalBlock<IMyCameraBlock>
        {
            public Camera(string name) : base(name) { base.obj.EnableRaycast = true; }
            public MyDetectedEntityInfo? Raycast(double dist_scan, float pitch_scan, float yaw_scan)
            {
                MyDetectedEntityInfo? result = null;
                if (base.obj.CanScan(dist_scan))
                {
                    result = base.obj.Raycast(dist_scan, pitch_scan, yaw_scan);
                }
                return result;
            }
            public string TextInfo() { StringBuilder values = new StringBuilder(); return values.ToString(); }
            public string GetTextDetectedEntityInfo(MyDetectedEntityInfo? info)
            {
                StringBuilder values = new StringBuilder();
                if (info != null)
                {
                    Vector3D? HitPosition = ((MyDetectedEntityInfo)info).HitPosition;
                    values.Append("РАССТОЯНИЕ   : " + (HitPosition != null ? Math.Round(((Vector3D)((Vector3D)HitPosition) - base.obj.GetPosition()).Length(), 2).ToString() : "") + "\n");
                    values.Append("Name         : " + ((MyDetectedEntityInfo)info).Name + "\n");
                    values.Append("Type         : " + ((MyDetectedEntityInfo)info).Type + "\n");
                    values.Append("HitPosition  : " + HitPosition + "\n");
                    values.Append("Orientation  : " + ((MyDetectedEntityInfo)info).Orientation + "\n");
                    values.Append("Velocity     : " + ((MyDetectedEntityInfo)info).Velocity + "\n");
                    values.Append("Relationship : " + ((MyDetectedEntityInfo)info).Relationship + "\n");
                    values.Append("BoundingBox  : " + ((MyDetectedEntityInfo)info).BoundingBox + "\n");
                }
                else { values.Append("РАССТОЯНИЕ   : \n"); values.Append("Name         : \n"); values.Append("Type         : \n"); values.Append("HitPosition  : \n"); values.Append("Orientation  : \n"); values.Append("Velocity     : \n"); values.Append("Relationship : \n"); values.Append("BoundingBox  : \n"); };
                return values.ToString();
            }
        }
        public class ShipController : BaseTerminalBlock<IMyShipController>
        {
            public ShipController(string name) : base(name) {  }
            public void OutText(StringBuilder values, int num_lcd) { if (base.obj is IMyTextSurfaceProvider) { IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider; if (num_lcd > ipp.SurfaceCount) return; IMyTextSurface ts = ipp.GetSurface(num_lcd); if (ts != null) { ts.WriteText(values, false); } } }
            public void OutText(string text, bool append, int num_lcd) { if (this.obj is IMyTextSurfaceProvider) { IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider; if (num_lcd > ipp.SurfaceCount) return; IMyTextSurface ts = ipp.GetSurface(num_lcd); if (ts != null) { ts.WriteText(text, append); } } }
            public StringBuilder GetText(int num_lcd) { StringBuilder values = new StringBuilder(); if (this.obj is IMyTextSurfaceProvider) { IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider; if (num_lcd > ipp.SurfaceCount) return null; IMyTextSurface ts = ipp.GetSurface(num_lcd); if (ts != null) { ts.ReadText(values); } } return values; }
        }
        public class Gyros : BaseListTerminalBlock<IMyGyro>
        {
            public Gyros(string name_obj) : base(name_obj) { }
            public Gyros(string name_obj, string tag) : base(name_obj, tag) { }
            public void SetOverride(bool OverrideOnOff, Vector3 settings, float Power = 1) { foreach (IMyGyro gyro in base.list_obj) { if ((!gyro.GyroOverride) && OverrideOnOff) gyro.ApplyAction("Override"); gyro.GyroPower = Power; gyro.Yaw = settings.GetDim(0); gyro.Pitch = settings.GetDim(1); gyro.Roll = settings.GetDim(2); } }
            public void SetOverride(bool OverrideOnOff = true, float OverrideValue = 0, float Power = 1) { foreach (IMyGyro gyro in base.list_obj) { if (((!gyro.GyroOverride) && OverrideOnOff) || ((gyro.GyroOverride) && !OverrideOnOff)) gyro.ApplyAction("Override"); gyro.GyroPower = Power; gyro.Yaw = OverrideValue; gyro.Pitch = OverrideValue; gyro.Roll = OverrideValue; } }
            public string TextDebug() { StringBuilder values = new StringBuilder(); values.Append("Yaw :" + base.list_obj.Select(g => g.Yaw).Average() + "\n"); values.Append("Pitch :" + base.list_obj.Select(g => g.Pitch).Average() + "\n"); values.Append("Roll :" + base.list_obj.Select(g => g.Roll).Average() + "\n"); return values.ToString(); }
        }
        public class Thrusts : BaseListTerminalBlock<IMyThrust>
        {
            private ShipController remote_control;
            public float Value;
            public string axis;
            //------------------------------------------------
            public List<IMyThrust> UpThrusters = new List<IMyThrust>();
            public List<IMyThrust> DownThrusters = new List<IMyThrust>();
            public List<IMyThrust> LeftThrusters = new List<IMyThrust>();
            public List<IMyThrust> RightThrusters = new List<IMyThrust>();
            public List<IMyThrust> ForwardThrusters = new List<IMyThrust>();
            public List<IMyThrust> BackwardThrusters = new List<IMyThrust>();
            public double UpThrMax { get { return UpThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double DownThrMax { get { return DownThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double LeftThrMax { get { return LeftThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double RightThrMax { get { return RightThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double ForwardThrMax { get { return ForwardThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double BackwardThrMax { get { return BackwardThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public Thrusts(string name_obj) : base(name_obj)
            {
            }
            public Thrusts(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public void InitThrusts(ShipController remote_control)
            {
                this.remote_control = remote_control;
                Matrix CockpitMatrix = new MatrixD();
                this.remote_control.obj.Orientation.GetMatrix(out CockpitMatrix);
                MatrixD OrientationCocpit = CockpitMatrix;
                // Список трастеров
                UpThrusters.Clear();
                DownThrusters.Clear();
                LeftThrusters.Clear();
                RightThrusters.Clear();
                ForwardThrusters.Clear();
                BackwardThrusters.Clear();
                // Орентация трастеров
                Matrix ThrusterMatrix = new MatrixD();
                foreach (IMyThrust thrust in this.list_obj)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == OrientationCocpit.Up)
                    {
                        UpThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Down)
                    {
                        DownThrusters.Add(thrust);
                    }
                    //X
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Left)
                    {
                        LeftThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Right)
                    {
                        RightThrusters.Add(thrust);
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Forward)
                    {
                        ForwardThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Backward)
                    {
                        BackwardThrusters.Add(thrust);
                    }
                }
            }
            public void ClearThrustOverridePersent()
            {
                SetOverridePercent(UpThrusters, 0f);
                SetOverridePercent(DownThrusters, 0f);
                SetOverridePercent(LeftThrusters, 0f);
                SetOverridePercent(RightThrusters, 0f);
                SetOverridePercent(ForwardThrusters, 0f);
                SetOverridePercent(BackwardThrusters, 0f);
            }
            public void SetOverridePercent(List<IMyThrust> Thrusts, float persent)
            {
                foreach (IMyThrust tr in Thrusts)
                {
                    tr.ThrustOverridePercentage = persent;
                }
            }
            public void SetOverridePercent(string axis, float persentValue)
            {
                if (axis == "U")
                {
                    SetOverridePercent(DownThrusters, persentValue);
                }
                else if (axis == "D")
                {
                    SetOverridePercent(UpThrusters, persentValue);
                }
                else if (axis == "L")
                {
                    SetOverridePercent(RightThrusters, persentValue);
                }
                else if (axis == "R")
                {
                    SetOverridePercent(LeftThrusters, persentValue);
                }
                else if (axis == "F")
                {
                    SetOverridePercent(BackwardThrusters, persentValue);
                }
                else if (axis == "B")
                {
                    SetOverridePercent(ForwardThrusters, persentValue);
                }
            }
            public void SetOverrideN(string axis, float OverrideValue)
            {
                double MaxThrust = 0;
                Value = 0;
                this.axis = axis;
                if (axis == "D") { MaxThrust = UpThrMax; SetOverridePercent("U", 0f); }
                else if (axis == "U") { MaxThrust = DownThrMax; SetOverridePercent("D", 0f); }
                else if (axis == "F") { MaxThrust = BackwardThrMax; SetOverridePercent("B", 0f); }
                else if (axis == "B") { MaxThrust = ForwardThrMax; SetOverridePercent("F", 0f); }
                else if (axis == "R") { MaxThrust = LeftThrMax; SetOverridePercent("L", 0f); }
                else if (axis == "L") { MaxThrust = RightThrMax; SetOverridePercent("R", 0f); }
                if (OverrideValue == 0)
                {
                    Value = 0;
                }
                else
                {
                    Value = (float)Math.Max(OverrideValue / MaxThrust, 0.1f);
                }
                SetOverridePercent(axis, Value);
            }
            public void SetOverrideAccel(string axis, float OverrideValue)
            {
                switch (axis)
                {
                    case "U":
                        if (OverrideValue < 0)
                        {
                            axis = "D";
                            OverrideValue = -OverrideValue;
                        }
                        else
                        {
                            OverrideValue += (float)this.remote_control.obj.GetNaturalGravity().Length();
                        }
                        break;
                    case "D":
                        if (OverrideValue < 0)
                        {
                            axis = "U";
                            OverrideValue = -OverrideValue;
                        }
                        else
                        {
                            OverrideValue -= (float)this.remote_control.obj.GetNaturalGravity().Length();
                        }
                        break;
                    case "L":
                        if (OverrideValue < 0)
                        {
                            axis = "R";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "R":
                        if (OverrideValue < 0)
                        {
                            axis = "L";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "F":
                        if (OverrideValue < 0)
                        {
                            axis = "B";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "B":
                        if (OverrideValue < 0)
                        {
                            axis = "F";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                }
                SetOverrideN(axis, OverrideValue * this.remote_control.obj.CalculateShipMass().PhysicalMass);
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("PhysicalMass : " + Math.Round(this.remote_control.obj.CalculateShipMass().PhysicalMass) + "\n");
                values.Append("TotalMass : " + Math.Round(this.remote_control.obj.CalculateShipMass().TotalMass) + "\n");
                values.Append("Grav         : " + Math.Round(this.remote_control.obj.GetNaturalGravity().Length()) + "\n");
                values.Append("axis         : " + axis + " , Value : " + Value + "\n");
                values.Append("------------------------------------------\n");
                values.Append("UP MAX       : " + PText.GetThrust((float)UpThrMax) + "\n");
                values.Append("DOWN MAX     : " + PText.GetThrust((float)DownThrMax) + "\n");
                values.Append("Forward MAX  : " + PText.GetThrust((float)ForwardThrMax) + "\n");
                values.Append("Backward MAX : " + PText.GetThrust((float)BackwardThrMax) + "\n");
                values.Append("Left MAX     : " + PText.GetThrust((float)LeftThrMax) + "\n");
                values.Append("Right MAX    : " + PText.GetThrust((float)RightThrMax) + "\n");
                return values.ToString();
            }
        }
        public class MyStorage
        {
            public MyStorage() { }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                solar_power.Axis = new Vector3D(GetValDouble("SPAxisX", str.ToString()), GetValDouble("SPAxisY", str.ToString()), GetValDouble("SPAxisZ", str.ToString()));
                solar_power.vector_axis = GetValBool("SPvector_axis", str.ToString());
                solar_power.parkong = GetValBool("SPparkong", str.ToString());
                for (int i = 0; i < count_room.Length; i++)
                {
                    int count = GetValInt("count_room_" + i, str.ToString());
                    count_room[i] = count;
                }
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                values.Append(solar_power.Axis.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "SPAxisX").Replace("Y", "SPAxisY").Replace("Z", "SPAxisZ") + ";\n");
                values.Append("SPvector_axis: " + solar_power.vector_axis.ToString() + ";\n");
                values.Append("SPparkong: " + solar_power.parkong.ToString() + ";\n");
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
        public class SolarPower {
            public Vector3D VS1 { get; set; }
            public Vector3D VS2 { get; set; }
            public Vector3D Axis { get; set; }
            public bool vector_axis { get; set; } = false;
            public bool parkong { get; set; } = false;
            public SolarPower()
            {

            }
            public void SetToVector() {
                double gF = Axis.Dot(cockpit_nav.obj.WorldMatrix.Forward);
                double gL = Axis.Dot(cockpit_nav.obj.WorldMatrix.Up);
                double gU = Axis.Dot(cockpit_nav.obj.WorldMatrix.Right);
                //Получаем сигналы по тангажу и крены операцией atan2
                double TargetRoll = (float)Math.Atan2(gL, -gU); // крен
                double TargetPitch = -(float)Math.Atan2(gF, -gU); // тангаж
                double TargetYaw = cockpit_nav.obj.RotationIndicator.Y;
                gyros.SetOverride(true, new Vector3D(TargetPitch, -TargetYaw, TargetRoll), 1);
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "vs1": VS1 = camera_course.obj.WorldMatrix.Forward; break;
                    case "vs2": VS2 = camera_course.obj.WorldMatrix.Forward; Axis = VS1.Cross(VS2); storage.SaveToStorage(); break;
                    case "set_to_vector": if (vector_axis) { vector_axis = false; } else { vector_axis = true; } storage.SaveToStorage(); break;
                    case "parking_panel": if (parkong) { parkong = false; } else { parkong = true; } storage.SaveToStorage(); break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    if (vector_axis)
                    {
                        thrusts.On();
                        SetToVector();
                    }
                    else
                    {
                        thrusts.ClearThrustOverridePersent();
                        gyros.SetOverride(false, 1);
                        if (cockpit_nav.obj.GetShipSpeed() < 0.1f) thrusts.Off();
                       
                    }
                }
            }
        }
    }
}

// door [door-gateway] [operators_space_left_down] [space]
// sn [door-gateway] [operators_space_left_down] [space]
// door [door-gateway] [operators_space_right_down] [space]
// sn [door-gateway] [operators_space_right_down] [space]

// door [door-gateway] [operators_space_left_down] [operators_space_right_down] [operators]
// sn [door-gateway] [operators_space_left_down] [operators_space_right_down] [operators]

// sn [door-transition] [module_transition] [module]
// sn [door-transition] [module_transition] [transition]
// door [door-transition] [module_transition]

// sn [door-transition] [cabin_energy_module_right] [energy_module_right]
// sn [door-transition] [cabin_energy_module_right] [cabin]
// door [door-transition] [cabin_energy_module_right]

//[OS-E1]-light [lighting_room] [operators]
//[OS-E1]-mLCD [door-info] [operators]

// [piston-wind-generator]
// [main-hinge]
// [MPB-1]-Шарнир [articulated-joint] 1-1 back
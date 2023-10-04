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
namespace SPB_EUROPE_DORS
{
    public sealed class Program : MyGridProgram
    {
        // v1.
        string NameObj = "[SPB-EUROPE-1]";

        string tag_door_gateway = "[door-gateway]";
        string tag_lighting_room = "[lighting_room]";
        string tag_info_tablo = "[door-info]";
        public enum room : int
        {
            none = 0,
            gateway = 1,    // Шлюзовая
            operators = 2,  // Операторская
            power = 3,      // Энергомодуль
            work = 4,       // Завод
            external = 5,   // Внешний периметр
        };
        public static string[] name_room = { "", "ШЛЮЗ", "ОПЕРАТОРСКАЯ", "ЭНЕРГО-МОДУЛЬ", "ЗАВОД", "ВНЕШНИЙ-ПЕРИМЕТР" };
        public static int[] count_room = { 0, 0, 0, 0, 0, 0 };
        public enum doors_gareways : int
        {
            external_gateway = 1,
            gateway_operators = 2,
            operators_power = 3,
            operators_work_1 = 4,
            operators_work_2 = 5,
            operators_external_1 = 6,
            operators_external_2 = 7,
            power_external = 8,
            work_external = 9,
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


        static Program _scr;
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
            public void Off()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off");
            }
            public void On()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On");
            }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            storage = new MyStorage();
            storage.LoadFromStorage();
            air_vent = new AirVent(NameObj);
            air_vent.On();
            air_info = new AirInfo(NameObj, tag_info_tablo);
            gateways_doors = new Gateways(NameObj, tag_door_gateway);
            room_light = new Lightings(NameObj, tag_lighting_room); // Освещение
            room_light.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                default:
                    break;
            }
            air_info.Logic(argument, updateSource);
            // Логика отработки шлюзовых дверей
            gateways_doors.Logic(argument, updateSource);
            // В космосе людей не считаем
            count_room[(int)room.external] = 0;
            // Логика отработки включения и выключения освещения
            room_light.Logic(argument, updateSource);
            storage.SaveToStorage();
            if (updateSource == UpdateType.Update10)
            {

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
                // Логика направдения движения
                if (sn1_active && !sn2_active && sn2.IsActive)
                {
                    // Выход
                    sn2_active = true;
                    count_room[(int)rm1]--;
                    count_room[(int)rm2]++;
                }
                if (sn2_active && !sn1_active && sn1.IsActive)
                {
                    // Вход
                    sn1_active = true;
                    count_room[(int)rm1]++;
                    count_room[(int)rm2]--;
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
                if (count_room[(int)rm1] < 0) count_room[(int)rm1] = 0;
                if (count_room[(int)rm2] < 0) count_room[(int)rm2] = 0;
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
                    room1 = room.none;
                    door2 = null;
                    sensor2 = null;
                    room2 = room.none;

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
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorsLight(string name_obj) : base(name_obj)
            {

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
        public class MyStorage
        {
            public Vector3D PlanetCenter = new Vector3D(0.50, 0.50, 0.50);
            public MyStorage()
            {

            }
            public double GetVal(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToDouble(val);
            }
            public int GetValInt(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToInt32(val);
            }
            public long GetValInt64(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToInt64(val);
            }
            public bool GetValBool(string Key, string str)
            {
                string val = "False";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToBoolean(val);
            }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();

                for (int i = 0; i < count_room.Length; i++)
                {
                    int count = GetValInt("count_room_" + i, str.ToString());
                    count_room[i] = count;
                }
                PlanetCenter = new Vector3D(GetVal("PX", str.ToString()), GetVal("PY", str.ToString()), GetVal("PZ", str.ToString()));
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                for (int i = 0; i < count_room.Length; i++)
                {
                    values.Append("count_room_" + i + ": " + (count_room[i]).ToString() + ";\n");
                }
                values.Append(PlanetCenter.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PX").Replace("Y", "PY").Replace("Z", "PZ") + ";\n");
                lcd_storage.OutText(values);
            }
        }
    }
}

// door [door-gateway] [operators_external_2] [external]
// sn [door-gateway] [operators_external_2] [external]
// door [door-gateway] [operators_external_2] [operators]
// sn [door-gateway] [operators_external_2] [operators]

// sn [door-transition] [module_transition] [module]
// sn [door-transition] [module_transition] [transition]
// door [door-transition] [module_transition]

// sn [door-transition] [cabin_energy_module_right] [energy_module_right]
// sn [door-transition] [cabin_energy_module_right] [cabin]
// door [door-transition] [cabin_energy_module_right]

//[SPB-MARS-1]-light [lighting_room] [operators]
//[SPB-MARS-1]-mLCD [door-info] [operators]

// [piston-wind-generator]
// [main-hinge]
// [MPB-1]-Шарнир [articulated-joint] 1-1 back
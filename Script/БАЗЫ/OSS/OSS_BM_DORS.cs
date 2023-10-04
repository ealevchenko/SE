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

/// <summary>
/// v1.0
/// Управление дверями и освещением на станции.
/// </summary>
namespace OSS_BM_DORS
{
    public sealed class Program : MyGridProgram
    {
        // v1.
        string NameObj = "[OSS]-"; // [OSS]-[BM]-
        string NameLCDInfo_Upr = "[OSS]-[BM]-LCD-INFO-UPR";
        string NameLCDTest = "[OSS]-[EML]-test";

        string tag_info_tablo = "[door-info]";
        string tag_door_transition = "[door-transition]";
        string tag_door_gateway = "[door-gateway]";
        string tag_lighting_room = "[lighting_room]";
        string tag_ref_room_hangar = "[ref_room]";
        public enum room : int
        {
            none = 0,
            cabin = 1,              // кабина
            space = 2,              // Космос
            operators = 3,          // операторская
            energy_module_left = 4, // энерг. мод
            energy_module_right = 5,// энерг. мод
            cargo_module = 6,       // груз. мод
            habitation_module = 7,      //  жилой. мод
            reactor_module_left = 8,    // энерг. мод реактор
            ice_module_right = 9,        // энерг. мод реактор
            waiting_hall = 10,        // Зал ожидания
            gateway_module = 11,        // Шлюз
            wardroom = 12,        // Зал ожидания

        };
        public static string[] name_room = { "", "Кабина", "Выход в космос", "Операторская", "Энерго-модуль", "Энерго-модуль", "Рабочий модуль", "Жилой модуль", "Ядерный реактор", "Склад льда", "Зал ожидания", "Шлюзовой модуль", "Кают-компания" };
        public static int[] count_room = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ,0, 0, 0};
        public enum doors_gareways : int
        {
            cabin_space = 1,
            cabin_cargo_module = 2,
            reactor_energy_module_left1 = 3,
            reactor_energy_module_left2 = 4,
            space_reactor_module_left1 = 5,
            space_reactor_module_left2 = 6,
            space_ice_module_right1 = 7,
            space_ice_module_right2 = 8,
            cargo_module_space = 9,
            space_habitation_module1 = 10,
            space_habitation_module4 = 11,
            space_gateway_module2 = 12,
            space_gateway_module3 = 13,
            space_ship_gateway_module_ship1 = 14,
            space_ship_gateway_module_ship2 = 15,
        }
        public enum door_transition : int
        {
            operators_habitation_module = 0,
            operators_cabin = 1,
            cabin_energy_module_left = 2,
            cabin_energy_module_right = 3,
            reactor_energy_module_left1 = 4,
            reactor_energy_module_left2 = 5,
            ice_energy_module_right1 = 6,
            ice_energy_module_right2 = 7,
            waiting_hall_habitation_module1 = 8,
            waiting_hall_habitation_module2 = 9,
            waiting_hall_gateway_module1 = 10,
            waiting_hall_gateway_module2 = 11,
            wardroom_habitation_module = 12,
        }

        // door [door-gateway] [space_ship_gateway_module_ship2] [gateway_module]
        // sn [door-gateway] [space_ship_gateway_module_ship2] [gateway_module]
        // door [door-gateway] [space_ship_gateway_module_ship2] [space]
        // sn [door-gateway] [space_ship_gateway_module_ship2] [space]

        // sn [door-transition] [wardroom_habitation_module] [wardroom]
        // sn [door-transition] [wardroom_habitation_module] [habitation_module]
        // door [door-transition] [wardroom_habitation_module]

        // sn [door-transition] [cabin_energy_module_right] [energy_module_right]
        // sn [door-transition] [cabin_energy_module_right] [cabin]
        // door [door-transition] [cabin_energy_module_right]

        //[OSS]-[EML]-light [lighting_room] [reactor_module_left]

        public static Color red = new Color(255, 0, 0);
        public static Color yellow = new Color(255, 255, 0);
        public static Color green = new Color(0, 128, 0);

        static LCD lcd_info_upr;
        static LCD test_lcd;

        AirInfo air_info;
        AirVent air_vent;
        ReflectorsLight reflectors_light;
        Gateways gateways_doors;
        Transitions transition_door;
        Lightings room_light;
        ReflectorLight ref_light;
        MechanicalConnectior mechanical_connectior;

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
        }
        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;

            public string CustomName { get { return ((IMyTerminalBlock)this.obj).CustomName; } set { ((IMyTerminalBlock)this.obj).CustomName = value; } }

            //public T _obj { get { return obj; } }

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
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_info_upr = new LCD(NameLCDInfo_Upr);
            test_lcd = new LCD(NameLCDTest);
            air_vent = new AirVent(NameObj);
            air_vent.On();
            air_info = new AirInfo(NameObj, tag_info_tablo);
            gateways_doors = new Gateways(NameObj, tag_door_gateway);
            transition_door = new Transitions(NameObj, tag_door_transition);
            room_light = new Lightings(NameObj, tag_lighting_room); // Освещение
            room_light.Off();
            ref_light = new ReflectorLight(NameObj, null);
            ref_light.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            mechanical_connectior = new MechanicalConnectior(NameObj);
            mechanical_connectior.AttachDetach(mechanical_connectior.IsAttached());
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            //StringBuilder values = new StringBuilder();
            // Получим данные
            //test_lcd.WriteText("" + "\n", false);
            // Логика отображения подписей двирей с учетом кислорода в помещении
            air_info.Logic(argument, updateSource);
            // Логика отработки шлюзовых дверей
            gateways_doors.Logic(argument, updateSource);
            transition_door.Logic(argument, updateSource);
            // В космосе людей не считаем
            count_room[(int)room.space] = 0;
            // Логика отработки включения и выключения освещения
            room_light.Logic(argument, updateSource);
            //test_lcd.WriteText("" + "\n", false);
            //test_lcd.WriteText("hangar:" + count_room[(int)room.hangar] + "\n", true);
            //test_lcd.WriteText("factory:" + count_room[(int)room.factory] + "\n", true);
            //test_lcd.WriteText("technical_1:" + count_room[(int)room.technical_1] + "\n", true);
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
        }
        public class InfoTablo : BaseListTerminalBlock<IMyTextPanel>
        {
            string tag;
            public InfoTablo(string name_obj, string tag) : base(name_obj)
            {
                this.tag = tag;
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    base.list_obj = list_obj.Where(x => x.CustomName.Contains(this.tag)).ToList();
                }
                _scr.Echo("Найдено TextPanel:[" + tag + "]: " + base.list_obj.Count());
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
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorsLight(string name_obj) : base(name_obj)
            {
            }
            public ReflectorsLight(string name_obj, string tag) : base(name_obj, tag)
            {

            }
        }
        public class Transition
        {
            door_transition door_tr;
            IMySensorBlock sn1;
            IMySensorBlock sn2;
            room rm1;
            room rm2;
            IMyDoor door1;
            bool sn1_active = false;    // датчик входа
            bool sn2_active = false;   // датчик выхода
            public Transition(door_transition dg, IMyDoor door1, IMySensorBlock sn1, room rm1, IMySensorBlock sn2, room rm2)
            {
                this.door_tr = dg;
                this.rm1 = rm1;
                this.rm2 = rm2;
                this.sn1 = sn1;
                string sn1_cd = sn1.CustomData; // 1.0f, 1.0f, 2.5f, 1.0f, 0.1f, 2.5f
                this.sn2 = sn2;
                this.door1 = door1;
                this.door1.ApplyAction("OnOff_On");
                this.door1.CloseDoor();
            }
            public void Logic()
            {
                if (!sn1.IsActive && !sn2.IsActive && door1.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    door1.CloseDoor();
                }
                if ((sn1.IsActive || sn2.IsActive) && door1.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    door1.OpenDoor();
                }
                // Логика направдения движения
                if (sn1_active && !sn2_active && sn2.IsActive)
                {
                    // Выход
                    //sn1_active = false;
                    sn2_active = true;
                    //count_room[(int)rm1]--;
                    //count_room[(int)rm2]++;
                }
                if (sn2_active && !sn1_active && sn1.IsActive)
                {
                    // Вход
                    sn1_active = true;
                    //sn2_active = false;
                    //count_room[(int)rm1]++;
                    //count_room[(int)rm2]--;
                }
                if (sn1_active && sn2_active && !sn1.IsActive && sn2.IsActive)
                {
                    // Выход
                    sn1_active = false;
                    //sn2_active = true;
                    count_room[(int)rm1]--;
                    count_room[(int)rm2]++;
                }
                if (sn1_active && sn2_active && sn1.IsActive && !sn2.IsActive)
                {
                    // Выход
                    sn2_active = false;
                    //sn1_active = true;
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
        public class Transitions
        {

            private List<IMyDoor> doors = new List<IMyDoor>();
            private List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            List<Transition> list_tr = new List<Transition>();
            public Transitions(string name_obj, string tag)
            {

                //StringBuilder values_info = new StringBuilder();
                //values_info.AppendFormat("Start" + "\n");
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                _scr.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                //values_info.AppendFormat("doors:" + doors.Count() + "\n");
                //values_info.AppendFormat("sensors:" + sensors.Count() + "\n");
                IMyDoor door1;
                IMySensorBlock sensor1;
                IMySensorBlock sensor2;
                room room1;
                room room2;
                //values_info.AppendFormat("Поиск дверей:" + "\n");
                foreach (door_transition gw in Enum.GetValues(typeof(door_transition)))
                {
                    door1 = null;
                    sensor1 = null;
                    sensor2 = null;
                    room1 = room.none;
                    room2 = room.none;
                    IMyDoor l_drs = doors.Where(d => d.CustomName.Contains("[" + gw.ToString() + "]")).FirstOrDefault();
                    List<IMySensorBlock> l_sns = sensors.Where(d => d.CustomName.Contains("[" + gw.ToString() + "]")).ToList();
                   // if (l_drs != null) values_info.Append("l_drs:" + l_drs.ToString() + "\n");
                    //values_info.Append("l_sns:" + l_sns.Count() + "\n");
                    if (l_drs != null && l_sns != null && l_sns.Count() == 2)
                    {
                        foreach (room rm in Enum.GetValues(typeof(room)))
                        {
                            //values_info.Append("room:" + rm.ToString() + "\n");
                            IMySensorBlock sn = l_sns.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
                            if (l_drs != null && sn != null)
                            {
                                door1 = l_drs;
                                if (sensor1 != null && sensor2 == null) { sensor2 = sn; room2 = rm; }
                                if (sensor1 == null) { sensor1 = sn; room1 = rm; }
                            }
                        }
                        if (door1 != null && sensor1 != null && sensor2 != null)
                        {
                            //values_info.Append("door1:" + door1.CustomName + "\n");
                            //values_info.Append("sensor1:" + sensor1.CustomName + "\n");
                            //values_info.Append("sensor2:" + sensor2.CustomName + "\n");
                            list_tr.Add(new Transition(gw, door1, sensor1, room1, sensor2, room2));
                        }
                    }

                }
                _scr.Echo("Найдено Transitions:[" + tag + "]: " + list_tr.Count());
                //lcd_info_upr.OutText(values_info);
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
                    foreach (Transition tr in list_tr)
                    {
                        tr.Logic();
                    }
                }

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
            //bool input_door = false;
            //bool output_door = false;
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
                    //sn1_active = false;
                    sn2_active = true;
                    //input_door = false;
                    //output_door = true;
                    count_room[(int)rm1]--;
                    count_room[(int)rm2]++;
                }
                if (sn2_active && !sn1_active && sn1.IsActive)
                {
                    // Вход
                    sn1_active = true;
                    //sn2_active = false;
                    //input_door = true;
                    //output_door = false;
                    count_room[(int)rm1]++;
                    count_room[(int)rm2]--;
                }
                if (sn2_active && sn1_active && !sn2.IsActive && !sn1.IsActive)
                {
                    // Вход
                    sn1_active = false;
                    sn2_active = false;
                    //input_door = false;
                    //output_door = false;
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
                //test_lcd.WriteText("Start" + "\n", false);
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                _scr.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                //test_lcd.WriteText("doors:" + doors.Count() + "\n", true);
                //test_lcd.WriteText("sensors:" + doors.Count() + "\n", true);
                IMyDoor door1;
                IMySensorBlock sensor1;
                room room1;
                IMyDoor door2;
                IMySensorBlock sensor2;
                room room2;
                //test_lcd.WriteText("Поиск дверей:" + "\n", false);
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
                    if (l_drs != null && l_drs.Count() == 2 && l_sns != null && l_sns.Count() == 2)
                    {
                        foreach (room rm in Enum.GetValues(typeof(room)))
                        {
                            //test_lcd.WriteText("room:" + rm + "\n", true);
                            IMyDoor dr = l_drs.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
                            IMySensorBlock sn = l_sns.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
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
                            //test_lcd.WriteText("door1:"+ door1.CustomName + "\n", true);
                            //test_lcd.WriteText("door2:"+ door2.CustomName + "\n", true);
                            //test_lcd.WriteText("sensor1:" + sensor1.CustomName + "\n", true);
                            //test_lcd.WriteText("sensor2:" + sensor2.CustomName + "\n", true);
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
                    //case "connected_on":
                    //    break;
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
        public class ReflectorLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorLight(string name_obj, string tag) : base(name_obj)
            {
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено ReflectorLight:[" + tag + "]: " + list_obj.Count());
            }
        }
        public class MechanicalConnectior : BaseListTerminalBlock<IMyMechanicalConnectionBlock>
        {
            public MechanicalConnectior(string name_obj) : base(name_obj)
            {

            }
            public MechanicalConnectior(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public bool IsAttached()
            {
                bool result = false;
                foreach (IMyMechanicalConnectionBlock obj in base.list_obj)
                {
                    if (obj.IsAttached) return true;
                }
                return result;
            }
            public void Attach()
            {
                foreach (IMyMechanicalConnectionBlock obj in base.list_obj)
                {
                    obj.Attach();
                }
            }
            public void Detach()
            {
                foreach (IMyMechanicalConnectionBlock obj in base.list_obj)
                {
                    obj.Detach();
                }
            }
            public void AttachDetach(bool on)
            {
                foreach (IMyMechanicalConnectionBlock obj in base.list_obj)
                {
                    if (on)
                    {
                        obj.Attach();
                    }
                    else
                    {
                        obj.Detach();
                    }

                }
            }
        }
    }
}

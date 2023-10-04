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
namespace MPB_UPR
{
    public sealed class Program : MyGridProgram
    {
        // v1.
        string NameObj = "[MPB-1]";

        string tag_door_transition = "[door-transition]";
        string tag_door_gateway = "[door-gateway]";
        string tag_lighting_room = "[lighting_room]";
        string tag_piston_wg = "[piston-wind-generator]";
        public enum room : int
        {
            none = 0,
            module = 1,     // Модуль
            external = 2,   // Выход
            transition = 3,   // ПЕРЕХОД
        };
        public static string[] name_room = { "", "МОДУЛЬ", "ВЫХОД", "ПЕРЕХОД" };
        public static int[] count_room = { 0, 0, 0, 0 };
        public enum doors_gareways : int
        {
            module_external_forw = 1,
            transition_external_back = 2,
        }
        public enum door_transition : int
        {
            module_transition = 0,
        }

        public static float speed_piston_wg = 1.0f;       // один оборот в минуту

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
        static LCD lcd_power;
        static LCD lcd_cs;
        static LCD lcd_fsp;

        ReflectorsLight reflectors_light;
        Gateways gateways_doors;
        Transitions transition_door;
        Lightings room_light;
        ReflectorLight ref_light;
        static PistonsBase pistons_base;
        MechanicalConnectior mechanical_connectior;
        FoldingSolarPanel folding_sp;
        CombatSystem combat_system;

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
            lcd_power = new LCD(NameObj + "-LCD-POWER");
            lcd_cs = new LCD(NameObj + "-LCD-CombatSystem");
            lcd_fsp = new LCD(NameObj + "-LCD-FoldingSolarPanel");

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
            pistons_base = new PistonsBase(NameObj, tag_piston_wg);
            folding_sp = new FoldingSolarPanel(NameObj);
            combat_system = new CombatSystem(NameObj);
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            // Логика отработки шлюзовых дверей
            gateways_doors.Logic(argument, updateSource);
            transition_door.Logic(argument, updateSource);
            folding_sp.Logic(argument, updateSource);
            combat_system.Logic(argument, updateSource);
            // В космосе людей не считаем
            count_room[(int)room.external] = 0;
            // Логика отработки включения и выключения освещения
            room_light.Logic(argument, updateSource);

            switch (argument)
            {
                case "open_pwg":
                    pistons_base.Open();
                    break;
                case "close_pwg":
                    pistons_base.Close();
                    break;
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                lcd_fsp.OutText(folding_sp.TextInfo(), false);                
                lcd_cs.OutText(combat_system.TextInfo(), false);
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
                    sn2_active = true;// Выход
                }
                if (sn2_active && !sn1_active && sn1.IsActive)
                {
                    sn1_active = true;// Вход
                }
                if (sn1_active && sn2_active && !sn1.IsActive && sn2.IsActive)
                {
                    // Выход
                    sn1_active = false;
                    count_room[(int)rm1]--;
                    count_room[(int)rm2]++;
                }
                if (sn1_active && sn2_active && sn1.IsActive && !sn2.IsActive)
                {
                    // Выход
                    sn2_active = false;
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
        public class PistonsBase : BaseListTerminalBlock<IMyPistonBase>
        {
            public PistonsBase(string name_obj, string tag) : base(name_obj)
            {
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено PistonBase:[" + tag + "]: " + list_obj.Count());
            }
            public void Open()
            {
                foreach (IMyPistonBase p in base.list_obj)
                {
                    p.Velocity = speed_piston_wg;
                    p.Extend();
                }
            }
            public void Close()
            {
                foreach (IMyPistonBase p in base.list_obj)
                {
                    p.Velocity = speed_piston_wg;
                    p.Retract();
                }
            }
            public float GetPosition()
            {
                float position = 0;
                foreach (IMyPistonBase p in base.list_obj)
                {
                    position += p.CurrentPosition;
                }
                return position;
            }

        }
        public class PistonBase : BaseTerminalBlock<IMyPistonBase>
        {
            public float? task_position { get; set; } = null;
            private float tolerance = 0.1f;
            private float multiply_speed = 0.5f;
            public PistonBase(string name_obj) : base(name_obj)
            {

            }
            public void SetPosition(float position)
            {
                if (this.obj == null) return;
                float speed = 0f;
                // Текущее положение
                double curennt_position = this.obj.CurrentPosition;
                if (curennt_position > (position + tolerance))
                {
                    speed = -(float)(Math.Abs(curennt_position - position) * multiply_speed);
                    this.obj.Velocity = speed;
                }
                else if (curennt_position < (position - tolerance))
                {
                    double dist = (position - curennt_position);
                    speed = (float)(Math.Abs(position - curennt_position) * multiply_speed);
                    this.obj.Velocity = speed;
                }
                else
                {
                    this.obj.Velocity = speed;
                    this.task_position = null;
                }
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
                    if (task_position != null)
                    {
                        SetPosition((float)task_position);
                    }
                }
            }
            public string TextInfo()
            {
                if (this.obj == null) return "";
                StringBuilder values = new StringBuilder();
                values.Append("ПОРШЕНЬ : " + this.obj.CustomName + "\n");
                values.Append("НИЗ: " + Math.Round(this.obj.LowestPosition, 1) + " ВЕРХ: " + Math.Round(this.obj.HighestPosition, 1) + "\n");
                values.Append("ПОЛОЖ : " + Math.Round(this.obj.CurrentPosition, 1) + " СКОРОСТЬ : " + Math.Round(this.obj.Velocity, 3) + " ЗАД : " + this.task_position + "\n");
                return values.ToString();
            }
        }
        public class MotorStators : BaseListTerminalBlock<IMyMotorStator>
        {
            public MotorStators(string name_obj, string tag) : base(name_obj)
            {
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено MotorStator:[" + tag + "]: " + list_obj.Count());
            }
            public void MotorVelocit(float speed)
            {
                foreach (IMyMotorStator p in base.list_obj)
                {
                    p.TargetVelocityRPM = speed;
                }
            }
            public double RadToGradus(float rad)
            {
                return rad * 180 / Math.PI;
            }
            public float GetAngle()
            {
                float angle = 0f;
                foreach (IMyMotorStator p in base.list_obj)
                {
                    angle += (float)RadToGradus(p.Angle);
                }
                return angle;
            }

        }
        public class MotorStator : BaseTerminalBlock<IMyMotorStator>
        {
            public float? task_degr { get; set; } = null;
            private float tolerance = 0.1f;
            private float multiply_speed = 0.1f;
            public MotorStator(string name_obj) : base(name_obj)
            {

            }
            public double RadToGradus(float rad)
            {
                return rad * 180 / Math.PI;
            }
            public void RotateToGradus(float degr)
            {
                if (this.obj == null) return;
                float speed = 0f;
                // Текущее положение
                double curennt_degr = RadToGradus(this.obj.Angle);
                if (curennt_degr > (degr + tolerance))
                {
                    double dist = curennt_degr - degr;
                    if (Math.Abs(dist) <= 180.1f)
                    {
                        speed = -(float)(Math.Abs(dist) * multiply_speed);
                    }
                    else
                    {
                        speed = (float)(Math.Abs(dist) * multiply_speed);
                    }

                    this.obj.TargetVelocityRPM = speed;
                }
                else if (curennt_degr < (degr - tolerance))
                {
                    double dist = (degr - curennt_degr);
                    if (Math.Abs(dist) <= 180.1f)
                    {
                        speed = (float)(Math.Abs(degr - curennt_degr) * multiply_speed);
                    }
                    else
                    {
                        speed = -(float)(Math.Abs(degr - curennt_degr) * multiply_speed);
                    }

                    this.obj.TargetVelocityRPM = speed;
                }
                else
                {
                    this.obj.TargetVelocityRPM = speed;
                    this.task_degr = null;
                }
            }
            public double GetCurrentGradus()
            {
                if (this.obj == null) return 0;
                return RadToGradus(this.obj.Angle);
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
                    if (task_degr != null)
                    {
                        RotateToGradus((float)task_degr);
                    }
                }
            }
            public string TextInfo()
            {
                if (this.obj == null) return "";
                StringBuilder values = new StringBuilder();
                values.Append("ШАРНИР : " + this.obj.CustomName + "\n");
                values.Append("БЛОК : " + (this.obj.RotorLock ? ired.ToString() : igreen.ToString()) + " НИЗ: " + Math.Round(this.obj.LowerLimitDeg, 1) + " ВЕРХ: " + Math.Round(this.obj.UpperLimitDeg, 1) + "\n");
                values.Append("УГОЛ : " + Math.Round(RadToGradus(this.obj.Angle), 1) + " СКОРОСТЬ : " + Math.Round(this.obj.TargetVelocityRPM, 3) + " ЗАД : " + this.task_degr + "\n");
                return values.ToString();
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
        public class FoldingSolarPanel
        {
            public bool open_forw, open_back = false;
            public bool close_forw, close_back = false;
            MotorStator hinge_main_back, rotor_main_back;
            MotorStator hinge_main_forw, rotor_main_forw;
            MotorStator hinge_node_back_1_1, hinge_node_back_1_2, hinge_node_back_2_1, hinge_node_back_2_2;
            MotorStator hinge_node_forw_1_1, hinge_node_forw_1_2, hinge_node_forw_2_1, hinge_node_forw_2_2;
            public FoldingSolarPanel(string name_obj)
            {
                hinge_main_back = new MotorStator("[MPB-1]-Шарнир [s-p-hinge-main-back]");
                rotor_main_back = new MotorStator("[MPB-1]-Ротор [s-p-rotor-main-back]");
                hinge_node_back_1_1 = new MotorStator("[MPB-1]-Шарнир 1_1 [s-p-hinge-node-back]");
                hinge_node_back_1_2 = new MotorStator("[MPB-1]-Шарнир 1_2 [s-p-hinge-node-back]");
                hinge_node_back_2_1 = new MotorStator("[MPB-1]-Шарнир 2_1 [s-p-hinge-node-back]");
                hinge_node_back_2_2 = new MotorStator("[MPB-1]-Шарнир 2_2 [s-p-hinge-node-back]");
                hinge_main_forw = new MotorStator("[MPB-1]-Шарнир [s-p-hinge-main-forw]");
                rotor_main_forw = new MotorStator("[MPB-1]-Ротор [s-p-rotor-main-forw]");
                hinge_node_forw_1_1 = new MotorStator("[MPB-1]-Шарнир 1_1 [s-p-hinge-node-forw]");
                hinge_node_forw_1_2 = new MotorStator("[MPB-1]-Шарнир 1_2 [s-p-hinge-node-forw]");
                hinge_node_forw_2_1 = new MotorStator("[MPB-1]-Шарнир 2_1 [s-p-hinge-node-forw]");
                hinge_node_forw_2_2 = new MotorStator("[MPB-1]-Шарнир 2_2 [s-p-hinge-node-forw]");
            }
            public bool IsOpenForw()
            {
                if (hinge_main_forw.GetCurrentGradus() > 89.9f && hinge_main_forw.task_degr == null
                    && hinge_node_forw_1_1.GetCurrentGradus() < 0.1f && hinge_node_forw_1_1.task_degr == null
                    && hinge_node_forw_1_2.GetCurrentGradus() < 0.1f && hinge_node_forw_1_2.task_degr == null
                    && hinge_node_forw_2_1.GetCurrentGradus() < 0.1f && hinge_node_forw_2_1.task_degr == null
                    && hinge_node_forw_2_2.GetCurrentGradus() < 0.1f && hinge_node_forw_2_2.task_degr == null)
                    return true;
                else return false;
            }
            public bool OpenForw()
            {
                bool result = false;
                if ((hinge_main_forw.GetCurrentGradus() < -0.1f || hinge_main_forw.GetCurrentGradus() >= 270f) && hinge_main_forw.task_degr == null)
                {
                    hinge_main_forw.obj.RotorLock = false;
                    hinge_main_forw.task_degr = 0;
                }
                else
                {
                    if ((hinge_main_forw.GetCurrentGradus() >= -0.1f || hinge_main_forw.GetCurrentGradus() <= 0.1f) && hinge_main_forw.task_degr == null)
                    {
                        //if (!hinge_main_forw.obj.RotorLock) hinge_main_forw.obj.RotorLock = true;
                        if (hinge_main_forw.GetCurrentGradus() < 89.9f && hinge_main_forw.task_degr == null)
                        {
                            hinge_main_forw.obj.RotorLock = false;
                            hinge_main_forw.task_degr = 90f;
                        }
                        if (hinge_node_forw_1_1.GetCurrentGradus() > 0.1f && hinge_node_forw_1_1.task_degr == null)
                        {
                            hinge_node_forw_1_1.obj.RotorLock = false;
                            hinge_node_forw_1_1.task_degr = 0f;
                        }
                        if (hinge_node_forw_1_2.GetCurrentGradus() > 0.1f && hinge_node_forw_1_2.task_degr == null)
                        {
                            hinge_node_forw_1_2.obj.RotorLock = false;
                            hinge_node_forw_1_2.task_degr = 0f;
                        }
                        if (hinge_node_forw_2_1.GetCurrentGradus() > 0.1f && hinge_node_forw_2_1.task_degr == null)
                        {
                            hinge_node_forw_2_1.obj.RotorLock = false;
                            hinge_node_forw_2_1.task_degr = 0f;
                        }
                        if (hinge_node_forw_2_2.GetCurrentGradus() > 0.1f && hinge_node_forw_2_2.task_degr == null)
                        {
                            hinge_node_forw_2_2.obj.RotorLock = false;
                            hinge_node_forw_2_2.task_degr = 0f;
                        }
                        if (IsOpenForw())
                        {
                            hinge_main_forw.obj.RotorLock = true;
                            hinge_node_forw_1_1.obj.RotorLock = true;
                            hinge_node_forw_1_2.obj.RotorLock = true;
                            hinge_node_forw_2_1.obj.RotorLock = true;
                            hinge_node_forw_2_2.obj.RotorLock = true;
                            rotor_main_forw.obj.TargetVelocityRad = 0f;
                            rotor_main_forw.obj.RotorLock = false;
                            result = true;
                        }
                    }
                }
                return result;
            }
            public bool IsCloseForw()
            {
                if ((hinge_main_forw.GetCurrentGradus() < -89.9f || hinge_main_forw.GetCurrentGradus() >= 270f) && hinge_main_forw.task_degr == null
                            && hinge_node_forw_1_1.GetCurrentGradus() > 89.9f && hinge_node_forw_1_1.task_degr == null
                            && hinge_node_forw_1_2.GetCurrentGradus() > 89.9f && hinge_node_forw_1_2.task_degr == null
                            && hinge_node_forw_2_1.GetCurrentGradus() > 89.9f && hinge_node_forw_2_1.task_degr == null
                            && hinge_node_forw_2_2.GetCurrentGradus() > 89.9f && hinge_node_forw_2_2.task_degr == null)
                    return true;
                else return false;
            }
            public bool CloseForw()
            {
                bool result = false;
                if (rotor_main_forw.GetCurrentGradus() > 0.1f && rotor_main_forw.task_degr == null)
                {
                    rotor_main_forw.obj.RotorLock = false;
                    rotor_main_forw.task_degr = 0;
                }
                else
                {
                    if (rotor_main_forw.GetCurrentGradus() < 0.1f && rotor_main_forw.task_degr == null)
                    {
                        rotor_main_forw.obj.TargetVelocityRad = 0f;
                        if (!rotor_main_forw.obj.RotorLock) rotor_main_forw.obj.RotorLock = true;

                        if (hinge_main_forw.GetCurrentGradus() > -89.9f && hinge_main_forw.task_degr == null)
                        {
                            hinge_main_forw.obj.RotorLock = false;
                            hinge_main_forw.task_degr = -90f;
                        }
                        if (hinge_node_forw_1_1.GetCurrentGradus() < 89.9f && hinge_node_forw_1_1.task_degr == null)
                        {
                            hinge_node_forw_1_1.obj.RotorLock = false;
                            hinge_node_forw_1_1.task_degr = 90f;
                        }
                        if (hinge_node_forw_1_2.GetCurrentGradus() < 89.9f && hinge_node_forw_1_2.task_degr == null)
                        {
                            hinge_node_forw_1_2.obj.RotorLock = false;
                            hinge_node_forw_1_2.task_degr = 90f;
                        }
                        if (hinge_node_forw_2_1.GetCurrentGradus() < 89.9f && hinge_node_forw_2_1.task_degr == null)
                        {
                            hinge_node_forw_2_1.obj.RotorLock = false;
                            hinge_node_forw_2_1.task_degr = 90f;
                        }
                        if (hinge_node_forw_2_2.GetCurrentGradus() < 89.9f && hinge_node_forw_2_2.task_degr == null)
                        {
                            hinge_node_forw_2_2.obj.RotorLock = false;
                            hinge_node_forw_2_2.task_degr = 90f;
                        }
                        if (IsCloseForw())
                        {
                            hinge_main_forw.obj.RotorLock = true;
                            hinge_node_forw_1_1.obj.RotorLock = true;
                            hinge_node_forw_1_2.obj.RotorLock = true;
                            hinge_node_forw_2_1.obj.RotorLock = true;
                            hinge_node_forw_2_2.obj.RotorLock = true;
                            result = true;
                        }
                    }
                }
                return result;
            }
            public bool IsOpenBack()
            {
                if (hinge_main_back.GetCurrentGradus() > 89.9f&& hinge_main_back.task_degr == null
                            && hinge_node_back_1_1.GetCurrentGradus() < 0.1f && hinge_node_back_1_1.task_degr == null
                            && hinge_node_back_1_2.GetCurrentGradus() < 0.1f && hinge_node_back_1_2.task_degr == null
                            && hinge_node_back_2_1.GetCurrentGradus() < 0.1f && hinge_node_back_2_1.task_degr == null
                            && hinge_node_back_2_2.GetCurrentGradus() < 0.1f && hinge_node_back_2_2.task_degr == null)
                    return true;
                else return false;
            }
            public bool OpenBack()
            {
                bool result = false;
                if ((hinge_main_back.GetCurrentGradus() < -0.1f || hinge_main_back.GetCurrentGradus() >= 270f) && hinge_main_back.task_degr == null)
                {
                    hinge_main_back.obj.RotorLock = false;
                    hinge_main_back.task_degr = 0;
                }
                else
                {
                    if ((hinge_main_back.GetCurrentGradus() >= -0.1f || hinge_main_back.GetCurrentGradus() <= 0.1f) && hinge_main_back.task_degr == null)
                    {
                        //if (!hinge_main_back.obj.RotorLock) hinge_main_back.obj.RotorLock = true;
                        if (hinge_main_back.GetCurrentGradus() < 89.9f && hinge_main_back.task_degr == null)
                        {
                            hinge_main_back.obj.RotorLock = false;
                            hinge_main_back.task_degr = 90f;
                        }
                        if (hinge_node_back_1_1.GetCurrentGradus() > 0.1f && hinge_node_back_1_1.task_degr == null)
                        {
                            hinge_node_back_1_1.obj.RotorLock = false;
                            hinge_node_back_1_1.task_degr = 0f;
                        }
                        if (hinge_node_back_1_2.GetCurrentGradus() > 0.1f && hinge_node_back_1_2.task_degr == null)
                        {
                            hinge_node_back_1_2.obj.RotorLock = false;
                            hinge_node_back_1_2.task_degr = 0f;
                        }
                        if (hinge_node_back_2_1.GetCurrentGradus() > 0.1f && hinge_node_back_2_1.task_degr == null)
                        {
                            hinge_node_back_2_1.obj.RotorLock = false;
                            hinge_node_back_2_1.task_degr = 0f;
                        }
                        if (hinge_node_back_2_2.GetCurrentGradus() > 0.1f && hinge_node_back_2_2.task_degr == null)
                        {
                            hinge_node_back_2_2.obj.RotorLock = false;
                            hinge_node_back_2_2.task_degr = 0f;
                        }
                        if (IsOpenBack())
                        {
                            hinge_main_back.obj.RotorLock = true;
                            hinge_node_back_1_1.obj.RotorLock = true;
                            hinge_node_back_1_2.obj.RotorLock = true;
                            hinge_node_back_2_1.obj.RotorLock = true;
                            hinge_node_back_2_2.obj.RotorLock = true;
                            rotor_main_back.obj.TargetVelocityRad = 0f;
                            rotor_main_back.obj.RotorLock = false;
                            result = true;
                        }
                    }

                }
                return result;
            }
            public bool IsCloseBack()
            {
                if ((hinge_main_back.GetCurrentGradus() < -89.9f || hinge_main_back.GetCurrentGradus() >= 270f)  && hinge_main_back.task_degr == null
                            && hinge_node_back_1_1.GetCurrentGradus() > 89.9f && hinge_node_back_1_1.task_degr == null
                            && hinge_node_back_1_2.GetCurrentGradus() > 89.9f && hinge_node_back_1_2.task_degr == null
                            && hinge_node_back_2_1.GetCurrentGradus() > 89.9f && hinge_node_back_2_1.task_degr == null
                            && hinge_node_back_2_2.GetCurrentGradus() > 89.9f && hinge_node_back_2_2.task_degr == null)
                    return true;
                else return false;
            }
            public bool CloseBack()
            {
                bool result = false;
                if (rotor_main_back.GetCurrentGradus() > 0.1f && rotor_main_back.task_degr == null)
                {
                    rotor_main_back.obj.RotorLock = false;
                    rotor_main_back.task_degr = 0;
                }
                else
                {
                    if (rotor_main_back.GetCurrentGradus() < 0.1f && rotor_main_back.task_degr == null)
                    {
                        rotor_main_back.obj.TargetVelocityRad = 0f;
                        if (!rotor_main_back.obj.RotorLock) rotor_main_back.obj.RotorLock = true;

                        if (hinge_main_back.GetCurrentGradus() > -89.9f && hinge_main_back.task_degr == null)
                        {
                            hinge_main_back.obj.RotorLock = false;
                            hinge_main_back.task_degr = -90f;
                        }
                        if (hinge_node_back_1_1.GetCurrentGradus() < 89.9f && hinge_node_back_1_1.task_degr == null)
                        {
                            hinge_node_back_1_1.obj.RotorLock = false;
                            hinge_node_back_1_1.task_degr = 90f;
                        }
                        if (hinge_node_back_1_2.GetCurrentGradus() < 89.9f && hinge_node_back_1_2.task_degr == null)
                        {
                            hinge_node_back_1_2.obj.RotorLock = false;
                            hinge_node_back_1_2.task_degr = 90f;
                        }
                        if (hinge_node_back_2_1.GetCurrentGradus() < 89.9f && hinge_node_back_2_1.task_degr == null)
                        {
                            hinge_node_back_2_1.obj.RotorLock = false;
                            hinge_node_back_2_1.task_degr = 90f;
                        }
                        if (hinge_node_back_2_2.GetCurrentGradus() < 89.9f && hinge_node_back_2_2.task_degr == null)
                        {
                            hinge_node_back_2_2.obj.RotorLock = false;
                            hinge_node_back_2_2.task_degr = 90f;
                        }
                        if (IsCloseBack())
                        {
                            hinge_main_back.obj.RotorLock = true;
                            hinge_node_back_1_1.obj.RotorLock = true;
                            hinge_node_back_1_2.obj.RotorLock = true;
                            hinge_node_back_2_1.obj.RotorLock = true;
                            hinge_node_back_2_2.obj.RotorLock = true;
                            result = true;
                        }
                    }
                }
                return result;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "open_sp":
                        close_forw = false;
                        close_back = false;
                        open_forw = true;
                        open_back = true;
                        break;
                    case "close_sp":
                        close_forw = true;
                        close_back = true;
                        open_forw = false;
                        open_back = false;
                        break;                    
                    case "open_sp_forw":
                        close_forw = false;
                        open_forw = true;
                        break;
                    case "close_sp_forw":
                        open_forw = false;
                        close_forw = true;
                        break;
                    case "open_sp_back":
                        close_back = false;
                        open_back = true;
                        break;
                    case "close_sp_back":
                        open_back = false;
                        close_back = true;
                        break;
                    case "clear_sp":
                        close_forw = false;
                        close_back = false;
                        open_forw = false;
                        open_back = false;
                        break;
                    default:
                        break;
                }
                hinge_main_back.Logic(argument, updateSource);
                rotor_main_back.Logic(argument, updateSource);
                hinge_node_back_1_1.Logic(argument, updateSource);
                hinge_node_back_1_2.Logic(argument, updateSource);
                hinge_node_back_2_1.Logic(argument, updateSource);
                hinge_node_back_2_2.Logic(argument, updateSource);
                hinge_main_forw.Logic(argument, updateSource);
                rotor_main_forw.Logic(argument, updateSource);
                hinge_node_forw_1_1.Logic(argument, updateSource);
                hinge_node_forw_1_2.Logic(argument, updateSource);
                hinge_node_forw_2_1.Logic(argument, updateSource);
                hinge_node_forw_2_2.Logic(argument, updateSource);
                if (updateSource == UpdateType.Update10)
                {
                    if (open_forw && OpenForw())
                    {
                        open_forw = false;
                    }
                    if (close_forw && CloseForw())
                    {
                        close_forw = false;
                    }
                    if (open_back && OpenBack())
                    {
                        open_back = false;
                    }
                    if (close_back && CloseBack())
                    {
                        close_back = false;
                    }
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СОЛНЕЧНАЯ ПАНЕЛ (Forw)" + (IsOpenForw() ? igreen.ToString() : (IsCloseForw() ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("КОМАНДА [ ОТК : " + (open_forw ? igreen.ToString() : ired.ToString()) + " ЗАК : " + (close_forw ? igreen.ToString() : ired.ToString()) + " ]\n");
                values.Append("\n");
                values.Append("СОЛНЕЧНАЯ ПАНЕЛ (Back)" + (IsOpenBack() ? igreen.ToString() : (IsCloseBack() ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("КОМАНДА [ ОТК : " + (open_back ? igreen.ToString() : ired.ToString()) + " ЗАК : " + (close_back ? igreen.ToString() : ired.ToString()) + " ]\n");
                return values.ToString();
            }
        }
        public class CombatSystem
        {
            public bool open = false;
            public bool close = false;
            MotorStator hinge_main_1, hinge_main_2, hinge_main_3, hinge_main_4, hinge_main_5, hinge_main_6, hinge_main_7, hinge_main_8;
            MotorStator hinge_turel_1, hinge_turel_2, hinge_turel_3, hinge_turel_4, hinge_turel_5, hinge_turel_6, hinge_turel_7, hinge_turel_8;
            PistonBase piston_1, piston_2, piston_3, piston_4, piston_5, piston_6, piston_7, piston_8;
            PistonBase piston_tower_1, piston_tower_2;
            public CombatSystem(string name_obj)
            {
                // [MPB-1]-Шарнир [c-s-hinge-main] 2
                // [MPB-1]-Поршень [c-s-pistone] 2
                // [MPB-1]-Шарнир [c-s-hinge-turel] 2
                // [MPB-1]-Шарнир [c-s-hinge-node] 2                
                hinge_main_1 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-main] 1");
                hinge_main_2 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-main] 2");
                hinge_main_3 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-main] 3");
                hinge_main_4 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-main] 4");
                hinge_main_5 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-main] 5");
                hinge_main_6 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-main] 6");
                hinge_main_7 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-main] 7");
                hinge_main_8 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-main] 8");
                piston_1 = new PistonBase("[MPB-1]-Поршень [c-s-pistone] 1");
                piston_2 = new PistonBase("[MPB-1]-Поршень [c-s-pistone] 2");
                piston_3 = new PistonBase("[MPB-1]-Поршень [c-s-pistone] 3");
                piston_4 = new PistonBase("[MPB-1]-Поршень [c-s-pistone] 4");
                piston_5 = new PistonBase("[MPB-1]-Поршень [c-s-pistone] 5");
                piston_6 = new PistonBase("[MPB-1]-Поршень [c-s-pistone] 6");
                piston_7 = new PistonBase("[MPB-1]-Поршень [c-s-pistone] 7");
                piston_8 = new PistonBase("[MPB-1]-Поршень [c-s-pistone] 8");
                hinge_turel_1 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-turel] 1");
                hinge_turel_2 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-turel] 2");
                hinge_turel_3 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-turel] 3");
                hinge_turel_4 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-turel] 4");
                hinge_turel_5 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-turel] 5");
                hinge_turel_6 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-turel] 6");
                hinge_turel_7 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-turel] 7");
                hinge_turel_8 = new MotorStator("[MPB-1]-Шарнир [c-s-hinge-turel] 8");
                piston_tower_1 = new PistonBase("[MPB-1]-Поршень [c-s-pistone-tower] 1");
                piston_tower_2 = new PistonBase("[MPB-1]-Поршень [c-s-pistone-tower] 2");
            }
            public bool IsOpen(ref MotorStator main, ref PistonBase piston, ref MotorStator turel)
            {
                if (main.GetCurrentGradus() > 88.9f && main.task_degr == null && piston.obj.CurrentPosition > 9.5f && piston.task_position == null && turel.GetCurrentGradus() > 89.5f && turel.task_degr == null)
                    return true;
                else return false;
            }
            public bool Open(ref MotorStator main, ref PistonBase piston, ref MotorStator turel)
            {
                bool result = false;
                if (main.GetCurrentGradus() < 88.9f && main.task_degr == null)
                {
                    main.obj.RotorLock = false;
                    main.task_degr = 89.1f;
                }
                else
                {
                    if (main.GetCurrentGradus() >= 88.9f && main.task_degr == null)
                    {
                        main.obj.RotorLock = true;
                        if (piston.obj.CurrentPosition < 9.5f && piston.task_position == null)
                        {
                            piston.task_position = 10.0f;
                        }
                        if (turel.GetCurrentGradus() < 89.9f && turel.task_degr == null)
                        {
                            turel.obj.RotorLock = false;
                            turel.task_degr = 90f;
                        }
                        if (IsOpen(ref main, ref piston, ref turel))
                        {
                            main.obj.RotorLock = true;
                            turel.obj.RotorLock = true;
                            result = true;
                        }
                    }

                }
                return result;
            }
            public bool IsOpenTower()
            {
                if (piston_tower_1.obj.CurrentPosition > 9.5f && piston_tower_1.task_position == null
                    && piston_tower_2.obj.CurrentPosition > 9.5f && piston_tower_2.task_position == null)
                    return true;
                else return false;
            }
            public bool OpenTower()
            {
                bool result = false;
                if (piston_tower_1.obj.CurrentPosition < 9.5f && piston_tower_1.task_position == null)
                {
                    piston_tower_1.task_position = 10.0f;
                }
                if (piston_tower_2.obj.CurrentPosition < 9.5f && piston_tower_2.task_position == null)
                {
                    piston_tower_2.task_position = 10.0f;
                }
                if (IsOpenTower())
                {
                    result = true;
                }
                return result;
            }
            public bool OpenAll()
            {
                return Open(ref hinge_main_1, ref piston_1, ref hinge_turel_1) &
                        Open(ref hinge_main_2, ref piston_2, ref hinge_turel_2) &
                        Open(ref hinge_main_3, ref piston_3, ref hinge_turel_3) &
                        Open(ref hinge_main_4, ref piston_4, ref hinge_turel_4) &
                        Open(ref hinge_main_5, ref piston_5, ref hinge_turel_5) &
                        Open(ref hinge_main_6, ref piston_6, ref hinge_turel_6) &
                        Open(ref hinge_main_7, ref piston_7, ref hinge_turel_7) &
                        Open(ref hinge_main_8, ref piston_8, ref hinge_turel_8) &
                        OpenTower();
            }
            public bool IsOpenAll()
            {
                return IsOpen(ref hinge_main_1, ref piston_1, ref hinge_turel_1) &
                        IsOpen(ref hinge_main_2, ref piston_2, ref hinge_turel_2) &
                        IsOpen(ref hinge_main_3, ref piston_3, ref hinge_turel_3) &
                        IsOpen(ref hinge_main_4, ref piston_4, ref hinge_turel_4) &
                        IsOpen(ref hinge_main_5, ref piston_5, ref hinge_turel_5) &
                        IsOpen(ref hinge_main_6, ref piston_6, ref hinge_turel_6) &
                        IsOpen(ref hinge_main_7, ref piston_7, ref hinge_turel_7) &
                        IsOpen(ref hinge_main_8, ref piston_8, ref hinge_turel_8) &
                        IsOpenTower();
            }
            public bool IsClose(ref MotorStator main, ref PistonBase piston, ref MotorStator turel)
            {
                if (main.GetCurrentGradus() < 0.5f && main.task_degr == null && piston.obj.CurrentPosition < 0.5f && piston.task_position == null && turel.GetCurrentGradus() < 0.1f && turel.task_degr == null)
                    return true;
                else return false;
            }
            public bool Close(ref MotorStator main, ref PistonBase piston, ref MotorStator turel)
            {
                bool result = false;
                if (piston.obj.CurrentPosition > 0.1f && piston.task_position == null)
                {
                    piston.task_position = 0.0f;
                }
                if (turel.GetCurrentGradus() > 0.1f && turel.task_degr == null)
                {
                    turel.obj.RotorLock = false;
                    turel.task_degr = 0f;
                }
                if (piston.obj.CurrentPosition < 0.5f && piston.task_position == null
                    && turel.GetCurrentGradus() < 0.1f && turel.task_degr == null)
                {
                    if (main.GetCurrentGradus() > 0.5f && main.task_degr == null)
                    {
                        main.obj.RotorLock = false;
                        main.task_degr = 0f;
                    }
                    if (main.GetCurrentGradus() < 0.5f && main.task_degr == null)
                    {
                        main.obj.RotorLock = true;
                        turel.obj.RotorLock = true;
                        result = true;
                    }
                }
                return result;
            }
            public bool IsCloseTower()
            {
                if (piston_tower_1.obj.CurrentPosition < 0.5f && piston_tower_1.task_position == null
                    && piston_tower_2.obj.CurrentPosition < 0.5f && piston_tower_2.task_position == null)
                    return true;
                else return false;
            }
            public bool CloseTower()
            {
                bool result = false;
                if (piston_tower_1.obj.CurrentPosition > 0.5f && piston_tower_1.task_position == null)
                {
                    piston_tower_1.task_position = 0.0f;
                }
                if (piston_tower_2.obj.CurrentPosition > 0.5f && piston_tower_2.task_position == null)
                {
                    piston_tower_2.task_position = 0.0f;
                }
                if (IsCloseTower())
                {
                    result = true;
                }
                return result;
            }
            public bool CloseAll()
            {
                return Close(ref hinge_main_1, ref piston_1, ref hinge_turel_1) &
                        Close(ref hinge_main_2, ref piston_2, ref hinge_turel_2) &
                        Close(ref hinge_main_3, ref piston_3, ref hinge_turel_3) &
                        Close(ref hinge_main_4, ref piston_4, ref hinge_turel_4) &
                        Close(ref hinge_main_5, ref piston_5, ref hinge_turel_5) &
                        Close(ref hinge_main_6, ref piston_6, ref hinge_turel_6) &
                        Close(ref hinge_main_7, ref piston_7, ref hinge_turel_7) &
                        Close(ref hinge_main_8, ref piston_8, ref hinge_turel_8) &
                        CloseTower();
            }
            public bool IsCloseAll()
            {
                return IsClose(ref hinge_main_1, ref piston_1, ref hinge_turel_1) &
                        IsClose(ref hinge_main_2, ref piston_2, ref hinge_turel_2) &
                        IsClose(ref hinge_main_3, ref piston_3, ref hinge_turel_3) &
                        IsClose(ref hinge_main_4, ref piston_4, ref hinge_turel_4) &
                        IsClose(ref hinge_main_5, ref piston_5, ref hinge_turel_5) &
                        IsClose(ref hinge_main_6, ref piston_6, ref hinge_turel_6) &
                        IsClose(ref hinge_main_7, ref piston_7, ref hinge_turel_7) &
                        IsClose(ref hinge_main_8, ref piston_8, ref hinge_turel_8) &
                        IsCloseTower();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "open_cs":
                        open = true;
                        close = false;
                        break;
                    case "close_cs":
                        open = false;
                        close = true;
                        break;
                    default:
                        break;
                }
                hinge_main_1.Logic(argument, updateSource);
                hinge_main_2.Logic(argument, updateSource);
                hinge_main_3.Logic(argument, updateSource);
                hinge_main_4.Logic(argument, updateSource);
                hinge_main_5.Logic(argument, updateSource);
                hinge_main_6.Logic(argument, updateSource);
                hinge_main_7.Logic(argument, updateSource);
                hinge_main_8.Logic(argument, updateSource);
                hinge_turel_1.Logic(argument, updateSource);
                hinge_turel_2.Logic(argument, updateSource);
                hinge_turel_3.Logic(argument, updateSource);
                hinge_turel_4.Logic(argument, updateSource);
                hinge_turel_5.Logic(argument, updateSource);
                hinge_turel_6.Logic(argument, updateSource);
                hinge_turel_7.Logic(argument, updateSource);
                hinge_turel_8.Logic(argument, updateSource);
                piston_1.Logic(argument, updateSource);
                piston_2.Logic(argument, updateSource);
                piston_3.Logic(argument, updateSource);
                piston_4.Logic(argument, updateSource);
                piston_5.Logic(argument, updateSource);
                piston_6.Logic(argument, updateSource);
                piston_7.Logic(argument, updateSource);
                piston_8.Logic(argument, updateSource);
                piston_tower_1.Logic(argument, updateSource);
                piston_tower_2.Logic(argument, updateSource);
                if (updateSource == UpdateType.Update10)
                {
                    if (open && OpenAll())
                    {
                        open = false;
                    }
                    if (close && CloseAll())
                    {
                        close = false;
                    }
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("БОЕВАЯ СИСТЕМА" + (IsOpenAll() ? igreen.ToString() : (IsCloseAll() ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("КОМАНДА [ ОТК : " + (open ? igreen.ToString() : ired.ToString()) + " ЗАК : " + (close ? igreen.ToString() : ired.ToString()) + " ]\n");
                values.Append("Вышка АВТОПУШКА : " + (IsOpenTower() ? igreen.ToString() : (IsCloseTower() ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("ТУРЕЛИ " + "\n");
                values.Append("|- [1] " + (IsOpen(ref hinge_main_1, ref piston_1, ref hinge_turel_1) ? igreen.ToString() : (IsClose(ref hinge_main_1, ref piston_1, ref hinge_turel_1) ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("|- [2] " + (IsOpen(ref hinge_main_2, ref piston_2, ref hinge_turel_2) ? igreen.ToString() : (IsClose(ref hinge_main_2, ref piston_2, ref hinge_turel_2) ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("|- [3] " + (IsOpen(ref hinge_main_3, ref piston_3, ref hinge_turel_3) ? igreen.ToString() : (IsClose(ref hinge_main_3, ref piston_3, ref hinge_turel_3) ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("|- [4] " + (IsOpen(ref hinge_main_4, ref piston_4, ref hinge_turel_4) ? igreen.ToString() : (IsClose(ref hinge_main_4, ref piston_4, ref hinge_turel_4) ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("|- [5] " + (IsOpen(ref hinge_main_5, ref piston_5, ref hinge_turel_5) ? igreen.ToString() : (IsClose(ref hinge_main_5, ref piston_5, ref hinge_turel_5) ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("|- [6] " + (IsOpen(ref hinge_main_6, ref piston_6, ref hinge_turel_6) ? igreen.ToString() : (IsClose(ref hinge_main_6, ref piston_6, ref hinge_turel_6) ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("|- [7] " + (IsOpen(ref hinge_main_7, ref piston_7, ref hinge_turel_7) ? igreen.ToString() : (IsClose(ref hinge_main_7, ref piston_7, ref hinge_turel_7) ? ired.ToString() : iyellow.ToString())) + "\n");
                values.Append("|- [8] " + (IsOpen(ref hinge_main_8, ref piston_8, ref hinge_turel_8) ? igreen.ToString() : (IsClose(ref hinge_main_8, ref piston_8, ref hinge_turel_8) ? ired.ToString() : iyellow.ToString())) + "\n");
                return values.ToString();
            }
        }
    }
}

// door [door-gateway] [transition_external_back] [transition]
// sn [door-gateway] [transition_external_back] [transition]
// door [door-gateway] [transition_external_back] [external]
// sn [door-gateway] [transition_external_back] [external]

// sn [door-transition] [module_transition] [module]
// sn [door-transition] [module_transition] [transition]
// door [door-transition] [module_transition]

// sn [door-transition] [cabin_energy_module_right] [energy_module_right]
// sn [door-transition] [cabin_energy_module_right] [cabin]
// door [door-transition] [cabin_energy_module_right]

//-light [lighting_room] [transition]

// [piston-wind-generator]
// [main-hinge]
// [MPB-1]-Шарнир [articulated-joint] 1-1 back
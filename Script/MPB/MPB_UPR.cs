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

        ReflectorsLight reflectors_light;
        Gateways gateways_doors;
        Transitions transition_door;
        Lightings room_light;
        ReflectorLight ref_light;
        static PistonBase piston_base;
        MechanicalConnectior mechanical_connectior;
        FoldingSolarPanel folding_sp;

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
            piston_base = new PistonBase(NameObj, tag_piston_wg);
            folding_sp = new FoldingSolarPanel(NameObj);
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
            // В космосе людей не считаем
            count_room[(int)room.external] = 0;
            // Логика отработки включения и выключения освещения
            room_light.Logic(argument, updateSource);

            switch (argument)
            {
                case "open_pwg":
                    piston_base.Open();
                    break;
                case "close_pwg":
                    piston_base.Close();
                    break;
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                lcd_power.OutText("ВЕТРОГЕНЕРАТОРЫ" + "\n", false);
                float pos = piston_base.GetPosition();
                lcd_power.OutText("|- Положение : " + pos / 4 + "\n", true);
                lcd_power.OutText("|- Выдвенут  : " + (pos == 0f ? ired.ToString() : (pos == 80f ? igreen.ToString() : iyellow.ToString())) + "\n", true);
                lcd_power.OutText("ВЕТРОГЕНЕРАТОРЫ" + "\n", true);
                lcd_power.OutText(folding_sp.TextInfo(), true);
                //lcd_power
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
                lcd_debug.OutText("Start" + "\n", false);
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                _scr.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                lcd_debug.OutText("doors:" + doors.Count() + "\n", true);
                lcd_debug.OutText("sensors:" + doors.Count() + "\n", true);
                IMyDoor door1;
                IMySensorBlock sensor1;
                room room1;
                IMyDoor door2;
                IMySensorBlock sensor2;
                room room2;
                lcd_debug.OutText("Поиск дверей:" + "\n", true);
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
                    lcd_debug.OutText("l_drs:" + l_drs.Count() + "\n", true);
                    lcd_debug.OutText("l_sns:" + l_sns.Count() + "\n", true);
                    if (l_drs != null && l_drs.Count() == 2 && l_sns != null && l_sns.Count() == 2)
                    {
                        foreach (room rm in Enum.GetValues(typeof(room)))
                        {
                            lcd_debug.OutText("room:" + rm + "\n", true);
                            IMyDoor dr = l_drs.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
                            IMySensorBlock sn = l_sns.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
                            lcd_debug.OutText("dr:" + (dr != null ? "ok" : "not") + "\n", true);
                            lcd_debug.OutText("sn:" + (sn != null ? "ok" : "not") + "\n", true);
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
                            lcd_debug.OutText("door1:" + door1.CustomName + "\n", true);
                            lcd_debug.OutText("door2:" + door2.CustomName + "\n", true);
                            lcd_debug.OutText("sensor1:" + sensor1.CustomName + "\n", true);
                            lcd_debug.OutText("sensor2:" + sensor2.CustomName + "\n", true);
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
        public class PistonBase : BaseListTerminalBlock<IMyPistonBase>
        {
            public PistonBase(string name_obj, string tag) : base(name_obj)
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

            //public bool open = false;
            //public bool close = false;
            IMyMotorStator hinge_main_back;
            IMyMotorStator hinge_main_forw;
            List<IMyMotorStator> hinge_node_back;
            List<IMyMotorStator> hinge_node_forw;
            public FoldingSolarPanel(string name_obj)
            {
                // [s-p-hinge-main-back]
                // [s-p-hinge-node-back]
                // [s-p-rotor-main-back]
                hinge_main_back = _scr.GridTerminalSystem.GetBlockWithName("[s-p-hinge-main-back]") as IMyMotorStator;
                hinge_main_forw = _scr.GridTerminalSystem.GetBlockWithName("[s-p-hinge-main-forw]") as IMyMotorStator;
                _scr.GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(hinge_node_back, r => r.CustomName.Contains("[s-p-hinge-node-back]"));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(hinge_node_forw, r => r.CustomName.Contains("[s-p-hinge-node-forw]"));
            }
            double RadToGradus(float rad)
            {
                return rad * 180 / Math.PI;
            }
            void SetRotate(float degrees, IMyMotorStator mortor, float min, float max, float speed)
            {
                mortor.TargetVelocityRPM = 0;
                // Текущее положение
                double motor_curennt_grad = RadToGradus(mortor.Angle);

                if (degrees < motor_curennt_grad)
                {
                    // Движим влево
                    // Если задали меньше чем уставка тогда движем
                    if (degrees > min)
                    {
                        mortor.LowerLimitDeg = degrees;
                        mortor.TargetVelocityRPM = speed * -1;
                    }
                }
                else
                {
                    // Движим вправо
                    // Если задали меньше чем уставка тогда движем
                    if (degrees < max)
                    {
                        mortor.UpperLimitDeg = degrees;
                        mortor.TargetVelocityRPM = speed;
                    }
                };


            }
            public void Open()
            {
                float ang = ms_main_back.GetAngle();
                float ang1 = articulated_back.GetAngle();
                if (ang < 0)
                {
                    ms_main_back.MotorVelocit(1f);
                }
                if (ang1 > 0)
                {
                    articulated_back.MotorVelocit(-1f);
                }
            }
            public void Close()
            {
                float ang = ms_main_back.GetAngle();
                float ang1 = articulated_back.GetAngle();
                if (ang > 0)
                {
                    ms_main_back.MotorVelocit(-1f);
                }
                if (ang1 < 90)
                {
                    articulated_back.MotorVelocit(1f);
                }
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "open_sp":
                        Open();
                        break;
                    case "close_sp":
                        Close();
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
                values.Append("Угол Main    : " + Math.Round(ms_main_back.GetAngle(), 2) + "\n");
                values.Append("Угол Artic    : " + Math.Round(articulated_back.GetAngle(), 2) + "\n");
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
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
/// v3.0
/// Управление дверями и освещение
/// </summary>
namespace MUL_H1_UPR
{
    public sealed class Program : MyGridProgram
    {
        // v3.
        string NameObj = "[MUL-H1]";
        string NameConnector = "[MUL-H1]-Коннектор парковка";
        string NameLCDInfo_Upr = "[MUL-H1]-LCD-INFO-UPR";

        string tag_door_gateway = "[door-gateway]";
        string tag_lighting_room = "[lighting_room]";
        public enum room : int
        {
            none = 0,
            cabin = 1,             // кабина
            space = 2,         // Космос
        };
        public static string[] name_room = { "", "Кабина", "Выход в космос" };
        public static int[] count_room = { 0, 0, 0 };
        public enum doors_gareways : int
        {
            space_cabin1 = 1,
            space_cabin2 = 2,
        }

        // door [door-gateway] [space_cabin2] [cabin]
        // sn [door-gateway] [space_cabin2] [cabin]
        // door [door-gateway] [space_cabin2] [space]
        // sn [door-gateway] [space_cabin2] [space]

        //light [lighting_room] [cabin]

        LCD lcd_info_upr;
        Connector connector;
        ReflectorsLight reflectors_light;
        Gateways gateways_doors;
        Lightings room_light;

        static Program _scr;

        bool ship_connect = false;
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

            connector = new Connector(NameConnector);
            ship_connect = connector.Connected;
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            gateways_doors = new Gateways(NameObj, tag_door_gateway);
            room_light = new Lightings(NameObj, tag_lighting_room); // Освещение
            room_light.Off();
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            gateways_doors.Logic(argument, updateSource);   // Логика отработки шлюзовых дверей
            room_light.Logic(argument, updateSource);       // Логика отработки освещения
            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                // Проверим корабль не припаркован
                if (!connector.Connected)
                {

                }
                else
                {
                    // Припаркован
                    reflectors_light.Off();
                }
            }
            //values_info.Append(connector.TextInfo());
            //lcd_info_upr.OutText(values_info);
            ship_connect = connector.Connected; // сохраним состояние
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
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("КОННЕКТОР: [" + GetInfoStatus() + "]" + "\n");
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
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorsLight(string name_obj) : base(name_obj)
            {
            }
            public ReflectorsLight(string name_obj, string tag) : base(name_obj, tag)
            {

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
    }
}

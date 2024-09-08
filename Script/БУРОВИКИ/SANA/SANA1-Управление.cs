using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRageMath;

namespace SANA1_UPR
{
    public sealed class Program : MyGridProgram
    {
        // Название 
        string NameObj = "SANA1";

        string NameCockpit = "SANA1-Кресло пилота";
        Cockpit cockpit;
        // Дверь левая выход в космос
        string NameDoorExt_left = "SANA1-Разд. дв. вых. левая external";
        string NameDoorInt_left = "SANA1-Разд. дв. вых. левая internal";
        sensor_option sensor_option_ext_left = new sensor_option()
        {
            name = "SANA1-Сенсор вх.дверь external",
            lf = 1.0f,
            rg = 1.0f,
            bt = 0.0f,
            tp = 4.0f,
            bc = 3.0f,
            fr = 2.5f
        };
        sensor_option sensor_option_int_left = new sensor_option()
        {
            name = "SANA1-Сенсор вх.дверь internal",
            lf = 0.0f,
            rg = 0.0f,
            bt = 3.0f,
            tp = 1.0f,
            bc = 0.0f,
            fr = 2.5f
        };
        DoorGateway door_gataway_left;
        // Дверь правая выход в космос
        string NameDoorExt_right = "SANA1-Разд. дв. вых. правая external";
        string NameDoorInt_right = "SANA1-Разд. дв. вых. правая internal";
        sensor_option sensor_option_ext_right = new sensor_option()
        {
            name = "SANA1-Сенсор вх.правая external",
            lf = 1.0f,
            rg = 1.0f,
            bt = 0.0f,
            tp = 4.0f,
            bc = 3.0f,
            fr = 2.5f
        };
        sensor_option sensor_option_int_right = new sensor_option()
        {
            name = "SANA1-Сенсор вх.правая internal",
            lf = 0.0f,
            rg = 0.0f,
            bt = 3.0f,
            tp = 1.0f,
            bc = 0.0f,
            fr = 2.5f
        };
        DoorGateway door_gataway_right;
        // Дверь перехода в маш зал
        string NameDoorExt_engine_room = "SANA1-Разд. дв. пер. маш. зал. external";
        string NameDoorInt_engine_room = "SANA1-Разд. дв. вых. маш. зал. internal";
        sensor_option sensor_option_ext_engine_room = new sensor_option()
        {
            name = "SANA1-Сенсор вх. маш. зал. external",
            lf = 1.0f,
            rg = 1.0f,
            bt = 3.0f,
            tp = 1.0f,
            bc = 0.0f,
            fr = 2.5f
        };
        sensor_option sensor_option_int_engine_room = new sensor_option()
        {
            name = "SANA1-Сенсор вх. маш. зал. internal",
            lf = 0.0f,
            rg = 0.0f,
            bt = 3.0f,
            tp = 2.5f,
            bc = 0.0f,
            fr = 3.0f
        };
        DoorGateway door_gataway_engine_room;
        // Дверь выхода в космом из маш зала
        string NameDoorExt_rear_exit = "SANA1-Разд. дв. вых. задний external";
        string NameDoorInt_rear_exit = "SANA1-Разд. дв. вых. задний internal";
        sensor_option sensor_option_ext_rear_exit = new sensor_option()
        {
            name = "SANA1-Сенсор вых. зад. external",
            lf = 1.0f,
            rg = 1.0f,
            bt = 3.0f,
            tp = 1.0f,
            bc = 0.0f,
            fr = 2.5f
        };
        sensor_option sensor_option_int_rear_exit = new sensor_option()
        {
            name = "SANA1-Сенсор вых. зад. internal",
            lf = 0.0f,
            rg = 0.0f,
            bt = 3.0f,
            tp = 1.0f,
            bc = 0.0f,
            fr = 2.5f
        };
        DoorGateway door_gataway_rear_exit;
        // Освещение
        InteriorLight light_room;
        // Генераторы газов
        GasGenerator gas_generators;
        // Баки
        GasTank gas_tank;
        // Трапстеры
        Thrust thrust;

        IMyTextPanel test_lcd, test_lcd1;
        IMyInteriorLight ltest;

        int count_operator_room = 0; // Кол людей в помещениии
        int count_engine_room = 0;  // Кол людей в помещениии

        static Program _scr;
        public class PText
        {
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
            static public string GetCountThrust(int count, int count_on)
            {
                return count + "|" + count_on;
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
        }
        public class FLib
        {
            static public void Off<T>(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_Off");
                }
            }
            static public void OffOfGroup<T>(List<T> list, string group)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(group))
                    {
                        obj.ApplyAction("OnOff_Off");
                    }
                }
            }
            static public void On<T>(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_On");
                }
            }
            static public void OnOfGroup<T>(List<T> list, string group)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(group))
                    {
                        obj.ApplyAction("OnOff_On");
                    }
                }
            }
        }
        public class BaseListTerminalBlock<T> where T : class
        {
            public enum MySubtypeName : int
            {
                none = 0,
                LargeBlockSmallAtmosphericThrust = 10,
                LargeBlockSmallAtmosphericThrustSciFi = 11,
                LargeBlockLargeAtmosphericThrust = 110,
                LargeBlockLargeAtmosphericThrustSciFi = 111,

                LargeBlockSmallHydrogenThrust = 20,
                LargeBlockSmallHydrogenThrustSciFi = 21,
                LargeBlockLargeHydrogenThrust = 120,
                LargeBlockLargeHydrogenThrustSciFi = 121,

                LargeBlockSmallThrust = 30,
                LargeBlockSmallThrustSciFi = 31,
                LargeBlockLargeThrust = 130,
                LargeBlockLargeThrustSciFi = 131,
            }

            public class MyGroupBlock
            {
                public int id { get; set; }
                public string Name { get; set; }
                public string FullName { get; set; }

                public List<MySubtypeName> list_subtype_name = new List<MySubtypeName>();
            }

            public List<MyGroupBlock> list_group = new List<MyGroupBlock>();

            public List<T> list_obj = new List<T>();
            public int Count { get { return list_obj.Count(); } }
            public BaseListTerminalBlock(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo(typeof(T).Name + "[" + name_obj + "]" + ((list_obj != null && list_obj.Count() > 0) ? ("Ок") : ("not found"))); ;
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
            private void OffOfGroup(List<T> list, string group)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(group))
                    {
                        obj.ApplyAction("OnOff_Off");
                    }
                }
            }
            public void OffOfGroup(string group)
            {
                OffOfGroup(list_obj, group);
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
            private void OnOfGroup(List<T> list, string group)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(group))
                    {
                        obj.ApplyAction("OnOff_On");
                    }
                }
            }
            public void OnOfGroup(string group)
            {
                OnOfGroup(list_obj, group);
            }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            test_lcd = GridTerminalSystem.GetBlockWithName("test_lcd") as IMyTextPanel;
            Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));
            test_lcd1 = GridTerminalSystem.GetBlockWithName("test_lcd1") as IMyTextPanel;
            Echo("test_lcd1: " + ((test_lcd1 != null) ? ("Ок") : ("not found")));

            ltest = GridTerminalSystem.GetBlockWithName("test_lampa") as IMyInteriorLight;
            Echo("ltest: " + ((ltest != null) ? ("Ок") : ("not found")));

            cockpit = new Cockpit(NameCockpit);
            door_gataway_left = new DoorGateway(sensor_option_ext_left, sensor_option_int_left, NameDoorExt_left, NameDoorInt_left);
            door_gataway_right = new DoorGateway(sensor_option_ext_right, sensor_option_int_right, NameDoorExt_right, NameDoorInt_right);
            door_gataway_engine_room = new DoorGateway(sensor_option_ext_engine_room, sensor_option_int_engine_room, NameDoorExt_engine_room, NameDoorInt_engine_room);
            door_gataway_rear_exit = new DoorGateway(sensor_option_ext_rear_exit, sensor_option_int_rear_exit, NameDoorExt_rear_exit, NameDoorInt_rear_exit);
            light_room = new InteriorLight(NameObj);
            light_room.Off();
            gas_generators = new GasGenerator(NameObj);
            gas_generators.Off();
            gas_tank = new GasTank(NameObj);
            gas_tank.On();
            thrust = new Thrust(NameObj);
            thrust.Off();
        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            //test_lcd.WriteText("old:" + old_parking.ToString() + " connector:"+connector.IsParkingEnabled.ToString() + "\n" , true);  
            //test_lcd.WriteText("clock="+ clock.ToString() +"updateSource-" + updateSource + "\n", false);            
            // Проверим логику
            int x = 0;
            door_gataway_left.Logic(argument, updateSource, ref count_operator_room, ref x);
            door_gataway_right.Logic(argument, updateSource, ref count_operator_room, ref x);
            door_gataway_engine_room.Logic(argument, updateSource, ref count_operator_room, ref count_engine_room);
            door_gataway_rear_exit.Logic(argument, updateSource, ref count_engine_room, ref x);

            switch (argument)
            {
                //case "connected_on":
                //    break;
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                thrust.GetValueThrusts(cockpit.IsUnderControl);
                //clock++;
                test_lcd.WriteText("IsInputDoor=" + door_gataway_left.IsInputDoor + "\n", false);
                test_lcd.WriteText("IsOutputDoor=" + door_gataway_left.IsOutputDoor + "\n", true);
                test_lcd.WriteText("count_operator_room=" + count_operator_room + "\n", true);
                test_lcd.WriteText("count_engine_room=" + count_engine_room + "\n", true);
                test_lcd.WriteText(gas_generators.GetStatusOfText(), true);
                test_lcd.WriteText(gas_tank.GetStatusOfText(), true);
                test_lcd1.WriteText(thrust.GetStatusOfText(false), false);

                if (count_operator_room > 0)
                {
                    light_room.OnOfGroup("operator_room");
                }
                else
                {
                    light_room.OffOfGroup("operator_room");
                    count_operator_room = 0;
                }
                if (count_engine_room > 0)
                {
                    light_room.OnOfGroup("engine_room");
                }
                else
                {
                    light_room.OffOfGroup("engine_room");
                    count_engine_room = 0;
                }

            }

        }
        public class Cockpit
        {
            IMyShipController obj;
            public bool IsUnderControl { get { return obj.IsUnderControl; } }
            public Cockpit(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipController;
                _scr.Echo("cockpit: " + ((obj != null) ? ("Ок") : ("not found")));
            }
            public void Dampeners(bool on)
            {
                obj.DampenersOverride = on;
            }
            // Получить axis горизонта
            public Vector3D GetAxisHorizon()
            {
                Vector3D grav = Vector3D.Normalize(obj.GetNaturalGravity());
                Vector3D axis = grav.Cross(obj.WorldMatrix.Down);
                if (grav.Dot(obj.WorldMatrix.Down) < 0)
                {
                    axis = Vector3D.Normalize(axis);
                }
                return axis;
            }
        }
        //------------------------------------------------------------
        public class sensor_option
        {
            public string name { get; set; }
            public float lf { get; set; }   //Left - Охват слева
            public float rg { get; set; }   //Right - Охват справа
            public float bt { get; set; }   //Bottom - Охват снизу
            public float tp { get; set; }   //Top - Охват сверху
            public float bc { get; set; }   //Back - Охват сзади
            public float fr { get; set; }   //Front - Охват спереди
        }
        public class Sensor
        {
            IMySensorBlock sensor;
            public bool PlayProximitySound { get { return sensor.PlayProximitySound; } set { sensor.PlayProximitySound = value; } }
            public bool IsActive { get { return sensor.IsActive; } }
            // Сенсор
            public Sensor(string name)
            {
                sensor = _scr.GridTerminalSystem.GetBlockWithName(name) as IMySensorBlock;
                _scr.Echo("sensor[" + name + "]: " + ((sensor != null) ? ("Ок") : ("not found")));

                SetExtend(0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f);
                sensor.PlayProximitySound = true;       // звук
                SetDetect(true, false, false, false, false, false, false, true, false, false, false);
            }
            public Sensor(string name, float lf, float rg, float bt, float tp, float bc, float fr)
            {
                sensor = _scr.GridTerminalSystem.GetBlockWithName(name) as IMySensorBlock;
                _scr.Echo("sensor[" + name + "]: " + ((sensor != null) ? ("Ок") : ("not found")));

                SetExtend(lf, rg, bt, tp, bc, fr);
                SetDetect(true, false, false, false, false, false, false, true, false, false, false);
            }
            public Sensor(sensor_option setup)
            {
                sensor = _scr.GridTerminalSystem.GetBlockWithName(setup.name) as IMySensorBlock;
                _scr.Echo("sensor[" + setup.name + "]: " + ((sensor != null) ? ("Ок") : ("not found")));

                SetExtend(setup.lf, setup.rg, setup.bt, setup.tp, setup.bc, setup.fr);
                SetDetect(true, false, false, false, false, false, false, true, false, false, false);
            }

            public void SetExtend(float lf, float rg, float bt, float tp, float bc, float fr)
            {
                sensor.LeftExtend = lf;//Left - Охват слева
                sensor.RightExtend = rg;//Right - Охват справа
                sensor.BottomExtend = bt;//Bottom - Охват снизу
                sensor.TopExtend = tp;//Top - Охват сверху
                sensor.BackExtend = bc;//Back - Охват сзади
                sensor.FrontExtend = fr;//Front - Охват спереди
            }
            public void SetDetect(bool Players, bool FloatingObjects, bool SmallShips, bool LargeShips, bool Stations, bool Subgrids,
                bool Asteroids, bool Owner, bool Friendly, bool Neutral, bool Enemy)
            {
                sensor.DetectPlayers = Players;            // Играки
                sensor.DetectFloatingObjects = FloatingObjects;   // Обнаруживать плавающие объекты
                sensor.DetectSmallShips = SmallShips;        // Малые корабли
                sensor.DetectLargeShips = LargeShips;        // Большие корабли
                sensor.DetectStations = Stations;          // Большие станции
                sensor.DetectSubgrids = Subgrids;          // Подсетки
                sensor.DetectAsteroids = Asteroids;         // Астероиды планеты
                sensor.DetectOwner = Owner;              // Владельцы блоков
                sensor.DetectFriendly = Friendly;          // Дружественные игроки
                sensor.DetectNeutral = Neutral;           // Нитральные игроки
                sensor.DetectEnemy = Enemy;             // Враги
            }

        }
        public class Door
        {
            IMyDoor door;
            public DoorStatus Status { get { return door.Status; } }
            public Door(string name)
            {
                door = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyDoor;
                _scr.Echo("door[" + name + "]: " + ((door != null) ? ("Ок") : ("not door")));
            }

            public void Open()
            {
                door.OpenDoor();
            }
            public void Close()
            {
                door.CloseDoor();
            }


        }
        // Класс Шлюзовая дверь
        public class DoorGateway
        {

            Sensor sn_door_external;
            Sensor sn_door_internal;
            Door door_external;
            Door door_internal;

            bool input_door = false;
            bool output_door = false;
            bool internal_active = false;    // датчик входа
            bool external_active = false;   // датчик выхода

            public bool IsActiveSensorInternal { get { return sn_door_internal.IsActive; } }
            public bool IsActiveSensorExternal { get { return sn_door_internal.IsActive; } }

            public bool IsInputDoor { get { return input_door; } set { input_door = value; } }  // Вошол 
            public bool IsOutputDoor { get { return output_door; } set { output_door = value; } } // Вышел 
            public DoorGateway(sensor_option sensor_option_ext, sensor_option sensor_option_int, string NameDoorExt, string NameDoorInt)
            {
                // Создадим объекты 
                sn_door_external = new Sensor(sensor_option_ext);
                sn_door_internal = new Sensor(sensor_option_int);
                door_external = new Door(NameDoorExt);
                door_internal = new Door(NameDoorInt);

            }
            public void Logic(string argument, UpdateType updateSource, ref int count_input, ref int count_output)
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
                    if (!sn_door_internal.IsActive && door_internal.Status == DoorStatus.Open)
                    {
                        // Игрок не найден возле внутр двери
                        door_internal.Close();
                    }
                    if (sn_door_internal.IsActive && door_internal.Status == DoorStatus.Closed && door_external.Status == DoorStatus.Closed)
                    {
                        // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                        door_internal.Open();
                    }
                    if (!sn_door_external.IsActive && door_external.Status == DoorStatus.Open)
                    {
                        // Игрок не найден возле внутр двери
                        door_external.Close();
                    }
                    if (sn_door_external.IsActive && door_external.Status == DoorStatus.Closed && door_internal.Status == DoorStatus.Closed)
                    {
                        // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                        door_external.Open();
                    }
                    // Логика направдения движения
                    if (internal_active && !external_active && sn_door_external.IsActive)
                    {
                        // Выход
                        //internal_active = false;
                        external_active = true;
                        input_door = false;
                        output_door = true;
                        count_input--;
                        count_output++;
                    }
                    if (external_active && !internal_active && sn_door_internal.IsActive)
                    {
                        // Вход
                        internal_active = true;
                        //external_active = false;
                        input_door = true;
                        output_door = false;
                        count_input++;
                        count_output--;
                    }
                    if (external_active && internal_active && !sn_door_external.IsActive && !sn_door_internal.IsActive)
                    {
                        // Вход
                        internal_active = false;
                        external_active = false;
                        input_door = false;
                        output_door = false;
                    }

                    if (!internal_active && !external_active)
                    {
                        // Выход
                        internal_active = sn_door_internal.IsActive;
                        external_active = sn_door_external.IsActive;
                    }
                }

            }
            public void ClearMoveDoor()
            {
                input_door = false;
                output_door = false;
                internal_active = false;    // датчик входа
                external_active = false;   // датчик выхода
            }

        }
        public class InteriorLight : BaseListTerminalBlock<IMyInteriorLight>
        {
            public InteriorLight(string name_obj) : base(name_obj)
            {

            }
        }
        public class GasGenerator
        {
            List<IMyGasGenerator> list_obj = new List<IMyGasGenerator>();
            public int Count { get { return list_obj.Count(); } }
            public GasGenerator(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(list_obj, r => r.CustomName.Contains(name_obj));
                _scr.Echo("GasGenerator[" + name_obj + "]" + ((list_obj != null && list_obj.Count() > 0) ? ("Ок") : ("not found")));
            }
            public void On()
            {
                foreach (IMyGasGenerator obj in list_obj)
                {
                    obj.ApplyAction("OnOff_On");
                }
            }
            public void OnOfGroup(string group)
            {
                foreach (IMyGasGenerator obj in list_obj)
                {
                    if (obj.CustomName.Contains(group))
                    {
                        obj.ApplyAction("OnOff_On");
                    }


                }
            }
            public void Off()
            {
                foreach (IMyGasGenerator obj in list_obj)
                {
                    obj.ApplyAction("OnOff_Off");
                }
            }
            public void OffOfGroup(string group)
            {
                foreach (IMyGasGenerator obj in list_obj)
                {
                    if (obj.CustomName.Contains(group))
                    {
                        obj.ApplyAction("OnOff_Off");
                    }
                }
            }
            public string GetStatusOfText()
            {
                string result = "ГЕН.H2/O2: ";
                foreach (IMyGasGenerator obj in list_obj)
                {
                    result += "[" + (obj.Enabled ? "+" : "-") + "]";
                }
                result += "\n";
                return result;
            }
        }
        public class GasTank : BaseListTerminalBlock<IMyGasTank>
        {
            public GasTank(string name_obj) : base(name_obj)
            {

            }
            public string GetStatusOfText()
            {
                string tdh2 = "";
                string tdo2 = "";
                double fr_h2 = 0;
                float cap_h2 = 0;
                int count_th2 = 0;
                double fr_o2 = 0;
                float cap_o2 = 0;
                int count_to2 = 0;
                foreach (IMyGasTank obj in list_obj)
                {
                    switch (obj.DefinitionDisplayNameText)
                    {
                        case "Водородный бак":
                            {
                                fr_h2 += obj.FilledRatio;
                                cap_h2 += obj.Capacity;
                                count_th2++;
                                tdh2 += "|  |-БАК:[" + (obj.Enabled ? "{+}" : "{-}") + (obj.Stockpile ? "{>}" : "{<}") + (obj.AutoRefillBottles ? "{A}" : "{ }") + "]" + PText.GetPersent(obj.FilledRatio) + PText.GetCapacityTanks(obj.FilledRatio, obj.Capacity) + "\n";
                                break;
                            }
                        case "Кислородный бак":
                            {
                                fr_o2 += obj.FilledRatio;
                                cap_o2 += obj.Capacity;
                                count_to2++;
                                tdo2 += "|  |-БАК:[" + (obj.Enabled ? "{+}" : "{-}") + (obj.Stockpile ? "{>}" : "{<}") + (obj.AutoRefillBottles ? "{A}" : "{ }") + "]" + PText.GetPersent(obj.FilledRatio) + PText.GetCapacityTanks(obj.FilledRatio, obj.Capacity) + "\n";
                                break;
                            }
                    }
                }
                string result = "";
                result += "|    H2:" + PText.GetCapacityTanks((fr_h2 / count_th2), cap_h2) + "\n";
                result += "|-+" + PText.GetScalePersent((fr_h2 / count_th2), 50) + "\n";
                result += tdh2;
                result += "|\n";
                result += "|    O2:" + PText.GetCapacityTanks((fr_o2 / count_to2), cap_o2) + "\n";
                result += "|-+" + PText.GetScalePersent((fr_o2 / count_to2), 50) + "\n";
                result += tdo2;
                result += "|\n";
                return result;
            }
        }
        public class Thrust : BaseListTerminalBlock<IMyThrust>
        {
            public enum location : int
            {
                none = 0,
                up = 1,
                down = 2,
                right = 3,
                left = 4,
                forward = 5,
                backward = 6,
            }
            public enum group_thrust : int
            {
                none = 0,
                atmospheric = 1,
                hydrogen = 2,
                ionic = 3,
            }

            public List<valus_thrust> list_value_thrusts = new List<valus_thrust>();
            public class valus_thrust
            {
                public location location = location.none;
                public int id_group = 0;
                public MySubtypeName subtype_name = MySubtypeName.none;
                public string definition_display_name_text = null;
                public int count = 0;                  // кол
                public int count_on = 0;               // кол вкл
                public float sum_to = 0;               // перехват тяги тяга МН
                public float sum_to_percent = 0;       // процент от макс перехват тяги тяга %
                public float sum_max_thrust = 0;       // Макс тяга МН
                public float sum_max_eff_thrust = 0;   // Макс эфектив тяга МН
                public float sum_cur_thrust = 0;       // Текущая тяга МН
            }
            public Thrust(string name_obj) : base(name_obj)
            {
                list_group.Add(new MyGroupBlock()
                {
                    id = 1,
                    Name = "АУ",
                    FullName = "",
                    list_subtype_name = new List<MySubtypeName>()
                    {
                        MySubtypeName.LargeBlockSmallAtmosphericThrust,
                        MySubtypeName.LargeBlockSmallAtmosphericThrustSciFi,
                        MySubtypeName.LargeBlockLargeAtmosphericThrust,
                        MySubtypeName.LargeBlockLargeAtmosphericThrustSciFi,
                    }
                });
                list_group.Add(new MyGroupBlock()
                {
                    id = 1,
                    Name = "ВУ",
                    FullName = "",
                    list_subtype_name = new List<MySubtypeName>()
                    {
                        MySubtypeName.LargeBlockSmallHydrogenThrust,
                        MySubtypeName.LargeBlockSmallHydrogenThrustSciFi,
                        MySubtypeName.LargeBlockLargeHydrogenThrust,
                        MySubtypeName.LargeBlockLargeHydrogenThrustSciFi,
                    }
                });
                list_group.Add(new MyGroupBlock()
                {
                    id = 1,
                    Name = "ИУ",
                    FullName = "",
                    list_subtype_name = new List<MySubtypeName>()
                    {
                        MySubtypeName.LargeBlockSmallThrust,
                        MySubtypeName.LargeBlockSmallThrustSciFi,
                        MySubtypeName.LargeBlockLargeThrust,
                        MySubtypeName.LargeBlockLargeThrustSciFi,
                    }
                });
            }
            //public void GetValueThrusts2(bool is_control)
            //{
            //    list_value_thrusts.Clear();
            //    foreach (location loc in Enum.GetValues(typeof(location))) { 

            //    }

            //    // Под управлением с контроллера 
            //    if (is_control)
            //    {
            //        list_Up = list_obj.Where(t => t.GridThrustDirection == Vector3I.Up).ToList();
            //        list_Down = list_obj.Where(t => t.GridThrustDirection == Vector3I.Down).ToList();
            //        list_Right = list_obj.Where(t => t.GridThrustDirection == Vector3I.Right).ToList();
            //        list_Left = list_obj.Where(t => t.GridThrustDirection == Vector3I.Left).ToList();
            //        list_Forward = list_obj.Where(t => t.GridThrustDirection == Vector3I.Forward).ToList();
            //        list_Backward = list_obj.Where(t => t.GridThrustDirection == Vector3I.Backward).ToList();
            //    }
            //    else
            //    {
            //        list_Up = list_obj.Where(t => t.CustomName.Contains(location.up.ToString())).ToList();
            //        list_Down = list_obj.Where(t => t.CustomName.Contains(location.down.ToString())).ToList();
            //        list_Right = list_obj.Where(t => t.CustomName.Contains(location.right.ToString())).ToList();
            //        list_Left = list_obj.Where(t => t.CustomName.Contains(location.left.ToString())).ToList();
            //        list_Forward = list_obj.Where(t => t.CustomName.Contains(location.forward.ToString())).ToList();
            //        list_Backward = list_obj.Where(t => t.CustomName.Contains(location.backward.ToString())).ToList();
            //    }
            //    foreach (MyGroupBlock obj in list_group)
            //    {
            //        foreach (MySubtypeName sub_num in obj.list_subtype_name)
            //        {
            //            if (list_Up.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Up, location.up, obj.id, sub_num)); }
            //            if (list_Down.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Down, location.down, obj.id, sub_num)); }
            //            if (list_Right.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Right, location.right, obj.id, sub_num)); }
            //            if (list_Left.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Left, location.left, obj.id, sub_num)); }
            //            if (list_Forward.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Forward, location.forward, obj.id, sub_num)); }
            //            if (list_Backward.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Backward, location.backward, obj.id, sub_num)); }
            //        }
            //    }
            //}
            //public void GetValueThrusts1(bool is_control)
            //{
            //    List<IMyThrust> list_Up;
            //    List<IMyThrust> list_Down;
            //    List<IMyThrust> list_Right;
            //    List<IMyThrust> list_Left;
            //    List<IMyThrust> list_Forward;
            //    List<IMyThrust> list_Backward;

            //    list_value_thrusts.Clear();

            //    foreach (location loc in Enum.GetValues(typeof(location)))
            //    {

            //    }

            //    // Под управлением с контроллера 
            //    if (is_control)
            //    {
            //        list_Up = list_obj.Where(t => t.GridThrustDirection == Vector3I.Up).ToList();
            //        list_Down = list_obj.Where(t => t.GridThrustDirection == Vector3I.Down).ToList();
            //        list_Right = list_obj.Where(t => t.GridThrustDirection == Vector3I.Right).ToList();
            //        list_Left = list_obj.Where(t => t.GridThrustDirection == Vector3I.Left).ToList();
            //        list_Forward = list_obj.Where(t => t.GridThrustDirection == Vector3I.Forward).ToList();
            //        list_Backward = list_obj.Where(t => t.GridThrustDirection == Vector3I.Backward).ToList();
            //    }
            //    else
            //    {
            //        list_Up = list_obj.Where(t => t.CustomName.Contains(location.up.ToString())).ToList();
            //        list_Down = list_obj.Where(t => t.CustomName.Contains(location.down.ToString())).ToList();
            //        list_Right = list_obj.Where(t => t.CustomName.Contains(location.right.ToString())).ToList();
            //        list_Left = list_obj.Where(t => t.CustomName.Contains(location.left.ToString())).ToList();
            //        list_Forward = list_obj.Where(t => t.CustomName.Contains(location.forward.ToString())).ToList();
            //        list_Backward = list_obj.Where(t => t.CustomName.Contains(location.backward.ToString())).ToList();
            //    }
            //    foreach (MyGroupBlock obj in list_group)
            //    {
            //        foreach (MySubtypeName sub_num in obj.list_subtype_name)
            //        {
            //            if (list_Up.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Up, location.up, obj.id, sub_num)); }
            //            if (list_Down.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Down, location.down, obj.id, sub_num)); }
            //            if (list_Right.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Right, location.right, obj.id, sub_num)); }
            //            if (list_Left.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Left, location.left, obj.id, sub_num)); }
            //            if (list_Forward.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Forward, location.forward, obj.id, sub_num)); }
            //            if (list_Backward.Count() > 0) { list_value_thrusts.Add(GetOptionThrust(list_Backward, location.backward, obj.id, sub_num)); }
            //        }
            //    }
            //}
            public location getLocation(IMyThrust obj, bool is_control)
            {
                if (is_control)
                {
                    if (obj.GridThrustDirection == Vector3I.Up) return location.up;
                    if (obj.GridThrustDirection == Vector3I.Down) return location.down;
                    if (obj.GridThrustDirection == Vector3I.Right) return location.right;
                    if (obj.GridThrustDirection == Vector3I.Left) return location.left;
                    if (obj.GridThrustDirection == Vector3I.Forward) return location.forward;
                    if (obj.GridThrustDirection == Vector3I.Backward) return location.backward;
                }
                else
                {
                    if (obj.CustomName.Contains(location.up.ToString()) == true) return location.up;
                    if (obj.CustomName.Contains(location.down.ToString()) == true) return location.down;
                    if (obj.CustomName.Contains(location.right.ToString()) == true) return location.right;
                    if (obj.CustomName.Contains(location.left.ToString()) == true) return location.left;
                    if (obj.CustomName.Contains(location.forward.ToString()) == true) return location.forward;
                    if (obj.CustomName.Contains(location.backward.ToString()) == true) return location.backward;
                }
                return location.none;
            }
            public int getGroup(IMyThrust obj)
            {
                switch ((MySubtypeName)Enum.Parse(typeof(MySubtypeName), obj.BlockDefinition.SubtypeName.ToString()))
                {
                    case MySubtypeName.LargeBlockSmallAtmosphericThrust:
                    case MySubtypeName.LargeBlockSmallAtmosphericThrustSciFi:
                    case MySubtypeName.LargeBlockLargeAtmosphericThrust:
                    case MySubtypeName.LargeBlockLargeAtmosphericThrustSciFi:
                        { return 1; }
                    case MySubtypeName.LargeBlockSmallHydrogenThrust:
                    case MySubtypeName.LargeBlockSmallHydrogenThrustSciFi:
                    case MySubtypeName.LargeBlockLargeHydrogenThrust:
                    case MySubtypeName.LargeBlockLargeHydrogenThrustSciFi:
                        { return 2; }
                    case MySubtypeName.LargeBlockSmallThrust:
                    case MySubtypeName.LargeBlockSmallThrustSciFi:
                    case MySubtypeName.LargeBlockLargeThrust:
                    case MySubtypeName.LargeBlockLargeThrustSciFi:
                        { return 3; }
                }
                return 0;
            }
            public void GetValueThrusts(bool is_control)
            {
                list_value_thrusts.Clear();

                //_scr.test_lcd1.WriteText("Старт" + "\n", false);

                foreach (IMyThrust obj in list_obj)
                {
                    valus_thrust val_thrust = list_value_thrusts.Where(o => o.location == getLocation(obj, is_control) && o.subtype_name.ToString() == obj.BlockDefinition.SubtypeName).FirstOrDefault();
                    //_scr.test_lcd1.WriteText("location=" + getLocation(obj, is_control) + "\n", true);
                    if (val_thrust == null)
                    {
                        //_scr.test_lcd1.WriteText("val_thrust=" + (MySubtypeName)Enum.Parse(typeof(MySubtypeName), obj.BlockDefinition.SubtypeName.ToString()) + "\n", true);
                        val_thrust = new valus_thrust()
                        {
                            location = getLocation(obj, is_control),
                            id_group = getGroup(obj),
                            definition_display_name_text = obj.DefinitionDisplayNameText,
                            subtype_name = (MySubtypeName)Enum.Parse(typeof(MySubtypeName), obj.BlockDefinition.SubtypeName.ToString()),
                            count = 1,
                            count_on = obj.Enabled ? 1 : 0,
                            sum_to = obj.ThrustOverride,
                            sum_to_percent = obj.ThrustOverridePercentage,
                            sum_max_thrust = obj.MaxThrust,
                            sum_max_eff_thrust = obj.MaxEffectiveThrust,
                            sum_cur_thrust = obj.CurrentThrust,
                        };
                        list_value_thrusts.Add(val_thrust);
                    }
                    else
                    {
                        val_thrust.count++;
                        if (obj.Enabled) val_thrust.count_on++;
                        val_thrust.sum_to += obj.ThrustOverride;
                        val_thrust.sum_to_percent += obj.ThrustOverridePercentage;
                        val_thrust.sum_max_thrust += obj.MaxThrust;
                        val_thrust.sum_max_eff_thrust += obj.MaxEffectiveThrust;
                        val_thrust.sum_cur_thrust += obj.CurrentThrust;
                    }
                }
            }
            public valus_thrust GetOptionThrust(List<IMyThrust> list, location location, int id_group, MySubtypeName subtype_name)
            {
                valus_thrust result = new valus_thrust()
                {
                    location = location,
                    id_group = id_group,
                    definition_display_name_text = null,
                    subtype_name = subtype_name,
                    count = 0,
                    count_on = 0,
                    sum_to = 0,
                    sum_to_percent = 0,
                    sum_max_thrust = 0,
                    sum_max_eff_thrust = 0,
                    sum_cur_thrust = 0,
                };
                foreach (IMyThrust obj in list.ToList().Where(d => d.BlockDefinition.SubtypeName == subtype_name.ToString()).ToList())
                {
                    result.count++;
                    if (obj.Enabled) result.count_on++;
                    result.definition_display_name_text = obj.DefinitionDisplayNameText;
                    result.sum_to += obj.ThrustOverride;
                    result.sum_to_percent += obj.ThrustOverridePercentage;
                    result.sum_max_thrust += obj.MaxThrust;
                    result.sum_max_eff_thrust += obj.MaxEffectiveThrust;
                    result.sum_cur_thrust += obj.CurrentThrust;
                }
                return result;
            }
            public int count_all_thrust { get { return list_value_thrusts.Select(c => c.count).Sum(); } }
            public int count_on_all_thrust { get { return list_value_thrusts.Select(c => c.count_on).Sum(); } }
            public int count_thrast(location loc)
            {
                return list_value_thrusts.Where(c => c.location == loc).Select(c => c.count).Sum();
            }
            public int count_on_thrast(location loc)
            {
                return list_value_thrusts.Where(c => c.location == loc).Select(c => c.count_on).Sum();
            }
            public float sum_to_thrast(location loc)
            {
                return sum_to_thrast(loc, group_thrust.atmospheric) + sum_to_thrast(loc, group_thrust.hydrogen) + sum_to_thrast(loc, group_thrust.ionic);
            }
            public float sum_cur_thrast(location loc)
            {
                return sum_cur_thrast(loc, group_thrust.atmospheric) + sum_cur_thrast(loc, group_thrust.hydrogen) + sum_cur_thrast(loc, group_thrust.ionic);
            }
            public float sum_max_thrast(location loc)
            {
                return sum_max_thrast(loc, group_thrust.atmospheric) + sum_max_thrast(loc, group_thrust.hydrogen) + sum_max_thrast(loc, group_thrust.ionic);
            }
            public int count_thrast(location loc, group_thrust group)
            {
                return list_value_thrusts.Where(c => c.location == loc && c.id_group == (int)group).Select(c => c.count).Sum();
            }
            public int count_on_thrast(location loc, group_thrust group)
            {
                return list_value_thrusts.Where(c => c.location == loc && c.id_group == (int)group).Select(c => c.count_on).Sum();
            }
            public float sum_to_thrast(location loc, group_thrust group)
            {
                return list_value_thrusts.Where(c => c.location == loc && c.id_group == (int)group).Select(c => c.sum_to).Sum();
            }
            public float sum_cur_thrast(location loc, group_thrust group)
            {
                return list_value_thrusts.Where(c => c.location == loc && c.id_group == (int)group).Select(c => c.sum_cur_thrust).Sum();
            }
            public float sum_max_thrast(location loc, group_thrust group)
            {
                return list_value_thrusts.Where(c => c.location == loc && c.id_group == (int)group).Select(c => c.sum_max_thrust).Sum();
            }
            public string getText(location loc)
            {
                switch (loc)
                {
                    case location.backward: return "ВПЕРЕД";
                    case location.forward: return "НАЗАД";
                    case location.up: return "ВНИЗ";
                    case location.down: return "ВВЕРХ";
                    case location.left: return "ВПРАВО";
                    case location.right: return "ВЛЕВО";
                    default: return "";
                }
            }
            public string getText(group_thrust group)
            {
                switch (group)
                {
                    case group_thrust.atmospheric: return "АУ";
                    case group_thrust.hydrogen: return "ВУ";
                    case group_thrust.ionic: return "ИУ";

                    default: return "";
                }
            }
            public string GetStatusOfText(bool detali)
            {
                string result = "";
                result += "Ускорителей:" + list_obj.Count() + "\n";
                result += "УСКОРИТЕЛЕЙ:" + count_on_all_thrust + "|" + count_all_thrust + "\n";

                foreach (location loc in new List<location>() { location.backward, location.forward, location.right, location.left, location.down, location.up })
                {
                    result += "|-" + getText(loc) + ": " + PText.GetCountThrust(count_on_thrast(loc), count_thrast(loc));
                    result += " " + PText.GetCountDetaliThrust(count_thrast(loc, group_thrust.atmospheric), count_thrast(loc, group_thrust.hydrogen), count_thrast(loc, group_thrust.ionic),
                        count_on_thrast(loc, group_thrust.atmospheric), count_on_thrast(loc, group_thrust.hydrogen), count_on_thrast(loc, group_thrust.ionic)) + "\n";
                    result += "  |-Пер:" + PText.GetThrust(sum_to_thrast(loc));
                    result += " " + PText.GetCurrentThrust((sum_cur_thrast(loc)), sum_max_thrast(loc)) + "\n";
                    result += "  |-" + PText.GetScalePersent((sum_cur_thrast(loc) / sum_max_thrast(loc)), 40) + "\n";
                    if (detali)
                    {
                        foreach (group_thrust group in Enum.GetValues(typeof(group_thrust)))
                        {
                            if (count_thrast(loc, group) > 0)
                            {
                                result += "    |-" + getText(group) + ": " + PText.GetCountThrust(count_on_thrast(loc, group), count_thrast(loc, group));
                                result += " Пер:" + PText.GetThrust(sum_to_thrast(loc, group));
                                result += " " + PText.GetCurrentThrust((sum_cur_thrast(loc, group)), sum_max_thrast(loc, group)) + "\n";
                                result += "    | " + PText.GetScalePersent((sum_cur_thrast(loc, group) / sum_max_thrast(loc, group)), 40) + "\n";
                            }
                        }
                    }
                }
                return result;
            }
        }
    }
}

//|- Лед: 28888т.
//|- Ген.H2: [+][+][+][+][+][+]
//  |- БАК H2: [З][.][-][+][A]
//    |- Генератор: [+][-]
//    |- Вод уск: [+][-]

//|-ВПЕРЕД: 5|30 [{A-0|10} {В-5|10} {И-0|10}]
//  |-Пер:10МН [10МН/50МН]
//  |-[....................................] - 100%
//    |-ВД: 10|10 Пер:10МН [10МН/50МН]
//    | [....................................] - 100%
//    |-ИД: 10|20 Пер:10МН [10МН/50МН]
//      [....................................] - 100%




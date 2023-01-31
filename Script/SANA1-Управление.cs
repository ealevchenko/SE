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

        IMyTextPanel test_lcd;
        IMyInteriorLight ltest;

        int count_operator_room = 0; // Кол лудей в помещениии
        int count_engine_room = 0;  // Кол лудей в помещениии

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
                    prog += ".";
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
                //clock++;
                test_lcd.WriteText("IsInputDoor=" + door_gataway_left.IsInputDoor + "\n", false);
                test_lcd.WriteText("IsOutputDoor=" + door_gataway_left.IsOutputDoor + "\n", true);
                test_lcd.WriteText("count_operator_room=" + count_operator_room + "\n", true);
                test_lcd.WriteText("count_engine_room=" + count_engine_room + "\n", true);
                test_lcd.WriteText(gas_generators.GetStatusOfText(), true);
                test_lcd.WriteText(gas_tank.GetStatusOfText(), true);
                test_lcd.WriteText(thrust.GetStatusOfText(cockpit.IsUnderControl), true);

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
            public enum ThrustSubtypeName : int
            {
                none = 0,
                LargeBlockSmallAtmosphericThrust = 10,
                LargeBlockSmallAtmosphericThrustSciFi = 11,
                LargeBlockSmallHydrogenThrust = 20,
                LargeBlockSmallHydrogenThrustSciFi = 21,
                LargeBlockSmallThrust = 30,
                LargeBlockSmallThrustSciFi = 31,
                LargeBlockLargeAtmosphericThrust = 110,
                LargeBlockLargeAtmosphericThrustSciFi = 111,
                LargeBlockLargeHydrogenThrust = 120,
                LargeBlockLargeHydrogenThrustSciFi = 121,
                LargeBlockLargeThrust = 130,
                LargeBlockLargeThrustSciFi = 131,
            }
            public enum location : int
            {
                none =0,
                up = 1,
                down = 2,
                right = 3,
                left = 4,
                forward = 5,
                backward = 6,
            }
            public class value_thrust
            {
                public location location = location.none;
                public ThrustSubtypeName thrust_subtype_name = ThrustSubtypeName.none;
                public string definition_display_name_text = null;
                public int count = 0;                  // кол
                public float sum_to = 0;               // перехват тяги тяга МН
                public float sum_to_percent = 0;       // процент от макс перехват тяги тяга %
                public float sum_max_thrust = 0;       // Макс тяга МН
                public float sum_max_eff_thrust = 0;   // Макс эфектив тяга МН
                public float sum_cur_thrust = 0;       // Текущая тяга МН
            }
            public Thrust(string name_obj) : base(name_obj)
            {

            }
            public value_thrust GetStatusThrustOfText(List<IMyThrust> list, location location, ThrustSubtypeName subtype_name)
            {
                value_thrust result = new value_thrust()
                {
                    location = location,
                    definition_display_name_text = null,
                    thrust_subtype_name = subtype_name,
                    count = 0,
                    sum_to = 0,
                    sum_to_percent = 0,
                    sum_max_thrust = 0,
                    sum_max_eff_thrust = 0,
                    sum_cur_thrust = 0,
                };
                foreach (IMyThrust obj in list.ToList().Where(d => d.BlockDefinition.SubtypeName == subtype_name.ToString()).ToList())
                {
                    result.count++;
                    result.definition_display_name_text = obj.DefinitionDisplayNameText;
                    result.sum_to += obj.ThrustOverride;
                    result.sum_to_percent += obj.ThrustOverridePercentage;
                    result.sum_max_thrust += obj.MaxThrust;
                    result.sum_max_eff_thrust += obj.MaxEffectiveThrust;
                    result.sum_cur_thrust += obj.CurrentThrust;
                }
                return result;
            }
            public string GetStatusOfText(bool is_control)
            {
                string result = "";
                // стоит сзади (уск ввперед)
                result += "Ускорителей:" + list_obj.Count() + "\n";
                //foreach (IMyThrust obj in list_obj)
                //{
                //    VRage.ObjectBuilders.SerializableDefinitionId id = obj.BlockDefinition;
                //    result += "id.SubtypeName " + id.SubtypeName + "\n";
                //}

                List<IMyThrust> list_Forward = new List<IMyThrust>();
                List<IMyThrust> list_Backward = new List<IMyThrust>();
                // Под управлением с контроллера 
                if (is_control)
                {
                    list_Forward = list_obj.Where(t => t.GridThrustDirection == Vector3I.Forward).ToList();
                    list_Backward = list_obj.Where(t => t.GridThrustDirection == Vector3I.Backward).ToList();
                }
                else
                {
                    list_Forward = list_obj.Where(t => t.CustomName.Contains(location.forward.ToString())).ToList();
                    list_Backward = list_obj.Where(t => t.CustomName.Contains(location.backward.ToString())).ToList();
                }
                result += "Ускорителей_Forward :" + list_Forward.Count() + "\n";
                result += "Ускорителей_Backward :" + list_Backward.Count() + "\n";

                //float sum_to = 0;               // перехват тяги тяга МН
                //float sum_to_percent = 0;       // процент от макс перехват тяги тяга %
                //float sum_max_thrust = 0;       // Макс тяга МН
                //float sum_max_eff_thrust = 0;   // Макс эфектив тяга МН
                //float sum_cur_thrust = 0;       // Текущая тяга МН
                //int count = 0;

                //result += "CustomData " + list_Backward[0].CustomData + "\n";
                //result += "CustomInfo " + list_Backward[0].CustomInfo+ "\n";
                //result += "CustomName " + list_Backward[0].CustomName+ "\n";
                //result += "CustomNameWithFaction " + list_Backward[0].CustomNameWithFaction+ "\n";
                //result += "DetailedInfo " + list_Backward[0].DetailedInfo + "\n";
                //result += "DisplayName " + list_Backward[0].DisplayName+ "\n";
                //result += "DisplayNameText " + list_Backward[0].DisplayNameText+ "\n";
                //result += "EntityId " + list_Backward[0].EntityId+ "\n";
                //result += "Name " + list_Backward[0].Name+ "\n";
                //result += "ToString " + list_Backward[0].ToString()+ "\n";

                //VRage.ObjectBuilders.SerializableDefinitionId id = list_Backward[0].BlockDefinition;
                //result += "id.ToString " + id.ToString() + "\n";
                //result += "id.SubtypeName " + id.SubtypeName + "\n";
                //result += "id.SubtypeId " + id.SubtypeId + "\n";
                //result += "id.SubtypeIdAttribute " + id.SubtypeIdAttribute + "\n";
                //result += "id.TypeIdString " + id.TypeIdString + "\n";
                //result += "id.TypeIdStringAttribute " + id.TypeIdStringAttribute + "\n";

                //IMyCameraBlock

                //foreach (IMyThrust obj in list_Backward)
                //{
                //    count++;
                //    sum_to += obj.ThrustOverride;
                //    sum_to_percent += obj.ThrustOverridePercentage;
                //    sum_max_thrust += obj.MaxThrust;
                //    sum_max_eff_thrust += obj.MaxEffectiveThrust;
                //    sum_cur_thrust += obj.CurrentThrust;
                //}
                //result += "БВУ:" + count + "\n";
                //result += "TO :" + PText.GetThrust(sum_to) + "\n";
                //result += "TOP :" + PText.GetPersent(sum_to_percent / count) + "\n";
                //result += "MT :" + PText.GetThrust(sum_max_thrust) + "\n";

                //result += "MET :" + PText.GetThrust(sum_max_eff_thrust) + "\n";
                //result += "CT :" + PText.GetThrust(sum_cur_thrust) + "\n";
                result += "ВПЕРЕД: [" + list_Backward.Count() + "]" + "\n";

                value_thrust result_backward_lha = GetStatusThrustOfText(list_Backward, location.backward, ThrustSubtypeName.LargeBlockLargeHydrogenThrust);
                result += "БВУ:" + result_backward_lha.count + " " + PText.GetCurrentThrust(result_backward_lha.sum_to, result_backward_lha.sum_cur_thrust, result_backward_lha.sum_max_thrust) + "\n";
                result += "" + PText.GetScalePersent((result_backward_lha.sum_cur_thrust / result_backward_lha.sum_max_thrust), 50) + "\n";

                //value_thrust result_backward_lia = GetStatusThrustOfText(list_Backward.ToList().Where(d => d.DefinitionDisplayNameText == "Большой ионный НФ-ускоритель").ToList(), "backward", "Большой ионный НФ-ускоритель");
                //result += "БИУ:" + result_backward_lia.count + " " + PText.GetCurrentThrust(result_backward_lia.sum_to, result_backward_lia.sum_cur_thrust, result_backward_lia.sum_max_thrust) + "\n";
                //result += "" + PText.GetScalePersent((result_backward_lia.sum_cur_thrust / result_backward_lia.sum_max_thrust), 50) + "\n";

                //foreach (IMyThrust obj in list_obj)
                //{
                //    switch (obj.DefinitionDisplayNameText)
                //    {
                //        case "Большой водородный ускоритель":
                //            {
                //                result += obj.DefinitionDisplayNameText + "\n";
                //                float TO = obj.ThrustOverride;
                //                float TOP = obj.ThrustOverridePercentage;
                //                float MT = obj.MaxThrust;
                //                float MET = obj.MaxEffectiveThrust;
                //                float CT = obj.CurrentThrust;
                //                result += "TO :" + PText.GetThrust(TO) + "\n";
                //                result += "TOP :" + TOP + PText.GetPersent(TOP) + "\n";
                //                result += "MT :" + PText.GetThrust(MT) + "\n";
                //                result += "MET :" + PText.GetThrust(MET) + "\n";
                //                result += "CT :" + PText.GetThrust(CT) + "\n";
                //                //result += "GTD :" + obj.GridThrustDirection =  + "\n";

                //                if (obj.GridThrustDirection == Vector3I.Forward) { }
                //                //fr_h2 += obj.FilledRatio;
                //                //cap_h2 += obj.Capacity;
                //                //count_th2++;
                //                //tdh2 += "|  |-БАК:[" + (obj.Enabled ? "{+}" : "{-}") + (obj.Stockpile ? "{>}" : "{<}") + (obj.AutoRefillBottles ? "{A}" : "{ }") + "] - " + (obj.FilledRatio * 100) + "% " + PText.GetCapacityTanks(obj.FilledRatio, obj.Capacity) + "\n";
                //                return result;
                //                //break;
                //            }
                //            //case "Кислородный бак":
                //            //    {
                //            //        fr_o2 += obj.FilledRatio;
                //            //        cap_o2 += obj.Capacity;
                //            //        count_to2++;
                //            //        tdo2 += "|  |-БАК:[" + (obj.Enabled ? "{+}" : "{-}") + (obj.Stockpile ? "{>}" : "{<}") + (obj.AutoRefillBottles ? "{A}" : "{ }") + "] - " + (obj.FilledRatio * 100) + "% " + PText.GetCapacityTanks(obj.FilledRatio, obj.Capacity) + "\n";
                //            //        break;
                //            //    }
                //    }

                //}
                //string result = "";
                //result += "|    H2:" + PText.GetCapacityTanks((fr_h2 / count_th2), cap_h2) + "\n";
                //result += "|-+" + PText.GetPersent((fr_h2 / count_th2), 50) + "\n";
                //result += tdh2;
                //result += "|\n";
                //result += "|    O2:" + PText.GetCapacityTanks((fr_o2 / count_to2), cap_o2) + "\n";
                //result += "|-+" + PText.GetPersent((fr_o2 / count_to2), 50) + "\n";
                //result += tdo2;
                //result += "|\n";
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

// ВПЕРЕД:6[{A-0|10},{В-5|10},{И-0|10}] Пер:10МН [10МН/50МН]
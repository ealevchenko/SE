using Sandbox.Game.Entities;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sandbox.Definitions.MyOxygenGeneratorDefinition;

// БАЗА-МЗ1
namespace БАЗА_МЗ1
{
    public sealed class Program : MyGridProgram
    {
        IMyTextPanel test_lcd;//, test_lcd1;


        string NameObj = "БАЗА-МЗ1";
        // Дверь левая выход в космос
        string NameDoorExt = "БАЗА-МЗ1-Раздвижная дверь external";
        string NameDoorInt = "БАЗА-МЗ1-Раздвижная дверь internal";
        sensor_option sensor_option_ext = new sensor_option()
        {
            name = "БАЗА-МЗ1-Сенсор вх.дверь external",
            lf = 1.0f,
            rg = 1.0f,
            bt = 3.0f,
            tp = 1.0f,
            bc = 0.0f,
            fr = 2.5f
        };
        sensor_option sensor_option_int = new sensor_option()
        {
            name = "БАЗА-МЗ1-Сенсор вх.дверь internal",
            lf = 1.0f,
            rg = 1.0f,
            bt = 3.0f,
            tp = 1.0f,
            bc = 0.0f,
            fr = 2.5f
        };
        DoorGateway door_gataway;
        InteriorLight light_room;       // Освещение
        GasTank gas_tank;               // Баки
        GasGenerator gas_generators;    // Генераторы газов


        int count_operator_room = 0; // Кол людей в помещениии

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
            // тест LCD
            test_lcd = GridTerminalSystem.GetBlockWithName("test_lcd") as IMyTextPanel;
            Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));

            door_gataway = new DoorGateway(sensor_option_ext, sensor_option_int, NameDoorExt, NameDoorInt);
            light_room = new InteriorLight(NameObj);    // Освещение
            light_room.Off();
            gas_tank = new GasTank(NameObj);            // БАКИ
            gas_tank.On();
            gas_generators = new GasGenerator(NameObj); // ГЕНЕРАТОРЫ



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
            // Шлюзовая дверь
            door_gataway.Logic(argument, updateSource, ref count_operator_room, ref x);
            switch (argument)
            {
                //case "connected_on":
                //    break;
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                test_lcd.WriteText("IsInputDoor=" + door_gataway.IsInputDoor + "\n", false);
                test_lcd.WriteText("IsOutputDoor=" + door_gataway.IsOutputDoor + "\n", true);
                test_lcd.WriteText("count_operator_room=" + count_operator_room + "\n", true);
                test_lcd.WriteText(gas_generators.GetStatusOfText(), true);
                test_lcd.WriteText(gas_tank.GetStatusOfText(), true);
                // контроль освещения
                if (count_operator_room > 0)
                {
                    light_room.OnOfGroup("operator_room");
                }
                else
                {
                    light_room.OffOfGroup("operator_room");
                    count_operator_room = 0;
                }
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
        // Класс лампы
        public class InteriorLight : BaseListTerminalBlock<IMyInteriorLight>
        {
            public InteriorLight(string name_obj) : base(name_obj)
            {

            }
        }
        // Класс Баки
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
        // Класс генераторы
        public class GasGenerator : BaseListTerminalBlock<IMyGasGenerator>
        {
            public GasGenerator(string name_obj) : base(name_obj)
            {

            }
            public string GetStatusOfText()
            {
                string result = "ГЕН.H2/O2: ";
                foreach (IMyGasGenerator obj in list_obj)
                {
                    result += "MaxCapacity " + obj.GetProperty("MaxCapacity").TypeName + "\n";
                    result += obj.BlockDefinition.ToString()+ "\n";
                    List<MyGasGeneratorResourceInfo> list = obj.BlockDefinition.ProducedGases();

                    result += "[" + (obj.Enabled ? "+" : "-") + "]";
                }
                result += "\n";
                return result;
            }
        }
    }
}

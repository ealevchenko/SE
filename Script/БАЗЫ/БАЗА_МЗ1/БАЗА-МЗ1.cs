using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using static Sandbox.Definitions.MyOxygenGeneratorDefinition;

// БАЗА-МЗ1
namespace БАЗА_МЗ1
{
    public sealed class Program : MyGridProgram
    {
        IMyTextPanel test_lcd, test_lcd1;

        string test_name = "БАЗА-МЗ1-Промышленный водородный бак";
        IMyTerminalBlock test;

        string NameObj = "БАЗА-МЗ1";

        door_gateway_option dg_option = new door_gateway_option()
        {
            ext_door_name = "БАЗА-МЗ1-Раздвижная дверь external",
            ext_sn_name = "БАЗА-МЗ1-Сенсор вх.дверь external",
            ext_sn = new float[] { 1.0f, 1.0f, 3.0f, 1.0f, 0.0f, 2.5f }, // lf, rg, bt, tp, bc, fr
            int_door_name = "БАЗА-МЗ1-Раздвижная дверь internal",
            int_sn_name = "БАЗА-МЗ1-Сенсор вх.дверь internal",
            int_sn = new float[] { 1.0f, 1.0f, 3.0f, 1.0f, 0.0f, 2.5f }, // lf, rg, bt, tp, bc, fr
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
            static public string GetCurrentOfMax(float cur, float max, string units)
            {
                return "[ " + cur + units + " / " + max + units + " ]";
            }
            static public string GetSign(int x, int y)
            {
                if (x == y) { return "="; }
                if (x > y) { return ">"; }
                if (x < y) { return "<"; }
                return "";
            }
            static public string GetCountObj(int count, int count_on)
            {
                return count_on + GetSign(count_on, count) + count;
            }
            //------------------------------------------------------------------------
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

                LargeHydrogenEngine = 40,
            }
            public List<T> list_obj = new List<T>();
            public int Count { get { return list_obj.Count(); } }
            public BaseListTerminalBlock(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo(typeof(T).Name + "[" + name_obj + "]" + ((list_obj != null && list_obj.Count() > 0) ? ("Ок") : ("not found"))); ;
            }
            public class values_obj
            {
                public int id_group = 0;
                public string TyepID = null;
                public string SubtyepID = null;
                public string definition_display_name_text = null;
                public int count = 0;                       // кол
                public int count_on = 0;                    // кол вкл
                //public int count_auto_refill_bottles = 0;   // кол авто заполнения балонов
                //public int count_stockpile = 0;             // кол авто заполнения балонов

                //public float max = 0;                       // Вместимость баков
                //public float cur = 0;                       // Вместимость баков
                //public float cur_persent = 0;               // % заполнения баков

                public float curr_mass = 0;
                public float curr_vol = 0;
                public float curr_max_vol = 0;

                public float inp_curr_power = 0;
                public float inp_max_power = 0;
                public int count_inp_power = 0;

                public float inp_curr_hydrogen = 0;
                public float inp_max_hydrogen = 0;
                public int count_inp_hydrogen = 0;

                public float inp_curr_oxygen = 0;
                public float inp_max_oxygen = 0;
                public int count_inp_oxygen = 0;

                public float out_curr_power = 0;
                public float out_max_power = 0;

                public float out_curr_hydrogen = 0;
                public float out_max_hydrogen = 0;

                public float out_curr_oxygen = 0;
                public float out_max_oxygen = 0;
            }
            //
            public List<values_obj> list_values = new List<values_obj>();

            public void GetValues()
            {
                list_values.Clear();
                //_scr.test_lcd1.WriteText("Старт" + "\n", false);
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    float curr_mass = 0;
                    float curr_vol = 0;
                    float curr_max_vol = 0;

                    float inp_curr_power = 0;
                    float inp_max_power = 0;
                    bool is_inp_power = false;

                    float inp_curr_hydrogen = 0;
                    float inp_max_hydrogen = 0;
                    bool is_inp_hydrogen = false;

                    float inp_curr_oxygen = 0;
                    float inp_max_oxygen = 0;
                    bool is_inp_oxygen = false;

                    float out_curr_power = 0;
                    float out_max_power = 0;

                    float out_curr_hydrogen = 0;
                    float out_max_hydrogen = 0;

                    float out_curr_oxygen = 0;
                    float out_max_oxygen = 0;

                    // Инвентарь
                    if (((IMyTerminalBlock)obj).HasInventory)
                    {
                        for (int i = 0; i < ((IMyTerminalBlock)obj).InventoryCount; i++)
                        {
                            IMyInventory inv = ((IMyTerminalBlock)obj).GetInventory(i);
                            if (inv != null)
                            {
                                curr_mass += ((float)inv.CurrentMass);
                                curr_vol += ((float)inv.CurrentVolume);
                                curr_max_vol += ((float)inv.MaxVolume);
                            }
                        }
                    }
                    //
                    MyResourceSinkComponent sink;
                    ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSinkComponent>(out sink);
                    if (sink != null)
                    {
                        var list = sink.AcceptedResources;
                        for (int j = 0; j < list.Count; ++j)
                        {
                            if (list[j].SubtypeId.ToString() == "Electricity")
                            {
                                inp_curr_power += sink.CurrentInputByType(list[j]);
                                inp_max_power += sink.MaxRequiredInputByType(list[j]);
                                is_inp_power = sink.IsPoweredByType(list[j]);
                            }
                            if (list[j].SubtypeId.ToString() == "Hydrogen")
                            {
                                inp_curr_hydrogen += sink.CurrentInputByType(list[j]);
                                inp_max_hydrogen += sink.MaxRequiredInputByType(list[j]);
                                is_inp_hydrogen = sink.IsPoweredByType(list[j]);
                            }
                            if (list[j].SubtypeId.ToString() == "Oxygen")
                            {
                                inp_curr_oxygen += sink.CurrentInputByType(list[j]);
                                inp_max_oxygen += sink.MaxRequiredInputByType(list[j]);
                                is_inp_oxygen = sink.IsPoweredByType(list[j]);
                            }
                        }
                    }
                    MyResourceSourceComponent source;
                    ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSourceComponent>(out source);
                    if (source != null)
                    {
                        var list = source.ResourceTypes;
                        for (int j = 0; j < list.Count; ++j)
                        {
                            if (list[j].SubtypeId.ToString() == "Electricity")
                            {
                                out_curr_power = source.CurrentOutputByType(list[j]);
                                out_max_power = source.DefinedOutputByType(list[j]);
                            }
                            if (list[j].SubtypeId.ToString() == "Oxygen")
                            {
                                out_curr_oxygen = source.CurrentOutputByType(list[j]);
                                out_max_oxygen = source.DefinedOutputByType(list[j]);
                            }
                            if (list[j].SubtypeId.ToString() == "Hydrogen")
                            {
                                out_curr_hydrogen = source.CurrentOutputByType(list[j]);
                                out_max_hydrogen = source.DefinedOutputByType(list[j]);
                            }
                        }
                    }

                    values_obj val_obj = list_values.Where(o => ((values_obj)o).TyepID == obj.BlockDefinition.TypeId.ToString() && ((values_obj)o).SubtyepID == obj.BlockDefinition.SubtypeId).FirstOrDefault();
                    if (val_obj == null)
                    {
                        val_obj = new values_obj()
                        {
                            id_group = 0,
                            definition_display_name_text = obj.DefinitionDisplayNameText,
                            TyepID = obj.BlockDefinition.TypeId.ToString(),
                            SubtyepID = obj.BlockDefinition.SubtypeId,
                            count = 1,
                            count_on = ((IMyFunctionalBlock)obj).Enabled ? 1 : 0,
                            curr_mass = curr_mass,
                            curr_vol = curr_vol,
                            curr_max_vol = curr_max_vol,
                            inp_curr_power = inp_curr_power,
                            inp_max_power = inp_max_power,
                            count_inp_power = is_inp_power ? 1 : 0,
                            inp_curr_hydrogen = inp_curr_hydrogen,
                            inp_max_hydrogen = inp_max_hydrogen,
                            count_inp_hydrogen = is_inp_hydrogen ? 1 : 0,
                            inp_curr_oxygen = inp_curr_oxygen,
                            inp_max_oxygen = inp_max_oxygen,
                            count_inp_oxygen = is_inp_oxygen ? 1 : 0,
                            out_curr_power = out_curr_power,
                            out_max_power = out_max_power,
                            out_curr_hydrogen = out_curr_hydrogen,
                            out_max_hydrogen = out_max_hydrogen,
                            out_curr_oxygen = out_curr_oxygen,
                            out_max_oxygen = out_max_oxygen,

                        };
                        list_values.Add(val_obj);
                    }
                    else
                    {
                        val_obj.count++;
                        if (((IMyFunctionalBlock)obj).Enabled) val_obj.count_on++;
                        val_obj.curr_mass = curr_mass;
                        val_obj.curr_vol = curr_vol;
                        val_obj.curr_max_vol = curr_max_vol;
                        val_obj.inp_curr_power = inp_curr_power;
                        val_obj.inp_max_power = inp_max_power;
                        if (is_inp_power) val_obj.count_inp_power++;
                        val_obj.inp_curr_hydrogen = inp_curr_hydrogen;
                        val_obj.inp_max_hydrogen = inp_max_hydrogen;
                        if (is_inp_hydrogen) val_obj.count_inp_hydrogen++;
                        val_obj.inp_curr_oxygen = inp_curr_oxygen;
                        val_obj.inp_max_oxygen = inp_max_oxygen;
                        if (is_inp_oxygen) val_obj.count_inp_oxygen++;
                        val_obj.out_curr_power = out_curr_power;
                        val_obj.out_max_power = out_max_power;
                        val_obj.out_curr_hydrogen = out_curr_hydrogen;
                        val_obj.out_max_hydrogen = out_max_hydrogen;
                        val_obj.out_curr_oxygen = out_curr_oxygen;
                        val_obj.out_max_oxygen = out_max_oxygen;
                    }
                }
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
        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;
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

            test = GridTerminalSystem.GetBlockWithName(test_name) as IMyTerminalBlock;
            // тест LCD
            test_lcd = GridTerminalSystem.GetBlockWithName("БАЗА-МЗ1-test_lcd") as IMyTextPanel;
            Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));
            test_lcd1 = GridTerminalSystem.GetBlockWithName("БАЗА-МЗ1-test_lcd1") as IMyTextPanel;
            Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));
            door_gataway = new DoorGateway(dg_option);
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
                StringBuilder values = new StringBuilder();
                test_lcd1.WriteText(values, false);
                // Получим данные
                gas_generators.GetValues();
                test_lcd.WriteText("", false);
                //test_lcd.WriteText("IsInputDoor=" + door_gataway.IsInputDoor + "\n", false);
                //test_lcd.WriteText("IsOutputDoor=" + door_gataway.IsOutputDoor + "\n", true);
                //test_lcd.WriteText("count_operator_room=" + count_operator_room + "\n", true);
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
            public Sensor(string name, float[] sn_option)
            {
                sensor = _scr.GridTerminalSystem.GetBlockWithName(name) as IMySensorBlock;
                _scr.Echo("sensor[" + name + "]: " + ((sensor != null) ? ("Ок") : ("not found")));
                if (sn_option != null && sn_option.Count() >= 6)
                {
                    SetExtend(sn_option[0], sn_option[1], sn_option[2], sn_option[3], sn_option[4], sn_option[5]);
                }
                else
                {
                    SetExtend(0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f);
                    _scr.Echo("sensor[" + name + "].sn_option: null");
                }
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
        public class door_gateway_option
        {

            public string ext_door_name { get; set; }
            public string ext_sn_name { get; set; }
            public float[] ext_sn { get; set; }
            public string int_door_name { get; set; }
            public string int_sn_name { get; set; }
            public float[] int_sn { get; set; }
        };
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
            //public DoorGateway(sensor_option sensor_option_ext, sensor_option sensor_option_int, string NameDoorExt, string NameDoorInt)
            //{
            //    // Создадим объекты 
            //    sn_door_external = new Sensor(sensor_option_ext);
            //    sn_door_internal = new Sensor(sensor_option_int);
            //    door_external = new Door(NameDoorExt);
            //    door_internal = new Door(NameDoorInt);

            //}
            public DoorGateway(door_gateway_option option)
            {
                sn_door_external = new Sensor(option.ext_sn_name, option.ext_sn);
                sn_door_internal = new Sensor(option.int_sn_name, option.int_sn);
                door_external = new Door(option.ext_door_name);
                door_internal = new Door(option.int_door_name);
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
            public class valus_gas_tank : values_obj
            {
                // кол вкл
                public int count_auto_refill_bottles = 0;   // кол авто заполнения балонов
                public int count_stockpile = 0;   // кол авто заполнения балонов
                public float capacity = 0;              // Вместимость баков
                public float filled_ratio = 0;              // % заполнения баков
            }

            List<valus_gas_tank> list_values_tanks = new List<valus_gas_tank>();
            //
            public List<valus_gas_tank> list_gas_tanks = new List<valus_gas_tank>();
            public GasTank(string name_obj) : base(name_obj)
            {

            }

            public void GetValues()
            {
                list_values_tanks.Clear();
                base.GetValues();
                foreach (values_obj v_obj in list_values) { 
                
                }
            }
            public string GetStatusOfText()
            {
                StringBuilder result = new StringBuilder();


                //result.Append("ГЕНЕРАТОРЫ O2/H2: " + PText.GetCountObj(count_all, count_on_all) + " А[" + count_auto_refill + "]" + " К[" + count_count_use_conveyor_system + "]" + "\n");
                //result.Append(" |- ПОТРЕБЛЕНИЕ: " + "[" + count_powered + "]" + PText.GetCurrentOfMax(curr_power, max_power, "кW") + "\n");
                //result.Append(" |  " + PText.GetScalePersent(curr_power / max_power, 40) + "\n");
                //result.Append(" |- ИНВЕНТАРЬ: " + PText.GetCurrentOfMax(curr_vol, curr_max_vol, "l") + " - " + curr_mass + "kg" + "\n");
                //result.Append(" |  " + PText.GetScalePersent(curr_vol / curr_max_vol, 40) + "\n");
                //result.Append(" |- ВЫРАБОТАННО:" + "\n");
                //result.Append("    |- H2: " + PText.GetCurrentOfMax(h2_curr_out, h2_max_out, "l") + "\n");
                //result.Append("    | " + PText.GetScalePersent(h2_curr_out / h2_max_out, 30) + "\n");
                //result.Append("    |- O2: " + PText.GetCurrentOfMax(o2_curr_out, o2_max_out, "l") + "\n");
                //result.Append("    | " + PText.GetScalePersent(o2_curr_out / o2_max_out, 30) + "\n");
                return result.ToString();

                //string tdh2 = "";
                //string tdo2 = "";
                //double fr_h2 = 0;
                //float cap_h2 = 0;
                //int count_th2 = 0;
                //double fr_o2 = 0;
                //float cap_o2 = 0;
                //int count_to2 = 0;
                //foreach (IMyGasTank obj in list_obj)
                //{
                //    switch (obj.DefinitionDisplayNameText)
                //    {
                //        case "Водородный бак":
                //            {
                //                fr_h2 += obj.FilledRatio;
                //                cap_h2 += obj.Capacity;
                //                count_th2++;
                //                tdh2 += "|  |-БАК:[" + (obj.Enabled ? "{+}" : "{-}") + (obj.Stockpile ? "{>}" : "{<}") + (obj.AutoRefillBottles ? "{A}" : "{ }") + "]" + PText.GetPersent(obj.FilledRatio) + PText.GetCapacityTanks(obj.FilledRatio, obj.Capacity) + "\n";
                //                break;
                //            }
                //        case "Кислородный бак":
                //            {
                //                fr_o2 += obj.FilledRatio;
                //                cap_o2 += obj.Capacity;
                //                count_to2++;
                //                tdo2 += "|  |-БАК:[" + (obj.Enabled ? "{+}" : "{-}") + (obj.Stockpile ? "{>}" : "{<}") + (obj.AutoRefillBottles ? "{A}" : "{ }") + "]" + PText.GetPersent(obj.FilledRatio) + PText.GetCapacityTanks(obj.FilledRatio, obj.Capacity) + "\n";
                //                break;
                //            }
                //    }
                //}
                //string result = "";
                //result += "|    H2:" + PText.GetCapacityTanks((fr_h2 / count_th2), cap_h2) + "\n";
                //result += "|-+" + PText.GetScalePersent((fr_h2 / count_th2), 50) + "\n";
                //result += tdh2;
                //result += "|\n";
                //result += "|    O2:" + PText.GetCapacityTanks((fr_o2 / count_to2), cap_o2) + "\n";
                //result += "|-+" + PText.GetScalePersent((fr_o2 / count_to2), 50) + "\n";
                //result += tdo2;
                //result += "|\n";
                //return result;
            }
        }
        // Класс генераторы
        public class GasGenerator : BaseListTerminalBlock<IMyGasGenerator>
        {
            //
            public class valus_gas_generator
            {
                public int id_group = 0;
                public MySubtypeName subtype_name = MySubtypeName.none;
                public string definition_display_name_text = null;
                public int count = 0;                       // кол
                public int count_on = 0;                    // кол вкл
                public int count_auto_refill = 0;           // кол авто заполнения
                public int count_use_conveyor_system = 0;   // кол авто заполнения
                public float sum_curr_mass = 0;
                public float sum_curr_vol = 0;
                public float sum_curr_max_vol = 0;
                public float sum_curr_power = 0;             // тек потребление электроэнергии
                public float sum_max_power = 0;             // макс потребление электроэнергии
                public int count_powered = 0;               // кол потребляемых электроэнергию
                public float sum_h2_curr_out = 0;
                public float sum_h2_max_out = 0;
                public float sum_o2_curr_out = 0;
                public float sum_o2_max_out = 0;
            }
            //
            public List<valus_gas_generator> list_gas_generator = new List<valus_gas_generator>();
            //
            public GasGenerator(string name_obj) : base(name_obj)
            {

            }
            //
            public int count_all { get { return list_gas_generator.Select(c => c.count).Sum(); } }
            public int count_on_all { get { return list_gas_generator.Select(c => c.count_on).Sum(); } }
            public int count_auto_refill { get { return list_gas_generator.Select(c => c.count_auto_refill).Sum(); } }
            public int count_count_use_conveyor_system { get { return list_gas_generator.Select(c => c.count_use_conveyor_system).Sum(); } }
            public int count_powered { get { return list_gas_generator.Select(c => c.count_powered).Sum(); } }
            public float max_power { get { return list_gas_generator.Select(c => c.sum_max_power).Sum(); } }
            public float curr_power { get { return list_gas_generator.Select(c => c.sum_curr_power).Sum(); } }
            public float curr_mass { get { return list_gas_generator.Select(c => c.sum_curr_mass).Sum(); } }
            public float curr_vol { get { return list_gas_generator.Select(c => c.sum_curr_vol).Sum(); } }
            public float curr_max_vol { get { return list_gas_generator.Select(c => c.sum_curr_max_vol).Sum(); } }
            public float h2_curr_out { get { return list_gas_generator.Select(c => c.sum_h2_curr_out).Sum(); } }
            public float h2_max_out { get { return list_gas_generator.Select(c => c.sum_h2_max_out).Sum(); } }
            public float o2_curr_out { get { return list_gas_generator.Select(c => c.sum_o2_curr_out).Sum(); } }
            public float o2_max_out { get { return list_gas_generator.Select(c => c.sum_o2_max_out).Sum(); } }
            public void GetValues()
            {
                list_gas_generator.Clear();

                //_scr.test_lcd1.WriteText("Старт" + "\n", false);

                foreach (IMyGasGenerator obj in list_obj)
                {
                    float curr_mass = 0;
                    float curr_vol = 0;
                    float curr_max_vol = 0;
                    float curr_power = 0;
                    float max_power = 0;
                    bool isPoweredBy = false;
                    float sum_h2_curr_out = 0;
                    float sum_h2_max_out = 0;
                    float sum_o2_curr_out = 0;
                    float sum_o2_max_out = 0;
                    valus_gas_generator val_obj = list_gas_generator.Where(o => o.subtype_name.ToString() == obj.BlockDefinition.SubtypeName).FirstOrDefault();
                    if (val_obj == null)
                    {
                        // Инвентарь
                        if (obj.HasInventory)
                        {
                            for (int i = 0; i < obj.InventoryCount; i++)
                            {
                                IMyInventory inv = obj.GetInventory(i);
                                if (inv != null)
                                {
                                    curr_mass += ((float)inv.CurrentMass);
                                    curr_vol += ((float)inv.CurrentVolume);
                                    curr_max_vol += ((float)inv.MaxVolume);
                                }
                            }
                        }
                        //
                        MyResourceSinkComponent sink;
                        obj.Components.TryGet<MyResourceSinkComponent>(out sink);
                        if (sink != null)
                        {
                            var list = sink.AcceptedResources;
                            for (int j = 0; j < list.Count; ++j)
                            {
                                isPoweredBy = sink.IsPoweredByType(list[j]);
                                curr_power += sink.CurrentInputByType(list[j]);
                                max_power += sink.MaxRequiredInputByType(list[j]);
                            }
                        }
                        MyResourceSourceComponent source;
                        obj.Components.TryGet<MyResourceSourceComponent>(out source);

                        if (source != null)
                        {
                            var list = source.ResourceTypes;
                            for (int j = 0; j < list.Count; ++j)
                            {
                                if (list[j].SubtypeId.ToString() == "Oxygen")
                                {
                                    sum_o2_curr_out = source.CurrentOutputByType(list[j]);
                                    sum_o2_max_out = source.DefinedOutputByType(list[j]);
                                }
                                if (list[j].SubtypeId.ToString() == "Hydrogen")
                                {
                                    sum_h2_curr_out = source.CurrentOutputByType(list[j]);
                                    sum_h2_max_out = source.DefinedOutputByType(list[j]);
                                }
                            }
                        }
                        val_obj = new valus_gas_generator()
                        {
                            id_group = 0,
                            definition_display_name_text = obj.DefinitionDisplayNameText,
                            //subtype_name = (MySubtypeName)Enum.Parse(typeof(MySubtypeName), obj.BlockDefinition.SubtypeName.ToString()),
                            count = 1,
                            count_on = obj.Enabled ? 1 : 0,
                            count_auto_refill = obj.AutoRefill ? 1 : 0,
                            count_use_conveyor_system = obj.UseConveyorSystem ? 1 : 0,
                            sum_curr_mass = curr_mass,
                            sum_curr_vol = curr_vol,
                            sum_curr_max_vol = curr_max_vol,
                            sum_curr_power = curr_power,
                            sum_max_power = max_power,
                            count_powered = isPoweredBy ? 1 : 0,
                            sum_h2_curr_out = sum_h2_curr_out,
                            sum_h2_max_out = sum_h2_max_out,
                            sum_o2_curr_out = sum_o2_curr_out,
                            sum_o2_max_out = sum_o2_max_out,
                        };
                        list_gas_generator.Add(val_obj);
                    }
                    else
                    {
                        val_obj.count++;
                        if (obj.Enabled) val_obj.count_on++;
                        if (obj.AutoRefill) val_obj.count_auto_refill++;
                        if (obj.UseConveyorSystem) val_obj.count_use_conveyor_system++;
                        val_obj.sum_curr_mass += curr_mass;
                        val_obj.sum_curr_vol += curr_vol;
                        val_obj.sum_curr_max_vol += curr_max_vol;
                        val_obj.sum_curr_power += curr_power;
                        val_obj.sum_max_power += max_power;
                        if (isPoweredBy) val_obj.count_powered++;
                        val_obj.sum_h2_curr_out += sum_h2_curr_out;
                        val_obj.sum_h2_max_out += sum_h2_max_out;
                        val_obj.sum_o2_curr_out += sum_o2_curr_out;
                        val_obj.sum_o2_max_out += sum_o2_max_out;
                    }
                }
            }

            public string GetStatusOfText()
            {
                StringBuilder result = new StringBuilder();
                result.Append("ГЕНЕРАТОРЫ O2/H2: " + PText.GetCountObj(count_all, count_on_all) + " А[" + count_auto_refill + "]" + " К[" + count_count_use_conveyor_system + "]" + "\n");
                result.Append(" |- ПОТРЕБЛЕНИЕ: " + "[" + count_powered + "]" + PText.GetCurrentOfMax(curr_power, max_power, "кW") + "\n");
                result.Append(" |  " + PText.GetScalePersent(curr_power / max_power, 40) + "\n");
                result.Append(" |- ИНВЕНТАРЬ: " + PText.GetCurrentOfMax(curr_vol, curr_max_vol, "l") + " - " + curr_mass + "kg" + "\n");
                result.Append(" |  " + PText.GetScalePersent(curr_vol / curr_max_vol, 40) + "\n");
                result.Append(" |- ВЫРАБОТАННО:" + "\n");
                result.Append("    |- H2: " + PText.GetCurrentOfMax(h2_curr_out, h2_max_out, "l") + "\n");
                result.Append("    | " + PText.GetScalePersent(h2_curr_out / h2_max_out, 30) + "\n");
                result.Append("    |- O2: " + PText.GetCurrentOfMax(o2_curr_out, o2_max_out, "l") + "\n");
                result.Append("    | " + PText.GetScalePersent(o2_curr_out / o2_max_out, 30) + "\n");
                return result.ToString();
            }
        }
    }
}

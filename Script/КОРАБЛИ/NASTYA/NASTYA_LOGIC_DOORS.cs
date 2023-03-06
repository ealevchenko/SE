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
using VRageMath;
using static System.Net.Mime.MediaTypeNames;

namespace NASTYA_LOGIC_DOORS
{
    public sealed class Program : MyGridProgram
    {
        static IMyTextPanel test_lcd;//, test_lcd1;

        public static Color red = new Color(255, 0, 0);
        public static Color yellow = new Color(255, 255, 0);
        public static Color green = new Color(0, 128, 0);
        public enum room : int
        {
            none = 0,
            basement = 1,           // подвал
            factory = 2,            // завод
            hangar = 3,             // ангар
            habitation = 4,         // жилой модуль ()
            medical = 5,            // медицинский модуль
            captain = 6,            // капитанская каюта
            assistant = 7,          // каюта помошника
            cryo_left = 8,          // крео-камера
            cryo_right = 9,         // крео-камера
            canteen = 10,           // столовая
            cabin = 11,             // кабина
            operators = 12,         // операторская
            hydrogen_left_1 = 13,   // водородный склад
            energy_left_1 = 14,     // энергетический модуль
            hydrogen_right_1 = 15,  // водородный склад
            energy_right_1 = 16,    // энергетический модуль
            reactor = 17,           // реактор
            technical_1 = 18,       // технический этаж
            hydrogen_left_2 = 19,   // водородный склад
            energy_left_2 = 20,     // энергетический модуль
            hydrogen_right_2 = 21,  // водородный склад
            energy_right_2 = 22,    // энергетический модуль
            training_left = 23,     // тренеровочный зал
            cabins_left = 24,       // каюты тех персонала
            training_right = 25,    // тренеровочный зал
            cabins_right = 26,      // каюты тех персонала
            gateway = 27,           // шлюзовая
            gateway_left = 28,      // шлюз левый		           
            gateway_right = 29,     // шлюз правый
            gateway_stern = 30,     // шлюз корма
            out_space = 31,         // Космос


        };
        public static string[] name_room = { "", "Подвал", "Завод", "Ангар", "Жилой модуль", "Мед-блок", "Капитан", "Помошник", "КРЕО-камеры", "КРЕО-камеры", "Столовая",
            "Кабина", "Операторская", "Водородный склад", "Энерго-модуль", "Водородный склад", "Энерго-модуль", "Реакторная", "Тех-этаж 1",
            "Водородный склад", "Энерго-модуль", "Водородный склад", "Энерго-модуль", "Тренеровочный зал", "Каюты", "Тренеровочный зал", "Каюты", "Шлюз", "Шлюз левый", "Шлюз правый", "Шлюз корма", "Выход в космос"};
        public static int[] count_room = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public enum doors_gareways : int
        {
            hangar_factory = 0,
            technical_1_hangar = 1,
            technical_1_factory = 2,
            hydrogen_gateway_left = 3,
            hydrogen_gateway_right = 4,
            gateway_stern_left = 5,
            gateway_stern_right = 6,
            factory_habitation = 7,
            operators_gateway_left1 = 8,
            operators_gateway_left2 = 9,
            operators_gateway_right1 = 10,
            operators_gateway_right2 = 11,
            operators_cabin1 = 12,
            operators_cabin2 = 13,
            technical_1_basement = 14
        }
        public enum door_transition : int
        {
            operators_habitation = 0,
            operators_technical_1 = 1,
            technical_1_training_left = 2,
            technical_1_training_right = 3,
            training_cabins_left = 4,
            training_cabins_right = 5,
            technical_1_gateway = 6,
            technical_hydrogen_left_1 = 7,
            technical_energy_left_1 = 8,
            technical_hydrogen_right_1 = 9,
            technical_energy_right_1 = 10,
            technical_hydrogen_left_2 = 11,
            technical_energy_left_2 = 12,
            technical_hydrogen_right_2 = 13,
            technical_energy_right_2 = 14,
            technical_habitation_1 = 15,
            technical_habitation_2 = 16,
            habitation_assistant = 17,
            habitation_captain = 18,
            habitation_medical = 19,
        }
        public enum door_slide : int
        {
            hangar_left_1 = 0,
            hangar_right_1 = 1,
            hangar_left_2 = 2,
            hangar_right_2 = 3,
        }

        string tag_info_tablo = "[door-info]";
        string tag_door_transition = "[door_transition]";
        string tag_door_gateway = "[door-gateway]";
        string tag_lighting_room = "[lighting_room]";
        string tag_ref_room_hangar = "[ref_room]";

        AirInfo air_info;
        AirVent air_vent;
        Gateways gateways_doors;
        Transitions transition_door;
        Lightings room_light;
        ReflectorLight ref_light;
        AirtightSlideDoor slide_door;

        string NameObj = "NASTYA1";

        // door [door-gateway] [hangar_factory] [hangar]
        // sn [door-gateway] [hangar_factory] [hangar]
        // door [door-gateway] [hangar_factory] [factory]
        // sn [door-gateway] [hangar_factory] [factory]

        //light [lighting_room] [factory]

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
        }
        public class BaseDoor<T> : BaseListTerminalBlock<T> where T : class
        {
            public BaseDoor(string name_obj) : base(name_obj)
            {

            }
            public float? OpenRatio(string tag)
            {
                float? open_ratio = base.list_obj.Where(d => ((IMyTerminalBlock)d).CustomName.Contains("[" + tag + "]")).Select(f => ((IMyDoor)f).OpenRatio).Sum();
                int? count = base.list_obj.Where(d => ((IMyTerminalBlock)d).CustomName.Contains("[" + tag + "]")).Count();
                return open_ratio != null && count != null ? open_ratio / count : null;
            }
            public bool IsOpen(string tag)
            {
                bool result = true;

                foreach (IMyDoor obj in base.list_obj.Where(d => ((IMyTerminalBlock)d).CustomName.Contains("[" + tag + "]")))
                {
                    if (obj.Status != DoorStatus.Open)
                    {
                        result = false; break;
                    }
                }
                return result;
            }
            public bool IsClosed(string tag)
            {
                bool result = true;

                foreach (IMyDoor obj in base.list_obj.Where(d => ((IMyTerminalBlock)d).CustomName.Contains("[" + tag + "]")))
                {
                    if (obj.Status != DoorStatus.Closed)
                    {
                        result = false; break;
                    }
                }
                return result;
            }
            public void Open(string tag)
            {
                foreach (IMyDoor obj in base.list_obj.Where(d => ((IMyTerminalBlock)d).CustomName.Contains("[" + tag + "]")))
                {
                    obj.OpenDoor();
                }
            }
            public void Close(string tag)
            {
                foreach (IMyDoor obj in base.list_obj.Where(d => ((IMyTerminalBlock)d).CustomName.Contains("[" + tag + "]")))
                {
                    obj.CloseDoor();
                }
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
            // тест LCD
            test_lcd = GridTerminalSystem.GetBlockWithName("NASTYA1-test_lcd") as IMyTextPanel;
            Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));
            //test_lcd1 = GridTerminalSystem.GetBlockWithName("NASTYA1-test_lcd1") as IMyTextPanel;
            //Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));
            //door_gataway_hangar_factory = new DoorGateway(dg_option_hangar_factory);
            air_vent = new AirVent(NameObj);
            air_vent.On();
            air_info = new AirInfo(NameObj, tag_info_tablo);
            gateways_doors = new Gateways(NameObj, tag_door_gateway);
            transition_door = new Transitions(NameObj, tag_door_transition);
            room_light = new Lightings(NameObj, tag_lighting_room); // Освещение
            room_light.Off();
            ref_light = new ReflectorLight(NameObj, null);
            ref_light.Off();
            slide_door = new AirtightSlideDoor(NameObj, null);
            slide_door.Close(door_slide.hangar_left_1.ToString());
            slide_door.Close(door_slide.hangar_left_2.ToString());
            slide_door.Close(door_slide.hangar_right_1.ToString());
            slide_door.Close(door_slide.hangar_right_2.ToString());
        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            //test_lcd.WriteText("old:" + old_parking.ToString() + " connector:"+connector.IsParkingEnabled.ToString() + "\n" , true);  
            //test_lcd.WriteText("clock="+ clock.ToString() +"updateSource-" + updateSource + "\n", false);            

            switch (argument)
            {
                case "open_hl1":
                    {
                        slide_door.Open(door_slide.hangar_left_1.ToString());
                        break;
                    }
                case "close_hl1":
                    {
                        slide_door.Close(door_slide.hangar_left_1.ToString());
                        break;
                    }
                case "open_hl2":
                    {
                        slide_door.Open(door_slide.hangar_left_2.ToString());
                        break;
                    }
                case "close_hl2":
                    {
                        slide_door.Close(door_slide.hangar_left_2.ToString());
                        break;
                    }
                case "open_hr1":
                    {
                        slide_door.Open(door_slide.hangar_right_1.ToString());
                        break;
                    }
                case "close_hr1":
                    {
                        slide_door.Close(door_slide.hangar_right_1.ToString());
                        break;
                    }
                case "open_hr2":
                    {
                        slide_door.Open(door_slide.hangar_right_2.ToString());
                        break;
                    }
                case "close_hr2":
                    {
                        slide_door.Close(door_slide.hangar_right_2.ToString());
                        break;
                    }
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                StringBuilder values = new StringBuilder();
                // Получим данные
                //test_lcd.WriteText("" + "\n", false);
                // Логика отображения подписей двирей с учетом кислорода в помещении
                air_info.Logic(argument, updateSource);
                // Логика отработки шлюзовых дверей
                gateways_doors.Logic(argument, updateSource);
                transition_door.Logic(argument, updateSource);
                // В космосе людей не считаем
                count_room[(int)room.gateway_left] = 0;
                count_room[(int)room.gateway_right] = 0;
                count_room[(int)room.gateway_stern] = 0;
                count_room[(int)room.out_space] = 0;
                // Логика отработки включения и выключения освещения
                room_light.Logic(argument, updateSource);
                // логика подсветки ангара прожекторами
                if (count_room[(int)room.hangar] > 0)
                {
                    ref_light.OnOfTag(tag_ref_room_hangar);
                }
                else
                {
                    ref_light.OffOfTag(tag_ref_room_hangar);
                }
                //test_lcd.WriteText("" + "\n", false);
                //test_lcd.WriteText("hangar:" + count_room[(int)room.hangar] + "\n", true);
                //test_lcd.WriteText("factory:" + count_room[(int)room.factory] + "\n", true);
                //test_lcd.WriteText("technical_1:" + count_room[(int)room.technical_1] + "\n", true);
            }
        }
        // Переходная дверь
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
        // Класс управления переходными дверями
        public class Transitions
        {
            private List<IMyDoor> doors = new List<IMyDoor>();
            private List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            List<Transition> list_tr = new List<Transition>();
            public Transitions(string name_obj, string tag)
            {
                //test_lcd.WriteText("Start" + "\n", false);
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                _scr.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                //test_lcd.WriteText("doors:" + doors.Count() + "\n", true);
                //test_lcd.WriteText("sensors:" + doors.Count() + "\n", true);
                IMyDoor door1;
                IMySensorBlock sensor1;
                IMySensorBlock sensor2;
                room room1;
                room room2;
                //test_lcd.WriteText("Поиск дверей:" + "\n", false);
                foreach (door_transition gw in Enum.GetValues(typeof(door_transition)))
                {
                    door1 = null;
                    sensor1 = null;
                    sensor2 = null;
                    room1 = room.none;
                    room2 = room.none;
                    IMyDoor l_drs = doors.Where(d => d.CustomName.Contains("[" + gw.ToString() + "]")).FirstOrDefault();
                    List<IMySensorBlock> l_sns = sensors.Where(d => d.CustomName.Contains("[" + gw.ToString() + "]")).ToList();
                    if (l_drs != null && l_sns != null && l_sns.Count() == 2)
                    {
                        foreach (room rm in Enum.GetValues(typeof(room)))
                        {
                            //test_lcd.WriteText("room:" + rm + "\n", true);
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
                            //test_lcd.WriteText("door1:" + door1.CustomName + "\n", true);
                            //test_lcd.WriteText("sensor1:" + sensor1.CustomName + "\n", true);
                            //test_lcd.WriteText("sensor2:" + sensor2.CustomName + "\n", true);
                            list_tr.Add(new Transition(gw, door1, sensor1, room1, sensor2, room2));
                        }
                    }

                }
                _scr.Echo("Найдено Transitions:[" + tag + "]: " + list_tr.Count());
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
        // Раздвежная дверь
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
        // Класс управления шлюзовыми дверями
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
        // Информационые табло дверей
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
        // Освещение
        public class InteriorLight : BaseListTerminalBlock<IMyInteriorLight>
        {
            public InteriorLight(string name_obj) : base(name_obj)
            {

            }

        }
        // освещение помещения
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
                    //case "connected_on":
                    //    break;
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
        // прожекторы
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
        // гермитичная раздвижная дверь
        public class AirtightSlideDoor : BaseDoor<IMyDoor>
        {
            public AirtightSlideDoor(string name_obj, string tag) : base(name_obj)
            {
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено AirtightSlideDoor:[" + tag + "]: " + list_obj.Count());
                test_lcd.WriteText("Поиск дверей:" + "\n", false);
                foreach (var item in list_obj)
                {
                    test_lcd.WriteText("дверь:" + item.CustomName + "\n", true);

                }
            }

        }

    }
}

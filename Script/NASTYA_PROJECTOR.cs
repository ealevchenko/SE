using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using static NASTYA_LOGIC_DOORS.Program;

namespace NASTYA_PROJECTOR
{
    public sealed class Program : MyGridProgram
    {
        static IMyTextPanel test_lcd;//, test_lcd1;

        string NameObj = "NASTYA1";
        string NameProjector = "NASTYA1-Проектор МС";
        string NamePiston = "NASTYA1-Поршень проектор МС";
        string NameSnProtect = "NASTYA1-Сенсор защиты сварщика МС";
        string NameWelderShipController = "NASTYA1-Кресло пилота сварщик МС";
        string NameLightWelder = "NASTYA1-лампа предупреждения сварщик";


        static float max_speed_pis = 0.5f;
        static float min_speed_pis = 0.05f;
        static float step_pis = 0.5f;

        float[] sn_protection_option = { 6.25f, 6.25f, 8.75f, 8.75f, 0, 20f };

        Projector prg_ms;
        Piston pis_prg;
        ShipWelder ship_prg;
        Sensor sn_protection;
        WelderShipController ws_controller;
        Lighting lighting_welder;


        static bool ship_on_off = false;        // признак работы сварщика

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

            prg_ms = new Projector(NameProjector);
            pis_prg = new Piston(NamePiston);
            ship_prg = new ShipWelder(NameObj);
            ship_prg.Off();
            sn_protection = new Sensor(NameSnProtect, sn_protection_option);
            ws_controller = new WelderShipController(NameWelderShipController);
            lighting_welder = new Lighting(NameLightWelder);
            lighting_welder.Off();
        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            //test_lcd.WriteText("old:" + old_parking.ToString() + " connector:"+connector.IsParkingEnabled.ToString() + "\n" , true);  
            //test_lcd.WriteText("clock="+ clock.ToString() +"updateSource-" + updateSource + "\n", false);
            // Проверим датчик защиты
            if (sn_protection.IsActive)
            {
                if (ship_on_off)
                {
                    pis_prg.Off();
                    ship_prg.Off();
                }
            }
            else
            {
                if (ship_on_off)
                {
                    pis_prg.On();
                    ship_prg.On();
                }
                prg_ms.Logic(argument, updateSource);
                pis_prg.Logic(argument, updateSource);
                ship_prg.Logic(argument, updateSource);
                switch (argument)
                {
                    //case "open_hl1":
                    //    {
                    //        //slide_door.Open(door_slide.hangar_left_1.ToString());
                    //        break;
                    //    }
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {

                }
            }
            if (ship_on_off)
            {
                lighting_welder.On();
            }
            else
            {
                lighting_welder.Off();
            }
        }
        // Переходная дверь
        public class Projector : BaseTerminalBlock<IMyProjector>
        {
            public Projector(string name) : base(name)
            {

            }

            public void add_X(int x)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X + x, pos.Y, pos.Z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void inc_X(int x)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X - x, pos.Y, pos.Z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void add_Y(int y)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X, pos.Y + y, pos.Z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void inc_Y(int y)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X, pos.Y - y, pos.Z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void add_Z(int z)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X, pos.Y, pos.Z + z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void inc_Z(int z)
            {
                Vector3I pos = base.obj.ProjectionOffset;
                Vector3I new_pos = new Vector3I(pos.X, pos.Y, pos.Z - z);
                base.obj.ProjectionOffset = new_pos;
                base.obj.UpdateOffsetAndRotation();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "incX":
                        base.obj.ApplyAction("IncreaseX");
                        break;
                    case "decX":
                        base.obj.ApplyAction("DecreaseX");
                        break;
                    case "incY":
                        base.obj.ApplyAction("IncreaseY");
                        break;
                    case "decY":
                        base.obj.ApplyAction("DecreaseY");
                        break;
                    case "incZ":
                        base.obj.ApplyAction("IncreaseZ");
                        break;
                    case "decZ":
                        base.obj.ApplyAction("DecreaseZ");
                        break;
                    case "rot_incX":
                        base.obj.ApplyAction("IncreaseRotX");
                        break;
                    case "rot_decX":
                        base.obj.ApplyAction("DecreaseRotX");
                        break;
                    case "rot_incY":
                        base.obj.ApplyAction("IncreaseRotY");
                        break;
                    case "rot_decY":
                        base.obj.ApplyAction("DecreaseRotY");
                        break;
                    case "rot_incZ":
                        base.obj.ApplyAction("IncreaseRotZ");
                        break;
                    case "rot_decZ":
                        base.obj.ApplyAction("DecreaseRotZ");
                        break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {

                }

            }
        }
        public class Piston : BaseTerminalBlock<IMyPistonBase>
        {
            bool move = false;
            float new_pos = 0;
            public Piston(string name) : base(name)
            {

            }
            public void Stop()
            {
                base.obj.Velocity = 0;
                move = false;
            }
            public void Parking()
            {
                move = false;
                base.obj.Velocity = max_speed_pis;
                base.obj.Retract();
            }
            public void StartPosition()
            {
                move = false;
                base.obj.Velocity = max_speed_pis;
                base.obj.Extend();
            }
            public void Up()
            {
                move = false;
                base.obj.Velocity = min_speed_pis;
                base.obj.Retract();
            }
            public void Down()
            {
                move = false;
                base.obj.Velocity = min_speed_pis;
                base.obj.Extend();
            }
            public void Step_Up()
            {
                new_pos = base.obj.CurrentPosition - step_pis;
                if (new_pos < 0) { new_pos = 0; }
                move = true;
            }
            public void Step_Down()
            {
                new_pos = base.obj.CurrentPosition + step_pis;
                if (new_pos > 10) { new_pos = 10; }
                move = true;
            }
            public void Move()
            {
                if (!move) return;
                float cur_pos = base.obj.CurrentPosition;
                if (cur_pos == new_pos)
                {
                    move = false;
                    new_pos = 0;
                }
                else if (cur_pos < new_pos)
                {
                    Down();
                }
                else
                {
                    Up();
                }
            }
            public void Move(int pos)
            {
                if (!move) return;
                new_pos = pos;
                if (new_pos < 0) { new_pos = 0; }
                if (new_pos > 10) { new_pos = 10; }
                move = true;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "prg_stop":
                        Stop();
                        break;
                    case "prg_parking":
                        Parking();
                        break;
                    case "prg_start":
                        StartPosition();
                        break;
                    case "prg_up":
                        Up();
                        break;
                    case "prg_down":
                        Down();
                        break;
                    case "prg_step_up":
                        Step_Up();
                        break;
                    case "prg_step_down":
                        Step_Down();
                        break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {
                    Move();
                }

            }
        }
        public class ShipWelder : BaseListTerminalBlock<IMyShipWelder>
        {
            public ShipWelder(string name_obj) : base(name_obj)
            {

            }

            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "ship_on":
                        base.On();
                        ship_on_off = true;
                        break;
                    case "ship_off":
                        base.Off();
                        ship_on_off = false;
                        break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {

                }

            }
        }
        public class Sensor : BaseTerminalBlock<IMySensorBlock>
        {

            public bool IsActive { get { return obj.IsActive; } }
            public Sensor(string name) : base(name)
            {

            }
            public Sensor(string name, float lf, float rg, float bt, float tp, float bc, float fr) : base(name)
            {
                SetExtend(lf, rg, bt, tp, bc, fr);
                SetDetect(true, false, false, false, false, false, false, true, false, false, false);
            }
            public Sensor(string name, float[] sn_option) : base(name)
            {
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
                obj.LeftExtend = lf;//Left - Охват слева
                obj.RightExtend = rg;//Right - Охват справа
                obj.BottomExtend = bt;//Bottom - Охват снизу
                obj.TopExtend = tp;//Top - Охват сверху
                obj.BackExtend = bc;//Back - Охват сзади
                obj.FrontExtend = fr;//Front - Охват спереди
            }
            public void SetDetect(bool Players, bool FloatingObjects, bool SmallShips, bool LargeShips, bool Stations, bool Subgrids,
                bool Asteroids, bool Owner, bool Friendly, bool Neutral, bool Enemy)
            {
                obj.DetectPlayers = Players;            // Играки
                obj.DetectFloatingObjects = FloatingObjects;   // Обнаруживать плавающие объекты
                obj.DetectSmallShips = SmallShips;        // Малые корабли
                obj.DetectLargeShips = LargeShips;        // Большие корабли
                obj.DetectStations = Stations;          // Большие станции
                obj.DetectSubgrids = Subgrids;          // Подсетки
                obj.DetectAsteroids = Asteroids;         // Астероиды планеты
                obj.DetectOwner = Owner;              // Владельцы блоков
                obj.DetectFriendly = Friendly;          // Дружественные игроки
                obj.DetectNeutral = Neutral;           // Нитральные игроки
                obj.DetectEnemy = Enemy;             // Враги
            }
        }
        public class WelderShipController : BaseTerminalBlock<IMyShipController>
        {
            public WelderShipController(string name) : base(name)
            {
                obj.ControlThrusters = false;
                obj.ControlWheels = false;
                obj.HandBrake = false;
                obj.ShowHorizonIndicator = false;

            }
        }
        public class Lighting : BaseTerminalBlock<IMyLightingBlock>
        {
            public Lighting(string name) : base(name)
            {

            }
        }
    }
}

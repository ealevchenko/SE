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

        //string NameObj = "NASTYA1";
        string NameProjector = "NASTYA1-Проектор МС";

        Projector prg_ms;

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
        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            //test_lcd.WriteText("old:" + old_parking.ToString() + " connector:"+connector.IsParkingEnabled.ToString() + "\n" , true);  
            //test_lcd.WriteText("clock="+ clock.ToString() +"updateSource-" + updateSource + "\n", false);
            //
            prg_ms.Logic(argument, updateSource);

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
                StringBuilder values = new StringBuilder();


                // Получим данные
                //test_lcd.WriteText("" + "\n", false);
                //test_lcd.WriteText("" + "\n", false);
                //test_lcd.WriteText("hangar:" + count_room[(int)room.hangar] + "\n", true);
                //test_lcd.WriteText("factory:" + count_room[(int)room.factory] + "\n", true);
                //test_lcd.WriteText("technical_1:" + count_room[(int)room.technical_1] + "\n", true);
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
                    case "addX":
                        add_X(1);
                        break;
                    case "incX":
                        inc_X(1);
                        break;
                    case "addY":
                        add_Y(1);
                        break;
                    case "incY":
                        inc_Y(1);
                        break;
                    case "addZ":
                        add_Z(1);
                        break;
                    case "incZ":
                        inc_Z(1);
                        break;
                    case "rot_incX":
                        base.obj.ApplyAction("IncreaseRotX");
                        break;
                    case "rot_decX":
                        base.obj.ApplyAction("DecreaseRotX");
                        break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {

                }

            }
        }
    }
}

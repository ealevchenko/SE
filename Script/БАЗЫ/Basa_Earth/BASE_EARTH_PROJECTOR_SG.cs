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

namespace BASE_EARTH_PROJECTOR_SG
{
    public sealed class Program : MyGridProgram
    {
        static IMyTextPanel test_lcd;//, test_lcd1;

        string NameObj = "[BASE-EA1]";
        string NameLCD_Test = "[BASE-EA1]-lcd_welder";
        string NameProjector = "[BASE-EA1]-Проектор сварщик МС";
        string NameSnProtect = "[BASE-EA1]-Сенсор защиты сварщика МС";
        string NameWelderShipController = "[BASE-EA1]-Кресло пилота сварщик МС [LCD]";

        string name_tag_light_welder = "[light_welder_sg]";
        string name_tag_ship_welder = "[ship_welder_sg]";
        string name_tag_pistons_welder = "[pistons_welder_sg]";

        static float max_speed_pis = 0.5f;
        static float min_speed_pis = 0.05f;
        static float step_pis = 0.5f;

        float[] sn_protection_option = { 10.0f, 10.0f, 10.0f, 10.0f, 10f, 0f };

        Projector prg_ms;
        Pistons pis_prg;
        ShipWelder ship_prg;
        Sensor sn_protection;
        WelderShipController ws_controller;
        Lightings lightings_welder;

        static bool ship_on_off = false;        // признак работы сварщика

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
            test_lcd = GridTerminalSystem.GetBlockWithName(NameLCD_Test) as IMyTextPanel;
            Echo("LCD test: " + ((test_lcd != null) ? ("Ок") : ("not found")));

            prg_ms = new Projector(NameProjector);
            pis_prg = new Pistons(NameObj, name_tag_pistons_welder);
            ship_prg = new ShipWelder(NameObj, name_tag_ship_welder);
            ship_prg.Off();
            sn_protection = new Sensor(NameSnProtect, sn_protection_option);
            ws_controller = new WelderShipController(NameWelderShipController);
            lightings_welder = new Lightings(NameObj, name_tag_light_welder);
            lightings_welder.Off();
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
                lightings_welder.On();
            }
            else
            {
                lightings_welder.Off();
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
        public class Pistons : BaseListTerminalBlock<IMyPistonBase>
        {
            bool move = false;
            float new_pos = 0;
            float min_pos = 0;
            float max_pos = 0;

            public Pistons(string NameObj) : base(NameObj)
            {
                max_pos = list_obj.Count() * 10;
                //step_pos = list_obj.Count() > 0 ? step_pis / list_obj.Count() : 0;
            }
            public Pistons(string name_obj, string tag) : base(name_obj, tag)
            {
                max_pos = list_obj.Count() * 10;
            }
            public void Stop()
            {
                foreach (IMyPistonBase obj in base.list_obj)
                {
                    obj.Velocity = 0;
                }
                move = false;
            }
            public void Parking()
            {
                move = false;
                foreach (IMyPistonBase obj in base.list_obj)
                {
                    obj.Velocity = max_speed_pis;
                    obj.Retract();
                }
            }
            public void EndPosition()
            {
                move = false;
                foreach (IMyPistonBase obj in base.list_obj)
                {
                    obj.Velocity = max_speed_pis;
                    obj.Extend();
                }
            }
            public void Down()
            {
                move = false;
                foreach (IMyPistonBase obj in base.list_obj)
                {
                    obj.Velocity = min_speed_pis;
                    obj.Retract(); // задвинуть
                }
            }
            public void Up()
            {
                move = false;
                foreach (IMyPistonBase obj in base.list_obj)
                {
                    obj.Velocity = min_speed_pis;
                    obj.Extend();  // выдвинуть
                }
            }
            public void Step_Up()
            {
                new_pos = 0;
                foreach (IMyPistonBase obj in base.list_obj)
                {
                    new_pos += obj.CurrentPosition;
                }
                new_pos -= step_pis;
                if (new_pos < this.min_pos) { new_pos = 0; }
                move = true;
            }
            public void Step_Down()
            {
                new_pos = 0;
                foreach (IMyPistonBase obj in base.list_obj)
                {
                    new_pos += obj.CurrentPosition;
                }
                new_pos += step_pis;
                if (new_pos > this.max_pos) { new_pos = this.max_pos; }
                move = true;
            }
            public void Move()
            {
                if (!move) return;

                float cur_pos = 0;
                foreach (IMyPistonBase obj in base.list_obj)
                {
                    cur_pos += obj.CurrentPosition;
                }
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
                if (new_pos < this.min_pos) { new_pos = this.min_pos; }
                if (new_pos > this.max_pos) { new_pos = this.max_pos; }
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
                    case "prg_end":
                        EndPosition();
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
            public ShipWelder(string name_obj, string tag) : base(name_obj, tag)
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
        public class Lightings : BaseListTerminalBlock<IMyLightingBlock>
        {
            public Lightings(string NameObj) : base(NameObj)
            {

            }
            public Lightings(string name_obj, string tag) : base(name_obj, tag)
            {

            }
        }
    }
}

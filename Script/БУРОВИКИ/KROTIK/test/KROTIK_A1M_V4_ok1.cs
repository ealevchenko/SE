using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRageMath;
/// <summary>
/// v4.0
/// </summary>
namespace KROTIK_A1M_V4_ok1
{
    public sealed class Program : MyGridProgram
    {
        // m.v3
        string NameObj = "[KROTIK_A1]";
        string NameCockpit = "[KROTIK_A1]-Промышленный кокпит [LCD]";
        string NameRemoteControl = "[KROTIK_A1]-ДУ Парковка";
        string NameConnector = "[KROTIK_A1]-Коннектор парковка";
        string NameCameraCourse = "[KROTIK_A1]-Камера парковка";
        string NameLCDInfo = "[KROTIK_A1]-LCD-INFO";

        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея

        const char green = '\uE001';
        const char blue = '\uE002';
        const char red = '\uE003';
        const char yellow = '\uE004';
        const char darkGrey = '\uE00F';

        static LCD lcd_info;
        Batterys bats;
        Connector connector;
        ShipDrill drill;
        ReflectorsLight reflectors_light;
        Cockpit cockpit;
        Navigation navigation;

        static Program _scr;
        public class PText
        {
            static public string GetVector3D(Vector3D vector)
            {
                return "X: " + Math.Round(vector.GetDim(0), 2).ToString() + "Y: " + Math.Round(vector.GetDim(1), 2).ToString() + "Z: " + Math.Round(vector.GetDim(2), 2).ToString();
            }
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
                return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]";
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
            static public string GetGPS(string name, Vector3D target)
            {
                return "GPS:" + name + ":" + target.GetDim(0) + ":" + target.GetDim(1) + ":" + target.GetDim(2) + ":\n";
            }
        }
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
            lcd_info = new LCD(NameLCDInfo);
            bats = new Batterys(NameObj);
            connector = new Connector(NameConnector);
            drill = new ShipDrill(NameObj);
            drill.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            cockpit = new Cockpit(NameCockpit);
            navigation = new Navigation(cockpit, NameObj, NameRemoteControl, NameCameraCourse);
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            bats.Logic(argument, updateSource);
            navigation.Logic(argument, updateSource);

            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {

            }
            //values_info.Append(bats.TextInfo());
            values_info.Append(connector.TextInfo());
            values_info.Append(drill.TextInfo());
            values_info.Append(navigation.TextInfo());
            cockpit.OutText(values_info, 0);
            //StringBuilder test_info = new StringBuilder();
            //cockpit.OutText(test_info, 1);

            StringBuilder test_info = new StringBuilder();
            test_info.Append(navigation.TextTEST());
            lcd_info.OutText(test_info);

            //lcd_info.OutText(test_info);
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
        public class Batterys : BaseListTerminalBlock<IMyBatteryBlock>
        {
            public int count_work_batterys { get { return list_obj.Where(n => !((IMyTerminalBlock)n).CustomName.Contains(tag_batterys_duty)).Count(); } }
            public bool charger = false;
            public Batterys(string name_obj) : base(name_obj)
            {
                Init();
            }
            public Batterys(string name_obj, string tag) : base(name_obj, tag)
            {
                Init();
            }
            public void Init()
            {
                base.On();
                charger = IsCharger();
            }
            public float MaxPower()
            {
                return base.list_obj.Select(b => b.MaxStoredPower).Sum();
            }
            public float CurrentPower()
            {
                return base.list_obj.Select(b => b.CurrentStoredPower).Sum();
            }
            public int CountCharger()
            {
                List<IMyBatteryBlock> res = base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Recharge).ToList();
                return res.Count();
            }
            public int CountAuto()
            {
                List<IMyBatteryBlock> res = base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Auto).ToList();
                return res.Count();
            }
            public bool IsCharger()
            {
                int count_charger = CountCharger();
                return count_work_batterys > 0 && count_charger > 0 && count_work_batterys == count_charger ? true : false;
            }
            public bool IsAuto()
            {
                int count_auto = CountAuto();
                return Count > 0 && count_auto > 0 && Count == count_auto ? true : false;
            }
            public void Charger()
            {
                foreach (IMyBatteryBlock obj in base.list_obj)
                {
                    // проверка батарея дежурного режима
                    if (!obj.CustomName.Contains(tag_batterys_duty))
                    {
                        obj.ChargeMode = ChargeMode.Recharge;
                    }
                }
                charger = IsCharger();
            }
            public void Auto()
            {
                foreach (IMyBatteryBlock obj in base.list_obj)
                {
                    obj.ChargeMode = ChargeMode.Auto;
                }
                charger = IsCharger();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "bat_charger":
                        Charger();
                        break;
                    case "bat_auto":
                        Auto();
                        break;
                    case "bat_toggle":
                        if (charger) { Auto(); } else { Charger(); }
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
                //БАТАРЕЯ: [10 - 10] [0.0MW / 0.0MW]
                //|- ЗАР:  [''''''''''''''''''''''''']-0%
                values.Append("БАТАРЕЯ: [" + Count + "] [А-" + CountAuto() + " З-" + CountCharger() + "]" + PText.GetCurrentOfMax(CurrentPower(), MaxPower(), "MW") + "\n");
                values.Append("|- ЗАР:  " + PText.GetScalePersent(CurrentPower() / MaxPower(), 20) + "\n");
                return values.ToString();
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
        public class ShipDrill : BaseListTerminalBlock<IMyShipDrill>
        {
            public ShipDrill(string name_obj) : base(name_obj)
            {
            }
            public ShipDrill(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("БУРЫ: " + (base.Enabled() ? "ВКЛ" : "ОТК") + "\n");
                return values.ToString();
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
        public class Cockpit : BaseTerminalBlock<IMyShipController>
        {
            private double current_height = 0;
            public float BaseMass { get { return base.obj.CalculateShipMass().BaseMass; } }
            public float TotalMass { get { return base.obj.CalculateShipMass().TotalMass; } }
            public float PhysicalMass { get { return base.obj.CalculateShipMass().PhysicalMass; } }
            public double CurrentHeight { get { return this.current_height; } }
            public double ShipSpeed { get { return base.obj.GetShipSpeed(); } }
            public MatrixD WorldMatrix { get { return base.obj.WorldMatrix; } }
            public IMyShipController _obj { get { return obj; } }
            public bool IsUnderControl { get { return obj.IsUnderControl; } }
            public Vector3D GetNaturalGravity { get { return obj.GetNaturalGravity(); } }
            public Matrix GetCockpitMatrix()
            {
                Matrix CockpitMatrix = new MatrixD();
                base.obj.Orientation.GetMatrix(out CockpitMatrix);
                return CockpitMatrix;
            }
            public Cockpit(string name) : base(name)
            {

            }
            public void Dampeners(bool on)
            {
                obj.DampenersOverride = on;
            }
            public double GetDistance(Vector3D target)
            {
                return (target - obj.GetPosition()).Length();
            }
            public void OutText(StringBuilder values, int num_lcd)
            {
                if (obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(values, false);
                    }
                }
            }
            public void OutText(string text, bool append, int num_lcd)
            {
                if (obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(text, append);
                    }
                }
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
                    // Получить высоту над поверхностью
                    base.obj.TryGetPlanetElevation(MyPlanetElevation.Surface, out current_height);
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Гравитация: " + base.obj.GetNaturalGravity().Length() + "\n");
                values.Append("BaseMass: " + this.BaseMass + "\n");
                values.Append("TotalMass: " + this.TotalMass + "\n");
                values.Append("PhysicalMass: " + this.PhysicalMass + "\n");
                values.Append("Скорость: " + base.obj.GetShipSpeed() + "\n");
                values.Append("Высота: " + current_height + "\n");
                //values.Append("LinearVelocity: " + base.obj.GetShipVelocities().LinearVelocity + "\n");
                //values.Append("LinearVelocity: " + base.obj.GetShipVelocities().LinearVelocity.Length() + "\n");
                //values.Append("AngularVelocity: " + base.obj.GetShipVelocities().AngularVelocity + "\n");
                //values.Append("AngularVelocity: " + base.obj.GetShipVelocities().AngularVelocity.Length() + "\n");
                return values.ToString();
            }
        }
        public class Navigation
        {
            Cockpit cockpit;
            IMyRemoteControl remote_control;
            IMyCameraBlock camera_course;
            List<IMyThrust> thrusts = new List<IMyThrust>();
            List<IMyGyro> gyros = new List<IMyGyro>();
            public bool compensate { get; private set; } = false;   // компенсируем вес
            public bool compensate_old { get; private set; } = false;   // компенсируем вес предыдущий
            public bool horizont { get; private set; } = false;     // держим горизонтальное направление
            public bool aim_point { get; private set; } = false;     // прицелится на точку
            public bool aim_vector { get; private set; } = false;     // прицелится по вектору
            //public bool fly_course { get; private set; } = false;   // Летим по курсу
            public enum programm : int
            {
                none = 0,
                fly_target = 1,         // лететь на точку
                fly_connect = 2,        // лететь на коннектор
                disconnect_fly = 3,     // отлететь от коннектора
            };
            public static string[] name_programm = { "", "Полет на точку", "Подлет к коннектору", "Отлет от коннектора" };
            programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                aim_point = 1,
                fly = 2,
                fly_horizont = 3,
                fly_curse = 4,
                fly_curse1 = 5,
                braking = 6,
                clr_speed = 7,
            };
            public static string[] name_mode = { "", "Наводимся на точку", "Полет", "Выравниваем по высоте", "На точку грубо", "На точку точно", "Тормоз", "Сброс скорости" };
            mode curent_mode = mode.none;

            public bool target { get; private set; } = false;       // рыскать на цель
            public bool height { get; private set; } = false;
            public bool curse { get; private set; } = false;
            //public bool clear_velocity { get; private set; } = false;
            //public bool clear_velocity_forward { get; private set; } = false;
            //public bool clear_velocity_up { get; private set; } = false;
            //public bool clear_velocity_left { get; private set; } = false;
            public bool curse_target { get; private set; } = false;
            //
            public Matrix CockpitMatrix { get; private set; } // Орентация коробля
            public Vector3D GravityVector { get; private set; } // Вектор гравитации
            public float PhysicalMass { get; private set; } // Физическая масса
            public Vector3D ShipWeight { get; private set; } // Вес коробля с учетом гравитации
            public Vector3D MyPos { get; private set; }
            public Vector3D MyPrevPos { get; private set; }
            public Vector3D VelocityVector { get; private set; }
            //public Vector3D UpVelocityVector { get; private set; }
            //public Vector3D ForwVelocityVector { get; private set; }
            //public Vector3D LeftVelocityVector { get; private set; }
            public double UpVelocity { get; private set; }
            public double ForwVelocity { get; private set; }
            public double LeftVelocity { get; private set; }
            public Vector3D LinearVelocity { get; private set; }
            public Vector3D VelocityThrust { get; private set; }    // компенсация скорости
            //------------------------------------------------
            public double UpThrMax { get; private set; } = 0;
            public double DownThrMax { get; private set; } = 0;
            public double LeftThrMax { get; private set; } = 0;
            public double RightThrMax { get; private set; } = 0;
            public double ForwardThrMax { get; private set; } = 0;
            public double BackwardThrMax { get; private set; } = 0;
            //--------------------------------------------------------
            public double ForwardThrust { get; private set; } = 0;
            public double LeftThrust { get; private set; } = 0;
            public double UpThrust { get; private set; } = 0;
            public double BackwardThrust { get; private set; } = 0;
            public double RightThrust { get; private set; } = 0;
            public double DownThrust { get; private set; } = 0;
            //
            public float? ap_forward { get; private set; } = null;  // Ускорение процент вперед-назад
            public float? ap_left { get; private set; } = null;  // Ускорение процент влево-вправо
            public float? ap_up { get; private set; } = null;  // Ускорение процент вверх-вниз

            //public float up { get; private set; } = 0;
            //public float down { get; private set; } = 0;
            //public float forward { get; private set; } = 0;
            //public float backward { get; private set; } = 0;
            //public float left { get; private set; } = 0;
            //public float right { get; private set; } = 0;
            //-------------------------------------------------------
            public float YawInput { get; private set; } = 0;
            public float YawTarget { get; private set; } = 0;
            public float YawVector { get; private set; } = 0;
            public float RollInput { get; private set; } = 0;
            public float PitchInput { get; private set; } = 0;
            //---------------------------------------------------------
            public double MyPositionHeightCentr { get; private set; }       // высота от центра земли к кораблю
            public double TargetPositionHeightCentr { get; private set; }   // высота от центра земли к точке
            public double DeltaHeight { get; private set; }                 // разница по высоте
            public double MaxSpeedHeight { get; private set; } = 100f;      // макс ускорение по высоте
            public double UpBrakingDistances { get; private set; }          // тормозной путь при подъеме вверх
            public double DownBrakingDistances { get; private set; }        // тормозной путь при движении вниз
            public double ForwardBrakingDistances { get; private set; }     // тормозной путь при движении вперед
            public double BackwardBrakingDistances { get; private set; }    // тормозной путь при движении назад
            public double VectorCurse { get; private set; }                 // длина вектора
            public double MaxSpeedCurse { get; private set; } = 100f;      // макс ускорение по курсу
            public double MinHeight { get; private set; } = 1.0f;           // растояние с которого начинается точный полет
            public double MinCurse { get; private set; } = 1.0f;            // растояние с которого начинается точный полет
            public double MinDeltaHeight { get; private set; } = 100f;      // растояние с которого начинается точный полет
            public double MinDeltaCurse { get; private set; } = 50f;    // растояние с которого начинается точный полет

            public Vector3D? TaskVector { get; private set; } = null;       // Вектор направления (лететь по курсу)
            public Vector3D? TackTarget { get; private set; } = null;       // Точка прицеливания (лететь на точку)
            //public Vector3D? TackTargetCalcPoint { get; private set; }      // Расчетная точка растояния к вектору (от центра к точке прицеливания)

            public string move_ud { get; private set; }
            public string move_fb { get; private set; }

            public Vector3D PlanetCentr = new Vector3D(0.50, 0.50, 0.50);
            public Vector3D Target1 = new Vector3D(53634.1408339977, -26848.4945197565, 11835.781022294); // GPS:Target1:53634.1408339977:-26848.4945197565:11835.781022294:
            public Vector3D Target2 = new Vector3D(54247.1045229673, -28025.4557401103, 9975.66911975904);  // GPS:Target2:54247.1045229673:-28025.4557401103:9975.66911975904:
            public Vector3D TargetConnector = new Vector3D();
            public Navigation(Cockpit cockpit, string NameObj, string NameRemoteControl, string NameCameraCourse)
            {
                this.cockpit = cockpit;
                remote_control = _scr.GridTerminalSystem.GetBlockWithName(NameRemoteControl) as IMyRemoteControl;
                camera_course = _scr.GridTerminalSystem.GetBlockWithName(NameCameraCourse) as IMyCameraBlock;
                _scr.GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusts, r => (r.CustomName.Contains(NameObj)));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros, r => (r.CustomName.Contains(NameObj)));
                _scr.Echo("remote_control: " + ((remote_control != null) ? ("Ок") : ("not block")));
                _scr.Echo("camera_course: " + ((camera_course != null) ? ("Ок") : ("not block")));
                _scr.Echo("thrusts: " + ((thrusts.Count() > 0) ? thrusts.Count().ToString() + "шт." : ("not block")));
                _scr.Echo("gyros: " + ((gyros.Count() > 0) ? gyros.Count().ToString() + "шт." : ("not block")));
            }
            public double GetBrakingDistances(double max_thrusts, double speed)
            {
                double a = (max_thrusts / 1000) * (1 / (cockpit.TotalMass / 1000));
                double t = (0 - speed) / -a; //t = (V - V[0]) / a
                double s = (speed * t) + ((-a) * Math.Pow(t, 2)) / 2; //S = V[0] * t + ( a * t^2 ) / 2
                return s;
            }
            public Vector3D GetTackTargetCalcVector(Vector3D TackTarget)
            {
                Vector3D VectorTarget = PlanetCentr - (Vector3D)TackTarget;
                Vector3D VectorShip = PlanetCentr - MyPos;
                return Vector3D.Reject(VectorShip, Vector3D.Normalize(VectorTarget));
            }
            public Vector3D GetTackTargetCalcPoint(Vector3D TackTarget)
            {
                return GetTackTargetCalcVector(TackTarget) + MyPos;
            }
            public float getPitch()
            {
                return gyros.Select(g => g.Pitch).Average();
            }
            public float getRoll()
            {
                return gyros.Select(g => g.Roll).Average();
            }
            public float getYaw()
            {
                return gyros.Select(g => g.Yaw).Average();
            }
            public void SetGyro(float Yaw, float Pitch, float Roll)
            {
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Yaw = Yaw;
                    gyro.Pitch = Pitch;
                    gyro.Roll = Roll;
                }
            }
            public void GyroOver(bool over)
            {
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Yaw = 0;
                    gyro.Pitch = 0;
                    gyro.Roll = 0;
                    gyro.GyroOverride = over;
                }
            }
            public void SetThrustOverridePersent(float up, float down, float left, float right, float forward, float backward)
            {
                Matrix ThrusterMatrix = new MatrixD();
                foreach (IMyThrust thrust in this.thrusts)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == CockpitMatrix.Up)
                    {
                        thrust.ThrustOverridePercentage = up;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
                    {
                        thrust.ThrustOverridePercentage = down;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
                    {
                        thrust.ThrustOverridePercentage = left;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
                    {
                        thrust.ThrustOverridePercentage = right;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
                    {
                        thrust.ThrustOverridePercentage = forward;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
                    {
                        thrust.ThrustOverridePercentage = backward;
                    }
                }
            }
            public void ClearThrustOverridePersent()
            {
                SetThrustOverridePersent(0f, 0f, 0f, 0f, 0f, 0f);
            }
            public void SetThrustOverridePersent(string axis, float value)
            {
                Matrix ThrusterMatrix = new MatrixD();
                foreach (IMyThrust thrust in this.thrusts)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == CockpitMatrix.Up && axis == "U")
                    {
                        thrust.ThrustOverridePercentage = value;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Down && axis == "D")
                    {
                        thrust.ThrustOverridePercentage = value;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Left && axis == "L")
                    {
                        thrust.ThrustOverridePercentage = value;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Right && axis == "R")
                    {
                        thrust.ThrustOverridePercentage = value;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Forward && axis == "F")
                    {
                        thrust.ThrustOverridePercentage = value;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Backward && axis == "B")
                    {
                        thrust.ThrustOverridePercentage = value;
                    }
                }
            }
            public void Update()
            {
                GravityVector = remote_control.GetNaturalGravity();
                PhysicalMass = remote_control.CalculateShipMass().PhysicalMass;
                ShipWeight = GravityVector * PhysicalMass;
                MyPrevPos = MyPos;
                MyPos = remote_control.GetPosition();
                // Скоростя
                VelocityVector = (MyPos - MyPrevPos) * 6;
                //UpVelocityVector = remote_control.WorldMatrix.Up * Vector3D.Dot(VelocityVector, remote_control.WorldMatrix.Up);
                //ForwVelocityVector = remote_control.WorldMatrix.Forward * Vector3D.Dot(VelocityVector, remote_control.WorldMatrix.Forward);
                //LeftVelocityVector = remote_control.WorldMatrix.Left * Vector3D.Dot(VelocityVector, remote_control.WorldMatrix.Left);
                UpVelocity = Vector3D.Dot(VelocityVector, remote_control.WorldMatrix.Up);
                ForwVelocity = Vector3D.Dot(VelocityVector, remote_control.WorldMatrix.Forward);
                LeftVelocity = Vector3D.Dot(VelocityVector, remote_control.WorldMatrix.Left);
                LinearVelocity = remote_control.GetShipVelocities().LinearVelocity;
                // Компенсация
                ShipWeight = GravityVector * PhysicalMass;
                //HoverThrust = Vector3D.Normalize(GravityVector) * PhysicalMass;
                //HoverThrust = Vector3D.Normalize(GravityVector) * ShipWeight;
                //HoverThrust = GravityVector * ShipWeight;
                //HoverThrust = ShipWeight;
                //VelocityThrust = HoverThrust * LinearVelocity;
                // 
                MyPositionHeightCentr = (PlanetCentr - MyPos).Length();
                // Рыскание на точку
                YawTarget = 0;
                if (TackTarget != null)
                {
                    Vector3D T = Vector3D.Normalize((Vector3D)TackTarget - MyPos);
                    //Рысканием прицеливаемся на точку Target.
                    double tF = T.Dot(remote_control.WorldMatrix.Forward);
                    double tL = T.Dot(remote_control.WorldMatrix.Left);
                    YawTarget = -(float)Math.Atan2(tL, tF);
                    // Контроль высоты
                    TargetPositionHeightCentr = (PlanetCentr - (Vector3D)TackTarget).Length();
                    DeltaHeight = MyPositionHeightCentr - TargetPositionHeightCentr;
                    //SpeedHeightTick = (MyPositionHeightCentr - OldHeight);
                    //SpeedHeight = SpeedHeightTick * 6; // Ускорение по высоте
                    //OldHeight = MyPositionHeightCentr;
                    // тормозной путь
                    if (UpVelocity < 0)
                    {
                        double res = GetBrakingDistances(DownThrMax, Math.Abs(UpVelocity));
                        DownBrakingDistances = res > 0.1f ? res + 100f : 0;
                    }
                    if (UpVelocity > 0) UpBrakingDistances = GetBrakingDistances(UpThrMax, Math.Abs(UpVelocity));
                    //if (SpeedHeight > 0) UpBrakingDistances = GetBrakingDistances(DownThrMax, Math.Abs(SpeedHeight));
                    if (ForwVelocity < 0)
                    {
                        double res = GetBrakingDistances(BackwardThrMax, Math.Abs(ForwVelocity));
                        BackwardBrakingDistances = res;
                    }
                    if (ForwVelocity > 0)
                    {
                        double res = GetBrakingDistances(ForwardThrMax, Math.Abs(ForwVelocity));
                        ForwardBrakingDistances = res > 0.1f ? res + 50f : 0;
                    }
                    // Контроль курса приближения
                    Vector3D VectorShipTarget = GetTackTargetCalcVector((Vector3D)TackTarget);
                    VectorCurse = -VectorShipTarget.Dot(remote_control.WorldMatrix.Forward);
                    //DeltaCurse = (VectorShipTarget).Length();
                    //SpeedCurseTick = (VectorCurse - OldCurse);
                    //SpeedCurse = SpeedCurseTick * 6; // Ускорение по высоте
                    //OldCurse = VectorCurse;
                }
                YawVector = 0;
                if (TaskVector != null)
                {
                    Vector3D T = (Vector3D)TaskVector;
                    //Рысканием прицеливаемся на точку Target.
                    double tF = T.Dot(remote_control.WorldMatrix.Forward);
                    double tL = T.Dot(remote_control.WorldMatrix.Left);
                    YawVector = -(float)Math.Atan2(tL, tF);
                }

                // Орентация коробля
                Matrix CPMatrix = new MatrixD();
                Matrix ThrusterMatrix = new MatrixD();
                remote_control.Orientation.GetMatrix(out CPMatrix);
                CockpitMatrix = CPMatrix;
                // Максимальная эфиктивность двигателей
                UpThrMax = 0;
                DownThrMax = 0;
                LeftThrMax = 0;
                RightThrMax = 0;
                ForwardThrMax = 0;
                BackwardThrMax = 0;
                foreach (IMyThrust thrust in thrusts)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == CockpitMatrix.Up)
                    {
                        UpThrMax += thrust.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
                    {
                        DownThrMax += thrust.MaxEffectiveThrust;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
                    {
                        LeftThrMax += thrust.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
                    {
                        RightThrMax += thrust.MaxEffectiveThrust;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
                    {
                        ForwardThrMax += thrust.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
                    {
                        BackwardThrMax += thrust.MaxEffectiveThrust;
                    }
                }
            }
            public void UpdateProgramm()
            {
                switch (curent_programm)
                {
                    case programm.fly_target:
                        {
                            ProgrammFlyTarget();
                            break;
                        }
                    default:
                        {
                            curent_programm = programm.none;
                            break;
                        };
                };
            }
            public void ProgrammFlyTarget()
            {
                if (DeltaHeight >= -MinHeight && DeltaHeight <= MinHeight && VectorCurse >= -MinCurse && VectorCurse <= MinCurse)
                {
                    // точка достигнута
                    curent_mode = mode.braking;
                }
                else
                {
                    //if (!aim_point && curent_mode != mode.aim_point && curent_mode != mode.none && Math.Abs(YawTarget) > 0.01f)
                    //{
                    //    //compensate = false; //
                    //    //if (remote_control.GetShipSpeed() < 0.1f)
                    //    //{
                    //    //    aim_vector = false;
                    //    //    aim_point = true;
                    //    //    curent_mode = mode.aim_point;
                    //    //}
                    //    curent_mode = mode.none;
                    //}
                    //else
                    //{
                    if (curent_mode == mode.none)
                    {
                        compensate = false; //
                        if (remote_control.GetShipSpeed() < 0.1f)
                        {
                            aim_vector = false;
                            aim_point = true;
                            curent_mode = mode.aim_point;
                        }
                    }
                    if (curent_mode == mode.aim_point)
                    {
                        if (!aim_point)
                        {
                            TaskVector = camera_course.WorldMatrix.Forward; // задали курс
                            aim_vector = true;
                            compensate = true;
                            curent_mode = mode.fly;
                        }
                    }
                    if (curent_mode == mode.fly_horizont && FlyHorizont())
                    {
                        //curent_mode = mode.fly;
                        compensate = false; //
                        if (remote_control.GetShipSpeed() < 0.1f)
                        {
                            aim_vector = false;
                            aim_point = true;
                            curent_mode = mode.aim_point;
                        }
                    }
                    if (curent_mode == mode.fly_curse && FlyCurse(MinCurse + 50))
                    {
                        //curent_mode = mode.fly;
                        compensate = false; //
                        if (remote_control.GetShipSpeed() < 0.1f)
                        {
                            aim_vector = false;
                            aim_point = true;
                            curent_mode = mode.aim_point;
                        }
                    }
                    if (curent_mode == mode.fly_curse1 && FlyCurse(MinCurse))
                    {
                        //curent_mode = mode.fly;
                        compensate = false; //
                        if (remote_control.GetShipSpeed() < 0.1f)
                        {
                            aim_vector = false;
                            aim_point = true;
                            curent_mode = mode.aim_point;
                        }
                    }
                    if (curent_mode == mode.fly)
                    {
                        if (DeltaHeight < -MinHeight || DeltaHeight > MinHeight)
                        {
                            // высота
                            curent_mode = mode.fly_horizont;
                        }
                        else if (VectorCurse < -(MinCurse + 50) || VectorCurse > MinCurse + 50)
                        {
                            // по курсу
                            curent_mode = mode.fly_curse;
                        }
                        else if (VectorCurse < -MinCurse || VectorCurse > MinCurse)
                        {
                            // по курсу точно
                            curent_mode = mode.fly_curse1;
                        }
                    }
                    //}
                }
                if (curent_mode == mode.braking)
                {
                    //ClearThrustOverridePersent();
                    ap_forward = null;
                    ap_left = null;
                    ap_up = null;
                    compensate = false;
                    if (remote_control.GetShipSpeed() < 0.1f)
                    {
                        aim_vector = false;
                        aim_point = false;
                        curent_mode = mode.none;
                        curent_programm = programm.none;
                    }
                }
            }
            // Прицелится
            public void TakeAim()
            {
                if (TackTarget != null) // Точка задана ?
                {
                    horizont = true;
                    if (YawTarget != 0.0f)
                    {
                        if (Math.Abs(YawTarget) <= 0.01f)
                        {
                            aim_point = false;
                        }
                    }
                }
            }
            public bool FlyHorizont()
            {
                ap_forward = null;
                ap_left = null;
                ap_up = null;
                if (DeltaHeight >= -MinHeight && DeltaHeight <= MinHeight)
                {
                    move_ud = "Стоп";
                    compensate = false;
                    return true;
                }
                else
                {
                    if (DeltaHeight < -MinHeight)
                    {
                        // надо вверх
                        if (UpVelocity < -0.5)
                        {
                            // ускорение вниз, надо тормозить
                            move_ud = "надо вверз-тормоз";
                            compensate = false;
                        }
                        else if (UpVelocity > 0.5)
                        {
                            if (UpBrakingDistances < Math.Abs(DeltaHeight))
                            {
                                if (Math.Abs(UpVelocity) < MaxSpeedHeight)
                                {
                                    // Ускоримся вверх
                                    move_ud = "Ускоримся вверх";
                                    compensate = true;
                                    ap_up = (Math.Abs(DeltaHeight) > 50 ? 1.0f : 0.4f);
                                }
                                else
                                {
                                    move_ud = "Летим с комп -вверх";
                                    compensate = true;
                                }
                            }
                            else
                            {
                                move_ud = "Тормозной путь-вверх";
                                compensate = false;
                            }
                        }
                        else
                        {
                            // Ускоримся вверх
                            move_ud = "Ускоримся вверх, скорость 0";
                            compensate = false;
                            ap_up = (Math.Abs(DeltaHeight) > 50 ? 1.0f : 0.4f);
                        }
                    }
                    else if (DeltaHeight > MinHeight)
                    {
                        // надо вниз
                        if (UpVelocity > 0.5)
                        {
                            move_ud = "надо вниз-тормоз";
                            compensate = false;
                        }
                        else if (UpVelocity < -0.5)
                        {
                            if (DownBrakingDistances < Math.Abs(DeltaHeight))
                            {
                                if (Math.Abs(UpVelocity) < MaxSpeedHeight)
                                {
                                    // Ускоримся вниз
                                    move_ud = "Ускоримся вниз";
                                    compensate = true;
                                    ap_up = -(Math.Abs(DeltaHeight) > 50 ? 1.0f : 0.4f);
                                }
                                else
                                {
                                    move_ud = "Летим с комп -вниз";
                                    compensate = true;
                                }
                            }
                            else
                            {
                                move_ud = "Тормозной путь";
                                compensate = false;
                            }
                        }
                        else
                        {
                            move_ud = "Ускоримся вниз,  скорость 0";
                            compensate = true;
                            ap_up = -(Math.Abs(DeltaHeight) > 50 ? 1.0f : 0.4f);
                        }
                    }
                }
                return false;
            }
            public bool FlyCurse(double MinCurse)
            {
                ap_forward = null;
                ap_left = null;
                ap_up = null;
                if (VectorCurse >= -MinCurse && VectorCurse <= MinCurse || (DeltaHeight < -(MinHeight + 10) || DeltaHeight > MinHeight + 10))
                {
                    move_fb = "Стоп";
                    compensate = false;
                    return true;
                }
                else
                {
                    if (VectorCurse < -MinCurse)
                    {
                        // надо вперед
                        if (ForwVelocity < -0.5)
                        {
                            // ускорение назад, надо тормозить
                            move_fb = "надо вперед-тормоз";
                            compensate = false;
                        }
                        else if (ForwVelocity > 0.5)
                        {
                            if (ForwardBrakingDistances < Math.Abs(VectorCurse))
                            {
                                if (Math.Abs(ForwVelocity) < MaxSpeedCurse)
                                {
                                    // Ускоримся вперед
                                    move_fb = "Ускоримся вперед";
                                    compensate = true;
                                    ap_forward = (Math.Abs(VectorCurse) > 50 ? 1.0f : 0.2f);
                                }
                                else
                                {
                                    move_fb = "Летим с комп -вперед";
                                    compensate = true;
                                }
                            }
                            else
                            {
                                move_fb = "Тормозной путь-вперед";
                                compensate = false;
                            }
                        }
                        else
                        {
                            // Ускоримся вперед
                            move_fb = "Ускоримся вперед, скорость 0";
                            compensate = true;
                            ap_forward = (Math.Abs(VectorCurse) > 50 ? 1.0f : 0.2f);
                        }
                    }
                    else if (VectorCurse > MinCurse)
                    {
                        // надо назад
                        if (ForwVelocity > 0.5)
                        {
                            move_fb = "надо назад-тормоз";
                            compensate = false;
                        }
                        else if (ForwVelocity < -0.5)
                        {
                            if (BackwardBrakingDistances < Math.Abs(VectorCurse))
                            {
                                if (Math.Abs(ForwVelocity) < MaxSpeedCurse)
                                {
                                    // Ускоримся назад
                                    move_fb = "Ускоримся назад";
                                    compensate = true;
                                    ap_forward = -(Math.Abs(VectorCurse) > 50 ? 1.0f : 0.2f);
                                }
                                else
                                {
                                    move_fb = "Летим с комп -назад";
                                    compensate = true;
                                }
                            }
                            else
                            {
                                move_fb = "Тормозной путь-назад";
                                compensate = false;
                            }
                        }
                        else
                        {
                            move_fb = "Ускоримся назад,  скорость 0";
                            compensate = true;
                            ap_forward = -(Math.Abs(VectorCurse) > 50 ? 1.0f : 0.2f);
                        }
                    }
                }
                return false;
            }
            public void UpdateThrust()
            {
                ForwardThrust = (ShipWeight).Dot(remote_control.WorldMatrix.Forward);
                LeftThrust = (ShipWeight).Dot(remote_control.WorldMatrix.Left);
                UpThrust = (ShipWeight).Dot(remote_control.WorldMatrix.Up);
                BackwardThrust = -ForwardThrust;
                RightThrust = -LeftThrust;
                DownThrust = -UpThrust;
                float up = (float)(UpThrust / UpThrMax);
                float down = (float)(DownThrust / DownThrMax);
                float forward = (float)(ForwardThrust / ForwardThrMax);
                float backward = (float)(BackwardThrust / BackwardThrMax);
                float left = (float)(LeftThrust / LeftThrMax);
                float right = (float)(RightThrust / RightThrMax);
                if (ap_forward > 0f) { backward += (float)ap_forward; forward = 0f; } else if (ap_forward < 0f) { forward += (float)ap_forward; backward = 0f; }
                if (ap_left > 0f) { right += (float)ap_left; left = 0f; } else if (ap_left < 0f) { left += (float)ap_left; right = 0f; }
                if (ap_up > 0f) { down += (float)ap_up; up = 0f; } else if (ap_up < 0f) { up += (float)ap_up; down = 0f; }
                if (compensate)
                {
                    SetThrustOverridePersent(up, down, left, right, forward, backward);
                }
                else
                {
                    if (ap_forward != null)
                    {
                        SetThrustOverridePersent(ap_forward > 0f ? "B" : "F", Math.Abs(ap_forward > 0f ? (float)backward : (float)forward));
                    }
                    if (ap_left != null)
                    {
                        SetThrustOverridePersent(ap_left > 0f ? "R" : "L", Math.Abs(ap_left > 0f ? (float)right : (float)left));
                    }
                    if (ap_up != null)
                    {
                        SetThrustOverridePersent(ap_up > 0f ? "D" : "U", Math.Abs(ap_up > 0f ? (float)down : (float)up));
                    }
                }
            }
            public void Horizon()
            {
                Vector3D GravNorm = Vector3D.Normalize(GravityVector);
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(remote_control.WorldMatrix.Forward);
                double gL = GravNorm.Dot(remote_control.WorldMatrix.Left);
                double gU = GravNorm.Dot(remote_control.WorldMatrix.Up);
                //Получаем сигналы по тангажу и крены операцией atan2
                RollInput = (float)Math.Atan2(gL, -gU); // крен
                PitchInput = -(float)Math.Atan2(gF, -gU); // тангаж
                if (!aim_point && !aim_vector)
                {
                    YawInput = 0;
                    if (remote_control.IsUnderControl)
                    {
                        YawInput = remote_control.RotationIndicator.Y;
                    }
                    else if (cockpit.IsUnderControl)
                    {
                        YawInput = cockpit._obj.RotationIndicator.Y;
                    }
                }
                else if (aim_point)
                {
                    YawInput = YawTarget;
                }
                else if (aim_vector)
                {
                    YawInput = YawVector;
                }
                SetGyro(YawInput, PitchInput, RollInput);
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ   : " + Math.Round(remote_control.GetShipSpeed(), 2) + "\n");
                values.Append("ГОРИЗОНТ   : " + (horizont ? green.ToString() : red.ToString()) + ",  T : " + (aim_point ? green.ToString() : red.ToString()) + ",  V : " + (aim_vector ? green.ToString() : red.ToString()) + "\n");
                values.Append("КОМПЕНСАЦИЯ: " + (compensate ? green.ToString() : red.ToString()) + "\n");
                values.Append("ПРОГРАММА: " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП: " + name_mode[(int)curent_mode] + "\n");
                //values.Append("К.ВЫСОТЫ   : " + (height ? "ВКЛ" : "ВЫК") + "\n");
                //values.Append("К.КУРСА    : " + (curse ? "ВКЛ" : "ВЫК") + "\n");
                values.Append("T0: " + PText.GetGPS("TargetConnector", TargetConnector) + "\n");
                values.Append("T1: " + PText.GetGPS("Target1", Target1) + "\n");
                values.Append("T2: " + PText.GetGPS("Target2", Target2) + "\n");
                return values.ToString();
            }
            public string TextTEST()
            {
                StringBuilder values = new StringBuilder();
                //values.Append("GravityVector: " + Math.Round(GravityVector.Length(), 2) + "\n");
                //values.Append("PhysicalMass: " + Math.Round(PhysicalMass, 2) + "\n");
                //values.Append("ShipWeight: " + Math.Round(ShipWeight.Length(), 2) + "\n");
                //values.Append("LeftV : " + Math.Round(LeftVelocityVector.Length(), 2) + "\n");
                //values.Append("UpVelocity: " + Math.Round(UpVelocity) + "\n");
                //values.Append("ForwVelocity: " + Math.Round(ForwVelocity) + "\n");
                //values.Append("LeftVelocity: " + Math.Round(LeftVelocity) + "\n");
                values.Append("move_horizont : " + move_ud + "ap_up      :" + ap_up + "\n");
                values.Append("move_curse    : " + move_fb + "ap_forward :" + ap_forward + "\n");
                values.Append("-----------------------------------------------\n");
                //values.Append("DeltaCurse  : " + Math.Round(DeltaCurse, 2) + "\n");
                values.Append("VectorCurse  : " + Math.Round(VectorCurse, 2) + "\n");
                values.Append("ForwVelocity     : " + Math.Round(ForwVelocity, 2) + "\n");
                values.Append("ForwardBrakingDistances  : " + Math.Round(ForwardBrakingDistances, 2) + "\n");
                values.Append("BackwardBrakingDistances  : " + Math.Round(BackwardBrakingDistances, 2) + "\n");
                //values.Append("SpeedCurse     : " + Math.Round(SpeedCurse, 2) + "\n");
                //values.Append("ForwardPower    : " + ForwardPower + "\n");
                values.Append("-----------------------------------------------\n");
                values.Append("DeltaHeight    : " + Math.Round(DeltaHeight, 2) + "\n");
                values.Append("UpVelocity     : " + Math.Round(UpVelocity, 2) + "\n");
                //values.Append("UpPower    : " + UpPower + "\n");
                values.Append("UpBrakingDistances  : " + Math.Round(UpBrakingDistances, 2) + "\n");
                values.Append("DownBrakingDistances  : " + Math.Round(DownBrakingDistances, 2) + "\n");
                //values.Append("Down-торм:     : " + Math.Round(DownBrakingDistances, 2) + "\n");
                //values.Append("Up-торм        : " + Math.Round(UpBrakingDistances, 2) + "\n");
                values.Append("-----------------------------------------------\n");
                values.Append("Yaw-target  : " + Math.Round(YawTarget, 2) + "\n");
                values.Append("Yaw-curse   : " + Math.Round(YawVector, 2) + "\n");
                values.Append("Yaw         : " + Math.Round(YawInput, 2) + "\n");
                values.Append("Roll        : " + Math.Round(RollInput, 2) + "\n");
                values.Append("Pitch       : " + Math.Round(PitchInput, 2) + "\n");
                values.Append("-----------------------------------------------\n");

                values.Append("-----------------------------------------------\n");
                values.Append("UP       : " + PText.GetThrust((float)UpThrust) + "\t, MAX : " + PText.GetThrust((float)UpThrMax) + "\n");
                values.Append("DOWN     : " + PText.GetThrust((float)DownThrust) + "\t, MAX : " + PText.GetThrust((float)DownThrMax) + "\n");
                values.Append("Forward  : " + PText.GetThrust((float)ForwardThrust) + "\t, MAX : " + PText.GetThrust((float)ForwardThrMax) + "\n");
                values.Append("Backward : " + PText.GetThrust((float)BackwardThrust) + "\t, MAX : " + PText.GetThrust((float)BackwardThrMax) + "\n");
                values.Append("Left     : " + PText.GetThrust((float)LeftThrust) + "\t, MAX : " + PText.GetThrust((float)LeftThrMax) + "\n");
                values.Append("Right    : " + PText.GetThrust((float)RightThrust) + "\t, MAX : " + PText.GetThrust((float)RightThrMax) + "\n");

                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "base_connection":
                        TargetConnector = remote_control.GetPosition();
                        break;
                    case "compensate_on":
                        compensate = true;
                        horizont = true;
                        target = false;
                        break;
                    case "compensate_off":
                        TackTarget = null;
                        aim_vector = false;
                        aim_point = false;
                        compensate = false;
                        horizont = false;
                        target = false;
                        height = false;
                        curse = false;
                        curse_target = false;
                        curent_programm = programm.none;
                        curent_mode = mode.none;
                        remote_control.DampenersOverride = true;
                        ap_forward = null;
                        ap_left = null;
                        ap_up = null;
                        ClearThrustOverridePersent();
                        break;
                    case "horizont_on":
                        horizont = true;
                        break;
                    case "horizont_off":
                        horizont = false;
                        break;
                    case "horizont":
                        if (horizont)
                        {
                            horizont = false;
                        }
                        else
                        {
                            horizont = true;
                        }
                        break;
                    case "T1":
                        //clear_velocity = false;
                        //Target(Target1);
                        break;
                    case "T2":
                        //clear_velocity = false;
                        //Target(Target2);
                        break;
                    case "TH1":
                        //TargetHeight(Target1);
                        break;
                    case "TH2":
                        //clear_velocity = false;
                        //TargetHeight(Target2);
                        break;
                    case "TC1":
                        //clear_velocity = false;
                        //TargetCurse(Target1);
                        break;
                    case "TC2":
                        //clear_velocity = false;
                        //TargetCurse(Target2);
                        break;
                    case "FT1":
                        //clear_velocity = false;
                        TackTarget = Target1;
                        curent_programm = programm.fly_target;
                        break;
                    case "FT2":
                        //clear_velocity = false;
                        TackTarget = Target2;
                        curent_programm = programm.fly_target;
                        break;
                    case "FT3":
                        //clear_velocity = false;
                        TackTarget = TargetConnector;
                        curent_programm = programm.fly_target;
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Обновим состояние навигации
                    Update();
                    GyroOver(horizont);
                    if (horizont)
                    {
                        Horizon();
                    }
                    if (aim_point)
                    {
                        TakeAim();
                    }
                    if (curent_programm != programm.none)
                    {
                        UpdateProgramm();
                    }
                    if (!compensate && compensate_old)
                    {
                        ClearThrustOverridePersent();
                        remote_control.DampenersOverride = true;
                    }
                    if (compensate && !compensate_old)
                    {
                        remote_control.DampenersOverride = false;
                    }
                    UpdateThrust();
                    compensate_old = compensate;
                }
            }
        }
    }
}


// 3. Чем ближе к точке убирать чувсвительность гироскопа
// T0: GPS:TargetConnector:53567.3705051079:-26769.2952025845:11925.7372278272:
// 
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
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRageMath;
/// <summary>
/// v4.0
/// </summary>
namespace KROTIK_A1M_NAV_4
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
            static public string GetGPS(string name, Vector3D target, int zero)
            {
                return "GPS:" + name + ":" + Math.Round(target.GetDim(0), zero) + " : " + Math.Round(target.GetDim(1), zero) + " : " + Math.Round(target.GetDim(2), zero) + ":\n";
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
            navigation = new Navigation(cockpit, connector, NameObj, NameRemoteControl, NameCameraCourse);
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
            public long? getRemoteConnector()
            {
                List<IMyShipConnector> list_conn = new List<IMyShipConnector>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn);
                foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList())
                {
                    if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 2) return conn.EntityId;
                }
                return null;
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
            Connector connector;
            IMyRemoteControl remote_control;
            IMyCameraBlock camera_course;
            List<IMyThrust> thrusts = new List<IMyThrust>();
            List<IMyGyro> gyros = new List<IMyGyro>();
            public bool compensate { get; private set; } = false;       // компенсируем вес
            public bool compensate_old { get; private set; } = false;   // компенсируем вес предыдущий
            public bool horizont { get; private set; } = false;         // держим горизонтальное направление
            public bool aim_point { get; private set; } = false;        // прицелится на точку
            public bool aim_vector { get; private set; } = false;       // прицелится по вектору
            public bool fly_target { get; private set; } = false;       // Летим на точку
            public bool control_horizont { get; private set; } = false; // контролируем горизонт
            public bool control_curs { get; private set; } = false;     // контролируем по курсу
            public Vector3D HoverThrust { get; private set; } = new Vector3D();
            public bool hover { get; private set; } = false;            // летим в режиме "Hover"
            public double Kv { get; private set; } = 1;                 //Коэффициент Kv, характеризующий пропорциональную зависимость между разностью требуемой и текущей высот и необходимой вертикальной скоростью
            public double Ka { get; private set; } = 5;                 //Коэффициент Ka, характеризующий пропорциональную зависимость между разностью требуемой и текущей верт. скоростей и желаемым ускорением
            public double MinHover { get; private set; } = 30;         // Минимальный диапазон включения или откл режима Hover
            public enum programm : int
            {
                none = 0,
                fly_targets = 1,        // полет по точкам
                fly_connect = 2,        // лететь на коннектор
                disconnect_fly = 3,     // отлететь от коннектора
            };
            public static string[] name_programm = { "", "Полет по точкам", "Подлет к коннектору", "Отлет от коннектора" };
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
            };
            public static string[] name_mode = { "", "ЦЕЛИМСЯ", "ЛЕТИМ", "ВЫСОТА", "КУРС БЫСТРО", "КУРС ТОЧНО", "ОСТАНОВ", };
            mode curent_mode = mode.none;
            //
            public Vector3D WM_Up { get; private set; } // WorldMatrix.Up - корабля
            public Vector3D WM_Down { get; private set; } // WorldMatrix.Down - корабля
            public Vector3D WM_Forward { get; private set; } // WorldMatrix.Forward - корабля
            public Vector3D WM_Backward { get; private set; } // WorldMatrix.Backward - корабля
            public Vector3D WM_Left { get; private set; } // WorldMatrix.Left - корабля
            public Vector3D WM_Right { get; private set; } // WorldMatrix.Right - корабля
            //
            public Matrix CockpitMatrix { get; private set; } // Орентация коробля
            public Vector3D GravityVector { get; private set; } // Вектор гравитации
            public float PhysicalMass { get; private set; } // Физическая масса
            public Vector3D ShipWeight { get; private set; } // Вес коробля с учетом гравитации
            public Vector3D MyPos { get; private set; }
            public Vector3D MyPrevPos { get; private set; }
            public Vector3D VelocityVector { get; private set; }
            public double UpVelocity { get; private set; }      // скорость up-down
            public double ForwVelocity { get; private set; }    // скорость forw-back
            public double LeftVelocity { get; private set; }    // скорость left-right
            public Vector3D LinearVelocity { get; private set; }
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
            //-------------------------------------------------------
            public float? ap_forward { get; private set; } = null;  // Ускорение процент вперед-назад
            public float? ap_left { get; private set; } = null;  // Ускорение процент влево-вправо
            public float? ap_up { get; private set; } = null;  // Ускорение процент вверх-вниз
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
            public float TaskHeightSpeed { get; private set; }              // разница по высоте
            public double MaxSpeedHeight { get; private set; } = 100f;      // макс ускорение по высоте
            public double MinHeight { get; private set; } = 1.0f;           // мин разница высоты (цель достигнута)
            public double MinDeltaHeight { get; private set; } = 100f;      // мин высота + тормозному пути
            //---------------------------------------------------------
            public double DeltaCurse { get; private set; }                  // разница по курсу
            public float TaskCurseSpeed { get; private set; }               // разница по высоте
            public double MaxSpeedCurse { get; private set; } = 50f;        // макс ускорение по курсу
            public double MinCurse { get; private set; } = 1.0f;            // мин разница по курсу (цель достигнута)
            public double AccuracyDeltaCurse { get; private set; } = 10f;   // точность подлета к цели (влияет на точность оставшегося расстояния до цели)
            public double MinDeltaCurse { get; private set; } = 100f;       // мин растояние по курсу точки + тормозному пути
            //---------------------------------------------------------
            public double UpBrakingDistances { get; private set; }          // тормозной путь при подъеме вверх
            public double DownBrakingDistances { get; private set; }        // тормозной путь при движении вниз
            public double ForwardBrakingDistances { get; private set; }     // тормозной путь при движении вперед
            public double BackwardBrakingDistances { get; private set; }    // тормозной путь при движении назад
            //---------------------------------------------------------
            public Vector3D? TaskVector { get; private set; } = null;       // Вектор направления (лететь по курсу)
            public Vector3D? TackTarget { get; private set; } = null;       // Точка прицеливания (лететь на точку)
            public double? TackHeight { get; private set; } = null;         // Заданая высота полета (от центра земли)
            public double? TackCurse { get; private set; } = null;         // Заданый курс полета ()
            public string move_ud { get; private set; } // для теста - убрать
            public string move_fb { get; private set; } // для теста - убрать

            public Vector3D PlanetCentr = new Vector3D(0.50, 0.50, 0.50);
            public Vector3D Target1 = new Vector3D(53634.1408339977, -26848.4945197565, 11835.781022294); // GPS:Target1:53634.1408339977:-26848.4945197565:11835.781022294:
            public Vector3D Target2 = new Vector3D(54247.1045229673, -28025.4557401103, 9975.66911975904);  // GPS:Target2:54247.1045229673:-28025.4557401103:9975.66911975904:
            public Vector3D TargetConnector = new Vector3D(53567.3682644915, -26769.3032342576, 11925.7283974891); //GPS:T0:53567.3682644915:-26769.3032342576:11925.7283974891:
            public float dist_h_conn { get; private set; } = 50f;  // дист от коннектора по горизонтали (+ для космоса)
            public float dist_v_conn { get; private set; } = 200f;  // дист от коннектора по вертикали
            public class ConnectorBase
            {
                public long? id { get; set; }
                public Vector3D point = new Vector3D();
                public Vector3D vector = new Vector3D();
                public bool? position = false;
                public bool? load = false;
            }
            ConnectorBase connector_base1 = new ConnectorBase()
            {
                id = 79876025562437155,
                point = new Vector3D(53567.3705051079, -26769.2952025845, 11925.7372278272),
                vector = new Vector3D(0, 0.41, 0.91),
                position = false,
                load = false
            };
            ConnectorBase connector_base2 = new ConnectorBase();
            ConnectorBase current_connector_base = null;   // текущий коннектор на который летим

            int? current_point_index = null;
            int count = 0;
            List<Vector3D> current_list_points = new List<Vector3D>();
            public Navigation(Cockpit cockpit, Connector connector, string NameObj, string NameRemoteControl, string NameCameraCourse)
            {
                this.cockpit = cockpit;
                this.connector = connector;
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
            //----------------------------------------
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
            //-----------------------------------------
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
            //--------------------------------------
            public void UpdateCalc()
            {
                WM_Up = remote_control.WorldMatrix.Up;
                WM_Down = remote_control.WorldMatrix.Down;
                WM_Forward = remote_control.WorldMatrix.Forward;
                WM_Backward = remote_control.WorldMatrix.Backward;
                WM_Left = remote_control.WorldMatrix.Left;
                WM_Right = remote_control.WorldMatrix.Right;

                GravityVector = remote_control.GetNaturalGravity();
                PhysicalMass = remote_control.CalculateShipMass().PhysicalMass;
                ShipWeight = GravityVector * PhysicalMass;
                MyPrevPos = MyPos;
                MyPos = remote_control.GetPosition();
                // Скоростя
                VelocityVector = (MyPos - MyPrevPos) * 6;
                UpVelocity = Vector3D.Dot(VelocityVector, WM_Up);
                ForwVelocity = Vector3D.Dot(VelocityVector, WM_Forward);
                LeftVelocity = Vector3D.Dot(VelocityVector, WM_Left);
                LinearVelocity = remote_control.GetShipVelocities().LinearVelocity;
                // Компенсация
                ShipWeight = GravityVector * PhysicalMass;
                HoverThrust = Vector3D.Normalize(GravityVector) * PhysicalMass * (-DeltaHeight * Kv - UpVelocity) * Ka; //UpVelocity
                // 
                MyPositionHeightCentr = (PlanetCentr - MyPos).Length();
                // Рыскание на точку
                //YawTarget = 0;
                if (TackTarget != null)
                {
                    Vector3D T = Vector3D.Normalize((Vector3D)TackTarget - MyPos);
                    //Рысканием прицеливаемся на точку Target.
                    double tF = T.Dot(WM_Forward);
                    double tL = T.Dot(WM_Left);
                    YawTarget = -(float)Math.Atan2(tL, tF);
                    // Контроль высоты
                    TargetPositionHeightCentr = (PlanetCentr - (Vector3D)TackTarget).Length();
                    DeltaHeight = MyPositionHeightCentr - TargetPositionHeightCentr;
                    // Контроль курса приближения
                    Vector3D VectorShipTarget = GetTackTargetCalcVector((Vector3D)TackTarget);
                    DeltaCurse = -VectorShipTarget.Dot(WM_Forward);
                    //if (DeltaCurse < -10f) { DeltaCurse += AccuracyDeltaCurse; }
                }
                // тормозной путь
                if (UpVelocity < 0)
                {
                    double res = GetBrakingDistances(DownThrMax, Math.Abs(UpVelocity));
                    DownBrakingDistances = res > 0.1f ? res + (Math.Abs(DeltaHeight) > MinDeltaHeight ? MinDeltaHeight : 0) : 0;
                }
                if (UpVelocity > 0)
                {
                    double res = GetBrakingDistances(UpThrMax, Math.Abs(UpVelocity));
                    UpBrakingDistances = res > 0.1f ? res + (Math.Abs(DeltaHeight) > MinDeltaHeight ? MinDeltaHeight : 0) : 0;
                }
                if (ForwVelocity < 0)
                {
                    double res = GetBrakingDistances(BackwardThrMax, Math.Abs(ForwVelocity));
                    BackwardBrakingDistances = res;
                }
                if (ForwVelocity > 0)
                {
                    double res = GetBrakingDistances(ForwardThrMax, Math.Abs(ForwVelocity));
                    ForwardBrakingDistances = res > 0.1f ? res + (Math.Abs(DeltaCurse) > MinDeltaCurse ? MinDeltaCurse : 0) : 0;
                }
                //YawVector = 0;
                if (TaskVector != null)
                {
                    Vector3D T = (Vector3D)TaskVector;
                    //Рысканием прицеливаемся на точку Target.
                    double tF = T.Dot(WM_Forward);
                    double tL = T.Dot(WM_Left);
                    YawVector = -(float)Math.Atan2(tL, tF);
                }
                //
                // Определим скорость
                TaskHeightSpeed = (float)Math.Sqrt(2 * Math.Abs(DeltaHeight) * GravityVector.Length()) / 2;
                MaxSpeedHeight = TaskHeightSpeed;
                TaskCurseSpeed = (float)Math.Sqrt(2 * Math.Abs(DeltaCurse) * GravityVector.Length()) / 2;
                MaxSpeedCurse = TaskCurseSpeed;
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
            public void UpdateThrust()
            {

                ForwardThrust = (ShipWeight).Dot(WM_Forward);
                BackwardThrust = -ForwardThrust;
                float forward = (float)(ForwardThrust / ForwardThrMax);
                float backward = (float)(BackwardThrust / BackwardThrMax);
                if (ap_forward > 0f) { backward += (float)ap_forward; forward = 0f; } else if (ap_forward < 0f) { forward -= (float)ap_forward; backward = 0f; }

                LeftThrust = (ShipWeight).Dot(WM_Left);
                RightThrust = -LeftThrust;
                float left = (float)(LeftThrust / LeftThrMax);
                float right = (float)(RightThrust / RightThrMax);
                if (ap_left > 0f) { right += (float)ap_left; left = 0f; } else if (ap_left < 0f) { left += (float)ap_left; right = 0f; }

                UpThrust = (ShipWeight).Dot(WM_Up);
                DownThrust = -UpThrust;
                float up = (float)(UpThrust / UpThrMax);
                float down = (float)(DownThrust / DownThrMax);
                if (ap_up > 0f) { down += (float)ap_up; up = 0f; } else if (ap_up < 0f) { up += (float)ap_up; down = 0f; }

                if (compensate)
                {
                    if (hover)
                    {
                        UpThrust = (ShipWeight + HoverThrust).Dot(WM_Up);
                        DownThrust = -UpThrust;
                        up = (float)(UpThrust / UpThrMax);
                        down = (float)(DownThrust / DownThrMax);
                    }
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
            public void UpdateProgramm()
            {
                switch (curent_programm)
                {
                    case programm.fly_connect:
                        {
                            FlyConnector();
                            break;
                        }
                        //default:
                        //    {
                        //        curent_programm = programm.none;
                        //        break;
                        //    };
                };
            }
            public void FlyHorizont()
            {
                ap_up = null;
                if (DeltaHeight >= -MinHeight && DeltaHeight <= MinHeight) //
                {
                    move_ud = "Стоп";
                    compensate = false; // Тормоз
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
                            compensate = false; // Тормоз
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
                                    ap_up = (Math.Abs(DeltaHeight) > 100f ? 1.0f : 0.4f);
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
                                compensate = false; // Тормоз
                            }
                        }
                        else
                        {
                            // Ускоримся вверх
                            move_ud = "Ускоримся вверх, скорость 0";
                            compensate = true;
                            ap_up = (Math.Abs(DeltaHeight) > 100f ? 1.0f : 0.4f);
                        }
                    }
                    else if (DeltaHeight > MinHeight)
                    {
                        // надо вниз
                        if (UpVelocity > 0.5)
                        {
                            move_ud = "надо вниз-тормоз";
                            compensate = false; // Тормоз
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
                                    ap_up = -(Math.Abs(DeltaHeight) > 100f ? 1.0f : 0.4f);
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
                                compensate = false; // Тормоз
                            }
                        }
                        else
                        {
                            move_ud = "Ускоримся вниз,  скорость 0";
                            compensate = true;
                            ap_up = -(Math.Abs(DeltaHeight) > 100f ? 1.0f : 0.4f);
                        }
                    }
                }
            }
            public void FlyCurse()
            {
                ap_forward = null;
                if (DeltaCurse >= -MinCurse && DeltaCurse <= MinCurse)
                {
                    move_fb = "Стоп";
                    compensate = false; // Тормоз
                }
                else
                {
                    if (DeltaCurse < -MinCurse)
                    {
                        // надо вперед
                        if (ForwVelocity < -0.5)
                        {
                            // ускорение назад, надо тормозить
                            move_fb = "надо вперед-тормоз";
                            compensate = false; // Тормоз
                        }
                        else if (ForwVelocity > 0.5)
                        {
                            if (ForwardBrakingDistances < Math.Abs(DeltaCurse))
                            {
                                if (Math.Abs(ForwVelocity) < MaxSpeedCurse)
                                {
                                    // Ускоримся вперед
                                    move_fb = "Ускоримся вперед";
                                    compensate = true;
                                    ap_forward = (Math.Abs(DeltaCurse) > 100f ? 1.0f : 0.2f);
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
                                compensate = false; // Тормоз
                            }
                        }
                        else
                        {
                            // Ускоримся вперед
                            move_fb = "Ускоримся вперед, скорость 0";
                            compensate = true;
                            ap_forward = (Math.Abs(DeltaCurse) > 100f ? 1.0f : 0.2f);
                        }
                    }
                    else if (DeltaCurse > MinCurse)
                    {
                        // надо назад
                        if (ForwVelocity > 0.5)
                        {
                            move_fb = "надо назад-тормоз";
                            compensate = false; // Тормоз
                        }
                        else if (ForwVelocity < -0.5)
                        {
                            if (BackwardBrakingDistances < Math.Abs(DeltaCurse))
                            {
                                if (Math.Abs(ForwVelocity) < MaxSpeedCurse)
                                {
                                    // Ускоримся назад
                                    move_fb = "Ускоримся назад";
                                    compensate = true;
                                    ap_forward = -(Math.Abs(DeltaCurse) > 100f ? 1.0f : 0.2f);
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
                                compensate = false; // Тормоз
                            }
                        }
                        else
                        {
                            move_fb = "Ускоримся назад,  скорость 0";
                            compensate = true;
                            ap_forward = -(Math.Abs(DeltaCurse) > 100f ? 1.0f : 0.2f);
                        }
                    }
                }
            }
            public void FlyTarget()
            {
                ap_forward = null;
                ap_left = null;
                ap_up = null;
                control_curs = false;
                control_horizont = false;
                hover = false;
                if ((DeltaHeight >= -MinHeight && DeltaHeight <= MinHeight && DeltaCurse >= -MinCurse && DeltaCurse <= MinCurse) || TackTarget == null)
                {
                    // точка достигнута
                    compensate = false; // Тормоз
                    if (remote_control.GetShipSpeed() < 0.01f)
                    {
                        aim_vector = false;
                        aim_point = false;
                        hover = false;
                        fly_target = false;
                    }
                }
                else
                {
                    if (DeltaHeight < -(MinHeight + MinHover) || DeltaHeight > (MinHeight + MinHover))
                    {
                        control_horizont = true;
                        hover = false;
                    }
                    else
                    {
                        if (Math.Abs(YawTarget) > 0.01f && (DeltaCurse < -(MinCurse + 10) || DeltaCurse > (MinCurse + 10)))
                        {
                            compensate = false; // Тормоз
                            aim_point = true;
                        }
                        // Летим или тормозим
                        //if (compensate || (!compensate && Math.Abs(LeftVelocity) < 0.01f))
                        //{
                            hover = true;
                            if (DeltaCurse < -MinCurse || DeltaCurse > MinCurse)
                            {
                                control_curs = true;
                            }
                        //}
                    }
                }
            }
            public bool FlyPoints(bool clear)
            {
                if (current_list_points != null && current_list_points.Count() > 0)
                {
                    if (current_point_index != null && !fly_target)
                    {
                        count++;
                        if (count > 5)
                        {
                            current_point_index--;
                            count = 0;
                        }
                        // долител
                        if (current_point_index < 0)
                        {
                            fly_target = false;
                            if (clear)
                            {
                                current_point_index = null;
                                current_list_points.Clear();
                            }
                            return true;
                        }
                        else
                        {
                            TackTarget = current_list_points[(int)current_point_index];
                            fly_target = true;
                            if (current_point_index == 0)
                            {
                                MinCurse = 1.0f;
                            }
                            else
                            {
                                MinCurse = 2.0f;
                            }

                            MaxSpeedCurse = 50.0f;
                        }
                    }
                    else if (current_point_index == null && !fly_target)
                    {
                        current_point_index = current_list_points.Count() - 1;
                        TackTarget = current_list_points[(int)current_point_index];
                        fly_target = true;
                        MinCurse = 2.0f;
                        MaxSpeedCurse = 50.0f;
                    }
                }
                return false;
            }
            public void FlyConnector()
            {
                if (current_connector_base != null)
                {
                    if (FlyPoints(false))
                    {
                        if (!fly_target)
                        {
                            // Зададим коннектор
                            TackTarget = current_connector_base.point;
                            fly_target = true;
                            MinCurse = 1.0f;
                            MaxSpeedCurse = 2.0f;
                        }
                        if (this.connector.Connectable)
                        {
                            fly_target = false;
                            curent_programm = programm.none;
                            current_point_index = null;
                            current_list_points.Clear();
                            compensate = false; // Тормоз
                            aim_vector = false;
                            aim_point = false;
                            hover = false;
                        }
                    }
                }
            }
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
                            TaskVector = WM_Forward; //camera_course.WorldMatrix.Forward; // задали курс
                            aim_vector = true;
                        }
                    }
                }
            }
            public void Horizon()
            {
                Vector3D GravNorm = Vector3D.Normalize(GravityVector);
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(WM_Forward);
                double gL = GravNorm.Dot(WM_Left);
                double gU = GravNorm.Dot(WM_Up);
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
            public void Connector1()
            {
                current_connector_base = connector_base1;
                Vector3D GravNorm = Vector3D.Normalize(GravityVector);
                Vector3D p0 = current_connector_base.point - (current_connector_base.vector * dist_h_conn);
                Vector3D p1 = p0 + (-GravNorm * dist_v_conn);
                current_point_index = null;
                current_list_points.Clear();
                current_list_points.Add(p0);
                current_list_points.Add(p1);
                curent_programm = programm.fly_connect;
            }
            //-------------------------------------
            public void SetPointConnection(ref ConnectorBase connector_base)
            {
                if (this.connector.Connected)
                {
                    connector_base.id = this.connector.getRemoteConnector();
                    Vector3D GravNorm = Vector3D.Normalize(GravityVector);
                    double vc = GravNorm.Dot(WM_Forward);
                    connector_base.point = remote_control.GetPosition();
                    connector_base.vector = WM_Forward;
                    connector_base.position = Math.Abs(vc) < 0.01f ? false : true;
                }
            }

            public void DrawLocalVectors()
            {
                Vector3D LocX = new Vector3D(remote_control.WorldMatrix.M11, remote_control.WorldMatrix.M12, remote_control.WorldMatrix.M13) + remote_control.GetPosition();
                Vector3D LocY = new Vector3D(remote_control.WorldMatrix.M21, remote_control.WorldMatrix.M22, remote_control.WorldMatrix.M23) + remote_control.GetPosition();
                Vector3D LocZ = new Vector3D(remote_control.WorldMatrix.M31, remote_control.WorldMatrix.M32, remote_control.WorldMatrix.M33) + remote_control.GetPosition();
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ    : " + Math.Round(remote_control.GetShipSpeed(), 2) + "\n");
                values.Append("ГОРИЗОНТ    : " + (horizont ? green.ToString() : red.ToString()) + ",  T : " + (aim_point ? green.ToString() : red.ToString()) + ",  V : " + (aim_vector ? green.ToString() : red.ToString()) + "\n");
                values.Append("КОМПЕНСАЦИЯ : " + (compensate ? green.ToString() : red.ToString()) + "\n");
                values.Append("НА ТОЧКУ : " + (fly_target ? green.ToString() : red.ToString()) + ", H : " + (control_horizont ? green.ToString() : red.ToString()) + ", C : " + (control_curs ? green.ToString() : red.ToString()) + "\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                //values.Append("T0: " + PText.GetGPS("TargetConnector", TargetConnector) + "\n");
                //values.Append("T1: " + PText.GetGPS("Target1", Target1) + "\n");
                //values.Append("T2: " + PText.GetGPS("Target2", Target2) + "\n");
                return values.ToString();
            }
            public string TextTEST()
            {
                //Vector3D LocX = new Vector3D(remote_control.WorldMatrix.M11, remote_control.WorldMatrix.M12, remote_control.WorldMatrix.M13) + remote_control.GetPosition();
                //Vector3D LocY = new Vector3D(remote_control.WorldMatrix.M21, remote_control.WorldMatrix.M22, remote_control.WorldMatrix.M23) + remote_control.GetPosition();
                //Vector3D LocZ = new Vector3D(remote_control.WorldMatrix.M31, remote_control.WorldMatrix.M32, remote_control.WorldMatrix.M33) + remote_control.GetPosition();

                StringBuilder values = new StringBuilder();
                //values.Append(PText.GetGPS("X :", LocX, 2) + "\n");
                //values.Append(PText.GetGPS("Y :", LocY, 2) + "\n");
                //values.Append(PText.GetGPS("Z :", LocZ, 2) + "\n");
                //values.Append(PText.GetGPS("HoverThrust", HoverThrust, 2) + "\n");
                //values.Append("UP       : " + PText.GetThrust((float)UpThrust) + "\t, MAX : " + PText.GetThrust((float)UpThrMax) + "\n");
                //values.Append("DOWN     : " + PText.GetThrust((float)DownThrust) + "\t, MAX : " + PText.GetThrust((float)DownThrMax) + "\n");
                values.Append("№точки: " + current_point_index + "\n");
                values.Append("Кол: " + current_list_points.Count() + "count: " + count + "\n");
                if (TackTarget != null)
                {
                    values.Append(PText.GetGPS("Tтек-", (Vector3D)TackTarget) + "\n");
                }
                values.Append("КУРС ---------------------------------------\n");
                values.Append("move_horizont : " + move_ud + ",  ap_up      :" + ap_up + "\n");
                values.Append("move_curse    : " + move_fb + ",  ap_forward :" + ap_forward + "\n");
                values.Append("LeftVelocity  : " + Math.Round(LeftVelocity, 2) + "\n");
                values.Append("КУРС ---------------------------------------\n");
                values.Append("|- DeltaCurse               : " + Math.Round(DeltaCurse, 2) + ", C : " + (control_curs ? green.ToString() : red.ToString()) + "\n");
                values.Append("|- ForwVelocity             : " + Math.Round(ForwVelocity, 2) + " TASK: " + Math.Round(TaskCurseSpeed, 2) + "\n");
                values.Append("|- ForwardBrakingDistances  : " + Math.Round(ForwardBrakingDistances, 2) + "\n");
                values.Append("|- BackwardBrakingDistances : " + Math.Round(BackwardBrakingDistances, 2) + "\n");
                values.Append("ВЫСОТА -------------------------------------\n");
                values.Append("|- DeltaHeight              : " + Math.Round(DeltaHeight, 2) + ", H : " + (control_horizont ? green.ToString() : red.ToString()) + ", Hower : " + (hover ? green.ToString() : red.ToString()) + "\n");
                values.Append("|- UpVelocity               : " + Math.Round(UpVelocity, 2) + " TASK: " + Math.Round(TaskHeightSpeed, 2) + "\n");
                values.Append("|- UpBrakingDistances       : " + Math.Round(UpBrakingDistances, 2) + "\n");
                values.Append("|- DownBrakingDistances     : " + Math.Round(DownBrakingDistances, 2) + "\n");
                values.Append("ГИРОСКОПЫ ----------------------------------\n");
                values.Append("|- Yaw-target  : " + Math.Round(YawTarget, 2) + ", T : " + (aim_point ? green.ToString() : red.ToString()) + ",  V : " + (aim_vector ? green.ToString() : red.ToString()) + "\n");
                values.Append("|- Yaw-curse   : " + Math.Round(YawVector, 2) + "\n");
                values.Append("|- Yaw         : " + Math.Round(YawInput, 2) + "\n");
                values.Append("|- Roll        : " + Math.Round(RollInput, 2) + "\n");
                values.Append("|- Pitch       : " + Math.Round(PitchInput, 2) + "\n");
                values.Append("-----------------------------------------------\n");
                values.Append("BASE-1 id=" + connector_base1.id + " position" + connector_base1.position + "\n");
                values.Append(PText.GetGPS("|-Point :", connector_base1.point, 2) + "\n");
                values.Append(PText.GetGPS("|-Vector :", connector_base1.vector, 2) + "\n");
                values.Append("BASE-2 id=" + connector_base2.id + " position" + connector_base2.position + "\n");
                values.Append(PText.GetGPS("|-Point :", connector_base2.point, 2) + "\n");
                values.Append(PText.GetGPS("|-Vector :", connector_base2.vector, 2) + "\n");

                //values.Append("UP       : " + PText.GetThrust((float)UpThrust) + "\t, MAX : " + PText.GetThrust((float)UpThrMax) + "\n");
                //values.Append("DOWN     : " + PText.GetThrust((float)DownThrust) + "\t, MAX : " + PText.GetThrust((float)DownThrMax) + "\n");
                //values.Append("Forward  : " + PText.GetThrust((float)ForwardThrust) + "\t, MAX : " + PText.GetThrust((float)ForwardThrMax) + "\n");
                //values.Append("Backward : " + PText.GetThrust((float)BackwardThrust) + "\t, MAX : " + PText.GetThrust((float)BackwardThrMax) + "\n");
                //values.Append("Left     : " + PText.GetThrust((float)LeftThrust) + "\t, MAX : " + PText.GetThrust((float)LeftThrMax) + "\n");
                //values.Append("Right    : " + PText.GetThrust((float)RightThrust) + "\t, MAX : " + PText.GetThrust((float)RightThrMax) + "\n");

                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "fly_targets":
                        current_point_index = null;
                        current_list_points.Clear();
                        current_list_points.Add(Target1);
                        current_list_points.Add(Target2);
                        curent_programm = programm.fly_targets;
                        break;
                    case "hover":
                        TackTarget = Target2;
                        compensate = true;
                        horizont = true;
                        hover = true;
                        break;
                    case "set_base1":
                        SetPointConnection(ref connector_base1);
                        break;
                    case "connect_base1":
                        Connector1();
                        //current_connector_base = connector_base1;
                        //curent_programm = programm.fly_connect;
                        break;
                    case "set_base2":
                        SetPointConnection(ref connector_base2);
                        break;
                    case "compensate_on":
                        compensate = true;
                        horizont = true;
                        break;
                    case "compensate_off":
                    case "clear":
                        TackTarget = null;
                        aim_vector = false;
                        aim_point = false;
                        compensate = false;
                        horizont = false;
                        fly_target = false;
                        current_point_index = null;
                        current_list_points.Clear();
                        current_connector_base = null;
                        curent_programm = programm.none;
                        curent_mode = mode.none;
                        remote_control.DampenersOverride = true;
                        control_horizont = false;
                        control_curs = false;
                        hover = false;
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
                    case "FT1":
                        TackTarget = Target1;
                        fly_target = true;
                        break;
                    case "FT2":
                        TackTarget = Target2;
                        fly_target = true;
                        break;
                    case "FT3":
                        TackTarget = TargetConnector;
                        fly_target = true;
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Обновим состояние навигации
                    UpdateCalc();
                    if (curent_programm != programm.none)
                    {
                        UpdateProgramm();
                    }
                    if (fly_target)
                    {
                        FlyTarget();
                    }
                    if (aim_point)
                    {
                        TakeAim();
                    }
                    GyroOver(horizont);
                    if (horizont)
                    {
                        Horizon();
                    }
                    if (control_curs)
                    {
                        FlyCurse();
                    }
                    if (control_horizont)
                    {
                        FlyHorizont();
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


// 0. Чем ближе к точке убирать чувсвительность гироскопа
// 1. изменение матрицы направления полета
// ?- Наладить полет сначало выыровнятся по верхней точке а затем к цели
// 2. Выполнить команду полет и конектор+
// 3. Выполнить команду коннектор- отлет
// 4. Выполнить команду полет к точкам с точностью (например увеличение и уменшение точности)
// +. Сохранение точек коннекторов и точек бурения
// 6. Сохранение параметров
// 7. Получение параметров % заряда и заполнения контейнеров
// 8. режим полета на коннектор, точку бурения.
// 9. Режим бурения

// T0: GPS:TargetConnector:53567.3705051079:-26769.2952025845:11925.7372278272:
//GPS:T0:53567.3682644915:-26769.3032342576:11925.7283974891:

//GPS: T1: 53567.327347393:-26810.0214231395:11834.3936969047:

//GPS: T2: 53742.7857451991:-26897.7059714201:11873.4548109712:


//BASE - 1 id = 79876025562437155 positionFalse

//GPS:| -Point::53567.35 : -26769.33 : 11925.75:

//GPS:| -Vector::0 : 0.41 : 0.91:

//BASE - 2 id = positionFalse


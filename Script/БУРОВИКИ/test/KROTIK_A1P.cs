using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
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
namespace KROTIK_A1M_NAV_My
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
        string NameLCDDebug = "[KROTIK_A1]-LCD-DEBUG";
        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея

        const char green = '\uE001';
        const char blue = '\uE002';
        const char red = '\uE003';
        const char yellow = '\uE004';
        const char darkGrey = '\uE00F';

        static LCD lcd_info;
        static LCD lcd_debug;
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
            lcd_debug = new LCD(NameLCDDebug);
            bats = new Batterys(NameObj);
            connector = new Connector(NameConnector);
            drill = new ShipDrill(NameObj);
            drill.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            cockpit = new Cockpit(NameCockpit);
            navigation = new Navigation(cockpit, connector, bats, drill, NameObj, NameRemoteControl, NameCameraCourse);
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
            values_info.Append(bats.TextInfo());
            values_info.Append(connector.TextInfo());
            values_info.Append(drill.TextInfo());
            values_info.Append(navigation.TextInfo1());
            cockpit.OutText(values_info, 0);
            StringBuilder values_info1 = new StringBuilder();
            values_info1.Append(navigation.TextInfo2());
            cockpit.OutText(values_info1, 1);
            //StringBuilder test_info = new StringBuilder();
            //test_info.Append(navigation.TextTEST());
            //lcd_info.OutText(test_info);
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
            public StringBuilder GetText()
            {
                StringBuilder values = new StringBuilder();
                if (base.obj != null)
                {
                    base.obj.ReadText(values);
                }
                return values;
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
            public float CurrentPersent()
            {
                return base.list_obj.Select(b => b.CurrentStoredPower).Sum() / base.list_obj.Select(b => b.MaxStoredPower).Sum();
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
                values.Append("КОННЕКТОР: " + (Connected ? green.ToString() : (Connectable ? yellow.ToString() : red.ToString())) + "\n");
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
                //return list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).Count().ToString();
                foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList())
                {
                    //_scr.Echo("remote_control: " + conn.DisplayNameText);
                    //if (conn.DisplayNameText.Trim() != conn.DisplayNameText.Trim() && (conn.GetPosition() - this.GetPosition()).Length() < 2) return conn.DisplayNameText;
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
                values.Append("БУРЫ: " + (base.Enabled() ? green.ToString() : red.ToString()) + "\n");
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
            public StringBuilder GetText(int num_lcd)
            {
                StringBuilder values = new StringBuilder();
                if (obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return null;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.ReadText(values);
                    }
                }
                return values;
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
            Batterys batterys;
            ShipDrill drills;
            IMyRemoteControl remote_control;
            IMyCameraBlock camera_course;
            List<IMyThrust> thrusts = new List<IMyThrust>();
            List<IMyGyro> gyros = new List<IMyGyro>();
            List<IMyTerminalBlock> cargos = new List<IMyTerminalBlock>();
            //---------------------------
            float GyroMult = 1f;
            float AlignAccelMult = 0.3f;
            float DrillGyroMult = 1f;
            float TargetSize = 100;
            float ReturnOnCharge = 0.2f;// Процент заряда
            float ReturnOffCharge = 0.9f;// Процент заряда
            float DrillSpeedLimit = 0.5f;
            float DrillAccel = 0.5f;
            float DrillDepth = 25;      // глубина шахты
            int MaxShafts = 50;         // макс кол ва
            float DrillFrameWidth = 10f; // размеры буровика
            float DrillFrameLength = 10f;

            public enum programm : int
            {
                none = 0,
                fly_connect_base = 1,   // лететь на базу
                fly_drill = 2,          // лететь к шахте
                start_drill = 3,        // начать бурение
            };
            public static string[] name_programm = { "", "ПОЛЕТ НА БАЗУ", "ПОЛЕТ К ШАХТЕ", "СТАРТ ДОБЫЧИ" };
            programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                base_operation = 1,
                un_dock = 2,
                to_base = 3,
                dock = 4,
                to_drill = 5,
                drill_align = 6,
                drill = 7,
                pull_up = 8,
                pull_out = 9,
            };
            public static string[] name_mode = { "", "БАЗА", "РАСТЫКОВКА", "К БАЗЕ", "СТЫКОВКА", "К ШАХТЕ", "НА ТОЧКУ БУРЕНИЯ", "БУРИМ", "ОСТАНОВИТЬ БУР", "ВЫТАЩИТЬ БУР" };

            mode curent_mode = mode.none;
            //------------------------------
            public Vector3D MyPos { get; private set; }
            public Vector3D MyPrevPos { get; private set; }
            public Vector3D VelocityVector { get; private set; }
            public Vector3D UpVelocityVector { get; private set; }
            public Vector3D ForwVelocityVector { get; private set; }
            public Vector3D LeftVelocityVector { get; private set; }
            public Vector3D GravVector { get; private set; }
            public float PhysicalMass { get; private set; } // Физическая масса
            public MatrixD WMCocpit { get; private set; } //
            public MatrixD OrientationCocpit { get; private set; } //
            //------------------------------------------------
            public double UpThrMax { get; private set; } = 0;
            public double DownThrMax { get; private set; } = 0;
            public double LeftThrMax { get; private set; } = 0;
            public double RightThrMax { get; private set; } = 0;
            public double ForwardThrMax { get; private set; } = 0;
            public double BackwardThrMax { get; private set; } = 0;
            //---------------------------------------------------
            public double ForwardThrust { get; private set; } = 0;
            public double LeftThrust { get; private set; } = 0;
            public double UpThrust { get; private set; } = 0;
            public double BackwardThrust { get; private set; } = 0;
            public double RightThrust { get; private set; } = 0;
            public double DownThrust { get; private set; } = 0;
            //
            public float XMaxA { get; private set; }
            public float YMaxA { get; private set; }
            public float ZMaxA { get; private set; }
            //---------------------------------------------------
            public float Distance { get; private set; }

            public Vector3D PlanetCenter = new Vector3D(0.50, 0.50, 0.50);
            private Vector3D BaseDockPoint = new Vector3D(0, 0, -200);
            private Vector3D ConnectorPoint = new Vector3D(0, 0, 3);
            private Vector3D DrillPoint = new Vector3D(0, 0, 0);
            public MatrixD DockMatrix { get; private set; }
            public MatrixD DrillMatrix { get; private set; }

            private Vector3D point_start_drill = new Vector3D(0, 0, 0);
            public int ShaftN { get; private set; }
            public bool PullUpNeeded { get; private set; } // Требуется подтянуть
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
                //id = 79876025562437155,
                //point = new Vector3D(53567.3705051079, -26769.2952025845, 11925.7372278272),
                //vector = new Vector3D(0, 0.41, 0.91),
                //position = false,
                //load = false
            };
            public int connector_distance { get; private set; } = 200;

            public string strStatus = "";

            public StringBuilder Status = new StringBuilder();

            public double FlyHeight;

            //public int MaxVolume { get; private set; }
            public int CriticalMass { get; private set; } = 400000;
            public int CurrentVolume { get; private set; }
            public int CurrentMass { get; private set; }
            public bool StoneDumpNeeded { get; private set; } // Признак нужно сбросить груз

            public bool CriticalMassReached { get; private set; }// Признак критической массы
            public bool EmergencyReturn = false;

            public bool go_home = false; // вернутся домой и остатся
            public bool pause = false;

            public float UpAccel { get; private set; }

            public Navigation(Cockpit cockpit, Connector connector, Batterys batterys, ShipDrill drills, string NameObj, string NameRemoteControl, string NameCameraCourse)
            {
                this.cockpit = cockpit;
                this.connector = connector;
                this.batterys = batterys;
                this.drills = drills;
                remote_control = _scr.GridTerminalSystem.GetBlockWithName(NameRemoteControl) as IMyRemoteControl;
                camera_course = _scr.GridTerminalSystem.GetBlockWithName(NameCameraCourse) as IMyCameraBlock;
                _scr.GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusts, r => (r.CustomName.Contains(NameObj)));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros, r => (r.CustomName.Contains(NameObj)));
                _scr.Echo("remote_control: " + ((remote_control != null) ? ("Ок") : ("not block")));
                _scr.Echo("camera_course: " + ((camera_course != null) ? ("Ок") : ("not block")));
                _scr.Echo("thrusts: " + ((thrusts.Count() > 0) ? thrusts.Count().ToString() + "шт." : ("not block")));
                _scr.Echo("gyros: " + ((gyros.Count() > 0) ? gyros.Count().ToString() + "шт." : ("not block")));

                List<IMyTerminalBlock> cargos_block = new List<IMyTerminalBlock>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargos_block, r => (r.CustomName.Contains(NameObj)));
                foreach (IMyTerminalBlock tb in cargos_block)
                {
                    if ((tb is IMyShipDrill) || (tb is IMyCargoContainer) || (tb is IMyShipConnector))
                    {
                        cargos.Add(tb);
                    }
                }
                _scr.Echo("cargos: " + ((cargos.Count() > 0) ? cargos.Count().ToString() + "шт." : ("not block")));
                LoadFromStorage();


            }
            //
            public void UpdateCargo()
            {
                CurrentVolume = 0;
                CurrentMass = 0;
                foreach (IMyTerminalBlock tb in cargos)
                {
                    CurrentVolume += (int)tb.GetInventory(0).CurrentVolume;
                    CurrentMass += (int)tb.GetInventory(0).CurrentMass;
                }
                if (PhysicalMass > CriticalMass)
                {
                    CriticalMassReached = true;
                }
                else
                {
                    CriticalMassReached = false;
                }
            }
            //-------------------------------------
            public MatrixD GetNormTransMatrixFromMyPos()
            {
                MatrixD mRot;
                Vector3D V3Dcenter = MyPos;
                Vector3D V3Dup = -Vector3D.Normalize(GravVector);
                Vector3D V3Dleft = Vector3D.Normalize(Vector3D.Reject(WMCocpit.Left, V3Dup));
                Vector3D V3Dfow = Vector3D.Normalize(Vector3D.Cross(V3Dleft, V3Dup));

                mRot = new MatrixD(V3Dleft.GetDim(0), V3Dleft.GetDim(1), V3Dleft.GetDim(2), 0, V3Dup.GetDim(0), V3Dup.GetDim(1), V3Dup.GetDim(2), 0, V3Dfow.GetDim(0), V3Dfow.GetDim(1), V3Dfow.GetDim(2), 0, 0, 0, 0, 1);
                mRot = MatrixD.Invert(mRot);
                return new MatrixD(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -V3Dcenter.GetDim(0), -V3Dcenter.GetDim(1), -V3Dcenter.GetDim(2), 1) * mRot;
            }
            public void SetDockMatrix()
            {
                if (this.connector.Connected)
                {
                    DockMatrix = GetNormTransMatrixFromMyPos();
                    connector_base1.id = this.connector.getRemoteConnector();
                    Vector3D GravNorm = Vector3D.Normalize(GravVector);
                    double vc = GravNorm.Dot(WMCocpit.Forward);
                    connector_base1.point = remote_control.GetPosition();
                    connector_base1.vector = WMCocpit.Forward;
                    connector_base1.position = Math.Abs(vc) < 0.01f ? false : true;
                    SaveToStorage();
                }

            }
            public void SetDrillMatrixDepo()
            {
                DrillMatrix = GetNormTransMatrixFromMyPos();
                ShaftN = 0;
                DrillPoint = new Vector3D(0, 0, 0);
                //point_start_drill = remote_control.GetPosition();
            }
            public void SetFlyHeight()
            {
                FlyHeight = (MyPos - PlanetCenter).Length();
                BaseDockPoint = new Vector3D(0, 0, -200);
                SaveToStorage();
            }
            //---------------------------------------------
            public void SetOverride(bool OverrideOnOff, Vector3 settings, float Power = 1)
            {
                foreach (IMyGyro Gyro in gyros)
                {
                    if ((!Gyro.GyroOverride) && OverrideOnOff)
                        Gyro.ApplyAction("Override");
                    Gyro.GyroPower = Power;
                    Gyro.Yaw = settings.GetDim(0);
                    Gyro.Pitch = settings.GetDim(1);
                    Gyro.Roll = settings.GetDim(2);
                }
            }
            public void SetOverride(bool OverrideOnOff = true, float OverrideValue = 0, float Power = 1)
            {
                foreach (IMyGyro Gyro in gyros)
                {
                    if (((!Gyro.GyroOverride) && OverrideOnOff) || ((Gyro.GyroOverride) && !OverrideOnOff))
                        Gyro.ApplyAction("Override");
                    Gyro.GyroPower = Power;
                    Gyro.Yaw = OverrideValue;
                    Gyro.Pitch = OverrideValue;
                    Gyro.Roll = OverrideValue;
                }
            }
            public Vector3D GetNavAngles(Vector3D Target, MatrixD InvMatrix, double sfiftX = 0, double shiftZ = 0)
            {
                Vector3D V3Dcenter = remote_control.GetPosition();
                Vector3D V3Dfow = remote_control.WorldMatrix.Forward + V3Dcenter;
                Vector3D V3Dup = remote_control.WorldMatrix.Up + V3Dcenter;
                Vector3D V3Dleft = remote_control.WorldMatrix.Left + V3Dcenter;
                Vector3D GravNorm = Vector3D.Normalize(GravVector) + V3Dcenter;

                V3Dcenter = Vector3D.Transform(V3Dcenter, InvMatrix);
                V3Dfow = (Vector3D.Transform(V3Dfow, InvMatrix)) - V3Dcenter;
                V3Dup = (Vector3D.Transform(V3Dup, InvMatrix)) - V3Dcenter;
                V3Dleft = (Vector3D.Transform(V3Dleft, InvMatrix)) - V3Dcenter;
                GravNorm = Vector3D.Normalize((Vector3D.Transform(GravNorm, InvMatrix)) - V3Dcenter - new Vector3D(sfiftX, 0, shiftZ));

                //Vector3D TargetNorm = Vector3D.Normalize(Vector3D.Reject(Target - V3Dcenter, GravNorm));

                //double TargetPitch = Vector3D.Dot(V3Dfow, Vector3D.Normalize(Vector3D.Reject(-GravNorm, V3Dleft)));
                //TargetPitch = Math.Acos(TargetPitch) - Math.PI / 2;

                //double TargetRoll = Vector3D.Dot(V3Dleft, Vector3D.Reject(-GravNorm, V3Dfow));
                //TargetRoll = Math.Acos(TargetRoll) - Math.PI / 2;

                //double TargetYaw = Math.Acos(Vector3D.Dot(V3Dfow, TargetNorm));
                //if ((V3Dleft - TargetNorm).Length() < Math.Sqrt(2))
                //    TargetYaw = -TargetYaw;

                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(V3Dfow);
                double gL = GravNorm.Dot(V3Dleft);
                double gU = GravNorm.Dot(V3Dup);
                //Получаем сигналы по тангажу и крены операцией atan2
                double TargetRoll = (float)Math.Atan2(gL, -gU); // крен
                double TargetPitch = -(float)Math.Atan2(gF, -gU); // тангаж

                Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter);
                //Vector3D TargetNorm = Vector3D.Normalize(Vector3D.Reject(Target - V3Dcenter, GravNorm));

                //Рысканием прицеливаемся на точку Target.
                double tF = TargetNorm.Dot(V3Dfow);
                double tL = TargetNorm.Dot(V3Dleft);
                double TargetYaw = -(float)Math.Atan2(tL, tF);


                if (double.IsNaN(TargetYaw)) TargetYaw = 0;
                if (double.IsNaN(TargetPitch)) TargetPitch = 0;
                if (double.IsNaN(TargetRoll)) TargetRoll = 0;

                return new Vector3D(TargetYaw, TargetPitch, TargetRoll);
            }
            public Vector3D GetNavAngles(Vector3D Target)
            {
                Vector3D GravNorm = Vector3D.Normalize(GravVector);
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(remote_control.WorldMatrix.Forward);
                double gL = GravNorm.Dot(remote_control.WorldMatrix.Left);
                double gU = GravNorm.Dot(remote_control.WorldMatrix.Up);
                //Получаем сигналы по тангажу и крены операцией atan2
                double TargetRoll = (float)Math.Atan2(gL, -gU); // крен
                double TargetPitch = -(float)Math.Atan2(gF, -gU); // тангаж

                Vector3D TargetNorm = Vector3D.Normalize(Target - MyPos);
                //Рысканием прицеливаемся на точку Target.
                double tF = TargetNorm.Dot(remote_control.WorldMatrix.Forward);
                double tL = TargetNorm.Dot(remote_control.WorldMatrix.Left);
                double TargetYaw = -(float)Math.Atan2(tL, tF);

                if (double.IsNaN(TargetYaw)) TargetYaw = 0;
                if (double.IsNaN(TargetPitch)) TargetPitch = 0;
                if (double.IsNaN(TargetRoll)) TargetRoll = 0;

                return new Vector3D(TargetYaw, TargetPitch, TargetRoll);
                //return new Vector3D(0, 0, 0);
            }
            //------------------------------------------------
            public void SetThrustOverridePersent(float up, float down, float left, float right, float forward, float backward)
            {
                Matrix ThrusterMatrix = new MatrixD();
                foreach (IMyThrust thrust in this.thrusts)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == OrientationCocpit.Up)
                    {
                        thrust.ThrustOverridePercentage = up;
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Down)
                    {
                        thrust.ThrustOverridePercentage = down;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Left)
                    {
                        thrust.ThrustOverridePercentage = left;
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Right)
                    {
                        thrust.ThrustOverridePercentage = right;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Forward)
                    {
                        thrust.ThrustOverridePercentage = forward;
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Backward)
                    {
                        thrust.ThrustOverridePercentage = backward;
                    }
                }
            }
            public void ClearThrustOverridePersent()
            {
                SetThrustOverridePersent(0f, 0f, 0f, 0f, 0f, 0f);
            }
            public void SetOverridePercent(string axis, float OverrideValue)
            {
                Matrix ThrusterMatrix = new MatrixD();
                foreach (IMyThrust thrust in thrusts)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == OrientationCocpit.Up)
                    {
                        if (axis == "D") { thrust.ThrustOverridePercentage = OverrideValue / 100; }
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Down)
                    {
                        if (axis == "U") { thrust.ThrustOverridePercentage = OverrideValue / 100; }
                    }
                    //X
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Left)
                    {
                        if (axis == "R") { thrust.ThrustOverridePercentage = OverrideValue / 100; }
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Right)
                    {
                        if (axis == "L") { thrust.ThrustOverridePercentage = OverrideValue / 100; }
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Forward)
                    {
                        if (axis == "B") { thrust.ThrustOverridePercentage = OverrideValue / 100; }
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Backward)
                    {
                        if (axis == "F") { thrust.ThrustOverridePercentage = OverrideValue / 100; }
                    }
                }
            }
            public void SetOverrideN(string axis, float OverrideValue)
            {
                double MaxThrust = 0;
                float Value = 0;
                ForwardThrust = 0;
                LeftThrust = 0;
                UpThrust = 0;
                BackwardThrust = 0;
                RightThrust = 0;
                DownThrust = 0;
                if (axis == "D") { MaxThrust = UpThrMax; SetOverridePercent("U", 0f); }
                else if (axis == "U") { MaxThrust = DownThrMax; SetOverridePercent("D", 0f); }
                else if (axis == "B") { MaxThrust = ForwardThrMax; SetOverridePercent("F", 0f); }
                else if (axis == "F") { MaxThrust = BackwardThrMax; SetOverridePercent("B", 0f); }
                else if (axis == "L") { MaxThrust = RightThrMax; SetOverridePercent("R", 0f); }
                else if (axis == "R") { MaxThrust = LeftThrMax; SetOverridePercent("L", 0f); }

                if (OverrideValue == 0)
                {
                    Value = 0;
                }
                else
                {
                    Value = (float)Math.Max(OverrideValue / MaxThrust, 0.1f);
                }
                Matrix ThrusterMatrix = new MatrixD();
                foreach (IMyThrust thrust in thrusts)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == OrientationCocpit.Up)
                    {
                        if (axis == "D") { thrust.ThrustOverridePercentage = Value; UpThrust = Value; }
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Down)
                    {
                        if (axis == "U") { thrust.ThrustOverridePercentage = Value; DownThrust = Value; }
                    }
                    //X
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Left)
                    {
                        if (axis == "R") { thrust.ThrustOverridePercentage = Value; LeftThrust = Value; }
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Right)
                    {
                        if (axis == "L") { thrust.ThrustOverridePercentage = Value; RightThrust = Value; }
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Forward)
                    {
                        if (axis == "B") { thrust.ThrustOverridePercentage = Value; ForwardThrust = Value; }
                    }
                    else if (ThrusterMatrix.Forward == OrientationCocpit.Backward)
                    {
                        if (axis == "F") { thrust.ThrustOverridePercentage = Value; BackwardThrust = Value; }
                    }
                }
            }
            public void SetOverrideAccel(string axis, float OverrideValue)
            {
                switch (axis)
                {
                    case "U":
                        OverrideValue += (float)GravVector.Length();
                        break;
                    case "L":
                        if (OverrideValue < 0)
                        {
                            axis = "R";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "R":
                        if (OverrideValue < 0)
                        {
                            axis = "L";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "F":
                        if (OverrideValue < 0)
                        {
                            axis = "B";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "B":
                        if (OverrideValue < 0)
                        {
                            axis = "F";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                }
                SetOverrideN(axis, OverrideValue * PhysicalMass);
            }
            //-----------------------------------------------
            public void FlyConnectBase()
            {
                if (curent_mode == mode.none)
                {
                    curent_mode = mode.to_base;
                    SaveToStorage();
                }
                if (curent_mode == mode.to_base && ToBase())
                {
                    curent_mode = mode.dock;
                    SaveToStorage();
                }
                if (curent_mode == mode.dock && Dock())
                {
                    Clear();
                    curent_programm = programm.none;
                    SaveToStorage();
                }
            }
            public void FlyDrill()
            {
                if (curent_mode == mode.none)
                {
                    if (connector.Connected)
                    {
                        curent_mode = mode.un_dock;
                        SaveToStorage();
                    }
                    else
                    {
                        curent_mode = mode.to_drill;
                        SaveToStorage();
                    }
                }
                if (curent_mode == mode.un_dock && UnDock())
                {
                    curent_mode = mode.to_drill;
                    SaveToStorage();
                }
                if (curent_mode == mode.to_drill && ToDrillPoint())
                {
                    Clear();
                    curent_programm = programm.none;
                    SaveToStorage();
                }
            }
            public void StartDrill()
            {
                if (curent_mode == mode.none)
                {
                    go_home = false;
                    if (connector.Connected)
                    {
                        curent_mode = mode.un_dock;
                        SaveToStorage();
                    }
                    else
                    {
                        curent_mode = mode.to_drill;
                        SaveToStorage();
                    }
                }
                else
                {
                    if (curent_mode == mode.un_dock && UnDock())
                    {
                        curent_mode = mode.to_drill;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.to_drill && ToDrillPoint())
                    {
                        curent_mode = mode.drill_align;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.drill_align && DrillAlign())
                    {
                        curent_mode = mode.drill;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.drill && Drill(out EmergencyReturn))
                    {
                        if (PullUpNeeded)
                            curent_mode = mode.pull_up;
                        else
                            curent_mode = mode.pull_out;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.pull_up && PullUp())
                    {
                        curent_mode = mode.drill;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.pull_out && PullOut())
                    {
                        if (EmergencyReturn || go_home) // || GoHome
                            curent_mode = mode.to_base;
                        else
                        {
                            SetNewShaft();
                            if (ShaftN >= MaxShafts)
                                curent_mode = mode.to_base;
                            else
                                curent_mode = mode.drill_align;
                        }
                        SaveToStorage();
                    }
                    if (curent_mode == mode.to_base && ToBase())
                    {
                        curent_mode = mode.dock;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.dock && Dock())
                    {
                        curent_mode = mode.base_operation;
                        SaveToStorage();
                    }
                    if (curent_mode == mode.base_operation)
                    {
                        batterys.Charger();
                        ClearThrustOverridePersent();
                        if (go_home || ShaftN >= MaxShafts)
                        {
                            Stop();
                        }
                        else
                        {
                            if (batterys.CurrentPersent() >= ReturnOffCharge && CurrentMass == 0f)
                            {
                                curent_mode = mode.un_dock;
                                batterys.Auto();
                                SaveToStorage();
                            }
                        }
                    }
                }
            }
            //-----------------------------------------------
            public void UpdateCalc()
            {
                MyPrevPos = MyPos;
                MyPos = remote_control.GetPosition();
                GravVector = remote_control.GetNaturalGravity();
                PhysicalMass = remote_control.CalculateShipMass().PhysicalMass;
                WMCocpit = remote_control.WorldMatrix;
                //
                VelocityVector = (MyPos - MyPrevPos) * 6;
                UpVelocityVector = WMCocpit.Up * Vector3D.Dot(VelocityVector, WMCocpit.Up);
                ForwVelocityVector = WMCocpit.Forward * Vector3D.Dot(VelocityVector, WMCocpit.Forward);
                LeftVelocityVector = WMCocpit.Left * Vector3D.Dot(VelocityVector, WMCocpit.Left);
                // Орентация коробля
                Matrix CPMatrix = new MatrixD();
                Matrix ThrusterMatrix = new MatrixD();
                remote_control.Orientation.GetMatrix(out CPMatrix);
                OrientationCocpit = CPMatrix;
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
                    if (ThrusterMatrix.Forward == CPMatrix.Up)
                    {
                        UpThrMax += thrust.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CPMatrix.Down)
                    {
                        DownThrMax += thrust.MaxEffectiveThrust;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CPMatrix.Left)
                    {
                        LeftThrMax += thrust.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CPMatrix.Right)
                    {
                        RightThrMax += thrust.MaxEffectiveThrust;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CPMatrix.Forward)
                    {
                        ForwardThrMax += thrust.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CPMatrix.Backward)
                    {
                        BackwardThrMax += thrust.MaxEffectiveThrust;
                    }
                }
                //-------------------
                YMaxA = Math.Abs((float)Math.Min(UpThrMax / PhysicalMass - GravVector.Length(), DownThrMax / PhysicalMass + GravVector.Length()));
                ZMaxA = (float)Math.Min(ForwardThrMax, BackwardThrMax) / PhysicalMass;
                XMaxA = (float)Math.Min(RightThrMax, LeftThrMax) / PhysicalMass;
                UpdateCargo();
            }
            public void Clear()
            {
                ClearThrustOverridePersent();
                SetOverride(false, 1);
                curent_mode = mode.none;
                SaveToStorage();
            }
            public void Stop()
            {
                ClearThrustOverridePersent();
                SetOverride(false, 1);
                curent_mode = mode.none;
                curent_programm = programm.none;
                go_home = false;
                pause = false;
                SaveToStorage();
            }
            public bool ToBase()
            {
                bool Complete = false;
                float MaxUSpeed, MaxFSpeed;
                Vector3D gyrAng = GetNavAngles(BaseDockPoint, DockMatrix);
                Vector3D point_to_base = connector_base1.point - (connector_base1.vector * connector_distance);
                //Vector3D gyrAng = GetNavAngles(point_to_base);
                Vector3D MyPosCon = Vector3D.Transform(MyPos, DockMatrix);
                Distance = (float)(BaseDockPoint - new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2))).Length();
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(FlyHeight - (MyPos - PlanetCenter).Length()) * YMaxA) / 1.2f;
                MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 1.2f;
                SetOverride(true, gyrAng * GyroMult, 1);
                SetOverridePercent("R", 0);
                SetOverridePercent("L", 0);

                if (UpVelocityVector.Length() < MaxUSpeed)
                    SetOverrideAccel("U", (float)((FlyHeight - (MyPos - PlanetCenter).Length()) * AlignAccelMult));
                else
                {
                    SetOverridePercent("U", 0);
                }
                if (Distance > TargetSize)
                {
                    if (ForwVelocityVector.Length() < MaxFSpeed)
                    {
                        SetOverrideAccel("F", (float)(Distance * AlignAccelMult));
                        SetOverridePercent("B", 0);
                    }
                    else
                    {
                        SetOverridePercent("F", 0);
                        SetOverridePercent("B", 0);
                    }
                }
                else
                {
                    ClearThrustOverridePersent();
                    SetOverride(false, 1);
                    curent_mode = mode.none;
                    Complete = true;
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, 0);
                return Complete;
            }
            public bool Dock()
            {
                bool Complete = false;
                float MaxUSpeed, MaxLSpeed, MaxFSpeed;
                Vector3D MyPosCon = Vector3D.Transform(MyPos, DockMatrix);
                Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                //Vector3D gyrAng = GetNavAngles(connector_base1.point);
                Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, DockMatrix)))).Length() + ConnectorPoint.Length());

                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(0)) * XMaxA) / 2;
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(1)) * YMaxA) / 2;
                MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 2;
                if (Distance < 15)
                    MaxFSpeed = MaxFSpeed / 5;
                if (Math.Abs(MyPosCon.GetDim(1)) < 1)
                    MaxUSpeed = 0.1f;
                SetOverride(true, gyrAng * GyroMult, 1);
                if (LeftVelocityVector.Length() < MaxLSpeed)
                    SetOverrideAccel("R", (float)(MyPosCon.GetDim(0) * AlignAccelMult));
                else
                {
                    SetOverridePercent("R", 0);
                    SetOverridePercent("L", 0);
                }
                float UpAccel = -(float)(MyPosCon.GetDim(1) * AlignAccelMult);
                float minUpAccel = 0.3f;
                if ((UpAccel < 0) && (UpAccel > -minUpAccel))
                    UpAccel = -minUpAccel;
                if ((UpAccel > 0) && (UpAccel < minUpAccel))
                    UpAccel = minUpAccel;

                if (UpVelocityVector.Length() < MaxUSpeed)
                    SetOverrideAccel("U", UpAccel);
                else
                {
                    SetOverridePercent("U", 0);
                }
                if (((Distance > 100) || ((Math.Abs(MyPosCon.GetDim(0)) < (Distance / 10 + 0.2f)) && (Math.Abs(MyPosCon.GetDim(1)) < (Distance / 10 + 0.2f)))) && (ForwVelocityVector.Length() < MaxFSpeed))
                {
                    SetOverrideAccel("F", (float)(Distance * AlignAccelMult));
                    SetOverridePercent("B", 0);
                }
                else
                {
                    SetOverridePercent("F", 0);
                    SetOverridePercent("B", 0);
                }
                if (Distance < 6)
                {
                    //myDriller.connectorBlock.Turn("On");
                    if (connector.Status == MyShipConnectorStatus.Connectable)
                    {
                        connector.Connect();
                    }
                    if (connector.Status == MyShipConnectorStatus.Connected)
                    {
                        ClearThrustOverridePersent();
                        SetOverride(false, 1);
                        curent_mode = mode.none;
                        Complete = true;
                    }
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed);
                return Complete;
            }
            public bool UnDock()
            {
                bool Complete = false;
                Distance = 0;
                connector.Disconnect();
                if (!connector.Connected)
                {
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, DockMatrix);
                    //Vector3D gyrAng = GetNavAngles(connector_base1.point);
                    Vector3D gyrAng = GetNavAngles(ConnectorPoint, DockMatrix);
                    Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, DockMatrix)))).Length() + ConnectorPoint.Length());
                    SetOverride(true, gyrAng * GyroMult, 1);
                    SetOverridePercent("U", 0);
                    SetOverridePercent("R", 0);
                    SetOverridePercent("L", 0);
                    SetOverridePercent("F", 0);
                    SetOverrideAccel("B", 3);
                    if (Distance > 50)
                    {
                        SetOverrideAccel("B", 0);
                        Complete = true;
                    }
                }
                OutStatusMode(0, 0, 0);
                return Complete;
            }
            public bool ToDrillPoint()
            {
                bool Complete = false;
                float MaxUSpeed, MaxFSpeed;
                Vector3D gyrAng = GetNavAngles(new Vector3D(0, 0, 0), DrillMatrix);
                //Vector3D gyrAng = GetNavAngles(point_start_drill);
                Vector3D MyPosDrill = Vector3D.Transform(MyPos, DrillMatrix);
                Distance = (float)(DrillPoint - new Vector3D(MyPosDrill.GetDim(0), 0, MyPosDrill.GetDim(2))).Length();

                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(FlyHeight - (MyPos - PlanetCenter).Length()) * YMaxA) / 1.2f;
                MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 1.2f;
                SetOverride(true, gyrAng * GyroMult, 1);
                SetOverridePercent("R", 0);
                SetOverridePercent("L", 0);

                if (UpVelocityVector.Length() < MaxUSpeed)
                    SetOverrideAccel("U", (float)((FlyHeight - (MyPos - PlanetCenter).Length()) * AlignAccelMult));
                else
                {
                    SetOverridePercent("U", 0);
                }
                //myDriller.TextOutput("TP1", "\n" + MaxFSpeed.ToString() + "\n" + ForwVelocityVector.Length().ToString() + "\n" + Distance.ToString());           
                if (Distance > TargetSize)
                {
                    if (ForwVelocityVector.Length() < MaxFSpeed)
                    {
                        SetOverrideAccel("F", (float)(Distance * AlignAccelMult));
                        SetOverridePercent("B", 0);
                    }
                    else
                    {
                        SetOverridePercent("F", 0);
                        SetOverridePercent("B", 0);
                    }
                }
                else
                {
                    Complete = true;
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, 0);
                return Complete;
            }
            public bool DrillAlign()
            {
                bool Complete = false;
                float MaxUSpeed, MaxLSpeed, MaxFSpeed;
                Vector3D MyPosDrill = Vector3D.Transform(MyPos, DrillMatrix) - DrillPoint;
                Vector3D gyrAng = GetNavAngles(MyPosDrill + DrillPoint + new Vector3D(0, 0, 1), DrillMatrix);
                //Vector3D gyrAng = GetNavAngles(point_start_drill + new Vector3D(0, 0, 1));
                SetOverride(true, gyrAng * GyroMult, 1);

                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(0)) * XMaxA) / 2;
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(1)) * YMaxA) / 2;
                MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(2)) * ZMaxA) / 2;
                if (double.IsNaN(MaxUSpeed)) MaxUSpeed = 0.1f;
                if (Math.Abs(MyPosDrill.GetDim(1)) < 1)
                    MaxUSpeed = 0.1f;
                if (LeftVelocityVector.Length() < MaxLSpeed)
                    SetOverrideAccel("R", (float)(MyPosDrill.GetDim(0) * AlignAccelMult));
                else
                {
                    SetOverridePercent("R", 0);
                    SetOverridePercent("L", 0);
                }
                if (ForwVelocityVector.Length() < MaxFSpeed)
                    SetOverrideAccel("B", (float)(MyPosDrill.GetDim(2) * AlignAccelMult));
                else
                {
                    SetOverridePercent("F", 0);
                    SetOverridePercent("B", 0);
                }
                if (UpVelocityVector.Length() < MaxUSpeed)
                {
                    UpAccel = -(float)(MyPosDrill.GetDim(1) * AlignAccelMult);
                    float minUpAccel = 0.3f;
                    if ((UpAccel < 0) && (UpAccel > -minUpAccel))
                        UpAccel = -minUpAccel;
                    if ((UpAccel > 0) && (UpAccel < minUpAccel))
                        UpAccel = minUpAccel;
                    SetOverrideAccel("U", UpAccel);
                }
                else
                {
                    SetOverridePercent("U", 0);
                }
                if (MyPosDrill.Length() < 0.5)
                {
                    Complete = true;
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed);
                return Complete;
            }
            public bool Drill(out bool Emergency)
            {
                bool Complete = false;
                Emergency = false;

                float MaxLSpeed, MaxFSpeed;
                Vector3D MyPosDrill = Vector3D.Transform(MyPos, DrillMatrix) - DrillPoint;

                double shiftX = MyPosDrill.GetDim(0) / 2;
                if (shiftX < 0) shiftX = Math.Max(shiftX, -0.1);
                else shiftX = Math.Min(shiftX, 0.1);
                double shiftZ = MyPosDrill.GetDim(2) / 2;
                if (shiftZ < 0) shiftZ = Math.Max(shiftZ, -0.1);
                else shiftZ = Math.Min(shiftZ, 0.1);

                Vector3D gyrAng = GetNavAngles(MyPosDrill + DrillPoint + new Vector3D(0, 0, 1), DrillMatrix, shiftX, shiftZ);
                SetOverride(true, gyrAng * DrillGyroMult, 1);

                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(0)) * XMaxA) / 5;
                MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(2)) * ZMaxA) / 5;

                if (LeftVelocityVector.Length() < MaxLSpeed)
                    SetOverrideAccel("R", (float)(MyPosDrill.GetDim(0) * 10));
                else
                {
                    SetOverridePercent("R", 0);
                    SetOverridePercent("L", 0);
                }
                if (ForwVelocityVector.Length() < MaxFSpeed)
                    SetOverrideAccel("B", (float)(MyPosDrill.GetDim(2) * 10));
                else
                {
                    SetOverridePercent("F", 0);
                    SetOverridePercent("B", 0);
                }

                if (StoneDumpNeeded && drills.Enabled())
                    drills.Off();
                else if (!StoneDumpNeeded && !drills.Enabled())
                    drills.On();

                if ((UpVelocityVector.Length() < DrillSpeedLimit) && (!StoneDumpNeeded))
                {
                    if ((Math.Abs(MyPosDrill.GetDim(0)) < 1) && (Math.Abs(MyPosDrill.GetDim(2)) < 1))
                        SetOverrideAccel("U", (-DrillAccel));
                    else
                    {
                        SetOverrideAccel("U", (DrillAccel));
                        PullUpNeeded = true;
                        Complete = true;
                    }
                }
                else
                {
                    SetOverridePercent("U", 0);
                }
                if (MyPosDrill.GetDim(1) < -DrillDepth) // растояние
                {
                    Complete = true;
                }
                else if (CriticalMassReached || batterys.CurrentPersent() <= ReturnOnCharge) // || myDriller.batteryBlock.LowPower // Нижний придел зарядки
                {
                    Complete = true;
                    Emergency = true;
                }
                OutStatusMode(MaxFSpeed, 0, MaxLSpeed); // DrillAccel
                return Complete;
            }
            public bool PullUp()
            {
                bool Complete = false;
                float MaxLSpeed, MaxFSpeed;
                Vector3D MyPosDrill = Vector3D.Transform(MyPos, DrillMatrix) - DrillPoint;
                Vector3D gyrAng = GetNavAngles(MyPosDrill + DrillPoint + new Vector3D(0, 0, 1), DrillMatrix);
                SetOverride(true, gyrAng * DrillGyroMult, 1);
                //myDriller.drillBlock.Turn("Off");           

                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(0)) * XMaxA) / 4;
                MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(2)) * ZMaxA) / 4;


                if (LeftVelocityVector.Length() < MaxLSpeed)
                    SetOverrideAccel("R", (float)(MyPosDrill.GetDim(0) * 0.5));
                else
                {
                    SetOverridePercent("R", 0);
                    SetOverridePercent("L", 0);
                }
                if (ForwVelocityVector.Length() < MaxFSpeed)
                    SetOverrideAccel("B", (float)(MyPosDrill.GetDim(2) * 0.5));
                else
                {
                    SetOverridePercent("F", 0);
                    SetOverridePercent("B", 0);
                }

                if (UpVelocityVector.Length() < DrillSpeedLimit * 5)
                    SetOverrideAccel("U", (DrillAccel * 2));
                else
                {
                    SetOverridePercent("U", 0);
                }

                if ((MyPosDrill.GetDim(1) > 0) || ((MyPosDrill.GetDim(0) < 0.15) && (MyPosDrill.GetDim(2) < 0.15)))
                {
                    Complete = true;
                    PullUpNeeded = false;
                }
                return Complete;
            }
            public bool PullOut()
            {
                bool Complete = false;
                float MaxLSpeed, MaxFSpeed;
                Vector3D MyPosDrill = Vector3D.Transform(MyPos, DrillMatrix) - DrillPoint;
                Vector3D gyrAng = GetNavAngles(MyPosDrill + DrillPoint + new Vector3D(0, 0, 1), DrillMatrix);
                SetOverride(true, gyrAng * DrillGyroMult, 1);
                drills.Off();

                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(0)) * XMaxA) / 2;
                MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosDrill.GetDim(2)) * ZMaxA) / 2;


                if (LeftVelocityVector.Length() < MaxLSpeed)
                    SetOverrideAccel("R", (float)(MyPosDrill.GetDim(0) * 1));
                else
                {
                    SetOverridePercent("R", 0);
                    SetOverridePercent("L", 0);
                }
                if (ForwVelocityVector.Length() < MaxFSpeed)
                    SetOverrideAccel("B", (float)(MyPosDrill.GetDim(2) * 1));
                else
                {
                    SetOverridePercent("F", 0);
                    SetOverridePercent("B", 0);
                }

                if ((UpVelocityVector.Length() < DrillSpeedLimit * 5) && (MyPosDrill.GetDim(0) < 0.5) && (MyPosDrill.GetDim(2) < 0.5))
                    SetOverrideAccel("U", (DrillAccel * 2));
                else
                {
                    SetOverridePercent("U", 0);
                }
                if (MyPosDrill.GetDim(1) > 0)
                    Complete = true;
                return Complete;
            }
            //-------------------------------------------------
            public int SetNewShaft()
            {
                ShaftN++;
                DrillPoint = GetSpiralXY(ShaftN, DrillFrameWidth, DrillFrameLength);
                return ShaftN;
            }
            private Vector3D GetSpiralXY(int p, float W, float L, int n = 20)
            {
                int positionX = 0, positionY = 0, direction = 0, stepsCount = 1, stepPosition = 0, stepChange = 0;
                int X = 0;
                int Y = 0;
                for (int i = 0; i < n * n; i++)
                {
                    if (i == p)
                    {
                        X = positionX;
                        Y = positionY;
                        break;
                    }
                    if (stepPosition < stepsCount)
                    {
                        stepPosition++;
                    }
                    else
                    {
                        stepPosition = 1;
                        if (stepChange == 1)
                        {
                            stepsCount++;
                        }
                        stepChange = (stepChange + 1) % 2;
                        direction = (direction + 1) % 4;
                    }
                    if (direction == 0) { positionY++; }
                    else if (direction == 1) { positionX--; }
                    else if (direction == 2) { positionY--; }
                    else if (direction == 3) { positionX++; }
                }
                return new Vector3D(X * W, 0, Y * L);
            }
            //-------------------------------------------------
            public void OutStatusMode(float MaxFSpeed, float MaxUSpeed, float MaxLSpeed)
            {
                StringBuilder values = new StringBuilder();
                values.Append(" STATUS\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("DeltaHeight: " + Math.Round(FlyHeight - (MyPos - PlanetCenter).Length()).ToString() + "\n");
                //values.Append("Height: " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(FlyHeight).ToString() + "\n");
                values.Append("Distance: " + Math.Round(Distance).ToString() + "\n");
                values.Append("------------------------------------------\n");
                values.Append("ZMaxA (F-B) : " + Math.Round(ZMaxA, 2).ToString() + "MaxFSpeed: " + Math.Round(MaxFSpeed, 2).ToString() + "\n");
                values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + "MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                values.Append("XMaxA (L-R) : " + Math.Round(XMaxA, 2).ToString() + "MaxLSpeed: " + Math.Round(MaxLSpeed, 2).ToString() + "\n");
                values.Append("------------------------------------------\n");
                values.Append("UP       : " + PText.GetThrust((float)UpThrust) + "\t, MAX : " + PText.GetThrust((float)UpThrMax) + "\n");
                values.Append("DOWN     : " + PText.GetThrust((float)DownThrust) + "\t, MAX : " + PText.GetThrust((float)DownThrMax) + "\n");
                values.Append("Forward  : " + PText.GetThrust((float)ForwardThrust) + "\t, MAX : " + PText.GetThrust((float)ForwardThrMax) + "\n");
                values.Append("Backward : " + PText.GetThrust((float)BackwardThrust) + "\t, MAX : " + PText.GetThrust((float)BackwardThrMax) + "\n");
                values.Append("Left     : " + PText.GetThrust((float)LeftThrust) + "\t, MAX : " + PText.GetThrust((float)LeftThrMax) + "\n");
                values.Append("Right    : " + PText.GetThrust((float)RightThrust) + "\t, MAX : " + PText.GetThrust((float)RightThrMax) + "\n");
                lcd_debug.OutText(values);
            }
            public double GetVal(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToDouble(val);
            }
            public int GetValInt(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToInt32(val);
            }
            public long GetValInt64(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToInt64(val);
            }
            public bool GetValBool(string Key, string str)
            {
                string val = "False";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToBoolean(val);
            }
            public void LoadFromStorage()
            {

                StringBuilder str = lcd_info.GetText();
                curent_programm = (programm)GetValInt("curent_programm", str.ToString());
                curent_mode = (mode)GetValInt("curent_mode", str.ToString());
                pause = GetValBool("pause", str.ToString());
                go_home = GetValBool("go_home", str.ToString());
                FlyHeight = GetVal("FlyHeight", str.ToString());
                ShaftN = GetValInt("ShaftN", str.ToString());
                EmergencyReturn = GetValBool("EmergencyReturn", str.ToString());
                connector_base1.id = GetValInt64("CB1_id", str.ToString());
                connector_base1.point = new Vector3D(GetVal("CB1_X", str.ToString()), GetVal("CB1_Y", str.ToString()), GetVal("CB1_Z", str.ToString()));
                connector_base1.vector = new Vector3D(GetVal("CBV1_X", str.ToString()), GetVal("CBV1_Y", str.ToString()), GetVal("CBV1_Z", str.ToString()));
                connector_base1.load = GetValBool("CB1_load", str.ToString());
                connector_base1.position = GetValBool("CB1_position", str.ToString());
                //
                //point_start_drill = new Vector3D(GetVal("PSD_X", str.ToString()), GetVal("PSD_Y", str.ToString()), GetVal("PSD_Z", str.ToString()));
                //
                DockMatrix = new MatrixD(GetVal("MC11", str.ToString()), GetVal("MC12", str.ToString()), GetVal("MC13", str.ToString()), GetVal("MC14", str.ToString()),
                GetVal("MC21", str.ToString()), GetVal("MC22", str.ToString()), GetVal("MC23", str.ToString()), GetVal("MC24", str.ToString()),
                GetVal("MC31", str.ToString()), GetVal("MC32", str.ToString()), GetVal("MC33", str.ToString()), GetVal("MC34", str.ToString()),
                GetVal("MC41", str.ToString()), GetVal("MC42", str.ToString()), GetVal("MC43", str.ToString()), GetVal("MC44", str.ToString()));
                DrillMatrix = new MatrixD(GetVal("MD11", str.ToString()), GetVal("MD12", str.ToString()), GetVal("MD13", str.ToString()), GetVal("MD14", str.ToString()),
                GetVal("MD21", str.ToString()), GetVal("MD22", str.ToString()), GetVal("MD23", str.ToString()), GetVal("MD24", str.ToString()),
                GetVal("MD31", str.ToString()), GetVal("MD32", str.ToString()), GetVal("MD33", str.ToString()), GetVal("MD34", str.ToString()),
                GetVal("MD41", str.ToString()), GetVal("MD42", str.ToString()), GetVal("MD43", str.ToString()), GetVal("MD44", str.ToString()));
                PlanetCenter = new Vector3D(GetVal("PX", str.ToString()), GetVal("PY", str.ToString()), GetVal("PZ", str.ToString()));
                BaseDockPoint = new Vector3D(0, 0, -200);
                DrillPoint = GetSpiralXY(ShaftN, DrillFrameWidth, DrillFrameLength);
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                values.Append("curent_programm: " + ((int)curent_programm).ToString() + ";\n");
                values.Append("curent_mode: " + ((int)curent_mode).ToString() + ";\n");
                values.Append("pause: " + pause.ToString() + ";\n");
                values.Append("go_home: " + go_home.ToString() + ";\n");

                values.Append("FlyHeight: " + Math.Round(FlyHeight, 0) + ";\n");
                values.Append("ShaftN: " + ShaftN.ToString() + ";\n");
                values.Append("EmergencyReturn: " + EmergencyReturn.ToString() + ";\n");
                //
                values.Append("CB1_id: " + connector_base1.id.ToString() + ";\n");
                values.Append(connector_base1.point.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "CB1_X").Replace("Y", "CB1_Y").Replace("Z", "CB1_Z") + ";\n");
                values.Append(connector_base1.vector.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "CBV1_X").Replace("Y", "CBV1_Y").Replace("Z", "CBV1_Z") + ";\n");
                values.Append("CB1_load: " + connector_base1.load.ToString() + ";\n");
                values.Append("CB1_position: " + connector_base1.position.ToString() + ";\n");
                //
                //values.Append(point_start_drill.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PSD_X").Replace("Y", "PSD_Y").Replace("Z", "PSD_Z") + ";\n");
                //
                values.Append(DockMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "MC"));
                values.Append(DrillMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "MD"));
                values.Append(PlanetCenter.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PX").Replace("Y", "PY").Replace("Z", "PZ") + ";\n");

                lcd_info.OutText(values);
            }
            public string TextInfo1()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ    : " + Math.Round(remote_control.GetShipSpeed(), 2) + "\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                return values.ToString();
            }
            public string TextInfo2()
            {
                StringBuilder values = new StringBuilder();
                //values.Append("СКОРОСТЬ    : " + Math.Round(remote_control.GetShipSpeed(), 2) + "\n");
                values.Append("Height            : " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(FlyHeight).ToString() + "\n");
                values.Append("Distance          : " + Math.Round(Distance).ToString() + "\n");
                values.Append("Phys./Crit.(Mass) : " + Math.Round(PhysicalMass).ToString() + " / " + CriticalMass + "\n");
                values.Append("CurrentVolume     : " + CurrentVolume + "\n");
                values.Append("CurrentMass       : " + CurrentMass + "\n");
                values.Append("Глубина шахты     : " + DrillDepth + "\n");
                values.Append("UpAccel     : " + UpAccel + "\n");
                return values.ToString();
            }
            public string TextTEST()
            {
                StringBuilder values = new StringBuilder();
                return values.ToString();
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "+":
                        SetOverridePercent("U", 0.4f);
                        break;
                    case "-":
                        SetOverridePercent("U", 0);
                        break;
                    case "load":
                        LoadFromStorage();
                        break;
                    case "save":
                        SaveToStorage();
                        break;
                    case "stop":
                        Stop();
                        break;
                    case "clear":
                        Clear();
                        curent_programm = programm.none;
                        break;
                    case "save_height":
                        SetFlyHeight();
                        break;
                    case "save_base1":
                        SetDockMatrix();
                        break;
                    case "save_drill":
                        SetDrillMatrixDepo();
                        break;
                    case "fly_base":
                        curent_programm = programm.fly_connect_base;
                        SaveToStorage();
                        break;
                    case "fly_drill":
                        curent_programm = programm.fly_drill;
                        SaveToStorage();
                        break;
                    case "start_drill":
                        curent_programm = programm.start_drill;
                        SaveToStorage();
                        break;
                    case "go_home":
                        {
                            go_home = true;
                            //if (thisDriller.Paused)
                            //    thisDriller.Pause();
                            break;
                        }
                    case "to_base":
                        curent_mode = mode.to_base;
                        SaveToStorage();
                        break;
                    case "dock":
                        curent_mode = mode.dock;
                        SaveToStorage();
                        break;
                    case "un_dock":
                        curent_mode = mode.un_dock;
                        SaveToStorage();
                        break;
                    case "to_drill":
                        curent_mode = mode.to_drill;
                        SaveToStorage();
                        break;
                    case "drill_align":
                        curent_mode = mode.drill_align;
                        SaveToStorage();
                        break;
                    case "drill":
                        curent_mode = mode.drill;
                        SaveToStorage();
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Обновим состояние навигации
                    UpdateCalc();
                    if (curent_programm == programm.fly_connect_base)
                    {
                        FlyConnectBase();
                    }
                    if (curent_programm == programm.fly_drill)
                    {
                        FlyDrill();
                    }
                    if (curent_programm == programm.start_drill)
                    {
                        StartDrill();
                    }
                    if (curent_mode == mode.un_dock)
                    {
                        if (UnDock() && curent_programm == programm.none)
                        {
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.to_base)
                    {
                        if (ToBase() && curent_programm == programm.none)
                        {
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.dock)
                    {
                        if (Dock() && curent_programm == programm.none)
                        {
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.to_drill)
                    {
                        if (ToDrillPoint() && curent_programm == programm.none)
                        {
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.drill_align)
                    {
                        if (DrillAlign() && curent_programm == programm.none)
                        {
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.drill)
                    {
                        if (Drill(out EmergencyReturn) && curent_programm == programm.none)
                        {
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.pull_up)
                    {
                        if (PullUp() && curent_programm == programm.none)
                        {
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.pull_out)
                    {
                        if (PullOut() && curent_programm == programm.none)
                        {
                            curent_mode = mode.none;
                        }
                    }
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


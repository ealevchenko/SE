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
namespace KROTIK_A1M_NAV_P
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
            //---------------------------
            float GyroMult = 1f;
            float AlignAccelMult = 0.3f;
            float TargetSize = 100;

            public Vector3D TargetNorm { get; private set; }
            //---------------------------
            public enum mode : int
            {
                none = 0,
                to_base = 1,
            };
            public static string[] name_mode = { "", "К БАЗЕ", };
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
            public List<IMyThrust> UpThrusters;
            public List<IMyThrust> DownThrusters;
            public List<IMyThrust> LeftThrusters;
            public List<IMyThrust> RightThrusters;
            public List<IMyThrust> ForwardThrusters;
            public List<IMyThrust> BackwardThrusters;
            public double UpThrMax { get; private set; } = 0;
            public double DownThrMax { get; private set; } = 0;
            public double LeftThrMax { get; private set; } = 0;
            public double RightThrMax { get; private set; } = 0;
            public double ForwardThrMax { get; private set; } = 0;
            public double BackwardThrMax { get; private set; } = 0;
            //---------------------------------------------------
            public float XMaxA { get; private set; }
            public float YMaxA { get; private set; }
            public float ZMaxA { get; private set; }

            public Vector3D PlanetCenter = new Vector3D(0.50, 0.50, 0.50);

            private Vector3D BaseDockPoint = new Vector3D(0, 0, -200);
            public MatrixD DockMatrix { get; private set; }

            public StringBuilder Status = new StringBuilder();


            public Navigation(Cockpit cockpit, Connector connector, string NameObj, string NameRemoteControl, string NameCameraCourse)
            {
                this.cockpit = cockpit;
                this.connector = connector;
                remote_control = _scr.GridTerminalSystem.GetBlockWithName(NameRemoteControl) as IMyRemoteControl;
                camera_course = _scr.GridTerminalSystem.GetBlockWithName(NameCameraCourse) as IMyCameraBlock;
                _scr.GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusts, r => (r.CustomName.Contains(NameObj)));
                _scr.GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros, r => (r.CustomName.Contains(NameObj)));
                // Орентация двигателей
                Matrix ThrLocM = new Matrix();
                Matrix MainLocM = new Matrix();
                remote_control.Orientation.GetMatrix(out MainLocM);
                UpThrusters = new List<IMyThrust>();
                DownThrusters = new List<IMyThrust>();
                LeftThrusters = new List<IMyThrust>();
                RightThrusters = new List<IMyThrust>();
                ForwardThrusters = new List<IMyThrust>();
                BackwardThrusters = new List<IMyThrust>();
                UpThrMax = 0;
                DownThrMax = 0;
                LeftThrMax = 0;
                RightThrMax = 0;
                ForwardThrMax = 0;
                BackwardThrMax = 0;
                foreach (IMyThrust Thrust in thrusts) { 
                    Thrust.Orientation.GetMatrix(out ThrLocM);
                    //Y
                    if (ThrLocM.Backward == MainLocM.Up)
                    {
                        UpThrusters.Add(Thrust);
                        UpThrMax += Thrust.MaxEffectiveThrust;
                    }
                    else if (ThrLocM.Backward == MainLocM.Down)
                    {
                        DownThrusters.Add(Thrust);
                        DownThrMax += Thrust.MaxEffectiveThrust;
                    }
                    //X
                    else if (ThrLocM.Backward == MainLocM.Left)
                    {
                        LeftThrusters.Add(Thrust);
                        LeftThrMax += Thrust.MaxEffectiveThrust;
                    }
                    else if (ThrLocM.Backward == MainLocM.Right)
                    {
                        RightThrusters.Add(Thrust);
                        RightThrMax += Thrust.MaxEffectiveThrust;
                    }
                    //Z
                    else if (ThrLocM.Backward == MainLocM.Forward)
                    {
                        ForwardThrusters.Add(Thrust);
                        ForwardThrMax += Thrust.MaxEffectiveThrust;
                    }
                    else if (ThrLocM.Backward == MainLocM.Backward)
                    {
                        BackwardThrusters.Add(Thrust);
                        BackwardThrMax += Thrust.MaxEffectiveThrust;
                    }                
                }
                _scr.Echo("remote_control: " + ((remote_control != null) ? ("Ок") : ("not block")));
                _scr.Echo("camera_course: " + ((camera_course != null) ? ("Ок") : ("not block")));
                _scr.Echo("thrusts: " + ((thrusts.Count() > 0) ? thrusts.Count().ToString() + "шт." : ("not block")));
                _scr.Echo("gyros: " + ((gyros.Count() > 0) ? gyros.Count().ToString() + "шт." : ("not block")));
            }
            //-------------------------------------
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

                TargetNorm = Vector3D.Normalize(Vector3D.Reject(Target - V3Dcenter, GravNorm));

                //double TargetPitch = Vector3D.Dot(V3Dfow, Vector3D.Normalize(Vector3D.Reject(-GravNorm, V3Dleft)));
                //TargetPitch = Math.Acos(TargetPitch) - Math.PI / 2;

                //double TargetRoll = Vector3D.Dot(V3Dleft, Vector3D.Reject(-GravNorm, V3Dfow));
                //TargetRoll = Math.Acos(TargetRoll) - Math.PI / 2;

                //double TargetYaw = Math.Acos(Vector3D.Dot(V3Dfow, TargetNorm));
                //if ((V3Dleft - TargetNorm).Length() < Math.Sqrt(2))
                //    TargetYaw = -TargetYaw;

                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(remote_control.WorldMatrix.Forward);
                double gL = GravNorm.Dot(remote_control.WorldMatrix.Left);
                double gU = GravNorm.Dot(remote_control.WorldMatrix.Up);
                //Получаем сигналы по тангажу и крены операцией atan2
                double TargetRoll = (float)Math.Atan2(gL, -gU); // крен
                double TargetPitch = -(float)Math.Atan2(gF, -gU); // тангаж

                //Рысканием прицеливаемся на точку Target.
                double tF = TargetNorm.Dot(remote_control.WorldMatrix.Forward);
                double tL = TargetNorm.Dot(remote_control.WorldMatrix.Left);
                double TargetYaw = -(float)Math.Atan2(tL, tF);

                if (double.IsNaN(TargetYaw)) TargetYaw = 0;
                if (double.IsNaN(TargetPitch)) TargetPitch = 0;
                if (double.IsNaN(TargetRoll)) TargetRoll = 0;

                //return new Vector3D(TargetYaw, TargetPitch, TargetRoll);
                return new Vector3D(0, 0, 0);
            }
            private void SetGroupThrust(List<IMyThrust> ThrList, float Thr)
            {
                for (int i = 0; i < ThrList.Count; i++)
                {
                     ThrList[i].ThrustOverridePercentage = Thr;
                }
            }
            public void SetThrF(Vector3D ThrVec)
            {
                SetGroupThrust(this.thrusts, 0f);
                //X
                if (ThrVec.X > 0)
                {
                    SetGroupThrust(RightThrusters, (float)(ThrVec.X / RightThrMax));
                }
                else
                {
                    SetGroupThrust(LeftThrusters, -(float)(ThrVec.X / LeftThrMax));
                }
                //Y
                if (ThrVec.Y > 0)
                {
                    SetGroupThrust(UpThrusters, (float)(ThrVec.Y / UpThrMax));
                }
                else
                {
                    SetGroupThrust(DownThrusters, -(float)(ThrVec.Y / DownThrMax));
                }
                //Z
                if (ThrVec.Z > 0)
                {
                    SetGroupThrust(BackwardThrusters, (float)(ThrVec.Z / BackwardThrMax));
                }
                else
                {
                    SetGroupThrust(ForwardThrusters, -(float)(ThrVec.Z / ForwardThrMax));
                }
            }
            public void SetThrA(Vector3D ThrVec)
            {
                SetThrF(ThrVec * PhysicalMass);
            }
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
                //-------------------
                YMaxA = (float)Math.Min(UpThrMax / PhysicalMass - GravVector.Length(), DownThrMax / PhysicalMass + GravVector.Length());
                ZMaxA = (float)Math.Min(ForwardThrMax, BackwardThrMax) / PhysicalMass;
                XMaxA = (float)Math.Min(RightThrMax, LeftThrMax) / PhysicalMass;
            }
            public bool ToBase()
            {
                bool Complete = false;
                float MaxUSpeed, MaxFSpeed;
                Vector3D gyrAng = GetNavAngles(BaseDockPoint, DockMatrix);
                Vector3D MyPosCon = Vector3D.Transform(MyPos, DockMatrix);
                float Distance = (float)(BaseDockPoint - new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2))).Length();

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
                // myDriller.TextOutput("TP1", "\n" + MaxFSpeed.ToString() + "\n" + ForwVelocityVector.Length().ToString() + "\n" + Distance.ToString());           
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
                    curent_mode = mode.none;
                    ClearThrustOverridePersent();
                    Complete = true;
                }
                Status.Clear();
                Status.Append(" STATUS\n");
                Status.Append("Task: To base\n");
                Status.Append("gyrAng: " + gyrAng.ToString() + "\n");
                Status.Append("YMaxA: " + Math.Round(YMaxA, 2).ToString() + "\n");
                Status.Append("ZMaxA: " + Math.Round(ZMaxA, 2).ToString() + "\n");
                Status.Append("MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                Status.Append("MaxFSpeed: " + Math.Round(MaxFSpeed, 2).ToString() + "\n");
                Status.Append("Height: " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(FlyHeight).ToString() + "\n");
                Status.Append("Distance: " + Math.Round(Distance).ToString() + "\n");
                return Complete;
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ    : " + Math.Round(remote_control.GetShipSpeed(), 2) + "\n");
                //values.Append("ГОРИЗОНТ    : " + (horizont ? green.ToString() : red.ToString()) + ",  T : " + (aim_point ? green.ToString() : red.ToString()) + ",  V : " + (aim_vector ? green.ToString() : red.ToString()) + "\n");
                //values.Append("КОМПЕНСАЦИЯ : " + (compensate ? green.ToString() : red.ToString()) + "\n");
                //values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("T0: " + PText.GetGPS("BaseDockPoint", BaseDockPoint) + "\n");
                values.Append("T1: " + PText.GetGPS("TargetNorm", TargetNorm) + "\n");
                values.Append("DockMatrix: " + DockMatrix.ToString() + "\n");
                //values.Append("T1: " + PText.GetGPS("Target1", Target1) + "\n");
                //values.Append("T2: " + PText.GetGPS("Target2", Target2) + "\n");
                return values.ToString();
            }
            public string TextTEST()
            {
                StringBuilder values = new StringBuilder();
                values.Append(Status);
                //
                //values.Append("move_curse    : " + move_fb + "ap_forward :" + ap_forward + "\n");
                //values.Append("КУРС ---------------------------------------\n");
                //values.Append("|- DeltaCurse               : " + Math.Round(DeltaCurse, 2) + "\n");
                //values.Append("|- ForwVelocity             : " + Math.Round(ForwVelocity, 2) + "\n");
                //values.Append("|- ForwardBrakingDistances  : " + Math.Round(ForwardBrakingDistances, 2) + "\n");
                //values.Append("|- BackwardBrakingDistances : " + Math.Round(BackwardBrakingDistances, 2) + "\n");
                //values.Append("ВЫСОТА -------------------------------------\n");
                //values.Append("|- DeltaHeight              : " + Math.Round(DeltaHeight, 2) + "\n");
                //values.Append("|- UpVelocity               : " + Math.Round(UpVelocity, 2) + "\n");
                //values.Append("|- UpBrakingDistances       : " + Math.Round(UpBrakingDistances, 2) + "\n");
                //values.Append("|- DownBrakingDistances     : " + Math.Round(DownBrakingDistances, 2) + "\n");
                //values.Append("ГИРОСКОПЫ ----------------------------------\n");
                //values.Append("|- Yaw-target  : " + Math.Round(YawTarget, 2) + "\n");
                //values.Append("|- Yaw-curse   : " + Math.Round(YawVector, 2) + "\n");
                //values.Append("|- Yaw         : " + Math.Round(YawInput, 2) + "\n");
                //values.Append("|- Roll        : " + Math.Round(RollInput, 2) + "\n");
                //values.Append("|- Pitch       : " + Math.Round(PitchInput, 2) + "\n");
                //values.Append("-----------------------------------------------\n");
                //values.Append("base1 id=" + connector_base1.id + " [" + connector_base1.list_point.Count() + "]\n");
                //int index = 0;
                //foreach (Vector3D p in connector_base1.list_point)
                //{
                //    values.Append(PText.GetGPS("T" + index, p) + "\n");
                //    index++;
                //}
                //values.Append("-----------------------------------------------\n");
                //values.Append("base1 id=" + connector_base2.id + " [" + connector_base2.list_point.Count() + "]\n");
                //index = 0;
                //foreach (Vector3D p in connector_base2.list_point)
                //{
                //    values.Append(PText.GetGPS("T" + index, p) + "\n");
                //    index++;
                //}
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
                    case "clear":
                        ClearThrustOverridePersent();
                        break;                    
                    case "save_height":
                        SetFlyHeight();
                        break;
                    case "save_base1":
                        SetDockMatrix();
                        break;
                    case "to_base":
                        curent_mode = mode.to_base;
                        break;
                    case "F":
                        SetOverrideAccel("F", (float)(ForwardThrMax));
                        break;
                    case "B":
                        SetOverrideAccel("B", (float)(BackwardThrMax));
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Обновим состояние навигации
                    UpdateCalc();
                    if (curent_mode == mode.to_base)
                    {
                        ToBase();
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


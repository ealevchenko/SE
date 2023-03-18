using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using static MUL_H1.Program;
using static MUL_H1.Program.Navigation;
using static SANA1_NAVIGATION.Program;
using static VRage.Game.MyObjectBuilder_CurveDefinition;
/// <summary>
/// v2.0
/// </summary>
namespace MUL_H1
{
    public sealed class Program : MyGridProgram
    {
        // v2.0
        string NameObj = "[MUL-H1]";
        string NameCockpit = "[MUL-H1]-Кресло пилота [LCD]";
        string NameRemoteControl = "[MUL-H1]-ДУ Парковка";
        string NameCameraForward = "[MUL-H1]-Камера вперед";
        string NameCameraConnector = "[MUL-H1]-Камера парковка";
        string NameConnector = "[MUL-H1]-Коннектор парковка";
        string NameLCDInfo = "[MUL-H1]-LCD-INFO";
        string NameLCDInfo_Upr = "[MUL-H1]-LCD-INFO-UPR";
        string NameLCDInfo_Debug = "[MUL-H1]-LCD-INFO-DEBUG";

        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея

        LCD lcd_info;
        LCD lcd_info_upr;
        static LCD lcd_info_debug;
        Batterys bats;
        Connector connector;
        ReflectorsLight reflectors_light;
        Gyros gyros;
        Thrusts thrusts;
        Cockpit cockpit;
        RemoteControl remote_control;
        Camera camera_forward;
        Camera camera_connector;
        Navigation navigation;

        static Program _scr;

        public double minHeight = 1000;

        bool ship_connect = false;
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
            lcd_info_upr = new LCD(NameLCDInfo_Upr);
            lcd_info_debug = new LCD(NameLCDInfo_Debug);
            cockpit = new Cockpit(NameCockpit);
            remote_control = new RemoteControl(NameRemoteControl);
            bats = new Batterys(NameObj);
            connector = new Connector(NameConnector);
            ship_connect = connector.Connected;
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
            camera_forward = new Camera(NameCameraForward);
            camera_connector = new Camera(NameCameraConnector);
            navigation = new Navigation(cockpit, remote_control, thrusts, gyros, camera_forward, camera_connector);
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            bats.Logic(argument, updateSource);
            cockpit.Logic(argument, updateSource);
            remote_control.Logic(argument, updateSource);
            navigation.Logic(argument, updateSource);
            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                // Проверим корабль не припаркован
                if (!connector.Connected)
                {
                    if (minHeight > cockpit.CurrentHeight)
                    {
                        bats.Auto();
                        thrusts.On();
                        cockpit.Dampeners(true);
                    }
                    // Проверка кокпит не под контроллем включить тормоз
                    //if (!cockpit.IsUnderControl)
                    //{
                    //    cockpit.Dampeners(true);
                    //    reflectors_light.Off();
                    //}
                }
                else
                {
                    // Припаркован
                    reflectors_light.Off();
                    cockpit.Dampeners(false);
                    bats.Charger();
                    thrusts.Off();
                }
            }
            values_info.Append(bats.TextInfo());
            values_info.Append(connector.TextInfo());
            values_info.Append(thrusts.TextInfo());
            //values_info.Append(remote_control.TextInfo());
            values_info.Append(cockpit.TextInfo());
            values_info.Append(navigation.TextInfo());
            //cockpit.OutText(values_info, 0);
            lcd_info_upr.OutText(values_info);
            ship_connect = connector.Connected; // сохраним состояние

            StringBuilder test_info = new StringBuilder();
            //test_info.Append("home1 : " + remote_control.home_position.ToString() + "\n");
            //test_info.Append("home2 : " + remote_control.home_position1.ToString() + "\n");
            //test_info.Append("home3 : " + remote_control.home_position2.ToString() + "\n");
            test_info.Append("ThrustsMax : " + thrusts.Forward_ThrustsMax / 1000 + "\n");
            test_info.Append("TotalMass : " + cockpit.TotalMass / 1000 + "\n");
            test_info.Append("ShipSpeed : " + cockpit.ShipSpeed + "\n");
            test_info.Append(navigation.TextTEST());
            //test_info.Append("Цель: " + target.ToString() + "\n");
            //test_info.Append("Растояние: " + cockpit.GetDistance(target_info.HitPosition != null ? (Vector3D)target_info.HitPosition : new Vector3D(0, 0, 0)).ToString() + "\n");
            //test_info.Append("SCAN: " + camera.obj.CanScan(dist_scan) + "\n");

            lcd_info.OutText(test_info);
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
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorsLight(string name_obj) : base(name_obj)
            {
            }
            public ReflectorsLight(string name_obj, string tag) : base(name_obj, tag)
            {

            }
        }
        public class Gyros : BaseListTerminalBlock<IMyGyro>
        {
            public Gyros(string name_obj) : base(name_obj)
            {
            }
            public Gyros(string name_obj, string tag) : base(name_obj, tag)
            {

            }

            public float getPitch() {
                return base.list_obj.Select(g => g.Pitch).Sum();
            }
            public float getRoll() {
                return base.list_obj.Select(g => g.Roll).Sum();
            }
            public float getYaw() {
                return base.list_obj.Select(g => g.Yaw).Sum();
            }
            public void SetGyro(float Yaw, float Pitch, float Roll)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    gyro.Yaw = Yaw;
                    gyro.Pitch = Pitch;
                    gyro.Roll = Roll;
                }
            }
            public void GyroOver(bool over)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    gyro.Yaw = 0;
                    gyro.Pitch = 0;
                    gyro.Roll = 0;
                    gyro.GyroOverride = over;
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Yaw" + this.getYaw() + "\n");
                values.Append("Pitch" + this.getPitch() + "\n");
                values.Append("Roll" + this.getRoll() + "\n");
                return values.ToString();
            }
        }
        public class Thrusts : BaseListTerminalBlock<IMyThrust>
        {
            Matrix ThrusterMatrix = new MatrixD();
            private double UpThrMax = 0;
            private double DownThrMax = 0;
            private double LeftThrMax = 0;
            private double RightThrMax = 0;
            private double ForwardThrMax = 0;
            private double BackwardThrMax = 0;
            public double Up_ThrustsMax { get { return this.UpThrMax; } }
            public double Down_ThrustsMax { get { return this.DownThrMax; } }
            public double Left_ThrustsMax { get { return this.LeftThrMax; } }
            public double Right_ThrustsMax { get { return this.RightThrMax; } }
            public double Forward_ThrustsMax { get { return this.ForwardThrMax; } }
            public double Backward_ThrustsMax { get { return this.BackwardThrMax; } }
            public Thrusts(string name_obj) : base(name_obj)
            {
            }
            public Thrusts(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            public void ClearThrMax()
            {
                UpThrMax = 0;
                DownThrMax = 0;
                LeftThrMax = 0;
                RightThrMax = 0;
                ForwardThrMax = 0;
                BackwardThrMax = 0;
            }
            public void GetMaxEffectiveThrust(Matrix CockpitMatrix)
            {
                ClearThrMax();
                foreach (IMyThrust Thruster in base.list_obj)
                {
                    Thruster.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == CockpitMatrix.Up)
                    {
                        UpThrMax += Thruster.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
                    {
                        DownThrMax += Thruster.MaxEffectiveThrust;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
                    {
                        LeftThrMax += Thruster.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
                    {
                        RightThrMax += Thruster.MaxEffectiveThrust;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
                    {
                        ForwardThrMax += Thruster.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
                    {
                        BackwardThrMax += Thruster.MaxEffectiveThrust;
                    }
                }
            }
            public void GetMaxThrust(Matrix CockpitMatrix)
            {
                ClearThrMax();
                foreach (IMyThrust Thruster in base.list_obj)
                {
                    Thruster.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == CockpitMatrix.Up)
                    {
                        UpThrMax += Thruster.MaxThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
                    {
                        DownThrMax += Thruster.MaxThrust;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
                    {
                        LeftThrMax += Thruster.MaxThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
                    {
                        RightThrMax += Thruster.MaxThrust;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
                    {
                        ForwardThrMax += Thruster.MaxThrust;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
                    {
                        BackwardThrMax += Thruster.MaxThrust;
                    }
                }
            }
            public void SetThrustOverride(Vector3 move, float persent)
            {
                foreach (IMyThrust Thruster in base.list_obj)
                {
                    Thruster.Orientation.GetMatrix(out ThrusterMatrix);

                    if (ThrusterMatrix.Forward == move)
                    {
                        Thruster.ThrustOverridePercentage = (float)persent;
                    }
                    else if (ThrusterMatrix.Backward == move)
                    {
                        Thruster.ThrustOverridePercentage = (float)persent;
                    }
                    else if (ThrusterMatrix.Up == move)
                    {
                        Thruster.ThrustOverridePercentage = (float)persent;
                    }
                    else if (ThrusterMatrix.Down == move)
                    {
                        Thruster.ThrustOverridePercentage = (float)persent;
                    }
                    else if (ThrusterMatrix.Left == move)
                    {
                        Thruster.ThrustOverridePercentage = (float)persent;
                    }
                    else if (ThrusterMatrix.Right == move)
                    {
                        Thruster.ThrustOverridePercentage = (float)persent;
                    }
                }
            }
            public void SetThrustOverridePersent(Matrix CockpitMatrix, float up, float down, float left, float right, float forward, float backward)
            {
                foreach (IMyThrust Thruster in base.list_obj)
                {
                    Thruster.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == CockpitMatrix.Up)
                    {
                        Thruster.ThrustOverridePercentage = up;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
                    {
                        Thruster.ThrustOverridePercentage = down;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
                    {
                        Thruster.ThrustOverridePercentage = left;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
                    {
                        Thruster.ThrustOverridePercentage = right;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
                    {
                        Thruster.ThrustOverridePercentage = forward;
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
                    {
                        Thruster.ThrustOverridePercentage = backward;
                    }
                }
            }
            public void SetThrustOverride(Matrix CockpitMatrix, double up_trust, double down_trust, double left_trust, double right_trust, double forward_trust, double backward_trust)
            {
                GetMaxEffectiveThrust(CockpitMatrix);

                foreach (IMyThrust Thruster in base.list_obj)
                {
                    Thruster.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == CockpitMatrix.Up)
                    {
                        Thruster.ThrustOverridePercentage = (float)(up_trust / UpThrMax);
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
                    {
                        Thruster.ThrustOverridePercentage = (float)(down_trust / DownThrMax);
                    }
                    //X
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
                    {
                        Thruster.ThrustOverridePercentage = (float)(left_trust / LeftThrMax);
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
                    {
                        Thruster.ThrustOverridePercentage = (float)(right_trust / RightThrMax);
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
                    {
                        Thruster.ThrustOverridePercentage = (float)(forward_trust / ForwardThrMax);
                    }
                    else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
                    {
                        Thruster.ThrustOverridePercentage = (float)(backward_trust / BackwardThrMax);
                    }
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Up: " + PText.GetThrust((float)UpThrMax) + ", ");
                values.Append("Down: " + PText.GetThrust((float)DownThrMax) + "\n");
                values.Append("Left: " + PText.GetThrust((float)LeftThrMax) + ", ");
                values.Append("Right: " + PText.GetThrust((float)RightThrMax) + "\n");
                values.Append("Forward: " + PText.GetThrust((float)ForwardThrMax) + ", ");
                values.Append("Backward: " + PText.GetThrust((float)BackwardThrMax) + "\n");
                return values.ToString();
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
                values.Append("Скорость: " + base.obj.GetShipSpeed() + "\n");
                values.Append("Высота: " + current_height + "\n");
                return values.ToString();
            }
        }
        public class RemoteControl : BaseTerminalBlock<IMyRemoteControl>
        {
            public Vector3D home_position = new Vector3D();
            public Vector3D home_position1 = new Vector3D();
            public Vector3D home_position2 = new Vector3D();
            public IMyShipController _obj { get { return obj; } }
            public bool IsUnderControl { get { return obj.IsUnderControl; } }
            public bool ControlThrusters { get { return obj.ControlThrusters; } }
            public RemoteControl(string name) : base(name)
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
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "home_position":

                        Vector3D vector_Forward = base.obj.WorldMatrix.Forward;
                        Vector3D vector_Up = base.obj.WorldMatrix.Up;
                        home_position = base.GetPosition() - (vector_Forward * 2);
                        home_position1 = (home_position - (vector_Forward * 20));
                        home_position2 = (home_position1 + (vector_Up * 100));
                        break;
                    case "auto_home":
                        base.obj.ClearWaypoints();
                        base.obj.AddWaypoint(home_position2, "БАЗА-Up");
                        base.obj.AddWaypoint(home_position1, "БАЗА-Forward");
                        base.obj.AddWaypoint(home_position, "БАЗА");
                        base.obj.SetAutoPilotEnabled(true);
                        break;
                    case "auto_off":
                        base.obj.SetAutoPilotEnabled(false);
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
                values.Append("ДОМ: " + home_position.ToString() + "\n");
                values.Append("АВТОПИЛОТ: " + (base.obj.IsAutoPilotEnabled ? "ВКЛ" : "ОТК") + "\n");
                return values.ToString();
            }
        }
        public class Camera : BaseTerminalBlock<IMyCameraBlock>
        {
            public Camera(string name) : base(name)
            {
                base.obj.EnableRaycast = true;
            }
            public bool CanScan(double distance)
            {

                return base.obj.CanScan(distance);
            }
            public MyDetectedEntityInfo? Raycast(double dist_scan, float pitch_scan, float yaw_scan)
            {
                MyDetectedEntityInfo? result = null;
                //base.obj.EnableRaycast = true;
                if (this.CanScan(dist_scan))
                {
                    result = base.obj.Raycast(dist_scan, pitch_scan, yaw_scan);
                }
                //base.obj.EnableRaycast = false;
                return result;
            }
            public Vector3D GetVectorForward()
            {
                return obj.WorldMatrix.Forward;
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();

                return values.ToString();
            }
            public string GetTextDetectedEntityInfo(MyDetectedEntityInfo? info)
            {
                StringBuilder values = new StringBuilder();
                if (info != null)
                {
                    values.Append("Name: " + ((MyDetectedEntityInfo)info).Name + "\n");
                    values.Append("Type: " + ((MyDetectedEntityInfo)info).Type + "\n");
                    values.Append("HitPosition: " + ((MyDetectedEntityInfo)info).HitPosition + "\n");
                    values.Append("Orientation: " + ((MyDetectedEntityInfo)info).Orientation + "\n");
                    values.Append("Velocity: " + ((MyDetectedEntityInfo)info).Velocity + "\n");
                    values.Append("Relationship: " + ((MyDetectedEntityInfo)info).Relationship + "\n");
                    values.Append("BoundingBox: " + ((MyDetectedEntityInfo)info).BoundingBox + "\n");
                    values.Append("TimeStamp: " + ((MyDetectedEntityInfo)info).TimeStamp + "\n");
                    values.Append("EntityId: " + ((MyDetectedEntityInfo)info).EntityId + "\n");
                };
                return values.ToString();
            }
        }
        // V1.0
        public class Navigation
        {
            Vector3D target;
            Vector3D course;
            MyDetectedEntityInfo? target_info;
            double dist_scan = 5000;
            float pitch_scan = 0;
            float yaw_scan = 0;

            public double ForwardThrust = 0;
            public double LeftThrust = 0;
            public double UpThrust = 0;
            public double BackwardThrust = 0;
            public double RightThrust = 0;
            public double DownThrust = 0;

            bool compensate = false;
            public enum orientation : int
            {
                none = 0,
                horizon_down = 1,
                horizon_backward = 2,
                horizon_forward = 3,
                course_forward = 4,
            };
            public static string[] name_horizont = { "", "Горизонт", "Вверх", "Вниз", "Курс" };
            public enum mode : int
            {
                none = 0,
                landing_planet = 1,
                taking_planet = 2,
                flying_course = 3,
                flying_target = 4,
            };
            public static string[] name_mode = { "", "Посадка на планету", "Взлет с планеты", "Полет по курсу", "Полет к цели" };
            orientation curent_orientation = orientation.none;
            mode curent_mode = mode.none;
            int pr_mode = 0;
            int max_spid = 200;
            int reserve_hieght = 400 + 300; // мин растояние на которое действует отключать двигатели (останавливается за 300м)

            public class braking
            {
                public double a { get; }
                public double t { get; }
                public double s { get; }
                public braking(double a, double t, double s)
                {
                    this.a = a; this.t = t; this.s = s;
                }
            }
            const int a_loading = -5; //(-3...-5)

            Cockpit cockpit;
            RemoteControl remote_control;
            Thrusts thrusts;
            Gyros gyros;
            Camera camera_forward;
            Camera camera_connector;

            public Navigation(Cockpit cockpit, RemoteControl remote_control, Thrusts thrusts, Gyros gyros, Camera camera_forward, Camera camera_connector)
            {
                this.cockpit = cockpit;
                this.remote_control = remote_control;
                this.thrusts = thrusts;
                this.gyros = gyros;
                this.camera_forward = camera_forward;
                this.camera_connector = camera_connector;
            }
            // Космос тормозной путь
            public braking GetBrakingSpace(double max_thrusts)
            {
                double a = (max_thrusts / 1000) * (1 / (cockpit.BaseMass / 1000));
                double t = cockpit.ShipSpeed / a;           //t = V / a
                double s = (a * Math.Pow(t, 2)) / 2;        // S = ( a * t^2 ) / 2
                return new braking(a, t, s);
            }
            // Посадка с гпавитацией тормозной путь
            public braking GetBrakingLanding(double max_thrusts)
            {
                double a = (max_thrusts / 1000) * (1 / (cockpit.BaseMass / 1000));
                double t = (0 - cockpit.ShipSpeed) / -a; //t = (V - V[0]) / a
                double s = (cockpit.ShipSpeed * t) + ((-a) * Math.Pow(t, 2)) / 2; //S = V[0] * t + ( a * t^2 ) / 2
                return new braking((double)-a, t, s);
            }
            public Vector3D GetGravNormalize()
            {
                Vector3D GravityVector = cockpit._obj.GetNaturalGravity();
                Vector3D GravNorm = Vector3D.Normalize(GravityVector);
                return GravNorm;
            }
            public void DownHorizon()
            {
                Vector3D GravNorm = GetGravNormalize();
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(cockpit._obj.WorldMatrix.Forward);
                double gL = GravNorm.Dot(cockpit._obj.WorldMatrix.Left);
                double gU = GravNorm.Dot(cockpit._obj.WorldMatrix.Up);

                //Получаем сигналы по тангажу и крены операцией atan2
                float RollInput = (float)Math.Atan2(gL, -gU); // крен
                float PitchInput = -(float)Math.Atan2(gF, -gU); // тангаж

                //На рыскание просто отправляем сигнал рыскания с контроллера. Им мы будем управлять вручную.
                float YawInput = 0;
                if (remote_control.IsUnderControl)
                {
                    YawInput = remote_control._obj.RotationIndicator.Y;
                }
                else if (cockpit.IsUnderControl)
                {
                    YawInput = cockpit._obj.RotationIndicator.Y;
                }
                gyros.SetGyro(YawInput, PitchInput, RollInput);
            }
            public void BackwardHorizon()
            {
                Vector3D GravNorm = GetGravNormalize();
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(cockpit._obj.WorldMatrix.Down);
                double gL = GravNorm.Dot(cockpit._obj.WorldMatrix.Left);
                double gU = GravNorm.Dot(cockpit._obj.WorldMatrix.Forward);

                //Получаем сигналы по тангажу и крены операцией atan2
                float YawInput = (float)Math.Atan2(gL, -gU); // крен  // YawInput
                float PitchInput = -(float)Math.Atan2(gF, -gU); // тангаж

                //На рыскание просто отправляем сигнал рыскания с контроллера. Им мы будем управлять вручную.
                float RollInput = 0;
                if (remote_control.IsUnderControl)
                {
                    RollInput = remote_control._obj.RotationIndicator.Y; // RollInput
                }
                else if (cockpit.IsUnderControl)
                {
                    RollInput = cockpit._obj.RotationIndicator.Y;
                }
                gyros.SetGyro(YawInput, PitchInput, RollInput);
            }
            public void ForwardHorizon()
            {
                Vector3D GravNorm = GetGravNormalize();
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(cockpit._obj.WorldMatrix.Up);
                double gL = GravNorm.Dot(cockpit._obj.WorldMatrix.Right);
                double gU = GravNorm.Dot(cockpit._obj.WorldMatrix.Backward);

                //Получаем сигналы по тангажу и крены операцией atan2
                float YawInput = (float)Math.Atan2(gL, -gU); // крен  // YawInput
                float PitchInput = -(float)Math.Atan2(gF, -gU); // тангаж

                //На рыскание просто отправляем сигнал рыскания с контроллера. Им мы будем управлять вручную.
                float RollInput = 0;
                if (remote_control.IsUnderControl)
                {
                    RollInput = remote_control._obj.RotationIndicator.Y; // RollInput
                }
                else if (cockpit.IsUnderControl)
                {
                    RollInput = cockpit._obj.RotationIndicator.Y;
                }
                gyros.SetGyro(YawInput, PitchInput, RollInput);
            }
            public void ForwardСourse()
            {
                Vector3D course = Vector3D.Normalize(this.course);

                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = course.Dot(cockpit._obj.WorldMatrix.Up);
                double gL = course.Dot(cockpit._obj.WorldMatrix.Right);
                double gU = course.Dot(cockpit._obj.WorldMatrix.Backward);

                //Получаем сигналы по тангажу и крены операцией atan2
                float YawInput = (float)Math.Atan2(gL, -gU); // крен  // YawInput
                float PitchInput = -(float)Math.Atan2(gF, -gU); // тангаж

                //На рыскание просто отправляем сигнал рыскания с контроллера. Им мы будем управлять вручную.
                float RollInput = 0;
                if (remote_control.IsUnderControl)
                {
                    RollInput = remote_control._obj.RotationIndicator.Y; // RollInput
                }
                else if (cockpit.IsUnderControl)
                {
                    RollInput = cockpit._obj.RotationIndicator.Y;
                }
                gyros.SetGyro(YawInput, PitchInput, RollInput);
            }
            public void CompensateWeight()
            {
                Vector3D GravityVector = cockpit.GetNaturalGravity;
                float ShipMass = cockpit.PhysicalMass;

                Vector3D ShipWeight = GravityVector * ShipMass;

                Vector3D HoverThrust = new Vector3D();

                ForwardThrust = (ShipWeight + HoverThrust).Dot(cockpit._obj.WorldMatrix.Forward);
                LeftThrust = (ShipWeight + HoverThrust).Dot(cockpit._obj.WorldMatrix.Left);
                UpThrust = (ShipWeight + HoverThrust).Dot(cockpit._obj.WorldMatrix.Up);
                BackwardThrust = -ForwardThrust;
                RightThrust = -LeftThrust;
                DownThrust = -UpThrust;
                this.thrusts.SetThrustOverride(cockpit.GetCockpitMatrix(), UpThrust, DownThrust, LeftThrust, RightThrust, ForwardThrust, BackwardThrust);

            }
            public void LandingPlanet()
            {
                switch (pr_mode)
                {
                    case 0:
                        {
                            // Проверка возможна операция?
                            curent_orientation = orientation.horizon_backward;
                            pr_mode = 1; // Набор скорости
                            break;
                        }
                    case 1:
                        {
                            this.thrusts.On();
                            this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 1.0f, 0);
                            pr_mode = 2; // проверка скорости и тормозного пути ()
                            break;
                        }
                    case 2:
                        {
                            if (this.cockpit.ShipSpeed >= max_spid)
                            {
                                // отключим трастеры
                                this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0);
                                this.thrusts.Off(); // свободное падение
                            }
                            braking londing = GetBrakingLanding(thrusts.Forward_ThrustsMax);
                            if (this.cockpit.CurrentHeight <= londing.s + reserve_hieght)
                            {
                                // Надо тормозить
                                pr_mode = 3;
                            }
                            break;
                        }
                    case 3:
                        {
                            this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0.0f);
                            this.thrusts.On();
                            cockpit.Dampeners(true);
                            pr_mode = 4; // Набор скорости
                            break;
                        }
                    case 4:
                        {
                            if (this.cockpit.ShipSpeed <= 0.2f)
                            {
                                // Скорость погашена сбросим режим посадки
                                curent_mode = mode.none;
                                pr_mode = 0;
                            }
                            break;
                        }
                }
            }
            public void TakingPlanet()
            {
                switch (pr_mode)
                {
                    case 0:
                        {
                            // Проверка возможна операция?
                            compensate = false;
                            curent_orientation = orientation.horizon_backward;
                            pr_mode = 1; // Набор скорости
                            break;
                        }
                    case 1:
                        {
                            this.thrusts.On();
                            this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 1.0f);
                            pr_mode = 2; // проверка скорости и включить компенсации ()
                            break;
                        }
                    case 2:
                        {
                            if (this.cockpit.ShipSpeed >= max_spid)
                            {
                                // отключим компенсаторы
                                compensate = true;
                                this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0);
                            }
                            if (cockpit.GetNaturalGravity.Length() <= 0.2f)
                            {
                                compensate = false;
                                cockpit.Dampeners(true);
                                pr_mode = 3;
                            }
                            break;
                        }
                    case 3:
                        {
                            if (this.cockpit.ShipSpeed <= 0.2f)
                            {
                                // Скорость погашена сбросим режим посадки
                                curent_mode = mode.none;
                                pr_mode = 0;
                            }
                            break;
                        }
                }
            }
            public void FlyingCourse()
            {
                StringBuilder test_info = new StringBuilder();
                test_info.Append("pr_mode : " + pr_mode + "\n");
                test_info.Append("pr_mode : " + gyros.TextInfo() + "\n");
                switch (pr_mode)
                {
                    case 0:
                        {
                            // Проверка возможна операция?
                            curent_orientation = orientation.course_forward;
                            pr_mode = 1; // Набор скорости
                            break;
                        }
                    case 1:
                        {
                            this.thrusts.On();
                            cockpit.Dampeners(false);
                            this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 1.0f);
                            pr_mode = 2; // проверка скорости и включить компенсации ()
                            break;
                        }
                    case 2:
                        {
                            target_info = camera_forward.Raycast(dist_scan, pitch_scan, yaw_scan);
                            if (target_info != null && ((MyDetectedEntityInfo)target_info).Type != MyDetectedEntityType.None)
                            {
                                braking space = GetBrakingSpace(thrusts.Forward_ThrustsMax);
                                double curr_dist = cockpit.GetDistance((Vector3D)((MyDetectedEntityInfo)target_info).HitPosition);
                                test_info.Append("Space: a=" + Math.Round(space.a, 2) + "t=" + Math.Round(space.t, 2) + "S=" + Math.Round(space.s, 2) + "\n");
                                test_info.Append("curr_dist : " + curr_dist + "\n");
                                if (curr_dist <= space.s + reserve_hieght)
                                {
                                    // Надо тормозить
                                    cockpit.Dampeners(true);
                                    curent_orientation = orientation.none;
                                    pr_mode = 3;
                                }
                            }
                            else
                            {
                                if (this.cockpit.ShipSpeed >= max_spid)
                                {
                                    this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0);
                                    //pr_mode = 3;
                                }
                            }
                            break;
                        }
                    case 3:
                        {
                            if (this.cockpit.ShipSpeed <= 0.2f)
                            {
                                // Скорость погашена сбросим режим посадки
                                curent_mode = mode.none;
                                pr_mode = 0;
                            }
                            break;
                        }
                }
                lcd_info_debug.OutText(test_info);
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "course":
                        course = camera_forward.GetVectorForward();
                        break;
                    case "target":
                        MyDetectedEntityInfo? tg_info = camera_forward.Raycast(this.dist_scan, this.pitch_scan, this.yaw_scan);
                        if (tg_info != null && ((MyDetectedEntityInfo)tg_info).Type != MyDetectedEntityType.None)
                        {
                            target = (Vector3D)((MyDetectedEntityInfo)tg_info).HitPosition;
                        }
                        break;
                    case "scan":
                        target_info = camera_forward.Raycast(dist_scan, pitch_scan, yaw_scan);
                        break;
                    case "horizont_down_on":
                        curent_orientation = orientation.horizon_down;
                        break;
                    case "horizont_down_off":
                        curent_orientation = orientation.none;
                        break;
                    case "horizont_down":
                        if (curent_orientation == orientation.horizon_down)
                        {
                            curent_orientation = orientation.none;
                        }
                        else
                        {
                            curent_orientation = orientation.horizon_down;
                        }
                        break;
                    case "horizont_backward_on":
                        curent_orientation = orientation.horizon_backward;
                        break;
                    case "horizont_backward_off":
                        curent_orientation = orientation.none;
                        break;
                    case "horizont_backward":
                        if (curent_orientation == orientation.horizon_backward)
                        {
                            curent_orientation = orientation.none;
                        }
                        else
                        {
                            curent_orientation = orientation.horizon_backward;
                        }
                        break;
                    case "horizont_forward_on":
                        curent_orientation = orientation.horizon_forward;
                        break;
                    case "horizont_forward_off":
                        curent_orientation = orientation.none;
                        break;
                    case "horizont_forward":
                        if (curent_orientation == orientation.horizon_forward)
                        {
                            curent_orientation = orientation.none;
                        }
                        else
                        {
                            curent_orientation = orientation.horizon_forward;
                        }
                        break;
                    case "course_forward_on":
                        curent_orientation = orientation.course_forward;
                        break;
                    case "course_forward_off":
                        curent_orientation = orientation.none;
                        break;
                    case "course_forward":
                        if (curent_orientation == orientation.course_forward)
                        {
                            curent_orientation = orientation.none;
                        }
                        else
                        {
                            curent_orientation = orientation.course_forward;
                        }
                        break;
                    case "mode_landing_planet_on":
                        curent_mode = mode.landing_planet;
                        break;
                    case "mode_landing_planet_off":
                        curent_mode = mode.none;
                        break;
                    case "mode_landing_planet":
                        if (curent_mode == mode.landing_planet)
                        {
                            curent_mode = mode.none;
                        }
                        else
                        {
                            curent_mode = mode.landing_planet;
                        }
                        break;
                    case "mode_taking_planet_on":
                        curent_mode = mode.taking_planet;
                        break;
                    case "mode_taking_planet_off":
                        curent_mode = mode.none;
                        break;
                    case "mode_taking_planet":
                        if (curent_mode == mode.taking_planet)
                        {
                            curent_mode = mode.none;
                        }
                        else
                        {
                            curent_mode = mode.taking_planet;
                        }
                        break;
                    case "mode_flying_course_on":
                        curent_mode = mode.flying_course;
                        break;
                    case "mode_flying_course_off":
                        curent_mode = mode.none;
                        break;
                    case "mode_flying_course":
                        if (curent_mode == mode.flying_course)
                        {
                            curent_mode = mode.none;
                        }
                        else
                        {
                            curent_mode = mode.flying_course;
                        }
                        break;
                    //
                    case "test100":
                        this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 1.0f);
                        break;
                    case "test0":
                        this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0.0f);
                        break;
                    case "compensate_on":
                        compensate = true;
                        break;
                    case "compensate_off":
                        compensate = false;
                        this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0.0f);
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Получим макс эффективность трастеров по направлениям
                    thrusts.GetMaxEffectiveThrust(cockpit.GetCockpitMatrix());
                    // Сбросим подпрограммы
                    if (curent_mode == mode.none)
                    {
                        pr_mode = 0;
                        curent_orientation = orientation.none;
                        this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0.0f);
                        cockpit.Dampeners(true);
                    }

                    // режим горизонт
                    if (curent_orientation == orientation.none)
                    {
                        gyros.GyroOver(false);
                    }
                    else
                    {
                        gyros.GyroOver(true);
                    }
                    // Режимы удержания горизонта
                    if (curent_orientation == orientation.horizon_down)
                    {
                        DownHorizon();
                    }
                    if (curent_orientation == orientation.horizon_backward)
                    {
                        BackwardHorizon();
                    }
                    if (curent_orientation == orientation.horizon_forward)
                    {
                        ForwardHorizon();
                    }
                    if (curent_orientation == orientation.course_forward)
                    {
                        ForwardСourse();
                    }
                    if (curent_mode == mode.landing_planet)
                    {
                        LandingPlanet();
                    }
                    if (curent_mode == mode.taking_planet)
                    {
                        TakingPlanet();
                    }
                    if (curent_mode == mode.flying_course)
                    {
                        FlyingCourse();
                    }
                    if (compensate)
                    {
                        CompensateWeight();
                    }
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ОРИНТАЦИЯ: " + name_horizont[(int)curent_orientation] + "\n");
                values.Append("РЕЖИМ: " + name_mode[(int)curent_mode] + ", pr : " + pr_mode + "\n");
                values.Append("КОМПЕНСАЦИЯ: " + (compensate ? "ВКЛ" : "ВЫК") + "\n");
                values.Append("Курс: " + course + "\n");
                values.Append("ЦЕЛЬ: " + PText.GetVector3D(target) + "\n");
                values.Append("РАССТОЯНИЕ: " + cockpit.GetDistance(target) + "\n");
                return values.ToString();
            }
            public string TextTEST()
            {
                StringBuilder values = new StringBuilder();
                braking space = GetBrakingSpace(thrusts.Forward_ThrustsMax);
                braking londing = GetBrakingLanding(thrusts.Forward_ThrustsMax);
                values.Append("Space: a=" + Math.Round(space.a, 2) + "t=" + Math.Round(space.t, 2) + "S=" + Math.Round(space.s, 2) + "\n");
                values.Append("Londing: a=" + Math.Round(londing.a, 2) + "t=" + Math.Round(londing.t, 2) + "S=" + Math.Round(londing.s, 2) + "\n");
                values.Append("Thrust: up=" + PText.GetThrust((float)UpThrust) + "down=" + PText.GetThrust((float)DownThrust) + "\n");
                values.Append("Thrust: left=" + PText.GetThrust((float)LeftThrust) + "right=" + PText.GetThrust((float)RightThrust) + "\n");
                values.Append("Thrust: forw=" + PText.GetThrust((float)ForwardThrust) + "back=" + PText.GetThrust((float)BackwardThrust) + "\n");
                values.Append("SCAN - " + camera_forward.CanScan(this.dist_scan) + "\n");
                values.Append("Растояние: " + (target_info != null && ((MyDetectedEntityInfo)target_info).HitPosition != null ? cockpit.GetDistance((Vector3D)((MyDetectedEntityInfo)target_info).HitPosition).ToString() : "") + "\n");
                values.Append(camera_forward.GetTextDetectedEntityInfo(target_info) + "\n");
                return values.ToString();
            }
        }
    }
}
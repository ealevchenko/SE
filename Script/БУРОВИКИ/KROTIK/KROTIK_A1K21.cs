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
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRageMath;
/// <summary>
/// v2.0
/// </summary>
namespace KROTIK_A1K21
{
    public sealed class Program : MyGridProgram
    {
        // v3.0
        string NameObj = "[KROTIK_A1]";
        string NameCockpit = "[KROTIK_A1]-Промышленный кокпит [LCD]";
        string NameRemoteControl = "[KROTIK_A1]-ДУ Парковка";
        string NameConnector = "[KROTIK_A1]-Коннектор парковка";
        string NameCameraCourse = "[KROTIK_A1]-Камера парковка";
        string NameLCDInfo = "[KROTIK_A1]-LCD-INFO";

        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея
        public enum or_mtr : int
        {
            not = 0, up = 1, down = 2, left = 3, right = 4, forward = 5, backward = 6
        };
        static LCD lcd_info;
        Batterys bats;
        Connector connector;
        ShipDrill drill;
        ReflectorsLight reflectors_light;
        Gyros gyros;
        Thrusts thrusts;
        Cockpit cockpit;
        RemoteControl remote_control;
        Camera camera_course;
        Navigation navigation;

        static Program _scr;

        bool ship_connect = false;
        //bool horizont = false;
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
            cockpit = new Cockpit(NameCockpit);
            remote_control = new RemoteControl(NameRemoteControl);
            bats = new Batterys(NameObj);
            connector = new Connector(NameConnector);
            ship_connect = connector.Connected;
            drill = new ShipDrill(NameObj);
            drill.Off();
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
            camera_course = new Camera(NameCameraCourse);
            navigation = new Navigation(cockpit, remote_control, thrusts, gyros, camera_course);
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
                //// Проверим корабль не припаркован
                //if (!connector.Connected)
                //{
                //    if (navigation.curent_programm == Navigation.programm.none && cockpit.CurrentHeight > 5.0f)
                //    {
                //        bats.Auto();
                //        thrusts.On();
                //        cockpit.Dampeners(true);
                //    }
                //    // Проверка кокпит не под контроллем включить тормоз
                //    if (!cockpit.IsUnderControl)
                //    {
                //        cockpit.Dampeners(true);
                //        reflectors_light.Off();
                //        drill.Off();
                //    }
                //    if (drill.Enabled())
                //    {
                //        reflectors_light.On();
                //    }
                //}
                //else
                //{
                //    // Припаркован
                //    reflectors_light.Off();
                //    drill.Off();
                //    cockpit.Dampeners(false);
                //    bats.Charger();
                //    thrusts.Off();
                //}
            }
            //values_info.Append(bats.TextInfo());
            values_info.Append(connector.TextInfo());
            values_info.Append(drill.TextInfo());
            //values_info.Append(remote_control.TextInfo());
            values_info.Append(navigation.TextInfo());

            cockpit.OutText(values_info, 0);

            ship_connect = connector.Connected; // сохраним состояние

            StringBuilder test_info = new StringBuilder();
            test_info.Append(navigation.TextTEST());
            lcd_info.OutText(test_info);
            //cockpit.OutText(test_info, 1);

            StringBuilder debug_info = new StringBuilder();
            debug_info.Append(remote_control.TextInfo());
            cockpit.OutText(debug_info, 1);
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
        public class Gyros : BaseListTerminalBlock<IMyGyro>
        {
            public Gyros(string name_obj) : base(name_obj)
            {
            }
            public Gyros(string name_obj, string tag) : base(name_obj, tag)
            {

            }

            public float getPitch()
            {
                return base.list_obj.Select(g => g.Pitch).Average();
            }
            public float getRoll()
            {
                return base.list_obj.Select(g => g.Roll).Average();
            }
            public float getYaw()
            {
                return base.list_obj.Select(g => g.Yaw).Average();
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
                values.Append("Yaw :" + this.getYaw() + "\n");
                values.Append("Pitch :" + this.getPitch() + "\n");
                values.Append("Roll :" + this.getRoll() + "\n");
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
            public MyShipVelocities GetShipVelocities { get { return base.obj.GetShipVelocities(); } }
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
        public class RemoteControl : BaseTerminalBlock<IMyRemoteControl>
        {
            public Vector3D base_connection = new Vector3D();
            public Vector3D base_pre_connection = new Vector3D();
            public Vector3D base_pre_vector_connection = new Vector3D();

            public Vector3D base_space_connection = new Vector3D();
            public Vector3D base_pre_space_connection = new Vector3D();
            public Vector3D base_pre_space_vector_connection = new Vector3D();

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
            public bool ControlThrusters { get { return obj.ControlThrusters; } }
            public Matrix GetCockpitMatrix()
            {
                Matrix CockpitMatrix = new MatrixD();
                base.obj.Orientation.GetMatrix(out CockpitMatrix);
                return CockpitMatrix;
            }
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
                    case "base_connection":
                        base_pre_vector_connection = base.obj.WorldMatrix.Forward;
                        base_connection = base.GetPosition();
                        base_pre_connection = (base_connection - (base_pre_vector_connection * 300));
                        break;
                    case "base_space_connection":
                        base_pre_space_vector_connection = base.obj.WorldMatrix.Forward;
                        base_space_connection = base.GetPosition();
                        base_pre_space_connection = (base_connection - (base_space_connection * 300));
                        break;

                    case "auto_home":
                        //base.obj.ClearWaypoints();
                        //base.obj.AddWaypoint(home_position2, "БАЗА-Up");
                        //base.obj.AddWaypoint(home_position1, "БАЗА-Forward");
                        //base.obj.AddWaypoint(home_position, "БАЗА");
                        //base.obj.SetAutoPilotEnabled(true);
                        break;
                    case "auto_off":
                        //base.obj.SetAutoPilotEnabled(false);
                        break;
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
                values.Append("Высота: " + current_height + "\n");
                values.Append("Гравитация: " + base.obj.GetNaturalGravity().Length() + "\n");
                values.Append("BaseMass: " + this.BaseMass + "\n");
                values.Append("TotalMass: " + this.TotalMass + "\n");
                values.Append("PhysicalMass: " + this.PhysicalMass + "\n");
                values.Append("Скорость: " + base.obj.GetShipSpeed() + "\n");
                //values.Append("ДОМ: " + base_connection.ToString() + "\n");
                //values.Append("КОСМОС: " + base_space_connection.ToString() + "\n");
                //values.Append("АВТОПИЛОТ: " + (base.obj.IsAutoPilotEnabled ? "ВКЛ" : "ОТК") + "\n");
                return values.ToString();
            }
        }
        public class Camera : BaseTerminalBlock<IMyCameraBlock>
        {
            public MyBlockOrientation Orientation { get { return base.obj.Orientation; } }
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
                    //values.Append("TimeStamp: " + ((MyDetectedEntityInfo)info).TimeStamp + "\n");
                    //values.Append("EntityId: " + ((MyDetectedEntityInfo)info).EntityId + "\n");
                };
                return values.ToString();
            }
        }
        public class Navigation
        {
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
            public class dist_braking
            {
                public double distance { get; }
                public double braking { get; }
                public bool active_braking { get; }
                public bool active_distance { get; }
                public dist_braking(double distance, double braking, bool active_braking, bool active_distance)
                {
                    this.distance = distance;
                    this.braking = braking;
                    this.active_braking = active_braking;
                    this.active_distance = active_distance;
                }
            }

            Cockpit cockpit;
            RemoteControl remote_control;
            Thrusts thrusts;
            Gyros gyros;
            Camera camera_course;
            //-------------------------------------------------------
            Matrix cockpit_matrix = new Matrix();
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
            public Vector3D gVector { get; private set; }
            public float g { get; private set; } = 0;
            public float PhysicalMass { get; private set; } = 0;
            public Vector3D ShipWeight { get; private set; } // Вес коробля вект гравит + физ масса
            public double CurrentSpeed { get; private set; }
            public double VerticalSpeedTick { get; private set; }
            public double VerticalSpeed { get; private set; }
            public double HorizontSpeedTick { get; private set; }
            public double HorizontSpeed { get; private set; }
            //public float XTaskSpeed { get; private set; }// скорость задание лево\право
            public float YTaskSpeed { get; private set; }// скорость задание вверх\вниз
            public float ZTaskSpeed { get; private set; }// скорость задание Вперед\назад
            public float MaxUSpeed { get; private set; }
            public or_mtr TaskCurrOrentation { get; private set; } = or_mtr.not;
            public double CurrentHeight { get; private set; }
            public double CurrentPlanetCentr { get; private set; }
            public Vector3D CurrentPosition { get; private set; }
            public Vector3D TackTarget { get; private set; }
            public double OldHeight { get; private set; }
            public double DeltaHeight { get; private set; }
            public double YTaskHeight { get; private set; } = 200f;
            public double YMinHeight { get; private set; } = 100f;
            public double YMaxHeight { get; private set; } = 3000f;
            public double OldCurse { get; private set; } = 0f;
            public double DeltaCurse { get; private set; } = 0f;
            public double YTaskCurse { get; private set; } = 0f;

            public string move = "";

            public Vector3D PlanetCentr = new Vector3D(0.50, 0.50, 0.50);
            public Vector3D Target1 = new Vector3D(53634.1408339977, -26848.4945197565, 11835.781022294); // GPS:Target1:53634.1408339977:-26848.4945197565:11835.781022294:
            public Vector3D Target2 = new Vector3D(54247.1045229673, -28025.4557401103, 9975.66911975904);  // GPS:Target2:54247.1045229673:-28025.4557401103:9975.66911975904:
            public double planeta_target { get; private set; } = 0;

            Vector3D? TaskVector = null;
            public float YawInput { get; private set; } = 0;
            public float RollInput { get; private set; } = 0;
            public float PitchInput { get; private set; } = 0;

            //Vector3D target = new Vector3D(26827.8655273466, -23658.4360006724, 99710.1771295082);
            Vector3D target = new Vector3D(108169.40, -36240.93, -17712.65);
            Vector3D course;
            Vector3D base_connection = new Vector3D();
            Vector3D base_pre_connection = new Vector3D();
            Vector3D base_space_connection = new Vector3D();
            Vector3D base_pre_vector_connection = new Vector3D();
            Vector3D station_space_connection = new Vector3D();
            Vector3D station_pre_space_connection = new Vector3D();
            Vector3D station_pre_space_vector_connection = new Vector3D();

            bool compensate = false;
            bool horizont = false;
            bool height = false;
            bool curse = false;

            MyDetectedEntityInfo? target_info;
            bool scan = false;
            double dist_scan = 5000;
            float pitch_scan = 0;
            float yaw_scan = 0;
            public enum orientation : int
            {
                none = 0,
                horizon_down = 1,
                course_forward = 2,
                target_forward = 3,
            };
            public static string[] name_orientation = { "", "Горизонт", "Курс", "Точка" };
            public enum programm : int
            {
                none = 0,
                flight_up = 1,
                flight_down = 2,
                flight_forward = 3,
                flight_backward = 4,
            };
            public static string[] name_programm = { "", "ВЗЛЕТ", "СПУСК", "ВПЕРЕД", "НАЗАД" };
            orientation curent_orientation = orientation.none;
            public programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                aiming = 1,
                speed = 2,
                speed_control = 3,
                flight = 4,
                braking = 5,
                braking_control = 6,
            };
            public static string[] name_mode = { "", "Наводка", "Разгон", "Контроль скорости", "Полет", "Торможение", "Контроль торможения" };
            mode curent_mode = mode.none;

            int max_spid = 100;
            int min_height_distance = 100; // 
            int max_height_distance = 4000; // 
            double start_disnance = 0;
            double task_speed = 0;
            or_mtr task_orentation = or_mtr.not;

            public Navigation(Cockpit cockpit, RemoteControl remote_control, Thrusts thrusts, Gyros gyros, Camera camera_course)
            {
                this.cockpit = cockpit;
                this.remote_control = remote_control;
                this.thrusts = thrusts;
                this.gyros = gyros;
                this.camera_course = camera_course;
            }
            public bool IsAimingOrentationGyros()
            {
                if (gyros.getPitch() != 0.0f || gyros.getRoll() != 0.0f)
                {
                    if (Math.Abs(gyros.getPitch()) + Math.Abs(gyros.getYaw()) + Math.Abs(gyros.getRoll()) < 0.01f)
                    {
                        return true;
                    }
                }
                return false;
            }
            public void Update()
            {
                Matrix ThrusterMatrix = new MatrixD();
                cockpit_matrix = remote_control.GetCockpitMatrix();
                // Определим макс
                foreach (IMyThrust Thruster in thrusts.list_obj)
                {
                    Thruster.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == cockpit_matrix.Up)
                    {
                        UpThrMax += Thruster.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == cockpit_matrix.Down)
                    {
                        DownThrMax += Thruster.MaxEffectiveThrust;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == cockpit_matrix.Left)
                    {
                        LeftThrMax += Thruster.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == cockpit_matrix.Right)
                    {
                        RightThrMax += Thruster.MaxEffectiveThrust;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == cockpit_matrix.Forward)
                    {
                        ForwardThrMax += Thruster.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == cockpit_matrix.Backward)
                    {
                        BackwardThrMax += Thruster.MaxEffectiveThrust;
                    }
                }
                gVector = remote_control.GetNaturalGravity;
                g = (float)remote_control.GetNaturalGravity.Length();
                PhysicalMass = remote_control.PhysicalMass;
                CurrentSpeed = remote_control.ShipSpeed;
                CurrentHeight = remote_control.CurrentHeight;
                CurrentPosition = remote_control.GetPosition();
                CurrentPlanetCentr = (PlanetCentr - CurrentPosition).Length();
            }
            public void Up(float power)
            {
                // UP MAX
                double delta = UpThrMax - UpThrust;
                if (delta > 0)
                {
                    UpThrust = UpThrust + (delta * power);
                }
                UpThrust = UpThrMax;
                DownThrust = -DownThrMax;
                move = "UP :" + power.ToString();
            }
            public void Down(float power)
            {
                double delta = DownThrMax - DownThrust;
                if (delta > 0)
                {
                    DownThrust = DownThrust + (delta * power);
                }
                UpThrust = -UpThrMax;
                move = "DOWN :" + power.ToString();
                //DownThrust = DownThrMax;
            }
            public void Forward(float power)
            {
                double delta = ForwardThrMax - ForwardThrust;
                if (delta > 0)
                {
                    ForwardThrust = ForwardThrust + (delta * power);
                }
                BackwardThrust = 0;
                move = "Forward :" + power.ToString();
            }
            public void Backward(float power)
            {
                double delta = BackwardThrMax - BackwardThrust;
                if (delta > 0)
                {
                    BackwardThrust = BackwardThrust + (delta * power);
                }
                ForwardThrust = 0;
                move = "Backward :" + power.ToString();
            }
            public void ControlHeight()
            {
                DeltaHeight = CurrentPlanetCentr - YTaskHeight;
                //DeltaHeight -= 1;
                VerticalSpeedTick = (CurrentPlanetCentr - OldHeight);
                VerticalSpeed = VerticalSpeedTick * 6; // Вертикальная скорость
                OldHeight = CurrentPlanetCentr;
                // Определим скорость
                YTaskSpeed = (float)Math.Sqrt(2 * Math.Abs(DeltaHeight) * g) / 2;
                if (DeltaHeight > -0.5f && DeltaHeight < 0.5f && VerticalSpeed >= -0.5 && VerticalSpeed < 0.5f)
                {
                    // стоп попали
                    move = "STOP";
                    //compensate = false;
                    height = false;
                }
                else
                {
                    if (VerticalSpeed < 0.5f)
                    {
                        // летим вниз
                        if (DeltaHeight < 0.5f)
                        {
                            // а, надо вверх - ТОРМОЗИМ
                            Down(1f);
                        }
                        else
                        {
                            if (Math.Abs(VerticalSpeed) < YTaskSpeed - VerticalSpeedTick)
                            {
                                // разгон
                                Up((float)(YTaskSpeed - Math.Abs(VerticalSpeed)) / YTaskSpeed); // разгон вверх
                            }
                            else if (Math.Abs(VerticalSpeed) > YTaskSpeed)
                            {
                                // тормоз
                                if (YTaskSpeed > 5)
                                {
                                    Down(1f);
                                }
                                else { Down(0.7f); }
                            }
                            else
                            {
                                // летим с компенсацией
                                move = "COMPENSATE";
                            }
                        }
                    }
                    else if (VerticalSpeed > 0.5f)
                    {
                        // летим вверх
                        if (DeltaHeight > 0.5f)
                        {
                            // а, надо вниз - ТОРМОЗИМ
                            Up(1f);
                        }
                        else
                        {
                            if (Math.Abs(VerticalSpeed) < YTaskSpeed - VerticalSpeedTick)
                            {
                                // разгон
                                //Down((float)(YTaskSpeed - Math.Abs(VerticalSpeed)) / YTaskSpeed); // разгон вверх
                                if (YTaskSpeed > 5)
                                {
                                    Down(1.0f); // разгон вверх
                                }
                                else
                                {
                                    Down(0.3f); // разгон вверх
                                }
                            }
                            else if (Math.Abs(VerticalSpeed) > YTaskSpeed)
                            {
                                // тормоз
                                //Up(1f);
                                if (YTaskSpeed > 5)
                                {
                                    Up(1f);
                                }
                                else { Down(0.3f); }
                            }
                            else
                            {
                                // летим с компенсацией
                                move = "COMPENSATE";
                            }
                        }
                    }
                    else
                    {
                        if (DeltaHeight < 0.3f)
                        {
                            // вверх
                            Down(1f);
                        }
                        else if (DeltaHeight > 0.3f)
                        {
                            // Вниз
                            Up(1f);
                        }
                    }
                }
            }
            public void ControlCurse()
            {

                if (TaskVector != null)
                {
                    DeltaCurse = (TackTarget - CurrentPosition).Length();
                    HorizontSpeedTick = (OldCurse-DeltaCurse);
                    HorizontSpeed = VerticalSpeedTick * 6; // Вертикальная скорость
                    OldCurse = DeltaCurse;
                    // Определим скорость
                    ZTaskSpeed = (float)Math.Sqrt(2 * Math.Abs(DeltaHeight) * g) / 2;
                    if (DeltaCurse > 0f && DeltaCurse < 0.5f && HorizontSpeed >= 0 && HorizontSpeed < 0.5f)
                    {
                        // стоп попали
                        move = "STOP";
                        //compensate = false;
                        curse = false;
                    }
                    else {
                        if (Math.Abs(HorizontSpeed) < ZTaskSpeed - HorizontSpeedTick)
                        {
                            // разгон
                            Backward((float)(ZTaskSpeed - Math.Abs(HorizontSpeed)) / ZTaskSpeed); // разгон вверх
                        }
                        else if (Math.Abs(HorizontSpeed) > ZTaskSpeed)
                        {
                            // тормоз
                            if (YTaskSpeed > 5)
                            {
                                Forward(1f);
                            }
                            else { Forward(0.7f); }
                        }
                    }
                }
            }
            public void CompensateWeight(bool on)
            {
                ForwardThrust = 0;
                LeftThrust = 0;
                UpThrust = 0;
                BackwardThrust = 0;
                RightThrust = 0;
                DownThrust = 0;
                if (on)
                {
                    //ShipWeight = gVector * PhysicalMass;
                    //Vector3D HoverThrust = new Vector3D();

                    Vector3D GravityVector = cockpit._obj.GetNaturalGravity();
                    float ShipMass = cockpit._obj.CalculateShipMass().PhysicalMass;

                    ShipWeight = GravityVector * ShipMass;

                    ForwardThrust = ShipWeight.Dot(cockpit._obj.WorldMatrix.Forward);
                    BackwardThrust = -ForwardThrust;

                    LeftThrust = ShipWeight.Dot(cockpit._obj.WorldMatrix.Left);
                    RightThrust = -LeftThrust;

                    UpThrust = ShipWeight.Dot(cockpit._obj.WorldMatrix.Up);
                    DownThrust = -UpThrust;

                    //if (height)
                    //{
                    //    ControlHeight();
                    //}
                    //if (curse)
                    //{
                    //    ControlCurse();
                    //}
                }
                else
                {
                    ForwardThrust = 0;
                    LeftThrust = 0;
                    UpThrust = 0;
                    BackwardThrust = 0;
                    RightThrust = 0;
                    DownThrust = 0;
                    YTaskSpeed = 0;
                    move = "off";
                }
                Matrix ThrusterMatrix = new MatrixD();
                // Распределим по трастерам
                foreach (IMyThrust Thruster in thrusts.list_obj)
                {
                    Thruster.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == cockpit_matrix.Up)
                    {
                        Thruster.ThrustOverridePercentage = (float)(UpThrust / UpThrMax);
                    }
                    else if (ThrusterMatrix.Forward == cockpit_matrix.Down)
                    {
                        Thruster.ThrustOverridePercentage = (float)(DownThrust / DownThrMax);
                    }
                    //X
                    else if (ThrusterMatrix.Forward == cockpit_matrix.Left)
                    {
                        Thruster.ThrustOverridePercentage = (float)(LeftThrust / LeftThrMax);
                    }
                    else if (ThrusterMatrix.Forward == cockpit_matrix.Right)
                    {
                        Thruster.ThrustOverridePercentage = (float)(RightThrust / RightThrMax);
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == cockpit_matrix.Forward)
                    {
                        Thruster.ThrustOverridePercentage = (float)(ForwardThrust / ForwardThrMax);
                    }
                    else if (ThrusterMatrix.Forward == cockpit_matrix.Backward)
                    {
                        Thruster.ThrustOverridePercentage = (float)(BackwardThrust / BackwardThrMax);
                    }
                }
                //lcd_info.OutText(test_info);
            }
            public void Horizon()
            {
                Vector3D GravityVector = remote_control.GetNaturalGravity;
                Vector3D GravNorm = Vector3D.Normalize(GravityVector);
                //На рыскание просто отправляем сигнал рыскания с контроллера. Им мы будем управлять вручную если не указан вектр.
                YawInput = 0;
                if (TaskVector != null)
                {
                    //вектор на точку
                    Vector3D T = Vector3D.Normalize((Vector3D)TaskVector);
                    //Рысканием прицеливаемся на точку Target.
                    double tF = T.Dot(remote_control.WorldMatrix.Forward);
                    double tL = T.Dot(remote_control.WorldMatrix.Left);
                    YawInput = -(float)Math.Atan2(tL, tF);
                }
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(remote_control._obj.WorldMatrix.Forward);
                double gL = GravNorm.Dot(remote_control._obj.WorldMatrix.Left);
                double gU = GravNorm.Dot(remote_control._obj.WorldMatrix.Up);

                //Получаем сигналы по тангажу и крены операцией atan2
                RollInput = (float)Math.Atan2(gL, -gU); // крен
                PitchInput = -(float)Math.Atan2(gF, -gU); // тангаж

                if (TaskVector == null)
                {
                    if (remote_control.IsUnderControl)
                    {
                        YawInput = remote_control._obj.RotationIndicator.Y;
                    }
                    else if (cockpit.IsUnderControl)
                    {
                        YawInput = cockpit._obj.RotationIndicator.Y;
                    }
                }
                gyros.SetGyro(YawInput, PitchInput, RollInput);
            }
            public void Target(Vector3D target)
            {
                TackTarget = target;
                planeta_target = (PlanetCentr - TackTarget).Length();
                YTaskHeight = planeta_target;
                TaskVector = TackTarget - CurrentPosition;
                compensate = true;
                horizont = true;
                height = false;
                curse = false;
            }
            public void TargetHeight()
            {
                //if (curent_mode == mode.none)
                //{
                //    YTaskHeight = 2000f;
                //    tack_vector = null;
                //    curent_mode = mode.aiming;
                //    horizon = true;
                //    compensate = true;
                //}
                //if (curent_mode == mode.aiming && IsAimingOrentationGyros())
                //{
                //    curent_mode = mode.flight;
                //}
                //if (curent_mode == mode.flight)
                //{
                //    double raz_height = YTaskHeight - remote_control.CurrentHeight;
                //    //YTaskSpeed = (float)Math.Sqrt(2 * raz_height * YMaxA) / 2;
                //    YTaskSpeed = (float)Math.Sqrt(2 * Math.Abs(raz_height) * g) / 2;
                //    if (raz_height < 0) { YTaskSpeed = YTaskSpeed * -1; }
                //    //YTaskSpeed = (float)Math.Sqrt(2 * Math.Abs(remote_control.GetPosition().GetDim(1)) * YMaxA);
                //}
                //if (curent_mode == mode.braking)
                //{
                //    YTaskSpeed = 0;
                //    horizon = false;
                //    compensate = false;
                //    curent_mode = mode.none;
                //    curent_programm = programm.none;
                //}
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "detect":
                        Detect();
                        break;
                    case "target_1":
                        Target1 = this.remote_control.GetPosition();
                        break;
                    case "target_2":
                        Target2 = this.remote_control.GetPosition();
                        break;
                    case "Target1":
                        Target(Target1);
                        break;
                    case "Target2":
                        Target(Target2);
                        break;
                    case "course":
                        course = camera_course.GetVectorForward();
                        break;
                    case "target":
                        MyDetectedEntityInfo? tg_info = camera_course.Raycast(this.dist_scan, this.pitch_scan, this.yaw_scan);
                        if (tg_info != null && ((MyDetectedEntityInfo)tg_info).Type != MyDetectedEntityType.None)
                        {
                            target = (Vector3D)((MyDetectedEntityInfo)tg_info).HitPosition;
                        }
                        break;
                    case "scan":
                        target_info = camera_course.Raycast(dist_scan, pitch_scan, yaw_scan);
                        break;
                    case "set_base_connection":
                        base_pre_vector_connection = this.remote_control.WorldMatrix.Forward;
                        base_connection = this.remote_control.GetPosition();
                        base_pre_connection = (base_connection - (base_pre_vector_connection * -300));
                        base_space_connection = (base_connection - (base_pre_vector_connection * -45000));
                        break;
                    case "set_station_space_connection":
                        station_pre_space_vector_connection = this.remote_control.WorldMatrix.Forward;
                        station_space_connection = this.remote_control.GetPosition();
                        station_pre_space_connection = (station_space_connection - (station_pre_space_vector_connection * 300));
                        break;
                    //
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
                    //
                    case "mode_flight_down_on":
                        curent_programm = programm.flight_down;
                        break;
                    case "mode_flight_down_off":
                        if (curent_programm == programm.flight_down)
                        {
                            curent_mode = mode.braking;
                        }
                        break;
                    case "mode_flight_down":
                        if (curent_programm == programm.flight_down)
                        {
                            curent_mode = mode.braking;
                        }
                        else
                        {
                            curent_programm = programm.flight_down;
                        }
                        break;
                    case "compensate_on":
                        compensate = true;
                        horizont = true;
                        break;
                    case "compensate_off":
                        compensate = false;
                        horizont = false;
                        this.thrusts.SetThrustOverridePersent(this.remote_control.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0.0f);
                        break;
                    case "back_1":
                        compensate = false;
                        horizont = false;
                        this.thrusts.SetThrustOverridePersent(this.remote_control.GetCockpitMatrix(), 0, 0, 0, 0, 0, 1.0f);
                        break;
                    case "forw_1":
                        compensate = false;
                        horizont = false;
                        this.thrusts.SetThrustOverridePersent(this.remote_control.GetCockpitMatrix(), 0, 0, 0, 0, 1.0f, 0.0f);
                        break;
                    case "height_on_200":
                        //YTaskHeight = 200f;
                        //compensate = true;
                        //horizon = true;
                        break;
                    case "height_on_1500":
                        //YTaskHeight = 1500f;
                        //compensate = true;
                        //horizon = true;
                        break;
                    case "height_on_2500":
                        //YTaskHeight = 2500f;
                        //compensate = true;
                        //horizon = true;
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Получим макс эффективность трастеров по направлениям
                    Update();
                    CompensateWeight(compensate);
                    cockpit.Dampeners(!compensate);
                    // режим горизонт
                    if (horizont)
                    {
                        gyros.GyroOver(true);
                    }
                    else
                    {
                        gyros.GyroOver(false);
                    }
                    // Режимы удержания горизонта
                    if (horizont)
                    {
                        Horizon();
                    }
                    //if (curent_programm == programm.flight_down)
                    //{
                    //    TargetHeight(); // Down();
                    //}
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                //values.Append("ОРИНТАЦИЯ: " + name_orientation[(int)curent_orientation] + "\n");
                values.Append("СКОРОСТЬ: " + Math.Round(remote_control.ShipSpeed, 2) + ", ЗАД: " + Math.Round(YTaskSpeed, 2) + "\n");
                values.Append("ГОРИЗОНТ: " + (horizont ? "ВКЛ" : "ВЫК") + "\n");
                values.Append("КОМПЕНСАЦИЯ: " + (compensate ? "ВКЛ" : "ВЫК") + "\n");
                //values.Append("ПРОГРАММА: " + name_programm[(int)curent_programm] + "\n");
                //values.Append("ЭТАП: " + name_mode[(int)curent_mode] + "\n");
                values.Append("T1: " + PText.GetGPS("Target1", Target1) + "\n");
                values.Append("T2: " + PText.GetGPS("Target2", Target2) + "\n");
                //values.Append("ЦЕЛЬ: " + PText.GetVector3D(target) + "\n");
                //values.Append("РАССТОЯНИЕ: " + remote_control.GetDistance(target) + "\n");
                return values.ToString();
            }
            public string TextTEST()
            {
                StringBuilder values = new StringBuilder();
                values.Append("g: " + Math.Round(g, 2) + "\n");
                values.Append("PhysicalMass: " + Math.Round(PhysicalMass, 2) + "\n");
                values.Append("ShipWeight: " + Math.Round(ShipWeight.Length(), 2) + "\n");
                values.Append("Target2: " + Math.Round((Target2 - CurrentPosition).Length(), 2) + "\n");
                values.Append("DeltaCurse: " + Math.Round(DeltaCurse, 2) + "\n");
                values.Append("OldCurse: " + Math.Round(OldCurse, 2) + "\n");
                values.Append("HorizontSpeed: " + Math.Round(HorizontSpeed, 2) + "\n");
                values.Append("ZTaskSpeed: " + Math.Round(ZTaskSpeed, 2) + "\n");
                //values.Append("Yaw: " + Math.Round(YawInput, 2) + "\n");
                //values.Append("Roll: " + Math.Round(RollInput, 2) + "\n");
                //values.Append("Pitch: " + Math.Round(PitchInput, 2) + "\n");
                values.Append("move: " + move + "\n");
                values.Append("CurrentPlanetCentr: " + Math.Round(CurrentPlanetCentr, 2) + "\n");
                values.Append("planeta_target: " + Math.Round(planeta_target, 2) + "\n");
                values.Append("DeltaHeight: " + Math.Round(DeltaHeight, 2) + "\n");
                values.Append("YTaskSpeed: " + Math.Round(YTaskSpeed, 2) + ", CurrentSpeed: " + Math.Round(CurrentSpeed, 2) + "\n");
                values.Append("UP   : " + PText.GetThrust((float)UpThrust) + ", MAX : " + PText.GetThrust((float)UpThrMax) + "\n");
                //values.Append("UP M-C: " + Math.Round(UpThrMax - UpThrust, 2) + "\n");
                values.Append("DOWN : " + PText.GetThrust((float)DownThrust) + ", MAX : " + PText.GetThrust((float)DownThrMax) + "\n");
                values.Append("Forward : " + PText.GetThrust((float)ForwardThrust) + ", MAX : " + PText.GetThrust((float)ForwardThrMax) + "\n");
                values.Append("Backward : " + PText.GetThrust((float)BackwardThrust) + ", MAX : " + PText.GetThrust((float)BackwardThrMax) + "\n");
                //values.Append("UP M-C: " + Math.Round(DownThrMax - DownThrust, 2) + "\n");
                values.Append("YTaskHeight: " + Math.Round(YTaskHeight, 2) + "\n");
                values.Append("CurrentHeight: " + Math.Round(CurrentHeight, 2) + "\n");
                values.Append("deltaSpeed: " + Math.Round(YTaskSpeed - CurrentSpeed, 2) + "\n");
                values.Append("VerticalSpeed: " + Math.Round(VerticalSpeed, 2) + "\n");

                return values.ToString();
            }
            public void Detect()
            {
                MyDetectedEntityInfo? DetectedObject = camera_course.Raycast(10000, 0, 0);
                StringBuilder values = new StringBuilder();
                values.Append("Обнаружено: \n");
                values.Append("Объект: " + ((MyDetectedEntityInfo)DetectedObject).Name + "\n");
                values.Append("Координаты: \n");
                values.Append("     X: " + ((MyDetectedEntityInfo)DetectedObject).Position.X + "\n");
                values.Append("     Y: " + ((MyDetectedEntityInfo)DetectedObject).Position.Y + "\n");
                values.Append("     Z: " + ((MyDetectedEntityInfo)DetectedObject).Position.Z + "\n");

                string GPS = "\nGPS:" + ((MyDetectedEntityInfo)DetectedObject).Name + ":" + ((MyDetectedEntityInfo)DetectedObject).Position.X + ":"
                                      + ((MyDetectedEntityInfo)DetectedObject).Position.Y + ":"
                                      + ((MyDetectedEntityInfo)DetectedObject).Position.Z + ":";
                values.Append(GPS);
                //Если обнаруженный объект - планета, устанавливаем PlanetXYZ
                if (((MyDetectedEntityInfo)DetectedObject).Type == MyDetectedEntityType.Planet)
                {
                    PlanetCentr = ((MyDetectedEntityInfo)DetectedObject).Position;
                }
                cockpit.OutText(values, 2);
            }
        }
    }
}
using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
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
using VRage.Game.ModAPI.Ingame;
using VRageMath;
/// <summary>
/// v2.0
/// </summary>
namespace KROTIK_A1
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
                // Проверим корабль не припаркован
                if (!connector.Connected)
                {
                    if (navigation.curent_programm == Navigation.programm.none && cockpit.CurrentHeight > 5.0f)
                    {
                        bats.Auto();
                        thrusts.On();
                        cockpit.Dampeners(true);
                    }
                    // Проверка кокпит не под контроллем включить тормоз
                    if (!cockpit.IsUnderControl)
                    {
                        cockpit.Dampeners(true);
                        reflectors_light.Off();
                        drill.Off();
                    }
                    if (drill.Enabled())
                    {
                        reflectors_light.On();
                    }
                }
                else
                {
                    // Припаркован
                    reflectors_light.Off();
                    drill.Off();
                    cockpit.Dampeners(false);
                    bats.Charger();
                    thrusts.Off();
                }
            }
            //values_info.Append(bats.TextInfo());
            values_info.Append(connector.TextInfo());
            values_info.Append(drill.TextInfo());
            //values_info.Append(remote_control.TextInfo());
            values_info.Append(navigation.TextInfo());

            cockpit.OutText(values_info, 0);
            ship_connect = connector.Connected; // сохраним состояние

            StringBuilder test_info = new StringBuilder();
            test_info.Append(remote_control.TextInfo());
            cockpit.OutText(test_info, 1);
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
            int count = 0;
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

            MyDetectedEntityInfo? target_info;
            double dist_scan = 5000;
            float pitch_scan = 0;
            float yaw_scan = 0;

            braking curr_braking = new braking(0, 0, 0);

            public double ForwardThrust = 0;
            public double LeftThrust = 0;
            public double UpThrust = 0;
            public double BackwardThrust = 0;
            public double RightThrust = 0;
            public double DownThrust = 0;

            bool compensate = false;
            bool compensate1 = false;
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
                landing_planet = 1,
                taking_planet = 2,
                flying_course = 3,
                flying_target = 4,
                flying_horizont = 5,
            };
            public static string[] name_programm = { "", "Посадка на планету", "Взлет с планеты", "Полет по курсу", "Полет к цели", "Полет по гравитации" };
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
            int reserve_distance = 200; // 
            int min_height_distance = 100; // 
            int max_height_distance = 4000; // 
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
            Cockpit cockpit;
            RemoteControl remote_control;
            Thrusts thrusts;
            Gyros gyros;
            Camera camera_course;

            public Navigation(Cockpit cockpit, RemoteControl remote_control, Thrusts thrusts, Gyros gyros, Camera camera_course)
            {
                this.cockpit = cockpit;
                this.remote_control = remote_control;
                this.thrusts = thrusts;
                this.gyros = gyros;
                this.camera_course = camera_course;
            }
            // Посадка с гпавитацией тормозной путь, Космос тормозной путь
            public braking GetBrakingLanding(double max_thrusts)
            {
                double a = (max_thrusts / 1000) * (1 / (remote_control.TotalMass / 1000)); // Посадка
                double t = (0 - remote_control.ShipSpeed) / -a; //t = (V - V[0]) / a
                double s = (remote_control.ShipSpeed * t) + ((-a) * Math.Pow(t, 2)) / 2; //S = V[0] * t + ( a * t^2 ) / 2
                return new braking((double)-a, t, s);
            }
            public braking GetTakingPlanet(double max_thrusts)
            {
                double a = (max_thrusts / 1000) * (1 / (remote_control.PhysicalMass / 1000)); // Посадка //double a = 0.1f; // Взлет
                double t = (0 - remote_control.ShipSpeed) / -a; //t = (V - V[0]) / a
                double s = (remote_control.ShipSpeed * t) + ((-a) * Math.Pow(t, 2)) / 2; //S = V[0] * t + ( a * t^2 ) / 2
                return new braking((double)-a, t, s);
            }

            public Vector3D GetGravNormalize()
            {
                Vector3D GravityVector = remote_control._obj.GetNaturalGravity();
                Vector3D GravNorm = Vector3D.Normalize(GravityVector);
                return GravNorm;
            }
            public double GetMaxThrustOfProgramm()
            {
                double max_Thrust = 0f;
                if (curent_programm == programm.landing_planet)
                {  // посадка
                    max_Thrust = thrusts.Down_ThrustsMax;
                }
                else if (curent_programm == programm.taking_planet)
                {   // взлет
                    max_Thrust = thrusts.Up_ThrustsMax;
                }
                else if (curent_programm == programm.flying_course || curent_programm == programm.flying_target || curent_programm == programm.flying_horizont)
                {   // курс и цель
                    max_Thrust = thrusts.Forward_ThrustsMax;
                }
                else
                {
                    max_Thrust = thrusts.Forward_ThrustsMax;
                }
                return max_Thrust;


            }
            //public or_mtr GetOrentationOfProgramm()
            //{
            //    or_mtr orentat = or_mtr.not;
            //    if (curent_programm == programm.flying_course || curent_programm == programm.flying_target || curent_programm == programm.flying_horizont)
            //    {   // курс и цель
            //        orentat = or_mtr.forward;
            //    }
            //    else
            //    {
            //        orentat = or_mtr.forward;
            //    }
            //    return orentat;
            //}
            public bool GetProtectionObstacle()
            {
                StringBuilder test_info = new StringBuilder();
                double? distance = null;
                double max_Thrust = GetMaxThrustOfProgramm();
                test_info.Append("max_Thrust :" + max_Thrust + "\n");
                braking space = GetBrakingLanding(max_Thrust);
                MyDetectedEntityInfo? ray = camera_course.Raycast(space.s + reserve_distance, pitch_scan, yaw_scan);
                if (ray != null && ((MyDetectedEntityInfo)ray).Type != MyDetectedEntityType.None)
                {
                    distance = ((Vector3D)((MyDetectedEntityInfo)ray).HitPosition - camera_course.GetPosition()).Length();
                }
                test_info.Append("space a:" + space.a + ", t:" + space.t + "\n");
                test_info.Append("space S:" + space.s + "\n");
                test_info.Append("distance :" + distance + "\n");
                lcd_info.OutText(test_info);
                return distance != null && distance <= space.s + reserve_distance ? true : false;
            }
            public bool GetMinHiegtPlanet()
            {
                StringBuilder test_info = new StringBuilder();
                double max_Thrust = GetMaxThrustOfProgramm();
                braking space = GetBrakingLanding(max_Thrust);
                test_info.Append("max_Thrust :" + max_Thrust + "\n");
                //test_info.Append("space a:" + space.a + ", t:" + space.t + "\n");
                test_info.Append("space S:" + Math.Round(space.s, 2) + "Height :" + Math.Round(remote_control.CurrentHeight, 2) + "\n");
                lcd_info.OutText(test_info);
                if (this.remote_control.CurrentHeight <= space.s + min_height_distance)
                {
                    return true;
                }
                return false;
            }
            public bool GetMaxHiegtPlanet()
            {
                StringBuilder test_info = new StringBuilder();
                double max_Thrust = GetMaxThrustOfProgramm();
                braking space = GetTakingPlanet(max_Thrust);
                test_info.Append("max_Thrust :" + max_Thrust + "\n");
                test_info.Append("space a:" + space.a + ", t:" + space.t + "\n");
                test_info.Append("space S:" + Math.Round(space.s, 2) + "Height :" + Math.Round(remote_control.CurrentHeight, 2) + "\n");
                lcd_info.OutText(test_info);
                if (this.remote_control.CurrentHeight >= max_height_distance - space.s)
                {
                    return true;
                }
                return false;
            }
            public bool GetTarget(Vector3D target)
            {
                double max_Thrust = GetMaxThrustOfProgramm();
                braking space = GetBrakingLanding(max_Thrust);
                double distance = this.remote_control.GetDistance(target);
                if (distance > -space.s && distance < space.s)
                {
                    return true;
                }
                return false;
            }
            public bool ModeOrentationGyros()
            {
                if (gyros.getPitch() != 0.0f || gyros.getYaw() != 0.0f)
                {
                    if (Math.Abs(gyros.getPitch()) + Math.Abs(gyros.getYaw()) < 0.01f)
                    {
                        return true;
                    }
                }
                return false;
            }
            public bool ModeSpeedOn(float up, float down, float left, float right, float forward, float backward)
            {
                this.thrusts.On();
                if (compensate) compensate = false;
                cockpit.Dampeners(false);
                this.thrusts.SetThrustOverridePersent(this.remote_control.GetCockpitMatrix(), up, down, left, right, forward, backward);
                return true;
            }
            public bool ModeSpeedControl()
            {
                if (this.remote_control.ShipSpeed >= max_spid)
                {
                    this.thrusts.SetThrustOverridePersent(this.remote_control.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0);
                    return true;
                }
                return false;
            }
            public bool ModeBrakingOn()
            {

                this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0);
                compensate = false;
                this.thrusts.On();
                cockpit.Dampeners(true);
                return true;
            }
            public bool ModeBrakingControl()
            {
                if (this.remote_control.ShipSpeed <= 0.01f)
                {
                    if (remote_control.GetNaturalGravity.Length() >= 0.01f)
                    {
                        compensate = true;
                        this.thrusts.On();
                    }
                    else
                    {
                        compensate = false;
                        this.thrusts.Off();
                    }
                    return true;
                }
                return false;
            }
            public void DownHorizon()
            {
                Vector3D GravNorm = GetGravNormalize();
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = GravNorm.Dot(remote_control._obj.WorldMatrix.Forward);
                double gL = GravNorm.Dot(remote_control._obj.WorldMatrix.Left);
                double gU = GravNorm.Dot(remote_control._obj.WorldMatrix.Up);

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
                double gF = GravNorm.Dot(remote_control._obj.WorldMatrix.Down);
                double gL = GravNorm.Dot(remote_control._obj.WorldMatrix.Left);
                double gU = GravNorm.Dot(remote_control._obj.WorldMatrix.Forward);

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
            public void Forward(Vector3D course)
            {
                //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
                double gF = course.Dot(remote_control._obj.WorldMatrix.Up);
                double gL = course.Dot(remote_control._obj.WorldMatrix.Right);
                double gU = course.Dot(remote_control._obj.WorldMatrix.Backward);

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
                Forward(GravNorm);
            }
            public void ForwardСourse()
            {
                Vector3D course = Vector3D.Normalize(this.course);
                Forward(course);
            }
            public void ForwardTarget()
            {
                //вектор на точку
                Vector3D course = Vector3D.Normalize(target - remote_control.GetPosition());
                Forward(course);
            }
            public void CompensateWeight()
            {
                Vector3D GravityVector = remote_control.GetNaturalGravity;
                float ShipMass = remote_control.PhysicalMass;
                Vector3D ShipWeight = GravityVector * ShipMass;
                Vector3D HoverThrust = new Vector3D();
                ForwardThrust = (ShipWeight + HoverThrust).Dot(remote_control._obj.WorldMatrix.Forward);
                LeftThrust = (ShipWeight + HoverThrust).Dot(remote_control._obj.WorldMatrix.Left);
                UpThrust = (ShipWeight + HoverThrust).Dot(remote_control._obj.WorldMatrix.Up);
                BackwardThrust = -ForwardThrust;
                RightThrust = -LeftThrust;
                DownThrust = -UpThrust;
                this.thrusts.SetThrustOverride(remote_control.GetCockpitMatrix(), UpThrust, DownThrust, LeftThrust, RightThrust, ForwardThrust, BackwardThrust);

            }
            public void CompensateWeight(double spid, or_mtr orentation)
            {
                Matrix ThrusterMatrix = new MatrixD();
                double UpThrMax = 0;
                double DownThrMax = 0;
                double LeftThrMax = 0;
                double RightThrMax = 0;
                double ForwardThrMax = 0;
                double BackwardThrMax = 0;

                Matrix rc_wm = remote_control.GetCockpitMatrix();
                foreach (IMyThrust Thruster in thrusts.list_obj)
                {
                    Thruster.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == rc_wm.Up)
                    {
                        UpThrMax += Thruster.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == rc_wm.Down)
                    {
                        DownThrMax += Thruster.MaxEffectiveThrust;
                    }
                    //X
                    else if (ThrusterMatrix.Forward == rc_wm.Left)
                    {
                        LeftThrMax += Thruster.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == rc_wm.Right)
                    {
                        RightThrMax += Thruster.MaxEffectiveThrust;
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == rc_wm.Forward)
                    {
                        ForwardThrMax += Thruster.MaxEffectiveThrust;
                    }
                    else if (ThrusterMatrix.Forward == rc_wm.Backward)
                    {
                        BackwardThrMax += Thruster.MaxEffectiveThrust;
                    }
                }
                StringBuilder test_info = new StringBuilder();
                Vector3D GravityVector = remote_control.GetNaturalGravity;
                float ShipMass = remote_control.PhysicalMass;
                Vector3D ShipWeight = GravityVector * ShipMass;
                Vector3D HoverThrust = new Vector3D();

                double curr_spid = remote_control.ShipSpeed;
                test_info.Append("curr_spid:" + Math.Round(curr_spid, 2) + " " + "spid:" + Math.Round(spid, 2) + "\n");



                ForwardThrust = (ShipWeight + HoverThrust).Dot(remote_control._obj.WorldMatrix.Forward);
                LeftThrust = (ShipWeight + HoverThrust).Dot(remote_control._obj.WorldMatrix.Left);
                UpThrust = (ShipWeight + HoverThrust).Dot(remote_control._obj.WorldMatrix.Up);

                if (curr_spid < (spid * 0.99f))
                {
                    //double pr = 1.0 - (curr_spid / spid);
                    //double zp = ForwardThrMax - ForwardThrust;
                    //ForwardThrust = -1 * (ForwardThrust + (zp*pr));
                    //ForwardThrust = -1 * (ForwardThrust + (ForwardThrMax - ForwardThrust)/2);
                    ForwardThrust = -ForwardThrMax;
                }

                if (curr_spid > (spid * 1.01f))
                {
                    // double pr = (spid / curr_spid);
                    //double zp = ForwardThrMax - ForwardThrust;                    
                    //ForwardThrust = (ForwardThrust + (zp * pr));
                    //ForwardThrust = (ForwardThrust + (ForwardThrMax - ForwardThrust) / 2);
                    ForwardThrust = ForwardThrMax;
                }

                BackwardThrust = -ForwardThrust;
                RightThrust = -LeftThrust;
                DownThrust = -UpThrust;
                test_info.Append("ForwardThrust:" + Math.Round(ForwardThrust, 2) + "\n");
                test_info.Append("LeftThrust:" + Math.Round(LeftThrust, 2) + "\n");
                test_info.Append("UpThrust:" + Math.Round(UpThrust, 2) + "\n");

                foreach (IMyThrust Thruster in thrusts.list_obj)
                {
                    Thruster.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == rc_wm.Up)
                    {
                        Thruster.ThrustOverridePercentage = (float)(UpThrust / UpThrMax);
                    }
                    else if (ThrusterMatrix.Forward == rc_wm.Down)
                    {
                        Thruster.ThrustOverridePercentage = (float)(DownThrust / DownThrMax);
                    }
                    //X
                    else if (ThrusterMatrix.Forward == rc_wm.Left)
                    {
                        Thruster.ThrustOverridePercentage = (float)(LeftThrust / LeftThrMax);
                    }
                    else if (ThrusterMatrix.Forward == rc_wm.Right)
                    {
                        Thruster.ThrustOverridePercentage = (float)(RightThrust / RightThrMax);
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == rc_wm.Forward)
                    {
                        Thruster.ThrustOverridePercentage = (float)(ForwardThrust / ForwardThrMax);
                    }
                    else if (ThrusterMatrix.Forward == rc_wm.Backward)
                    {
                        Thruster.ThrustOverridePercentage = (float)(BackwardThrust / BackwardThrMax);
                    }
                }
                lcd_info.OutText(test_info);
            }
            public void ProgrammLandingPlanet()
            {
                //StringBuilder test_info = new StringBuilder();
                if (remote_control.GetNaturalGravity.Length() >= 0.01f)
                {
                    //test_info.Append("SCAN :" + camera_course.CanScan(this.dist_scan) + "\n");
                    //test_info.Append(gyros.TextInfo());
                    if (GetMinHiegtPlanet())
                    {
                        curent_mode = mode.braking;
                    }
                    else
                    {
                        if (curent_mode == mode.braking || curent_mode == mode.braking_control)
                        {
                            curent_mode = mode.speed;
                        }
                    }
                    // Первый запуск
                    if (curent_mode == mode.none)
                    {
                        curent_orientation = orientation.horizon_down;
                        curent_mode = mode.aiming; // Прицелимся
                    }
                }
                else
                {
                    curent_mode = mode.braking;
                }
                if (curent_mode == mode.aiming && ModeOrentationGyros())
                {
                    curent_mode = mode.speed; // Разгон
                }
                if (curent_mode == mode.speed)
                {
                    ModeSpeedOn(1.0f, 0, 0, 0, 0, 0);
                    curent_mode = mode.speed_control;
                }
                if (curent_mode == mode.speed_control && ModeSpeedControl())
                {
                    curent_mode = mode.flight;
                }
                if (curent_mode == mode.flight)
                {
                    compensate = true;
                    //this.thrusts.Off();
                }
                if (curent_mode == mode.braking)
                {
                    ModeBrakingOn();
                    curent_mode = mode.braking_control;
                }
                if (curent_mode == mode.braking_control && ModeBrakingControl())
                {
                    curent_programm = programm.none;
                    curent_mode = mode.none;
                    curent_orientation = orientation.none;
                }
                //lcd_info_debug.OutText(test_info);
            }
            public void ProgrammTakingPlanet()
            {
                //StringBuilder test_info = new StringBuilder();
                if (remote_control.GetNaturalGravity.Length() >= 0.01f)
                {
                    //test_info.Append("SCAN :" + camera_course.CanScan(this.dist_scan) + "\n");
                    //test_info.Append(gyros.TextInfo());
                    if ((GetProtectionObstacle() || GetMaxHiegtPlanet()) && curent_mode != mode.none && curent_mode != mode.aiming)
                    {
                        curent_mode = mode.braking;
                    }
                    else
                    {
                        if (curent_mode == mode.braking || curent_mode == mode.braking_control)
                        {
                            curent_mode = mode.speed;
                        }
                    }
                    // Первый запуск
                    if (curent_mode == mode.none)
                    {
                        curent_orientation = orientation.horizon_down;
                        curent_mode = mode.aiming; // Прицелимся
                    }
                }
                else
                {
                    curent_mode = mode.braking;
                }
                if (curent_mode == mode.aiming && ModeOrentationGyros())
                {
                    curent_mode = mode.speed; // Разгон
                }
                if (curent_mode == mode.speed)
                {
                    ModeSpeedOn(0, 1.0f, 0, 0, 0, 0);
                    curent_mode = mode.speed_control;
                }
                if (curent_mode == mode.speed_control && ModeSpeedControl())
                {
                    curent_mode = mode.flight;
                }
                if (curent_mode == mode.flight)
                {
                    if (remote_control.GetNaturalGravity.Length() >= 0.01f)
                    {
                        compensate = true;
                        this.thrusts.On();
                    }
                    else
                    {
                        compensate = false;
                        this.thrusts.Off();
                    }
                }
                if (curent_mode == mode.braking)
                {
                    ModeBrakingOn();
                    curent_mode = mode.braking_control;
                }
                if (curent_mode == mode.braking_control && ModeBrakingControl())
                {
                    curent_programm = programm.none;
                    curent_mode = mode.none;
                    curent_orientation = orientation.none;
                }
                //lcd_info_debug.OutText(test_info);
            }
            public void ProgrammFlyingCourse()
            {
                //StringBuilder test_info = new StringBuilder();
                //test_info.Append("count :" + count + "\n");
                //count++;
                //test_info.Append("SCAN :" + camera_course.CanScan(this.dist_scan) + "\n");
                //test_info.Append(gyros.TextInfo());
                if (GetProtectionObstacle())
                {
                    curent_mode = mode.braking;
                }
                // Первый запуск
                if (curent_mode == mode.none)
                {
                    curent_orientation = orientation.course_forward;
                    curent_mode = mode.aiming; // Прицелимся
                }
                if (curent_mode == mode.aiming && ModeOrentationGyros())
                {
                    curent_mode = mode.speed; // Разгон
                }
                if (curent_mode == mode.speed)
                {
                    ModeSpeedOn(0, 0, 0, 0, 0, 1.0f);
                    curent_mode = mode.speed_control;
                }
                if (curent_mode == mode.speed_control && ModeSpeedControl())
                {
                    curent_mode = mode.flight;
                }
                if (curent_mode == mode.flight)
                {
                    if (remote_control.GetNaturalGravity.Length() >= 0.01f)
                    {
                        compensate = true;
                        this.thrusts.On();
                    }
                    else
                    {
                        compensate = false;
                        this.thrusts.Off();
                    }
                }
                if (curent_mode == mode.braking)
                {
                    ModeBrakingOn();
                    curent_mode = mode.braking_control;
                }
                if (curent_mode == mode.braking_control && ModeBrakingControl())
                {
                    curent_programm = programm.none;
                    curent_mode = mode.none;
                    curent_orientation = orientation.none;
                }
                //lcd_info_debug.OutText(test_info);
            }
            public void ProgrammFlyingHorizont()
            {
                //StringBuilder test_info = new StringBuilder();
                if (remote_control.GetNaturalGravity.Length() >= 0.01f)
                {
                    //test_info.Append("count :" + count + "\n");
                    //test_info.Append("SCAN :" + camera_course.CanScan(this.dist_scan) + "\n");
                    //test_info.Append(gyros.TextInfo());
                    if (GetProtectionObstacle() && curent_mode != mode.none && curent_mode != mode.aiming)
                    {
                        curent_mode = mode.braking;
                    }
                    // Первый запуск
                    if (curent_mode == mode.none)
                    {
                        curent_orientation = orientation.horizon_down;
                        curent_mode = mode.aiming; // Прицелимся
                    }
                }
                else
                {
                    curent_mode = mode.braking;
                }
                if (curent_mode == mode.aiming && ModeOrentationGyros())
                {
                    curent_mode = mode.speed; // Разгон
                }
                if (curent_mode == mode.speed)
                {
                    ModeSpeedOn(0, 0, 0, 0, 0, 1.0f);
                    curent_mode = mode.speed_control;
                }
                if (curent_mode == mode.speed_control && ModeSpeedControl())
                {
                    curent_mode = mode.flight;
                }
                if (curent_mode == mode.flight)
                {
                    if (remote_control.GetNaturalGravity.Length() >= 0.01f)
                    {
                        compensate = true;
                        this.thrusts.On();
                    }
                    else
                    {
                        compensate = false;
                        this.thrusts.Off();
                    }
                }
                if (curent_mode == mode.braking)
                {
                    ModeBrakingOn();
                    curent_mode = mode.braking_control;
                }
                if (curent_mode == mode.braking_control && ModeBrakingControl())
                {
                    curent_programm = programm.none;
                    curent_mode = mode.none;
                    curent_orientation = orientation.none;
                }
                //lcd_info_debug.OutText(test_info);
            }
            public void ProgrammFlyingTarget()
            {
                //StringBuilder test_info = new StringBuilder();
                //test_info.Append("SCAN :" + camera_course.CanScan(this.dist_scan) + "\n");
                //test_info.Append(gyros.TextInfo());
                if (GetProtectionObstacle() || GetTarget(this.target))
                {
                    curent_mode = mode.braking;
                }
                // Первый запуск
                if (curent_mode == mode.none)
                {
                    curent_orientation = orientation.target_forward;
                    curent_mode = mode.aiming; // Прицелимся
                }
                if (curent_mode == mode.aiming && ModeOrentationGyros())
                {
                    curent_mode = mode.speed; // Разгон
                }
                if (curent_mode == mode.speed)
                {
                    ModeSpeedOn(0, 0, 0, 0, 0, 1.0f);
                    curent_mode = mode.speed_control;
                }
                if (curent_mode == mode.speed_control && ModeSpeedControl())
                {
                    curent_mode = mode.flight;
                }
                if (curent_mode == mode.flight)
                {
                    if (remote_control.GetNaturalGravity.Length() >= 0.01f)
                    {
                        compensate = true;
                        this.thrusts.On();
                    }
                    else
                    {
                        compensate = false;
                        this.thrusts.Off();
                    }
                }
                if (curent_mode == mode.braking)
                {
                    ModeBrakingOn();
                    curent_mode = mode.braking_control;
                }
                if (curent_mode == mode.braking_control && ModeBrakingControl())
                {
                    curent_programm = programm.none;
                    curent_mode = mode.none;
                    curent_orientation = orientation.none;
                }
                //lcd_info_debug.OutText(test_info);
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
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
                    //
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
                    //
                    case "target_forward_on":
                        curent_orientation = orientation.target_forward;
                        break;
                    case "target_forward_off":
                        curent_orientation = orientation.none;
                        break;
                    case "target_forward":
                        if (curent_orientation == orientation.target_forward)
                        {
                            curent_orientation = orientation.none;
                        }
                        else
                        {
                            curent_orientation = orientation.target_forward;
                        }
                        break;
                    //
                    case "mode_landing_planet_on":
                        curent_programm = programm.landing_planet;
                        break;
                    case "mode_landing_planet_off":
                        if (curent_programm == programm.landing_planet)
                        {
                            curent_mode = mode.braking;
                        }
                        break;
                    case "mode_landing_planet":
                        if (curent_programm == programm.landing_planet)
                        {
                            curent_mode = mode.braking;
                        }
                        else
                        {
                            curent_programm = programm.landing_planet;
                        }
                        break;
                    //
                    case "mode_taking_planet_on":
                        curent_programm = programm.taking_planet;
                        break;
                    case "mode_taking_planet_off":
                        if (curent_programm == programm.taking_planet)
                        {
                            curent_mode = mode.braking;
                        }
                        break;
                    case "mode_taking_planet":
                        if (curent_programm == programm.taking_planet)
                        {
                            curent_mode = mode.braking;
                        }
                        else
                        {
                            curent_programm = programm.taking_planet;
                        }
                        break;
                    //
                    case "mode_flying_course_on":
                        curent_programm = programm.flying_course;
                        break;
                    case "mode_flying_course_off":
                        if (curent_programm == programm.flying_course)
                        {
                            curent_mode = mode.braking;
                        }
                        break;
                    case "mode_flying_course":
                        if (curent_programm == programm.flying_course)
                        {
                            curent_mode = mode.braking;
                        }
                        else
                        {
                            curent_programm = programm.flying_course;
                        }
                        break;
                    //
                    case "mode_flying_target_on":
                        curent_programm = programm.flying_target;
                        break;
                    case "mode_flying_target_off":
                        if (curent_programm == programm.flying_target)
                        {
                            curent_mode = mode.braking;
                        }
                        break;
                    case "mode_flying_target":
                        if (curent_programm == programm.flying_target)
                        {
                            curent_mode = mode.braking;
                        }
                        else
                        {
                            curent_programm = programm.flying_target;
                        }
                        break;
                    //
                    case "mode_flying_horizont_on":
                        curent_programm = programm.flying_horizont;
                        break;
                    case "mode_flying_horizont_off":
                        if (curent_programm == programm.flying_horizont)
                        {
                            curent_mode = mode.braking;
                        }
                        break;
                    case "mode_flying_horizont":
                        if (curent_programm == programm.flying_horizont)
                        {
                            curent_mode = mode.braking;
                        }
                        else
                        {
                            curent_programm = programm.flying_horizont;
                        }
                        break;
                    //
                    case "bascward_100":
                        this.thrusts.SetThrustOverridePersent(this.remote_control.GetCockpitMatrix(), 0, 0, 0, 0, 0, 1.0f);
                        break;
                    case "bascward_0":
                        this.thrusts.SetThrustOverridePersent(this.remote_control.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0.0f);
                        break;
                    case "compensate_on":
                        compensate = true;
                        break;
                    case "compensate_off":
                        compensate = false;
                        this.thrusts.SetThrustOverridePersent(this.remote_control.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0.0f);
                        break;
                    case "compensate1_on":
                        compensate1 = true;
                        break;
                    case "compensate1_off":
                        compensate1 = false;
                        this.thrusts.SetThrustOverridePersent(this.remote_control.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0.0f);
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Получим макс эффективность трастеров по направлениям
                    thrusts.GetMaxEffectiveThrust(remote_control.GetCockpitMatrix());
                    // Сбросим подпрограммы
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
                    if (curent_orientation == orientation.course_forward)
                    {
                        ForwardСourse();
                    }
                    if (curent_orientation == orientation.target_forward)
                    {
                        ForwardTarget();
                    }
                    if (compensate)
                    {
                        CompensateWeight();
                    }
                    if (compensate1)
                    {
                        CompensateWeight(90.0f, or_mtr.backward);
                    }
                    if (curent_programm == programm.landing_planet)
                    {
                        ProgrammLandingPlanet();
                    }
                    if (curent_programm == programm.taking_planet)
                    {
                        ProgrammTakingPlanet();
                    }
                    if (curent_programm == programm.flying_course)
                    {
                        ProgrammFlyingCourse();
                    }
                    if (curent_programm == programm.flying_target)
                    {
                        ProgrammFlyingTarget();
                    }
                    if (curent_programm == programm.flying_horizont)
                    {
                        ProgrammFlyingHorizont();
                    }
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ОРИНТАЦИЯ: " + name_orientation[(int)curent_orientation] + "\n");
                values.Append("КОМПЕНСАЦИЯ: " + (compensate ? "ВКЛ" : "ВЫК") + "\n");
                values.Append("ПРОГРАММА: " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП: " + name_mode[(int)curent_mode] + "\n");
                values.Append("Курс: " + course + "\n");
                values.Append("ЦЕЛЬ: " + PText.GetVector3D(target) + "\n");
                values.Append("РАССТОЯНИЕ: " + remote_control.GetDistance(target) + "\n");
                return values.ToString();
            }
            public string TextTEST()
            {
                StringBuilder values = new StringBuilder();
                values.Append("base: " + base_connection + "\n");
                values.Append("base_pren: " + base_pre_connection + "\n");
                values.Append("base_space: " + base_space_connection + "\n");
                values.Append("station: " + station_space_connection + "\n");
                values.Append("station_pre: " + station_pre_space_connection + "\n");
                //braking space = GetBrakingSpace(thrusts.Forward_ThrustsMax);
                //braking londing = GetBrakingLanding(thrusts.Forward_ThrustsMax);
                //values.Append("Space: a=" + Math.Round(space.a, 2) + "t=" + Math.Round(space.t, 2) + "S=" + Math.Round(space.s, 2) + "\n");
                //values.Append("Londing: a=" + Math.Round(londing.a, 2) + "t=" + Math.Round(londing.t, 2) + "S=" + Math.Round(londing.s, 2) + "\n");
                //values.Append("Thrust: up=" + PText.GetThrust((float)UpThrust) + "down=" + PText.GetThrust((float)DownThrust) + "\n");
                //values.Append("Thrust: left=" + PText.GetThrust((float)LeftThrust) + "right=" + PText.GetThrust((float)RightThrust) + "\n");
                //values.Append("Thrust: forw=" + PText.GetThrust((float)ForwardThrust) + "back=" + PText.GetThrust((float)BackwardThrust) + "\n");
                //values.Append("SCAN - " + camera_course.CanScan(this.dist_scan) + "\n");
                //values.Append("Растояние: " + (target_info != null && ((MyDetectedEntityInfo)target_info).HitPosition != null ? cockpit.GetDistance((Vector3D)((MyDetectedEntityInfo)target_info).HitPosition).ToString() : "") + "\n");
                //values.Append(camera_course.GetTextDetectedEntityInfo(target_info) + "\n");
                return values.ToString();
            }
        }
    }
}
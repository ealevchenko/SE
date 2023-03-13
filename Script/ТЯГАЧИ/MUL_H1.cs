using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
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
        string NameConnector = "[MUL-H1]-Коннектор парковка";
        string NameLCDInfo = "[MUL-H1]-LCD-INFO";

        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея

        LCD lcd_info;
        Batterys bats;
        Connector connector;
        ReflectorsLight reflectors_light;
        Gyros gyros;
        Thrusts thrusts;
        Cockpit cockpit;
        RemoteControl remote_control;


        static Program _scr;

        public double minHeight = 1000; // мин растояние на которое действует отключать двигатели
        bool ship_connect = false;
        bool horizont = false;

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
        public void KeepHorizon()
        {
            Vector3D GravityVector = cockpit._obj.GetNaturalGravity();
            Vector3D GravNorm = Vector3D.Normalize(GravityVector);

            //Получаем проекции вектора прицеливания на все три оси блока ДУ. 
            double gF = GravNorm.Dot(cockpit._obj.WorldMatrix.Forward);
            double gL = GravNorm.Dot(cockpit._obj.WorldMatrix.Left);
            double gU = GravNorm.Dot(cockpit._obj.WorldMatrix.Up);

            //Получаем сигналы по тангажу и крены операцией atan2
            float RollInput = (float)Math.Atan2(gL, -gU);
            float PitchInput = -(float)Math.Atan2(gF, -gU);

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
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj);
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
            switch (argument)
            {
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
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                thrusts.GetMaxEffectiveThrust(cockpit.GetCockpitMatrix());

                // Проверим корабль не припаркован
                if (!connector.Connected)
                {
                    if (minHeight > cockpit.CurrentHeight)
                    {
                        bats.Auto();
                        thrusts.On();
                        cockpit.Dampeners(true);
                    }



                    // режим горизонт
                    gyros.GyroOver(horizont);
                    if (horizont)
                    {
                        KeepHorizon();
                    }
                    // Проверка кокпит не под контроллем включить тормоз
                    if (!cockpit.IsUnderControl)
                    {
                        cockpit.Dampeners(true);
                        reflectors_light.Off();
                    }
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
            //values_info.Append(thrusts.TextInfo());
            values_info.Append(remote_control.TextInfo());
            values_info.Append(cockpit.TextInfo());
            values_info.Append("РЕЖИМ: " + (horizont ? "ГОРИЗОНТ" : "") + "\n");

            cockpit.OutText(values_info, 0);
            ship_connect = connector.Connected; // сохраним состояние

            StringBuilder test_info = new StringBuilder();
            //test_info.Append("home1 : " + remote_control.home_position.ToString() + "\n");
            //test_info.Append("home2 : " + remote_control.home_position1.ToString() + "\n");
            //test_info.Append("home3 : " + remote_control.home_position2.ToString() + "\n");
            test_info.Append("ThrustsMax : " + thrusts.Forward_ThrustsMax / 1000 + "\n");
            test_info.Append("TotalMass : " + cockpit.TotalMass / 1000 + "\n");
            test_info.Append("ShipSpeed : " + cockpit.ShipSpeed + "\n");

            double a = (thrusts.Forward_ThrustsMax / 1000) * (1 / (cockpit.TotalMass / 1000));
            test_info.Append("a : " + Math.Round(a, 2) + "\n");
            //t = V / a
            double t = cockpit.ShipSpeed / a;
            test_info.Append("t : " + Math.Round(t, 2) + "\n");
            // S = ( a * t^2 ) / 2
            double S = (a * Math.Pow(t, 2)) / 2;
            // Math.Round(S, 1)
            test_info.Append("S : " + Math.Round(S, 1) + "\n");

            //t = (V - V[0]) / a
            double tp = (0-cockpit.ShipSpeed) / -5;
            test_info.Append("tp : " + Math.Round(tp, 2) + "\n");
            //S = V[0] * t + ( a * t^2 ) / 2
            double Sp = (cockpit.ShipSpeed * tp) + ((-5) * Math.Pow(tp, 2)) / 2;
            test_info.Append("Sp : " + Math.Round(Sp, 2) + "\n");

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
        }
        public class Thrusts : BaseListTerminalBlock<IMyThrust>
        {
            Matrix ThrusterMatrix = new MatrixD();
            double UpThrMax = 0;
            double DownThrMax = 0;
            double LeftThrMax = 0;
            double RightThrMax = 0;
            double ForwardThrMax = 0;
            double BackwardThrMax = 0;
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
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("UpThrMax: " + PText.GetThrust((float)UpThrMax) + "\n");
                values.Append("DownThrMax: " + PText.GetThrust((float)DownThrMax) + "\n");
                values.Append("LeftThrMax: " + PText.GetThrust((float)LeftThrMax) + "\n");
                values.Append("RightThrMax: " + PText.GetThrust((float)RightThrMax) + "\n");
                values.Append("ForwardThrMax: " + PText.GetThrust((float)ForwardThrMax) + "\n");
                values.Append("BackwardThrMax: " + PText.GetThrust((float)BackwardThrMax) + "\n");
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
                    base.obj.TryGetPlanetElevation(MyPlanetElevation.Surface, out current_height);
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Гравитация: " + base.obj.GetNaturalGravity().Length() + "\n");
                values.Append("ТГравитация: " + base.obj.GetTotalGravity().Length() + "\n");
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
    }
}
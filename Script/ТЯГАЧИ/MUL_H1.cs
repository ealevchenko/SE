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
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

/// <summary>
/// v2.0
/// </summary>
namespace MUL_H1
{
    public sealed class Program : MyGridProgram
    {
        // v2.1
        string NameObj = "[MUL-H1]";
        string NameCockpit = "[MUL-H1]-Кресло пилота [LCD]";
        string NameRemoteControl = "[MUL-H1]-ДУ Парковка";
        string NameCameraCourse = "[MUL-H1]-Камера [collision_protection] course";
        //string NameCameraForward = "[MUL-H1]-Камера forward";
        string NameCameraConnector = "[MUL-H1]-Камера парковка";
        string NameConnector = "[MUL-H1]-Коннектор парковка";
        string NameLCDInfo = "[MUL-H1]-LCD-INFO";
        string NameLCDInfo_Upr = "[MUL-H1]-LCD-INFO-UPR";
        string NameLCDInfo_Debug = "[MUL-H1]-LCD-INFO-DEBUG";

        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея
        string tag_door_gateway = "[door-gateway]";
        string tag_lighting_room = "[lighting_room]";
        string tag_collision_protection = "[collision_protection]";

        public enum or_mtr : int
        {
            not = 0, up = 1, down = 2, left = 3, right = 4, forward = 5, backward = 6
        };

        public enum room : int
        {
            none = 0,
            cabin = 1,             // кабина
            space = 2,         // Космос
        };
        public static string[] name_room = { "", "Кабина", "Выход в космос" };
        public static int[] count_room = { 0, 0, 0 };

        public enum doors_gareways : int
        {
            space_cabin1 = 1,
            space_cabin2 = 2,
        }

        // door [door-gateway] [space_cabin2] [cabin]
        // sn [door-gateway] [space_cabin2] [cabin]
        // door [door-gateway] [space_cabin2] [space]
        // sn [door-gateway] [space_cabin2] [space]

        //light [lighting_room] [cabin]

        LCD lcd_info;
        LCD lcd_info_upr;
        static LCD lcd_info_debug;
        Batterys bats;
        Connector connector;
        ReflectorsLight reflectors_light;
        Gyros gyros;
        GasTanks gastanks;
        Thrusts thrusts;
        Cockpit cockpit;
        RemoteControl remote_control;
        Camera camera_course;
        Camera camera_connector;
        Navigation navigation;
        Gateways gateways_doors;
        Lightings room_light;

        CollisionProtection collision_protection;

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
            gastanks = new GasTanks(NameObj);
            thrusts = new Thrusts(NameObj);
            camera_course = new Camera(NameCameraCourse);
            camera_connector = new Camera(NameCameraConnector);
            gateways_doors = new Gateways(NameObj, tag_door_gateway);
            room_light = new Lightings(NameObj, tag_lighting_room); // Освещение
            room_light.Off();
            collision_protection = new CollisionProtection(NameObj, tag_collision_protection); // Защита от столкновений
            //collision_protection.Scan = true;
            navigation = new Navigation(cockpit, remote_control, thrusts, gyros, camera_course, camera_connector, collision_protection);
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
            gateways_doors.Logic(argument, updateSource);   // Логика отработки шлюзовых дверей
            room_light.Logic(argument, updateSource);       // Логика отработки освещения
            //collision_protection.GetDistance(cockpit.GetCockpitMatrix());
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
                    gastanks.Stockpile(false);
                    if (minHeight > cockpit.CurrentHeight)
                    {
                        bats.Auto();
                        thrusts.On();
                        cockpit.Dampeners(true);
                    }
                }
                else
                {
                    // Припаркован
                    reflectors_light.Off();
                    gastanks.Stockpile(true);
                    cockpit.Dampeners(false);
                    bats.Charger();
                    thrusts.Off();
                }
            }
            values_info.Append(bats.TextInfo());
            values_info.Append(gastanks.TextInfo());
            values_info.Append(connector.TextInfo());
            //values_info.Append(thrusts.TextInfo());
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
            //test_info.Append("ThrustsMax : " + thrusts.Forward_ThrustsMax / 1000 + "\n");
            //test_info.Append("TotalMass : " + cockpit.TotalMass / 1000 + "\n");
            //test_info.Append("ShipSpeed : " + cockpit.ShipSpeed + "\n");
            //test_info.Append(navigation.TextTEST());
            test_info.Append("Scan :" + collision_protection.Scan);
            test_info.Append(collision_protection.TextInfo());
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
        public class GasTanks : BaseListTerminalBlock<IMyGasTank>
        {
            public GasTanks(string name_obj) : base(name_obj)
            {
                AutoRefillBottles(true);
            }
            public GasTanks(string name_obj, string tag) : base(name_obj, tag)
            {
                AutoRefillBottles(true);
            }
            public void AutoRefillBottles(bool on)
            {
                foreach (IMyGasTank obj in base.list_obj)
                {
                    obj.AutoRefillBottles = on;
                }
            }
            public void Stockpile(bool on)
            {
                foreach (IMyGasTank obj in base.list_obj)
                {
                    obj.Stockpile = on;
                }
            }
            public float MaxCapacity()
            {
                return base.list_obj.Select(b => b.Capacity).Sum();
            }
            public double FilledRatio()
            {
                return base.list_obj.Select(b => b.FilledRatio).Average();
            }
            public int CountAutoRefillBottles()
            {
                return base.list_obj.Where(b => b.AutoRefillBottles == true).Select(t => t.AutoRefillBottles).Count();
            }
            public int CountStockpiles()
            {
                return base.list_obj.Where(b => b.Stockpile == true).Select(t => t.Stockpile).Count();
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                //БАТАРЕЯ: [10 - 10] [0.0MW / 0.0MW]
                //|- ЗАР:  [''''''''''''''''''''''''']-0%
                values.Append("БАКИ H2: [" + Count + "] [А-" + CountAutoRefillBottles() + " З-" + CountStockpiles() + "]" + PText.GetCapacityTanks(FilledRatio(), MaxCapacity()) + "\n");
                values.Append("|- НАП:  " + PText.GetScalePersent(FilledRatio(), 20) + "\n");
                return values.ToString();
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

                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ДОМ: " + base_connection.ToString() + "\n");
                values.Append("КОСМОС: " + base_space_connection.ToString() + "\n");
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
        // V1.0
        public class CollisionProtection : BaseListTerminalBlock<IMyCameraBlock>
        {
            public bool Scan { get { return scan; } set { scan = value; } }
            bool scan = false;
            public class DistationOfOrentation
            {
                public or_mtr orentation { get; set; }
                public double? distanse { get; set; }
            }
            public List<DistationOfOrentation> list_dist_orent = new List<DistationOfOrentation>();
            double dist_scan = 5000;
            float pitch_scan = 0f;
            float yaw_scan = 0f;
            public CollisionProtection(string name_obj, string tag) : base(name_obj)
            {
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено CollisionProtection:[" + tag + "]: " + list_obj.Count());
            }
            public void GetDistance(Matrix CockpitMatrix)
            {
                list_dist_orent = GetListDistance(CockpitMatrix, dist_scan);
            }
            public List<DistationOfOrentation> GetListDistance(Matrix CockpitMatrix, double dist_scan)
            {

                StringBuilder test_info = new StringBuilder();
                List<DistationOfOrentation> list = new List<DistationOfOrentation>();
                Matrix CameraMatrix = new MatrixD();
                foreach (IMyCameraBlock camera in base.list_obj)
                {
                    DistationOfOrentation dsor;
                    or_mtr curr = or_mtr.not;
                    camera.EnableRaycast = scan;
                    if (scan)
                    {
                        camera.Orientation.GetMatrix(out CameraMatrix);
                        if (CameraMatrix.Forward == CockpitMatrix.Up)
                        {
                            curr = or_mtr.up;
                        }
                        else if (CameraMatrix.Forward == CockpitMatrix.Down)
                        {
                            curr = or_mtr.down;
                        }
                        else if (CameraMatrix.Forward == CockpitMatrix.Left)
                        {
                            curr = or_mtr.left;
                        }
                        else if (CameraMatrix.Forward == CockpitMatrix.Right)
                        {
                            curr = or_mtr.right;
                        }
                        else if (CameraMatrix.Forward == CockpitMatrix.Forward)
                        {
                            curr = or_mtr.forward;
                        }
                        else if (CameraMatrix.Forward == CockpitMatrix.Backward)
                        {
                            curr = or_mtr.backward;
                        }
                        //test_info.Append("CanScan: " + camera.CanScan(dist_scan) + "\n");
                        //curr == or_mtr.forward ? dist_scan_forward : dist_scan;
                        if (camera.CanScan(dist_scan))
                        {
                            MyDetectedEntityInfo? res = camera.Raycast(dist_scan, pitch_scan, yaw_scan);
                            if (res != null && ((MyDetectedEntityInfo)res).Type != MyDetectedEntityType.None)
                            {
                                dsor = new DistationOfOrentation()
                                {
                                    orentation = curr,
                                    distanse = ((Vector3D)((MyDetectedEntityInfo)res).HitPosition - camera.GetPosition()).Length()
                                };
                                list.Add(dsor);
                            }
                        }
                    }
                }
                return list;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "collision_protection_on":
                        scan = true;
                        break;
                    case "collision_protection_off":
                        scan = false;
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
                values.Append("Найдено:" + list_dist_orent.Count() + "\n");
                foreach (DistationOfOrentation ds_or in list_dist_orent)
                {
                    values.Append(ds_or.orentation.ToString() + " :" + ds_or.distanse + "\n");
                }
                return values.ToString();
            }
        }
        // V1.1
        public class Navigation
        {
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

            List<CollisionProtection.DistationOfOrentation> list_dist_scan = new List<CollisionProtection.DistationOfOrentation>();

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
                target_forward = 5,
            };
            public static string[] name_horizont = { "", "Горизонт", "Вверх", "Вниз", "Курс", "Точка" };
            public enum programm : int
            {
                none = 0,
                orientation = 1,
                landing_planet = 2,
                taking_planet = 3,
                flying_course = 4,
                flying_target = 5,
            };
            public static string[] name_programm = { "", "Орентация", "Посадка на планету", "Взлет с планеты", "Полет по курсу", "Полет к цели" };
            orientation curent_orientation = orientation.none;
            programm curent_programm = programm.none;
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
            int max_spid = 200;
            int reserve_hieght = 400 + 300; // мин растояние на которое действует отключать двигатели (останавливается на гравитации за 300м)

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
            Camera camera_connector;
            CollisionProtection collision_protection;

            public Navigation(Cockpit cockpit, RemoteControl remote_control, Thrusts thrusts, Gyros gyros, Camera camera_course,
                Camera camera_connector, CollisionProtection collision_protection)
            {
                this.cockpit = cockpit;
                this.remote_control = remote_control;
                this.thrusts = thrusts;
                this.gyros = gyros;
                this.camera_course = camera_course;
                this.camera_connector = camera_connector;
                this.collision_protection = collision_protection;
            }
            // Посадка с гпавитацией тормозной путь, Космос тормозной путь
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
            public bool GetProtectionObstacle()
            {
                double max_Thrust = 0f;
                or_mtr orent_scan = or_mtr.not;
                if (curent_programm == programm.landing_planet)
                {  // посадка
                    max_Thrust = thrusts.Backward_ThrustsMax;
                    orent_scan = or_mtr.backward;
                }
                else if (curent_programm == programm.taking_planet)
                {   // взлет
                    max_Thrust = thrusts.Forward_ThrustsMax;
                    orent_scan = or_mtr.forward;
                }
                else if (curent_programm == programm.flying_course || curent_programm == programm.flying_target)
                {   // курс и цель
                    max_Thrust = thrusts.Forward_ThrustsMax;
                    orent_scan = or_mtr.forward;
                }
                else
                {
                    max_Thrust = thrusts.Forward_ThrustsMax;
                    orent_scan = or_mtr.forward;
                }
                braking space = GetBrakingLanding(max_Thrust);
                list_dist_scan = collision_protection.GetListDistance(cockpit.GetCockpitMatrix(), space.s + reserve_hieght);
                double? distance = list_dist_scan.Where(o => o.orentation == orent_scan).Select(d => d.distanse).Min();
                return distance != null && distance <= space.s + reserve_hieght ? true : false;
            }
            public bool GetTarget(Vector3D target)
            {
                double max_Thrust = 0f;
                if (curent_programm == programm.landing_planet)
                {  // посадка
                    max_Thrust = thrusts.Backward_ThrustsMax;
                }
                else if (curent_programm == programm.taking_planet)
                {   // взлет
                    max_Thrust = thrusts.Forward_ThrustsMax;
                }
                else if (curent_programm == programm.flying_course || curent_programm == programm.flying_target)
                {   // курс и цель
                    max_Thrust = thrusts.Forward_ThrustsMax;
                }
                else
                {
                    max_Thrust = thrusts.Forward_ThrustsMax;
                }
                braking space = GetBrakingLanding(max_Thrust);
                double distance = this.cockpit.GetDistance(target);
                if (distance > -space.s && distance < space.s)
                {
                    return true;
                }
                return false;
            }
            public bool GetHiegtPlanet()
            {
                double max_Thrust = 0f;
                if (curent_programm == programm.landing_planet)
                {  // посадка
                    max_Thrust = thrusts.Backward_ThrustsMax;
                }
                else if (curent_programm == programm.taking_planet)
                {   // взлет
                    max_Thrust = thrusts.Forward_ThrustsMax;
                }
                else if (curent_programm == programm.flying_course || curent_programm == programm.flying_target)
                {   // курс и цель
                    max_Thrust = thrusts.Forward_ThrustsMax;
                }
                else
                {
                    max_Thrust = thrusts.Forward_ThrustsMax;
                }
                braking space = GetBrakingLanding(max_Thrust);
                if (this.cockpit.CurrentHeight <= space.s + reserve_hieght)
                {
                    return true;
                }
                return false;
            }
            public bool ModeOrentationGyros()
            {
                if (Math.Abs(gyros.getPitch()) + Math.Abs(gyros.getYaw()) < 0.01f)
                {
                    return true;
                }
                return false;
            }
            public bool ModeSpeedOn(float up, float down, float left, float right, float forward, float backward)
            {
                this.thrusts.On();
                if (compensate) compensate = false;
                cockpit.Dampeners(false);
                this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), up, down, left, right, forward, backward);
                return true;
            }
            public bool ModeSpeedControl()
            {
                if (this.cockpit.ShipSpeed >= max_spid)
                {
                    this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0);
                    if (cockpit.GetNaturalGravity.Length() >= 0.01f)
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
                if (this.cockpit.ShipSpeed <= 0.01f)
                {
                    if (cockpit.GetNaturalGravity.Length() >= 0.01f)
                    {
                        //compensate = true;
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
            public void Forward(Vector3D course)
            {
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
                Vector3D course = Vector3D.Normalize(target - cockpit.GetPosition());
                Forward(course);
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
                StringBuilder test_info = new StringBuilder();
                test_info.Append("SCAN :" + camera_course.CanScan(this.dist_scan) + "\n");
                test_info.Append(gyros.TextInfo());
                collision_protection.Scan = true;
                if (GetProtectionObstacle() || GetHiegtPlanet())
                {
                    curent_mode = mode.braking;
                }
                // Первый запуск
                if (curent_mode == mode.none && cockpit.GetNaturalGravity.Length() >= 0.01f)
                {
                    curent_orientation = orientation.horizon_backward;
                    curent_mode = mode.aiming; // Прицелимся
                }
                if (curent_mode == mode.aiming && ModeOrentationGyros())
                {
                    curent_mode = mode.speed; // Разгон
                }
                if (curent_mode == mode.speed)
                {
                    ModeSpeedOn(0, 0, 0, 0, 1.0f, 0);
                    curent_mode = mode.speed_control;
                }
                if (curent_mode == mode.speed_control && ModeSpeedControl())
                {
                    curent_mode = mode.flight;
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
                lcd_info_debug.OutText(test_info);
            }
            public void TakingPlanet()
            {
                //if (GetProtectionObstacle())
                //{
                //    pr_mode = 3; // тормозим и выходим
                //}
                //switch (pr_mode)
                //{
                //    case 0:
                //        {
                //            if (cockpit.GetNaturalGravity.Length() >= 0.01f)
                //            {
                //                // Проверка возможна операция?
                //                compensate = false;
                //                curent_orientation = orientation.horizon_backward;
                //                pr_mode = 1; // Набор скорости
                //            }
                //            else
                //            {
                //                curent_programm = programm.none;
                //            }
                //            break;
                //        }
                //    case 1:
                //        {
                //            this.thrusts.On();
                //            this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 1.0f);
                //            pr_mode = 2; // проверка скорости и включить компенсации ()
                //            break;
                //        }
                //    case 2:
                //        {
                //            if (this.cockpit.ShipSpeed >= max_spid)
                //            {
                //                compensate = true;
                //                this.thrusts.SetThrustOverridePersent(this.cockpit.GetCockpitMatrix(), 0, 0, 0, 0, 0, 0);
                //            }
                //            if (cockpit.GetNaturalGravity.Length() <= 0.2f)
                //            {
                //                compensate = false;
                //                cockpit.Dampeners(true);
                //                pr_mode = 3;
                //            }
                //            break;
                //        }
                //    case 3:
                //        {
                //            if (this.cockpit.ShipSpeed <= 0.2f)
                //            {
                //                // Скорость погашена сбросим режим посадки
                //                curent_programm = programm.none;
                //                pr_mode = 0;
                //            }
                //            break;
                //        }
                //}
            }
            public void FlyingCourse()
            {
                StringBuilder test_info = new StringBuilder();
                test_info.Append("SCAN :" + camera_course.CanScan(this.dist_scan) + "\n");
                test_info.Append(gyros.TextInfo());
                collision_protection.Scan = true;
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
                if (curent_mode == mode.braking)
                {
                    ModeBrakingOn();
                    curent_mode = mode.braking_control;
                }
                if (curent_mode == mode.braking_control && ModeBrakingControl()) {
                    curent_programm = programm.none;
                    curent_mode = mode.none;
                    curent_orientation =  orientation.none;
                }
                lcd_info_debug.OutText(test_info);
            }
            public void FlyingTarget()
            {
                StringBuilder test_info = new StringBuilder();
                test_info.Append("SCAN :" + camera_course.CanScan(this.dist_scan) + "\n");
                test_info.Append(gyros.TextInfo());
                collision_protection.Scan = true;
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
                lcd_info_debug.OutText(test_info);
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
                    case "collision_protection":
                        collision_protection.Scan = true;
                        collision_protection.GetDistance(cockpit.GetCockpitMatrix());
                        break;
                    case "set_base_connection":
                        base_pre_vector_connection = this.cockpit.WorldMatrix.Forward;
                        base_connection = this.cockpit.GetPosition();
                        base_pre_connection = (base_connection - (base_pre_vector_connection * -300));
                        base_space_connection = (base_connection - (base_pre_vector_connection * -45000));
                        break;
                    case "set_station_space_connection":
                        station_pre_space_vector_connection = this.cockpit.WorldMatrix.Forward;
                        station_space_connection = this.cockpit.GetPosition();
                        station_pre_space_connection = (station_space_connection - (station_pre_space_vector_connection * 300));
                        break;
                    case "horizont_down_on":
                        curent_programm = programm.orientation;
                        curent_orientation = orientation.horizon_down;
                        break;
                    case "horizont_down_off":
                        curent_programm = programm.none;
                        curent_orientation = orientation.none;
                        break;
                    case "horizont_down":
                        if (curent_orientation == orientation.horizon_down)
                        {
                            curent_programm = programm.none;
                            curent_orientation = orientation.none;
                        }
                        else
                        {
                            curent_programm = programm.orientation;
                            curent_orientation = orientation.horizon_down;
                        }
                        break;
                    case "horizont_backward_on":
                        curent_programm = programm.orientation;
                        curent_orientation = orientation.horizon_backward;
                        break;
                    case "horizont_backward_off":
                        curent_programm = programm.none;
                        curent_orientation = orientation.none;
                        break;
                    case "horizont_backward":
                        if (curent_orientation == orientation.horizon_backward)
                        {
                            curent_programm = programm.none;
                            curent_orientation = orientation.none;
                        }
                        else
                        {
                            curent_programm = programm.orientation;
                            curent_orientation = orientation.horizon_backward;
                        }
                        break;
                    case "horizont_forward_on":
                        curent_programm = programm.orientation;
                        curent_orientation = orientation.horizon_forward;
                        break;
                    case "horizont_forward_off":
                        curent_programm = programm.none;
                        curent_orientation = orientation.none;
                        break;
                    case "horizont_forward":
                        if (curent_orientation == orientation.horizon_forward)
                        {
                            curent_programm = programm.none;
                            curent_orientation = orientation.none;
                        }
                        else
                        {
                            curent_programm = programm.orientation;
                            curent_orientation = orientation.horizon_forward;
                        }
                        break;
                    case "course_forward_on":
                        curent_programm = programm.orientation;
                        curent_orientation = orientation.course_forward;
                        break;
                    case "course_forward_off":
                        curent_programm = programm.none;
                        curent_orientation = orientation.none;
                        break;
                    case "course_forward":
                        if (curent_orientation == orientation.course_forward)
                        {
                            curent_programm = programm.none;
                            curent_orientation = orientation.none;
                        }
                        else
                        {
                            curent_programm = programm.orientation;
                            curent_orientation = orientation.course_forward;
                        }
                        break;
                    case "target_forward_on":
                        curent_programm = programm.orientation;
                        curent_orientation = orientation.target_forward;
                        break;
                    case "target_forward_off":
                        curent_programm = programm.none;
                        curent_orientation = orientation.none;
                        break;
                    case "target_forward":
                        if (curent_orientation == orientation.target_forward)
                        {
                            curent_programm = programm.none;
                            curent_orientation = orientation.none;
                        }
                        else
                        {
                            curent_programm = programm.orientation;
                            curent_orientation = orientation.target_forward;
                        }
                        break;
                    case "mode_landing_planet_on":
                        curent_programm = programm.landing_planet;
                        break;
                    case "mode_landing_planet_off":
                        curent_mode = mode.braking;
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
                    case "mode_taking_planet_on":
                        curent_programm = programm.taking_planet;
                        break;
                    case "mode_taking_planet_off":
                        curent_programm = programm.none;
                        break;
                    case "mode_taking_planet":
                        if (curent_programm == programm.taking_planet)
                        {
                            curent_programm = programm.none;
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
                        //curent_programm = programm.none;
                        curent_mode =  mode.braking;
                        break;
                    case "mode_flying_course":
                        if (curent_programm == programm.flying_course)
                        {
                            curent_mode = mode.braking;
                            //curent_programm = programm.none;
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
                        //curent_programm = programm.none;
                        curent_mode = mode.braking;
                        break;
                    case "mode_flying_target":
                        if (curent_programm == programm.flying_target)
                        {
                            //curent_programm = programm.none;
                            curent_mode = mode.braking;
                        }
                        else
                        {
                            curent_programm = programm.flying_target;
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
                    if (curent_programm == programm.none)
                    {
                        //pr_mode = 0;
                        //curent_mode = mode.none;
                        //curent_orientation = orientation.none;

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
                    if (curent_orientation == orientation.target_forward)
                    {
                        ForwardTarget();
                    }
                    if (compensate)
                    {
                        CompensateWeight();
                    }
                    if (curent_programm == programm.landing_planet)
                    {
                        LandingPlanet();
                    }
                    if (curent_programm == programm.taking_planet)
                    {
                        TakingPlanet();
                    }
                    if (curent_programm == programm.flying_course)
                    {
                        FlyingCourse();
                    }
                    if (curent_programm == programm.flying_target)
                    {
                        FlyingTarget();
                    }

                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ОРИНТАЦИЯ: " + name_horizont[(int)curent_orientation] + "\n");
                values.Append("КОМПЕНСАЦИЯ: " + (compensate ? "ВКЛ" : "ВЫК") + "\n");
                values.Append("ПРОГРАММА: " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП: " + name_mode[(int)curent_mode] + "\n");
                values.Append("Курс: " + course + "\n");
                values.Append("ЦЕЛЬ: " + PText.GetVector3D(target) + "\n");
                values.Append("РАССТОЯНИЕ: " + cockpit.GetDistance(target) + "\n");
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
        public class Gateway
        {
            doors_gareways door_gtw;
            IMySensorBlock sn1;
            IMySensorBlock sn2;
            room rm1;
            IMyDoor door1;
            IMyDoor door2;
            room rm2;
            //bool input_door = false;
            //bool output_door = false;
            bool sn1_active = false;    // датчик входа
            bool sn2_active = false;   // датчик выхода
            public Gateway(doors_gareways dg, IMyDoor door1, IMySensorBlock sn1, room rm1, IMyDoor door2, IMySensorBlock sn2, room rm2)
            {
                this.door_gtw = dg;
                this.rm1 = rm1;
                this.rm2 = rm2;
                this.sn1 = sn1;
                string sn1_cd = sn1.CustomData; // 1.0f, 1.0f, 2.5f, 1.0f, 0.1f, 2.5f
                this.sn2 = sn2;
                this.door1 = door1;
                this.door2 = door2;
                this.door1.ApplyAction("OnOff_On");
                this.door2.ApplyAction("OnOff_On");
                this.door1.CloseDoor();
                this.door2.CloseDoor();
            }

            public void Logic()
            {
                if (!sn1.IsActive && door1.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    door1.CloseDoor();
                }
                if (sn1.IsActive && door1.Status == DoorStatus.Closed && door2.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    door1.OpenDoor();
                }
                if (!sn2.IsActive && door2.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    door2.CloseDoor();
                }
                if (sn2.IsActive && door2.Status == DoorStatus.Closed && door1.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    door2.OpenDoor();
                }
                // Логика направдения движения
                if (sn1_active && !sn2_active && sn2.IsActive)
                {
                    // Выход
                    //sn1_active = false;
                    sn2_active = true;
                    //input_door = false;
                    //output_door = true;
                    count_room[(int)rm1]--;
                    count_room[(int)rm2]++;
                }
                if (sn2_active && !sn1_active && sn1.IsActive)
                {
                    // Вход
                    sn1_active = true;
                    //sn2_active = false;
                    //input_door = true;
                    //output_door = false;
                    count_room[(int)rm1]++;
                    count_room[(int)rm2]--;
                }
                if (sn2_active && sn1_active && !sn2.IsActive && !sn1.IsActive)
                {
                    // Вход
                    sn1_active = false;
                    sn2_active = false;
                    //input_door = false;
                    //output_door = false;
                }

                if (!sn1_active && !sn2_active)
                {
                    // Выход
                    sn1_active = sn1.IsActive;
                    sn2_active = sn2.IsActive;
                }
                if (count_room[(int)rm1] < 0) count_room[(int)rm1] = 0;
                if (count_room[(int)rm2] < 0) count_room[(int)rm2] = 0;
            }
        }
        public class Gateways
        {
            private List<IMyDoor> doors = new List<IMyDoor>();
            private List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            List<Gateway> list_gtw = new List<Gateway>();
            public Gateways(string name_obj, string tag)
            {
                //test_lcd.WriteText("Start" + "\n", false);
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                _scr.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                //test_lcd.WriteText("doors:" + doors.Count() + "\n", true);
                //test_lcd.WriteText("sensors:" + doors.Count() + "\n", true);
                IMyDoor door1;
                IMySensorBlock sensor1;
                room room1;
                IMyDoor door2;
                IMySensorBlock sensor2;
                room room2;
                //test_lcd.WriteText("Поиск дверей:" + "\n", false);
                foreach (doors_gareways gw in Enum.GetValues(typeof(doors_gareways)))
                {
                    door1 = null;
                    sensor1 = null;
                    room1 = room.none;
                    door2 = null;
                    sensor2 = null;
                    room2 = room.none;

                    List<IMyDoor> l_drs = doors.Where(d => d.CustomName.Contains("[" + gw.ToString() + "]")).ToList();
                    List<IMySensorBlock> l_sns = sensors.Where(d => d.CustomName.Contains("[" + gw.ToString() + "]")).ToList();
                    if (l_drs != null && l_drs.Count() == 2 && l_sns != null && l_sns.Count() == 2)
                    {
                        foreach (room rm in Enum.GetValues(typeof(room)))
                        {
                            //test_lcd.WriteText("room:" + rm + "\n", true);
                            IMyDoor dr = l_drs.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
                            IMySensorBlock sn = l_sns.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
                            if (dr != null && sn != null)
                            {
                                if (door1 != null && door2 == null) { door2 = dr; room2 = rm; }
                                if (door1 == null) { door1 = dr; room1 = rm; }
                                if (sensor1 != null && sensor2 == null) { sensor2 = sn; }
                                if (sensor1 == null) { sensor1 = sn; }
                            }
                        }
                        if (door1 != null && door2 != null && sensor1 != null && sensor2 != null)
                        {
                            //test_lcd.WriteText("door1:"+ door1.CustomName + "\n", true);
                            //test_lcd.WriteText("door2:"+ door2.CustomName + "\n", true);
                            //test_lcd.WriteText("sensor1:" + sensor1.CustomName + "\n", true);
                            //test_lcd.WriteText("sensor2:" + sensor2.CustomName + "\n", true);
                            list_gtw.Add(new Gateway(gw, door1, sensor1, room1, door2, sensor2, room2));
                        }
                    }
                }
                _scr.Echo("Найдено Gateways:[" + tag + "]: " + list_gtw.Count());
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    //case "connected_on":
                    //    break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {
                    foreach (Gateway gateway in list_gtw)
                    {
                        gateway.Logic();
                    }
                }

            }
        }
        public class Lightings : BaseListTerminalBlock<IMyInteriorLight>
        {
            public Lightings(string name_obj, string tag) : base(name_obj)
            {
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => n.CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено Lighting:[" + tag + "]: " + list_obj.Count());
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
                    foreach (room rm in Enum.GetValues(typeof(room)))
                    {
                        if (count_room[(int)rm] > 0)
                        {
                            OnOfTag(rm.ToString());
                        }
                        else
                        {
                            OffOfTag(rm.ToString());
                        }
                    }
                }

            }
        }
    }
}
// Запись координат базы в блок
// добавить соеден в алгориьм стыковки
// своб полет - сделать режим разгон и тормоз (сравнить режимы) посадка предусм координаты планеты или базы
// сделать остановку на точку таргет
// Определить эффект мощьность по стороне 3д коорд трмоза 
// + добавить контроль безопасности (столкновения) по камерам (по сторонам)